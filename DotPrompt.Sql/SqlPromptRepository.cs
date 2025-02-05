using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DotPrompt.Sql;
using DotPrompt.Sql.Types;

namespace DotPrompt.Sql;

/// <summary>
/// 
/// </summary>
/// <param name="connection"></param>
public class SqlPromptRepository(IDbConnection connection) : IPromptRepository
{
    private readonly IDbConnection _connection = connection;

    /// <summary>
    /// Adds a single SqlPromptEntity to the database
    /// </summary>
    /// <param name="entity">The configured SqlEntityPrompt instance</param>
    /// <exception cref="ApplicationException">An exception if there is a database connection or issue with a conflict</exception>
    public async Task<bool> AddSqlPrompt(SqlPromptEntity entity)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            // Convert Parameters to a DataTable for TVP
            var parametersTable = new DataTable();
            parametersTable.Columns.Add("ParameterName", typeof(string));
            parametersTable.Columns.Add("ParameterValue", typeof(string));

            if (entity.Parameters != null)
            {
                foreach (var param in entity.Parameters)
                {
                    parametersTable.Rows.Add(param.Key, param.Value);
                }
            }

            // Convert Defaults to a DataTable for TVP
            var defaultsTable = new DataTable();
            defaultsTable.Columns.Add("ParameterName", typeof(string));
            defaultsTable.Columns.Add("DefaultValue", typeof(string));

            if (entity.Default != null)
            {
                foreach (var def in entity.Default)
                {
                    defaultsTable.Rows.Add(def.Key, def.Value);
                }
            }

            // Output parameter to check if a new version was inserted
            var parameters = new DynamicParameters();
            parameters.Add("PromptName", entity.PromptName);
            parameters.Add("Model", entity.Model);
            parameters.Add("OutputFormat", entity.OutputFormat);
            parameters.Add("MaxTokens", entity.MaxTokens);
            parameters.Add("SystemPrompt", entity.SystemPrompt);
            parameters.Add("UserPrompt", entity.UserPrompt);
            parameters.Add("Parameters", parametersTable.AsTableValuedParameter("PromptParameterType"));
            parameters.Add("Defaults", defaultsTable.AsTableValuedParameter("ParameterDefaultType"));
            parameters.Add("IsNewVersion", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            // Execute the stored procedure
            await _connection.ExecuteAsync(
                "sp_AddSqlPrompt",
                parameters,
                transaction,
                commandType: CommandType.StoredProcedure
            );

            transaction.Commit();

            // Return true if a new version was inserted, false otherwise
            return parameters.Get<bool>("IsNewVersion");
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