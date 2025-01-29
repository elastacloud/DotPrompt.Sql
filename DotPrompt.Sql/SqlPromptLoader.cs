using System.Data;
using Dapper;

namespace DotPrompt.Sql;

using System;
using System.Collections.Generic;
using DotPrompt.Sql.Types;

/// <summary>
/// A class which defines CRUD operations for a SqlEntityPrompt
/// </summary>
/// <param name="connection">An Open IDBConnection for a SQL database</param>
public class SqlPromptLoader(IDbConnection connection)
{
    private readonly IDbConnection _connection = connection;

    /// <summary>
    /// Adds a single SqlPromptEntity to the database
    /// </summary>
    /// <param name="entity">The configured SqlEntityPrompt instance</param>
    /// <exception cref="ApplicationException">An exception if there is a database connection or issue with a conflict</exception>
    public async Task AddSqlPrompt(SqlPromptEntity? entity)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            // Insert into PromptFile table and get the PromptId
            string? insertPromptFileQuery = DatabaseConfigReader.LoadQuery("InsertPromptFile.sql");

            if (insertPromptFileQuery != null)
            {
                var promptId = await _connection.ExecuteScalarAsync<int>(
                    insertPromptFileQuery, 
                    new
                    {
                        entity.PromptName,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ModifiedAt = DateTimeOffset.UtcNow,
                        Model = entity.Model ?? (object)DBNull.Value,
                        entity.OutputFormat,
                        entity.MaxTokens,
                        entity.SystemPrompt,
                        entity.UserPrompt
                    },
                    transaction
                );

                // Insert parameters into PromptParameters table
                if (entity.Parameters != null)
                    foreach (var param in entity.Parameters)
                    {
                        string? insertParametersQuery = DatabaseConfigReader.LoadQuery("InsertPromptParameters.sql");

                        if (insertParametersQuery == null) continue;
                        var parameterId = await _connection.ExecuteScalarAsync<int>(
                            insertParametersQuery,
                            new
                            {
                                PromptId = promptId,
                                ParameterName = param.Key,
                                ParameterValue = param.Value
                            },
                            transaction
                        );

                        // Insert corresponding default values into ParameterDefaults table
                        object? defaultValue = null;
                        if (entity.Default != null && !entity.Default.TryGetValue(param.Key, out defaultValue))
                            continue;
                        string? insertDefaultsQuery = DatabaseConfigReader.LoadQuery("InsertPromptDefaults.sql");

                        if (insertDefaultsQuery != null)
                            await _connection.ExecuteAsync(
                                insertDefaultsQuery,
                                new
                                {
                                    ParameterId = parameterId,
                                    DefaultValue = defaultValue ?? (object)DBNull.Value
                                },
                                transaction
                            );
                    }
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new ApplicationException($"Error inserting data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Loads a set of SQL prompts from the database
    /// </summary>
    /// <returns>A collection of SqlPromptEntity which can be converted back and forth into a prompt file</returns>
    public IEnumerable<SqlPromptEntity> Load()
    {
        string? query = DatabaseConfigReader.LoadQuery("LoadPrompts.sql");

        var promptDictionary = new Dictionary<int, SqlPromptEntity>();

        if (query != null)
        {
            var result = _connection.Query<SqlPromptEntity, PromptParameter, SqlPromptEntity>(
                query,
                (prompt, param) =>
                {
                    if (!promptDictionary.TryGetValue(prompt.PromptId, out var promptEntity))
                    {
                        promptEntity = prompt;
                        promptEntity.Parameters = new Dictionary<string, string>();
                        promptEntity.Default = new Dictionary<string, object>();
                        promptDictionary.Add(prompt.PromptId, promptEntity);
                    }

                    if (string.IsNullOrEmpty(param.ParameterName)) return promptEntity;
                    if (promptEntity.Parameters != null && !promptEntity.Parameters.ContainsKey(param.ParameterName))
                    {
                        promptEntity.Parameters.Add(param.ParameterName, param.ParameterValue);
                    }

                    if (promptEntity.Default == null || promptEntity.Default.ContainsKey(param.ParameterName))
                        return promptEntity;
                    if (param.DefaultValue != null)
                        promptEntity.Default.Add(param.ParameterName, param.DefaultValue);

                    return promptEntity;
                },
                splitOn: "ParameterId"
            );
        }

        return promptDictionary.Values;
    }
}