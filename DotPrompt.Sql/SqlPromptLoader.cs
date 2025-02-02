namespace DotPrompt.Sql;

/// <summary>
/// Loads a repository and executes crud operations against a prompt store
/// </summary>
public class SqlPromptLoader
{
    private readonly IPromptRepository _promptRepository;

    /// <summary>
    /// Takes an IPromptRepository and adds to the SQL store 
    /// </summary>
    /// <param name="promptRepository">The prompt repository being injected</param>
    public SqlPromptLoader(IPromptRepository promptRepository)
    {
        _promptRepository = promptRepository;
    }

    /// <summary>
    /// Adds a new prompt to the repository
    /// </summary>
    /// <param name="entity">The SQL prompt to add</param>
    /// <returns>Whether it successfully added verion 1 or more</returns>
    public async Task<bool> AddSqlPrompt(SqlPromptEntity entity)
    {
        return await _promptRepository.AddSqlPrompt(entity);
    }

    /// <summary>
    /// Loads all prompts from the store
    /// </summary>
    /// <returns>An enumeration of prompts from the store</returns>
    public IEnumerable<SqlPromptEntity> Load()
    {
        return _promptRepository.Load();
    }
}