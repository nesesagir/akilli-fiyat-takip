FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/PriceTracker.sln ./
COPY backend/src/PriceTracker.Domain/PriceTracker.Domain.csproj src/PriceTracker.Domain/
COPY backend/src/PriceTracker.Application/PriceTracker.Application.csproj src/PriceTracker.Application/
COPY backend/src/PriceTracker.Infrastructure/PriceTracker.Infrastructure.csproj src/PriceTracker.Infrastructure/
COPY backend/src/PriceTracker.API/PriceTracker.API.csproj src/PriceTracker.API/

RUN dotnet restore src/PriceTracker.API/PriceTracker.API.csproj

COPY backend/src/ ./src/
RUN dotnet publish src/PriceTracker.API/PriceTracker.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Puppeteer/Chromium bağımlılıkları (scraper)
RUN apt-get update && apt-get install -y --no-install-recommends \
    chromium \
    fonts-liberation \
    libnss3 \
    libatk-bridge2.0-0 \
    libgtk-3-0 \
    libx11-xcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV Hangfire__Enabled=false
ENV PriceCheck__Enabled=true
ENV PriceCheck__IntervalMinutes=30
ENV Database__ApplyMigrationsOnStartup=true

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PriceTracker.API.dll"]
