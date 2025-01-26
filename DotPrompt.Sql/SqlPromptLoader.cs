namespace DotPrompt.Sql;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class SqlPromptLoader(SqlConnection connection)
{
    private readonly SqlConnection _connection = connection;

    public async Task AddSqlPrompt(SqlPromptEntity entity)
    {
        await using SqlTransaction transaction = _connection.BeginTransaction();
        try
        {
            // Insert into PromptFile table and get the PromptId
            string insertPromptFileQuery = @"
                        INSERT INTO PromptFile (PromptName, CreatedAt, ModifiedAt, Model, OutputFormat, MaxTokens, SystemPrompt, UserPrompt)
                        OUTPUT INSERTED.PromptId
                        VALUES (@PromptName, @CreatedAt, @ModifiedAt, @Model, @OutputFormat, @MaxTokens, @SystemPrompt, @UserPrompt)";

            int promptId;
            await using (SqlCommand cmd = new SqlCommand(insertPromptFileQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@PromptName", entity.PromptName);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
                cmd.Parameters.AddWithValue("@ModifiedAt", DateTimeOffset.UtcNow);
                cmd.Parameters.AddWithValue("@Model", entity.Model ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OutputFormat", entity.OutputFormat);
                cmd.Parameters.AddWithValue("@MaxTokens", entity.MaxTokens);
                cmd.Parameters.AddWithValue("@SystemPrompt", entity.SystemPrompt);
                cmd.Parameters.AddWithValue("@UserPrompt", entity.UserPrompt);

                promptId = (int)await cmd.ExecuteScalarAsync();
            }

            // Insert parameters into PromptParameters table
            foreach (var param in entity.Parameters)
            {
                string insertParametersQuery = @"
                            INSERT INTO PromptParameters (PromptId, ParameterName, ParameterValue)
                            OUTPUT INSERTED.ParameterId
                            VALUES (@PromptId, @ParameterName, @ParameterValue)";

                int parameterId;
                await using (SqlCommand cmd = new SqlCommand(insertParametersQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@PromptId", promptId);
                    cmd.Parameters.AddWithValue("@ParameterName", param.Key);
                    cmd.Parameters.AddWithValue("@ParameterValue", param.Value);

                    parameterId = (int)await cmd.ExecuteScalarAsync();

                    // Insert corresponding default values into ParameterDefaults table
                    if (!entity.Default.TryGetValue(param.Key, out var defaultValue)) continue;
                    string insertDefaultsQuery = @"
                                    INSERT INTO ParameterDefaults (ParameterId, DefaultValue)
                                    VALUES (@ParameterId, @DefaultValue)";

                    await using (SqlCommand defaultCmd = new SqlCommand(insertDefaultsQuery, connection, transaction))
                    {
                        defaultCmd.Parameters.AddWithValue("@ParameterId", parameterId);
                        defaultCmd.Parameters.AddWithValue("@DefaultValue",
                            defaultValue?.ToString() ?? (object)DBNull.Value);
                        await defaultCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new ApplicationException($"Error inserting data: {ex.Message}");
        }
    }

    public IEnumerable<SqlPromptEntity> Load()
    {
        var prompts = new List<SqlPromptEntity>();

        string query = @"
        SELECT 
            pf.PromptId, 
            pf.PromptName, 
            pf.CreatedAt, 
            pf.ModifiedAt, 
            pf.Model, 
            pf.OutputFormat, 
            pf.MaxTokens, 
            pf.SystemPrompt, 
            pf.UserPrompt,
            pp.ParameterName, 
            pp.ParameterValue,
            pd.DefaultValue
        FROM PromptFile pf
        LEFT JOIN PromptParameters pp ON pf.PromptId = pp.PromptId
        LEFT JOIN ParameterDefaults pd ON pp.ParameterId = pd.ParameterId
        ORDER BY pf.PromptId";

        using SqlCommand command = new SqlCommand(query, connection);
        using SqlDataReader reader = command.ExecuteReader();
        var promptLookup = new Dictionary<int, SqlPromptEntity>();

        while (reader.Read())
        {
            int promptId = reader.GetInt32(reader.GetOrdinal("PromptId"));

            if (!promptLookup.TryGetValue(promptId, out var promptEntity))
            {
                promptEntity = new SqlPromptEntity
                {
                    PromptId = promptId,
                    PromptName = reader.GetString(reader.GetOrdinal("PromptName")),
                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt"))
                        ? null
                        : reader.GetDateTimeOffset(reader.GetOrdinal("CreatedAt")),
                    ModifiedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt"))
                        ? null
                        : reader.GetDateTimeOffset(reader.GetOrdinal("ModifiedAt")),
                    Model = reader.IsDBNull(reader.GetOrdinal("Model"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Model")),
                    OutputFormat = reader.GetString(reader.GetOrdinal("OutputFormat")),
                    MaxTokens = reader.GetInt32(reader.GetOrdinal("MaxTokens")),
                    SystemPrompt = reader.GetString(reader.GetOrdinal("SystemPrompt")),
                    UserPrompt = reader.GetString(reader.GetOrdinal("UserPrompt")),
                    Parameters = new Dictionary<string, string>(),
                    Default = new Dictionary<string, object>()
                };

                promptLookup[promptId] = promptEntity;
                prompts.Add(promptEntity);
            }

            // Add parameters if they exist
            if (reader.IsDBNull(reader.GetOrdinal("ParameterName")) ||
                reader.IsDBNull(reader.GetOrdinal("ParameterValue"))) continue;
            string paramName = reader.GetString(reader.GetOrdinal("ParameterName"));
            string paramValue = reader.GetString(reader.GetOrdinal("ParameterValue"));
            if (!promptEntity.Parameters.ContainsKey(paramName))
            {
                promptEntity.Parameters.Add(paramName, paramValue);
            }
            object defaultValue = reader.GetValue(reader.GetOrdinal("DefaultValue"));
            if (!promptEntity.Default.ContainsKey(paramName))
            {
                promptEntity.Default.Add(paramName, defaultValue);
            }

        }

        return prompts;
    }
}