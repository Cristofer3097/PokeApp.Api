# Etapa 1: Compilación y Publicación
# Se usa la imagen del SDK de .NET que tiene todas las herramientas.
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["PokeApp.Api.csproj", "./"]
RUN dotnet restore "./PokeApp.Api.csproj"
COPY . .
RUN dotnet publish "PokeApp.Api.csproj" -c Release -o /app/publish

# Etapa 2: Imagen Final
# Se usa la imagen de ASP.NET que es más ligera, solo para ejecutar la app.
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
# Se copian los archivos compilados desde la etapa anterior, llamada "build".
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PokeApp.Api.dll"]