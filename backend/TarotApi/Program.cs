using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TarotApi.Data;
using TarotApi.Middleware;
using TarotApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Environment variables ──────────────────────────────────────────
var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? throw new InvalidOperationException("SUPABASE_JWT_SECRET is not set");
var supabaseUrl = Environment.GetEnvironmentVariable("PUBLIC_SUPABASE_URL")
    ?? throw new InvalidOperationException("PUBLIC_SUPABASE_URL is not set");
var dbConnectionString = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("SUPABASE_DB_CONNECTION_STRING is not set");
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];

// ── Services ───────────────────────────────────────────────────────

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
builder.Services.AddAuthorization();

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

var app = builder.Build();

// ── Middleware ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
