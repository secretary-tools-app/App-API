# Estágio de Build
from mcr.microsoft.com/dotnet/sdk:8.0 AS build
workdir /src
copy ["AtasApi.csproj", "./"]
run dotnet restore "AtasApi.csproj"
copy . .
run dotnet publish -c Release -o /app/publish

# Estágio de Execução
from mcr.microsoft.com/dotnet/aspnet:8.0
workdir /app
copy --from=build /app/publish .

# Cria a pasta para o banco de dados persistente
run mkdir -p /app/data

# Indica ao runtime que este diretório deve ser persistido em deploys e reinícios
volume ["/app/data"]

exposer 8080
env ASPNETCORE_URLS=http://+:8080
env ConnectionString="Data Source=/app/data/atas.db"
env SchemaPath="database/schema_inicial.sql"

entrypoint ["dotnet", "AtasApi.dll"]