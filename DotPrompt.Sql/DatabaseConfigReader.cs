using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotPrompt.Sql;

public abstract class DatabaseConfigReader
{
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
}