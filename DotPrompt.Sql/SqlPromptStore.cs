using System.Data;
using System.Data.SqlClient;

namespace DotPrompt.Sql;

/// <summary>
/// Implementation of the IPromptStore for any SQL Server database
/// </summary>
public class SqlTablePromptStore(string promptFile, IPromptRepository repository) : IPromptStore
{
    private readonly string _promptFile = promptFile;

    /// <summary>
    /// Loads the prompts from SQL
    /// </summary>
    public IEnumerable<PromptFile> Load()
    {
        var loader = new SqlPromptLoader(repository);
        var sqlPromptEntities = loader.Load();
        return sqlPromptEntities.Select(entity => entity.ToPromptFile())!;
    }

    /// <summary>
    /// Saves the prompt to SQL
    /// </summary>
    /// <param name="promptFile">The prompt file</param>
    /// <param name="name">The name of the file</param>
    public void Save(PromptFile promptFile, string? name)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a SQL connection given appropriate connection config
    /// </summary>
    private static async Task<IDbConnection> GetSqlClient(string yamlFilePath)
    {
        var config = DatabaseConfigReader.ReadYamlConfig(yamlFilePath);
        var connector = new DatabaseConnector();
        return await connector.ConnectToDatabase(config);
    }
}