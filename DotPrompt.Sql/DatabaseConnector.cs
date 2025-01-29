using System.Data;
using Microsoft.Data.SqlClient;

namespace DotPrompt.Sql;

using System;
using System.Data.SqlClient;
/// <summary>
/// Used tp connection to the database and open a connection
/// </summary>
public class DatabaseConnector
{
    /// <summary>
    /// Provides a database connection and opens the connection - creates all tables if they don't exist
    /// </summary>
    /// <param name="config">The config from the yaml file to help with the database connection</param>
    /// <returns>An open connection</returns>
    /// <exception cref="ApplicationException">Raised when the connection cannot be opened</exception>
    public async Task<IDbConnection> ConnectToDatabase(DatabaseConfig config)
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
        string? sqlCreate = DatabaseConfigReader.LoadQuery("CreateDefaultPromptTables.sql");
        await using SqlCommand command = new SqlCommand(sqlCreate, connection);
        await command.ExecuteNonQueryAsync();
    }
    
    private static string BuildConnectionString(DatabaseConfig config)
    {
        if (config.AadAuthentication)
        {
            return $"Server={config.Server};Database={config.Database};Authentication=Active Directory Default;";
        }

        return config.IntegratedAuthentication ? $"Server={config.Server};Database={config.Database};Integrated Security=True;" : $"Server={config.Server};Database={config.Database};User Id={config.Username};Password={config.Password};";
    }
}
