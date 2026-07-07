# ── Stage 1: build with the full SDK ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj alone first: Docker caches layers, so restore only re-runs
# when dependencies change, not on every code edit.
COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ── Stage 2: run with only the slim runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Container listens on 8080 (the aspnet image's non-root default)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EsportsTracker.Api.dll"]