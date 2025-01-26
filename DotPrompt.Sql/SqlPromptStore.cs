using System.Data.SqlClient;

namespace DotPrompt.Sql;

/// <summary>
/// Implementation of the IPromptStore for any SQL Server database
/// </summary>
public class SqlTablePromptStore(string promptFile) : IPromptStore
{
    private readonly string _promptFile = promptFile;

    /// <summary>
    /// Loads the prompts from SQL
    /// </summary>
    public IEnumerable<PromptFile> Load()
    {
        var sqlConnection = GetSqlClient(_promptFile).Result;
        var loader = new SqlPromptLoader(sqlConnection);
        var sqlPromptEntities = loader.Load();
        return sqlPromptEntities.Select(entity => entity.ToPromptFile());
    }

    /// <summary>
    /// Gets a SQL connection
    /// </summary>
    private static async Task<SqlConnection> GetSqlClient(string yamlFilePath)
    {
        var config = DatabaseConfigReader.ReadYamlConfig(yamlFilePath);
        var connector = new DatabaseConnector();
        return await connector.ConnectToDatabase(config);
    }
}