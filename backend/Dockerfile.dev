FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Copy solution and project file first for layer caching
COPY TarotApi.sln ./
COPY TarotApi/TarotApi.csproj ./TarotApi/

# Restore dependencies (cached unless .csproj changes)
RUN dotnet restore

# Copy everything else
COPY . .

EXPOSE 5098

# dotnet watch for hot reload during development
CMD ["dotnet", "watch", "run", "--project", "TarotApi", "--urls", "http://0.0.0.0:5098"]
