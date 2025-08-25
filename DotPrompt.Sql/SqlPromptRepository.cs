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
            _connection
                .Query<SqlPromptEntity, PromptParameter, SqlPromptEntity>(
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
                )
                .AsList(); // Force enumeration so mapping executes
        }

        return promptDictionary.Values;
    }

    /// <inheritdoc />
    public async Task<SqlPromptEntity?> GetLatestPromptByName(string promptName)
    {
        // Load the SQL query to fetch the latest prompt by name
        string? query = DatabaseConfigReader.LoadQuery("GetLatestPromptByName.sql");
        if (string.IsNullOrEmpty(query))
        {
            throw new InvalidOperationException(
                "Failed to load SQL query file 'GetLatestPromptByName.sql'. The file could not be found or loaded.");
        }

        // Retrieve the prompt row
        var prompt = await _connection.QueryFirstOrDefaultAsync<SqlPromptEntity>(
            query,
            new { PromptName = promptName }
        );

        if (prompt == null)
        {
            return null;
        }

        // Query to fetch parameters and defaults for the latest version of this prompt
        const string parameterQuery = @"
            WITH LatestVersion AS (
                SELECT MAX(VersionNumber) AS VersionNumber
                FROM PromptParameters
                WHERE PromptId = @PromptId
            )
            SELECT pp.ParameterName, pp.ParameterValue, pd.DefaultValue
            FROM PromptParameters pp
            LEFT JOIN ParameterDefaults pd ON pp.ParameterId = pd.ParameterId AND pp.VersionNumber = pd.VersionNumber
            CROSS JOIN LatestVersion
            WHERE pp.PromptId = @PromptId
              AND pp.VersionNumber = LatestVersion.VersionNumber;";

        var parameters = await _connection.QueryAsync<PromptParameter>(
            parameterQuery,
            new { PromptId = prompt.PromptId }
        );

        prompt.Parameters = new Dictionary<string, string>();
        prompt.Default = new Dictionary<string, object>();

        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param.ParameterName)) return prompt;
            if (!prompt.Parameters.ContainsKey(param.ParameterName))
            {
                prompt.Parameters.Add(param.ParameterName, param.ParameterValue);
            }

            if (param.DefaultValue == null || prompt.Default.ContainsKey(param.ParameterName)) return prompt;
            prompt.Parameters.TryAdd(param.ParameterName, param.ParameterValue);

            if (param.DefaultValue != null)
            {
                prompt.Default.TryAdd(param.ParameterName, param.DefaultValue);
            }
        }

        return prompt;
    }
}