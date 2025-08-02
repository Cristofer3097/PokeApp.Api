# Usa la imagen oficial de .NET SDK como base para compilar
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["PokeApp.Api/PokeApp.Api.csproj", "PokeApp.Api/"]
RUN dotnet restore "PokeApp.Api/PokeApp.Api.csproj"

COPY . .
WORKDIR "/src/PokeApp.Api"
RUN dotnet build "PokeApp.Api.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "PokeApp.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokeApp.Api.dll"]