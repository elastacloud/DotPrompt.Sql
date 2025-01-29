using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotPrompt.Sql;

/// <summary>
/// Used to get connection configuration and SQL DDL and DML statements
/// </summary>
public abstract class DatabaseConfigReader
{
    /// <summary>
    /// Reads in a YAML file with the database config
    /// </summary>
    /// <param name="filePath">The path to the yaml file</param>
    /// <returns>A database config instance containing all of the connection details</returns>
    /// <exception cref="FileNotFoundException">Raised if the yaml file isn't found</exception>
    public static DatabaseConfig ReadYamlConfig(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"YAML configuration file not found: {filePath}");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties() 
            .Build();

        using var reader = new StreamReader(filePath);
        var yamlData = reader.ReadToEnd();

        return deserializer.Deserialize<DatabaseConfig>(yamlData);
    }
    /// <summary>
    /// Loads a query from an embedded resource
    /// </summary>
    /// <param name="resourceName">The name of the resource file which should end in .sql</param>
    /// <returns>A string value of the file contents which should be a SQL query</returns>
    /// <exception cref="FileNotFoundException">Raised if the resource is not found</exception>
    public static string? LoadQuery(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(str => str.EndsWith(resourceName));

        if (resourcePath == null)
            throw new FileNotFoundException($"Resource {resourceName} not found.");

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return null;
    }
}