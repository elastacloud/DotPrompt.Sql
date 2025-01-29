namespace DotPrompt.Sql;
/// <summary>
/// A configuration class to hold the connection details for a SQL database connection
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// The server address - in azure should be configured .database.windows.net
    /// </summary>
    public required string Server { get; set; }
    /// <summary>
    /// The database instance name
    /// </summary>
    public required string Database { get; set; }
    /// <summary>
    /// The username to connect to the database with 
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// The password used to connect to the database
    /// </summary>
    public string? Password { get; set; }
    /// <summary>
    /// Whether it uses AD authentication based on the NT user
    /// </summary>
    public bool IntegratedAuthentication { get; set; }
    /// <summary>
    /// Whether it uses AAD authentication based on the AAD user or service principal
    /// </summary>
    public bool AadAuthentication { get; set; }
}