# Etapa 1: Compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PokeApp.Api.csproj", "./"]
RUN dotnet restore "./PokeApp.Api.csproj"
COPY . .
RUN dotnet publish "PokeApp.Api.csproj" -c Release -o /app/publish

# Etapa 2: Crear la imagen final para ejecución
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokeApp.Api.dll"]