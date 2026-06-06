# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY TarotApi.sln ./
COPY TarotApi/TarotApi.csproj ./TarotApi/
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish TarotApi/TarotApi.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5098
ENV ASPNETCORE_URLS=http://+:5098
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TarotApi.dll"]
