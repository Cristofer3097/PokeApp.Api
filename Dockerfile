# Etapa 1: Compilación y Publicación
# CORRECCIÓN: Se cambia la versión del SDK de .NET a 9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["PokeApp.Api.csproj", "./"]
RUN dotnet restore "./PokeApp.Api.csproj"
COPY . .
RUN dotnet publish "PokeApp.Api.csproj" -c Release -o /app/publish

# Etapa 2: Imagen Final
# CORRECCIÓN: Se cambia la versión del Runtime de ASP.NET a 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PokeApp.Api.dll"]