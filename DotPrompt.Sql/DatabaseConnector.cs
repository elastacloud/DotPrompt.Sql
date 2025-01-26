namespace DotPrompt.Sql;

using System;
using System.Data.SqlClient;

public class DatabaseConnector
{
    public async Task<SqlConnection> ConnectToDatabase(DatabaseConfig config)
    {
        string connectionString = BuildConnectionString(config);

        try
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await CreatePromptTables(connection);
            Console.WriteLine("Connected to the database successfully!");
            return connection;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error connecting to database, please check config", ex);
        }
    }

    private async Task CreatePromptTables(SqlConnection connection)
    {
        // 1. Does the prompt table exist already
        bool tableExists = false;
        string queryPromptFileExists = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = 'PromptFile'";
        await using (SqlCommand command = new SqlCommand(queryPromptFileExists, connection))
        {
            tableExists = (int) (await command.ExecuteScalarAsync() ?? 0) > 0;
        }

        if (!tableExists)
        {
            string sqlCreate = @"
            CREATE TABLE PromptFile (
                PromptId INT IDENTITY(1,1) PRIMARY KEY,
                PromptName VARCHAR(255) NOT NULL UNIQUE,
                CreatedAt DATETIMEOFFSET NULL,
                ModifiedAt DATETIMEOFFSET NULL,
                Model VARCHAR(255) NULL,
                OutputFormat VARCHAR(255) NOT NULL DEFAULT '',
                MaxTokens INT NOT NULL,
                SystemPrompt NVARCHAR(MAX) NOT NULL DEFAULT '',
                UserPrompt NVARCHAR(MAX) NOT NULL DEFAULT ''
            );

            -- Create the PromptParameters table
            CREATE TABLE PromptParameters (
                ParameterId INT IDENTITY(1,1) PRIMARY KEY,
                PromptId INT NOT NULL,
                ParameterName VARCHAR(255) NOT NULL,
                ParameterValue VARCHAR(255) NOT NULL,
                CONSTRAINT FK_Parameters_PromptFile FOREIGN KEY (PromptId)
                    REFERENCES PromptFile(PromptId) ON DELETE CASCADE
            );

            -- Create the ParameterDefaults table
            CREATE TABLE ParameterDefaults (
                DefaultId INT IDENTITY(1,1) PRIMARY KEY,
                ParameterId INT NOT NULL,
                DefaultValue VARCHAR(255) NOT NULL,
                Description NVARCHAR(500) NULL,
                CONSTRAINT FK_ParameterDefaults_PromptParameters FOREIGN KEY (ParameterId)
                    REFERENCES PromptParameters(ParameterId) ON DELETE CASCADE
            );

            -- Additional Indexes (if needed for better performance)
            CREATE INDEX IX_PromptParameters_PromptId ON PromptParameters(PromptId);
            CREATE INDEX IX_ParameterDefaults_ParameterId ON ParameterDefaults(ParameterId);
            ";
                
            await using SqlCommand command = new SqlCommand(sqlCreate, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
    
    private static string BuildConnectionString(DatabaseConfig config)
    {
        if (config.AADAuthentication)
        {
            return $"Server={config.Server};Database={config.Database};Authentication=Active Directory Default;";
        }

        return config.IntegratedAuthentication ? $"Server={config.Server};Database={config.Database};Integrated Security=True;" : $"Server={config.Server};Database={config.Database};User Id={config.Username};Password={config.Password};";
    }
}
