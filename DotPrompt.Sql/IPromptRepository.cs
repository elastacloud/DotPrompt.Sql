namespace DotPrompt.Sql;

/// <summary>
/// Defines a prompt repository which will be injected into a loader
/// </summary>
public interface IPromptRepository
{
    /// <summary>
    /// Adds a SQL prompt and upversions the prompt if it's changed
    /// </summary>
    /// <param name="entity">The prompt entity that is being added or upversioned</param>
    /// <returns>A boolean to denote whether it added the prompt or not</returns>
    Task<bool> AddSqlPrompt(SqlPromptEntity entity);
    /// <summary>
    /// Loads all instances of the prompt from the catalog but only the latest versions
    /// </summary>
    /// <returns>An enumeration of prompts with different names</returns>
    IEnumerable<SqlPromptEntity> Load();
}