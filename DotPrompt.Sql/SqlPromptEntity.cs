using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotPrompt.Sql;

/// <summary>
/// Represents a record held in the storage table
/// </summary>
public class SqlPromptEntity
{
    /// <summary>
    /// Gets, sets the timestamp of the entry
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the modified date 
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// A primary key returned from the database based on autoincrements 
    /// </summary>
    public int PromptId { get; set; }
    /// <summary>
    /// A unique name for the prompt
    /// </summary>
    public required string PromptName { get; set; }

    /// <summary>
    /// Gets, sets the model to use
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets, sets the output format
    /// </summary>
    public string OutputFormat { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets the maximum number of tokens
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Gets, sets the parameter information which is held as a JSON string value
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }

    /// <summary>
    /// Gets, sets the default values which are held as a JSON string value
    /// </summary>
    public Dictionary<string, object>? Default { get; set; }

    /// <summary>
    /// Gets, sets the system prompt template
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets the user prompt template
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Returns the prompt entity record into a <see cref="PromptFile"/> instance
    /// </summary>
    /// <returns></returns>
    public PromptFile? ToPromptFile()
    {
        // Generate the new prompt file
        Dictionary<string, string>? parameters = this.Parameters;
        Dictionary<string, object>? dictionary = this.Default;
        if (parameters != null)
        {
            if (dictionary != null)
            {
                var promptFile = new PromptFile
                {
                    Name = PromptName,
                    Config = new PromptConfig
                    {
                        OutputFormat = Enum.Parse<OutputFormat>(OutputFormat, true),
                        MaxTokens = MaxTokens,
                        Input = new InputSchema
                        {
                            Parameters = parameters,
                            Default =dictionary
                        }
                    },
                    Prompts = new Prompts
                    {
                        System = SystemPrompt,
                        User = UserPrompt
                    }
                };

                return promptFile;
            }
        }

        return null;
    }
    /// <summary>
    /// Takes a file location and loads a prompt file and converts into a SqlPromptEntity 
    /// </summary>
    /// <param name="fileLocation">The location of the prompt file</param>
    /// <returns>A SqlPromptEntity containing the definition of the prompt file</returns>
    /// <exception cref="FileNotFoundException">Raised if the prompt file is not found</exception>
    public static SqlPromptEntity? FromPromptFile(string fileLocation)
    {
        if (!File.Exists(fileLocation))
        {
            throw new FileNotFoundException($"The specified file was not found: {fileLocation}");
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();

        var yamlContent = File.ReadAllText(fileLocation);
        var yamlData = deserializer.Deserialize<dynamic>(yamlContent);
        SqlPromptEntity? sqlPromptEntity = null;
        try
        {
            sqlPromptEntity = new SqlPromptEntity
            {
                Model = yamlData["model"],
                PromptName = yamlData["config"]["name"],
                OutputFormat = yamlData["config"]["outputFormat"],
                MaxTokens = Convert.ToInt32(yamlData["config"]["maxTokens"]),
                Parameters = yamlData["config"]["input"]["parameters"] != null
                    ? ConvertToDictionary(yamlData["config"]["input"]["parameters"])
                    : new Dictionary<string, string>(),
                Default = yamlData["config"]["input"]["default"] != null
                    ? ConvertToObjectDictionary(yamlData["config"]["input"]["default"])
                    : new Dictionary<string, object>(),
                SystemPrompt = yamlData["prompts"]["system"],
                UserPrompt = yamlData["prompts"]["user"]
            };
        }
        catch (KeyNotFoundException knfe)
        {
            throw new ApplicationException($"Mandatory key not present", knfe);
        }

        return sqlPromptEntity;
    }

    private static Dictionary<string, string> ConvertToDictionary(dynamic input)
    {
        var dictionary = new Dictionary<string, string>();
        foreach (var key in input.Keys)
        {
            dictionary[key] = input[key]?.ToString() ?? string.Empty;
        }
        return dictionary;
    }

    private static Dictionary<string, object> ConvertToObjectDictionary(dynamic input)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (var key in input.Keys)
        {
            dictionary[key] = input[key];
        }
        return dictionary;
    }
}