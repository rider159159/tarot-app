using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TarotApi.Data;
using TarotApi.Middleware;
using TarotApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Environment variables ──────────────────────────────────
var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? throw new InvalidOperationException("SUPABASE_JWT_SECRET is not set");
var supabaseUrl = Environment.GetEnvironmentVariable("PUBLIC_SUPABASE_URL")
    ?? throw new InvalidOperationException("PUBLIC_SUPABASE_URL is not set");
var dbConnectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("SUPABASE_DB_CONNECTION_STRING is not set");
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];

// Supabase user IDs (JWT sub claim) allowed to call /api/admin/* endpoints.
// Empty means no admins — admin endpoints reject every request (fail closed).
var adminUserIds = (Environment.GetEnvironmentVariable("ADMIN_USER_IDS") ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
if (adminUserIds.Count == 0)
    Console.WriteLine("[WARN] ADMIN_USER_IDS is not set — /api/admin endpoints will reject all requests");

// ── Services ──────────────────────────────────────────

// Controllers with global [Authorize]
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Tarot services
builder.Services.AddSingleton<TarotService>();
builder.Services.AddScoped<ReadingService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PromptBuilder>();

builder.Services.AddEndpointsApiExplorer();

// Swagger with Bearer token support
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your Supabase JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication — Supabase uses ES256 (asymmetric), so we fetch the public key
// via JWKS. The HS256 JWT secret is not used for signature validation here.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
        options.RequireHttpsMetadata = supabaseUrl.StartsWith("https");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return sub is not null && adminUserIds.Contains(sub);
        }));
});

// EF Core + PostgreSQL
builder.Services.AddDbContext<TarotDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Rate limiter: only applied to endpoints decorated with [EnableRateLimiting].
// Behind the Caddy reverse proxy, the actual client IP arrives via X-Forwarded-For.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("anonymous-draw", httpContext =>
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var ip = (forwardedFor?.Split(',')[0].Trim())
                 ?? httpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
