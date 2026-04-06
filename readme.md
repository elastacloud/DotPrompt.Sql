# DotPrompt.Sql

A SQL store for the [DotPrompt](https://github.com/elastacloud/dotprompt) library. It stores prompt files in a set of related SQL Server tables, with automatic versioning — a new version is only inserted when the content of a prompt actually changes.

A CLI is provided to add prompt files directly to the database.

## Requirements

- .NET 10
- SQL Server, Azure SQL DB, or Microsoft Fabric SQL DB

## Installation

```
dotnet add package DotPrompt.Sql
```

## CLI Usage

```
DotPrompt.Sql.Cli ./prompts/basic.prompt ./sample.yaml
```

The YAML file should contain the connection details for SQL Server, Azure SQL DB, or Microsoft Fabric SQL DB:

```yaml
server: "myserver.database.windows.net"
database: "mydatabase"
tablename: "mytable"
username: "myuser"
password: "mypassword"
integrated_authentication: false
aad_authentication: true
```

## Supported Features

DotPrompt.Sql supports all current DotPrompt prompt file features:

| Feature | Supported |
|---|---|
| Name | ✅ |
| Version | ✅ |
| Model | ✅ |
| System prompt | ✅ |
| User prompt | ✅ |
| Temperature | ✅ |
| MaxTokens | ✅ |
| Parameters | ✅ |
| Default values | ✅ |
| Output format | ✅ |
| Output schema (JSON Schema) | ✅ |
| Few-shot examples | ✅ |

## Using the Store in Code

Instantiate `SqlTablePromptStore` with an `IPromptRepository` backed by an open `IDbConnection`:

```csharp
IDbConnection connection = new SqlConnection(connectionString);
IPromptRepository repository = new SqlPromptRepository(connection);
IPromptStore store = new SqlTablePromptStore(repository);

// Load all latest-version prompts
IEnumerable<PromptFile> prompts = store.Load();

// Save a prompt file
store.Save(myPromptFile, name: null);
```

## Database Schema

Tables are created automatically on first run via `CreateDefaultPromptTables.sql`. The schema uses:

- **`PromptFile`** — one row per prompt version, with a unique constraint on `(PromptName, VersionNumber)`
- **`PromptParameters`** — parameter name/type pairs linked to a prompt version
- **`ParameterDefaults`** — default values linked to parameters

New columns added in v0.2.3:

| Column | Type | Purpose |
|---|---|---|
| `Temperature` | `FLOAT NULL` | Maps to `PromptConfig.Temperature` |
| `OutputSchema` | `NVARCHAR(MAX) NULL` | JSON Schema document for structured output |
| `FewShots` | `NVARCHAR(MAX) NULL` | JSON array of few-shot user/response pairs |

Existing databases are migrated automatically — the SQL scripts use `IF NOT EXISTS` guards to add missing columns without data loss.

## Architecture

![Open Source Diagrams.png](Open%20Source%20Diagrams.png)

You can review the DotPrompt library [here](https://github.com/elastacloud/dotprompt).

