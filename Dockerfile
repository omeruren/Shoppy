# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` layer-caches independently of source changes.
COPY src/Shoppy/Shoppy.Entity/Shoppy.Entity.csproj src/Shoppy/Shoppy.Entity/
COPY src/Shoppy/Shoppy.DataAccess/Shoppy.DataAccess.csproj src/Shoppy/Shoppy.DataAccess/
COPY src/Shoppy/Shoppy.Business/Shoppy.Business.csproj src/Shoppy/Shoppy.Business/
COPY src/Shoppy/Shoppy.WebAPI/Shoppy.WebAPI.csproj src/Shoppy/Shoppy.WebAPI/

RUN dotnet restore src/Shoppy/Shoppy.WebAPI/Shoppy.WebAPI.csproj

COPY src/Shoppy/Shoppy.Entity/ src/Shoppy/Shoppy.Entity/
COPY src/Shoppy/Shoppy.DataAccess/ src/Shoppy/Shoppy.DataAccess/
COPY src/Shoppy/Shoppy.Business/ src/Shoppy/Shoppy.Business/
COPY src/Shoppy/Shoppy.WebAPI/ src/Shoppy/Shoppy.WebAPI/

RUN dotnet publish src/Shoppy/Shoppy.WebAPI/Shoppy.WebAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Base image ships a non-root "app" user (UID 64198) — run as that instead of root.
USER app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Shoppy.WebAPI.dll"]
