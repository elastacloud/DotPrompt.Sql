namespace DotPrompt.Sql.Test;

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class SqlPromptEntityTests : IDisposable
{
    private readonly List<string> _testFiles = new();

    // Helper method to create and track test YAML files
    private void CreateTestYamlFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
        _testFiles.Add(filePath); // Track the file for later cleanup
    }

    [Fact]
    public void FromPromptFile_ValidYaml_ReturnsSqlPromptEntity()
    {
        // Arrange
        string filePath = "test_prompt.yaml";
        string yamlContent = @"
model: gpt-4
config:
  name: TestPrompt
  outputFormat: json
  maxTokens: 200
  input:
    parameters:
      param1: value1
      param2: value2
    default:
      param1: default1
      param2: default2
prompts:
  system: System message
  user: User message";
        
        CreateTestYamlFile(filePath, yamlContent);

        // Act
        var result = SqlPromptEntity.FromPromptFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("gpt-4", result.Model);
        Assert.Equal("TestPrompt", result.PromptName);
        Assert.Equal("json", result.OutputFormat);
        Assert.Equal(200, result.MaxTokens);
        Assert.Equal("System message", result.SystemPrompt);
        Assert.Equal("User message", result.UserPrompt);
        Assert.Equal("value1", result.Parameters["param1"]);
        Assert.Equal("default1", result.Default["param1"]);
    }

    [Fact]
    public void FromPromptFile_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        string invalidPath = "non_existent.yaml";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => SqlPromptEntity.FromPromptFile(invalidPath));
        Assert.Contains("The specified file was not found", exception.Message);
    }

    [Fact]
    public void FromPromptFile_MissingMandatoryFields_ThrowsException()
    {
        // Arrange
        string filePath = "test_missing_optional.yaml";
        string yamlContent = @"
config:
  name: TestPrompt
  outputFormat: json
  maxTokens: 100
prompts:
  system: Default system prompt
  user: Default user prompt";

        CreateTestYamlFile(filePath, yamlContent);

        // Act & Assert
        Assert.Throws<ApplicationException>(() => SqlPromptEntity.FromPromptFile(filePath));
    }

    [Fact]
    public void FromPromptFile_InvalidDataType_ThrowsException()
    {
        // Arrange
        string filePath = "test_invalid_type.yaml";
        string yamlContent = @"
model: gpt-4
config:
  name: TestPrompt
  outputFormat: json
  maxTokens: not_a_number
prompts:
  system: System prompt
  user: User prompt";

        CreateTestYamlFile(filePath, yamlContent);

        // Act & Assert
        Assert.Throws<FormatException>(() => SqlPromptEntity.FromPromptFile(filePath));
    }

    // Cleanup method called after each test
    public void Dispose()
    {
        foreach (var file in _testFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
