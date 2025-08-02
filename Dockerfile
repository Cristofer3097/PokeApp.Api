# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# CORRECCIÓN: Se quita "PokeApp.Api/" de las rutas
COPY ["PokeApp.Api.csproj", "./"]
RUN dotnet restore "./PokeApp.Api.csproj"

# Se copia el resto de los archivos
COPY . .
RUN dotnet publish "PokeApp.Api.csproj" -c Release -o /app/publish

# Etapa 2: Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokeApp.Api.dll"]