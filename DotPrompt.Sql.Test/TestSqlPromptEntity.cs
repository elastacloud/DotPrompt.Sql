using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Reflection;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using Dapper;
using DotPrompt.Sql;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

public class SqlPromptRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServerContainer;
    private IDbConnection _connection;
    private SqlPromptRepository _repository;

    public SqlPromptRepositoryTests()
    {
        _sqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong(!)Password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();

        _connection = new SqlConnection(_sqlServerContainer.GetConnectionString());
        _connection.Open();

        _repository = new SqlPromptRepository(_connection);
        await InitializeDatabase();
    }

    public Task DisposeAsync()
    {
        _sqlServerContainer.StopAsync();
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    private static string LoadSql(string resourceName)
    {
        // Get the assembly containing the embedded SQL files
        var assembly = Assembly.Load("DotPrompt.Sql"); // Name of the referenced assembly

        // Find the full resource name (includes namespace path)
        string? fullResourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

        if (fullResourceName == null)
        {
            throw new FileNotFoundException($"Resource {resourceName} not found in assembly {assembly.FullName}");
        }

        // Read the embedded resource stream
        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    private async Task InitializeDatabase()
    {
        string tables = LoadSql("CreateDefaultPromptTables.sql");
        await _connection.ExecuteAsync(tables);

        string procs = LoadSql("AddSqlPrompt.sql");
        await _connection.ExecuteAsync(procs);
    }

    [Fact]
    public async Task AddSqlPrompt_ValidPrompt_InsertsSuccessfully()
    {
        // Arrange
        var entity = new SqlPromptEntity
        {
            PromptName = "myprompt",
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 500,
            SystemPrompt = "Optimize SQL queries.",
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string>
            {
                { "Temperature", "0.7" },
                { "TopP", "0.9" }
            },
            Default = new Dictionary<string, object>
            {
                { "Temperature", "0.5" }
            }
        };

        // Act
        bool result = await _repository.AddSqlPrompt(entity);

        // Assert
        Assert.True(result, "Expected new prompt version to be inserted.");
    }

    [Fact]
    public async Task AddSqlPrompt_SamePromptNoChanges_DoesNotInsertNewVersion()
    {
        // Arrange
        var entity = new SqlPromptEntity
        {
            PromptName = "myprompt",
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 200,
            SystemPrompt = "Optimize SQL queries.",
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string>
            {
                { "Temperature", "0.7" },
                { "TopP", "0.9" }
            },
            Default = new Dictionary<string, object>
            {
                { "Temperature", "0.5" }
            }
        };

        await _repository.AddSqlPrompt(entity); // First insert

        // Act
        bool result = await _repository.AddSqlPrompt(entity); // Try inserting again with no changes

        // Assert
        Assert.False(result, "No new version should be inserted when nothing has changed.");
    }

    [Fact]
    public async Task AddSqlPrompt_WhenMaxTokensChanges_ShouldInsertNewVersion()
    {
        // Arrange
        var entity1 = new SqlPromptEntity
        {
            PromptName = "noprompt",
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 500,
            SystemPrompt = "Optimize SQL queries.",
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string> { { "Temperature", "0.7" } },
            Default = new Dictionary<string, object> { { "Temperature", "0.5" } }
        };

        var entity2 = new SqlPromptEntity
        {
            PromptName = "noprompt", // Same prompt name
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 512, // Changed value
            SystemPrompt = "Optimize SQL queries.",
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string> { { "Temperature", "0.7" } },
            Default = new Dictionary<string, object> { { "Temperature", "0.5" } }
        };

        await _repository.AddSqlPrompt(entity1); // Insert first version

        // Act
        bool result = await _repository.AddSqlPrompt(entity2);

        // Assert
        Assert.True(result, "A new version should be inserted when MaxTokens changes.");
    }
    
    [Fact]
    public async Task GetSqlPromptByName_GivenTwoPromptsOfTheSameNameAreAdded_ShouldRetrieveLatest()
    {
        // Arrange
        var entity1 = new SqlPromptEntity
        {
            PromptName = "noprompt",
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 500,
            SystemPrompt = "Optimize SQL queries.",
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string> { { "Temperature", "0.7" } },
            Default = new Dictionary<string, object> { { "Temperature", "0.5" } }
        };

        var entity2 = new SqlPromptEntity
        {
            PromptName = "noprompt", // Same prompt name
            Model = "gpt4",
            OutputFormat = "json",
            MaxTokens = 512, // Changed value
            SystemPrompt = "Optimize SQL queries 2.", // changed value
            UserPrompt = "Suggest indexing improvements.",
            Parameters = new Dictionary<string, string> { { "Temperature", "0.7" } },
            Default = new Dictionary<string, object> { { "Temperature", "0.5" } }
        };

        await _repository.AddSqlPrompt(entity1); // Insert first version

        // Act
        bool result = await _repository.AddSqlPrompt(entity2);
        
        var prompt = await _repository.GetLatestPromptByName("noprompt");

        // Assert
        Assert.Equal(entity2.MaxTokens, prompt.MaxTokens);
        Assert.Equal(entity2.SystemPrompt, prompt.SystemPrompt);
        Assert.Equal(entity1.UserPrompt, prompt.UserPrompt);
    }
}