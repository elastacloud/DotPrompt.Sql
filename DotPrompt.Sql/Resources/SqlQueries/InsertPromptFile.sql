INSERT INTO PromptFile (PromptName, CreatedAt, ModifiedAt, Model, OutputFormat, MaxTokens, SystemPrompt, UserPrompt)
    OUTPUT INSERTED.PromptId
VALUES (@PromptName, @CreatedAt, @ModifiedAt, @Model, @OutputFormat, @MaxTokens, @SystemPrompt, @UserPrompt)