namespace DotPrompt.Sql.Types;
/// <summary>
/// Used to define a parameter that can be used in the prompt file
/// </summary>
public class PromptParameter
{
    /// <summary>
    /// A database id which is incremental and a unique primary key
    /// </summary>
    public int ParameterId { get; set; }
    /// <summary>
    /// The name of parameter which should be unique within the prompt file and can have the ? after it to denote nullable
    /// </summary>
    public required string ParameterName { get; set; }
    /// <summary>
    /// The value of the parameter which could be nullable
    /// </summary>
    public required string ParameterValue { get; set; }
    /// <summary>
    /// A default value which can be defined once per parameter
    /// </summary>
    public object? DefaultValue { get; set; }
}