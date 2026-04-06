using System.Text.Json;
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
    /// Gets, sets the prompt version number
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets, sets the model to use
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets, sets the output format
    /// </summary>
    public string OutputFormat { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets the JSON schema for structured output (serialised as JSON). Populated when OutputFormat is JsonSchema.
    /// </summary>
    public string? OutputSchema { get; set; }

    /// <summary>
    /// Gets, sets the maximum number of tokens
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Gets, sets the optional temperature value for the model
    /// </summary>
    public float? Temperature { get; set; }

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
    /// Gets, sets the few-shot examples serialised as a JSON array
    /// </summary>
    public string? FewShots { get; set; }

    /// <summary>
    /// Returns the prompt entity record into a <see cref="PromptFile"/> instance
    /// </summary>
    public PromptFile ToPromptFile()
    {
        var promptFile = new PromptFile
        {
            Name = PromptName,
            Version = Version,
            Model = Model,
            Config = new PromptConfig
            {
                OutputFormat = Enum.Parse<OutputFormat>(OutputFormat, true),
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                Output = new Output
                {
                    Format = Enum.Parse<OutputFormat>(OutputFormat, true),
                    Schema = string.IsNullOrEmpty(OutputSchema)
                        ? null
                        : JsonSerializer.Deserialize<object>(OutputSchema)
                },
                Input = new InputSchema
                {
                    Parameters = Parameters ?? new Dictionary<string, string>(),
                    Default = Default ?? new Dictionary<string, object>()
                }
            },
            Prompts = new Prompts
            {
                System = SystemPrompt,
                User = UserPrompt
            },
            FewShots = string.IsNullOrEmpty(FewShots)
                ? []
                : JsonSerializer.Deserialize<FewShotPair[]>(FewShots) ?? []
        };

        return promptFile;
    }

    /// <summary>
    /// Creates a <see cref="SqlPromptEntity"/> from a <see cref="PromptFile"/> instance
    /// </summary>
    /// <param name="promptFile">The parsed prompt file to map from</param>
    /// <returns>A new <see cref="SqlPromptEntity"/></returns>
    public static SqlPromptEntity FromPromptFile(PromptFile promptFile)
    {
        var outputSchema = promptFile.Config.Output?.Schema is not null
            ? JsonSerializer.Serialize(promptFile.Config.Output.Schema)
            : null;

        var fewShots = promptFile.FewShots.Length > 0
            ? JsonSerializer.Serialize(promptFile.FewShots)
            : null;

        return new SqlPromptEntity
        {
            PromptName = promptFile.Name,
            Version = promptFile.Version,
            Model = promptFile.Model,
            OutputFormat = promptFile.Config.OutputFormat.ToString(),
            OutputSchema = outputSchema,
            MaxTokens = promptFile.Config.MaxTokens,
            Temperature = promptFile.Config.Temperature,
            Parameters = promptFile.Config.Input.Parameters,
            Default = promptFile.Config.Input.Default,
            SystemPrompt = promptFile.Prompts?.System ?? string.Empty,
            UserPrompt = promptFile.Prompts?.User ?? string.Empty,
            FewShots = fewShots
        };
    }

    /// <summary>
    /// Takes a file location, parses the prompt file and converts it into a <see cref="SqlPromptEntity"/>
    /// </summary>
    /// <param name="fileLocation">The location of the prompt file</param>
    /// <returns>A <see cref="SqlPromptEntity"/> containing the definition of the prompt file</returns>
    /// <exception cref="FileNotFoundException">Raised if the prompt file is not found</exception>
    public static SqlPromptEntity FromPromptFile(string fileLocation)
    {
        var promptFile = PromptFile.FromFile(fileLocation);
        return FromPromptFile(promptFile);
    }
}
