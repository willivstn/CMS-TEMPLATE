# Multi-stage build for the CMS.Web application.
# Build context is the repository root (so Central Package Management files and all
# projects are available to restore).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-wide build configuration first for better layer caching.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ ./src/

RUN dotnet restore src/CMS.Web/CMS.Web.csproj
RUN dotnet publish src/CMS.Web/CMS.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Kestrel listens on 8080 inside the container; terminate TLS at a reverse proxy.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "CMS.Web.dll"]
