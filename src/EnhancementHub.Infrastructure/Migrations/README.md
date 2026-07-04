# Database Migrations

The initial schema migration is generated at `Migrations/InitialCreate`.

## Apply migrations

```bash
dotnet ef database update \
  --project src/EnhancementHub.Infrastructure/EnhancementHub.Infrastructure.csproj
```

## Add new migrations

```bash
cd src/EnhancementHub.Infrastructure
dotnet ef migrations add <MigrationName> --output-dir Migrations
```

## Provider selection

`AddInfrastructure` reads `Database:Provider` (`SQLite` or `PostgreSQL`). When omitted, SQLite is used if `ConnectionStrings:Default` contains `Data Source=`.

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=enhancementhub.db"
  },
  "Database": {
    "Provider": "SQLite"
  }
}
```

For PostgreSQL:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=enhancementhub;Username=postgres;Password=postgres"
  },
  "Database": {
    "Provider": "PostgreSQL"
  }
}
```
