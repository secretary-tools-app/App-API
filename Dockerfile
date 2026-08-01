# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AtasApi.csproj", "./"]
RUN dotnet restore "AtasApi.csproj"
COPY . .
RUN dotnet publish "AtasApi.csproj" -c Release -o /app/publish

# Estágio de Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Cria a pasta para o banco de dados persistente
RUN mkdir -p /app/data

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionString="Data Source=/app/data/atas.db"
ENV SchemaPath="database/schema_inicial.sql"

ENTRYPOINT ["dotnet", "AtasApi.dll"]