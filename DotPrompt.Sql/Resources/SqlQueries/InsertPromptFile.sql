INSERT INTO PromptFile (PromptName, CreatedAt, ModifiedAt, Model, OutputFormat, OutputSchema, MaxTokens, Temperature, SystemPrompt, UserPrompt, FewShots)
    OUTPUT INSERTED.PromptId
VALUES (@PromptName, @CreatedAt, @ModifiedAt, @Model, @OutputFormat, @OutputSchema, @MaxTokens, @Temperature, @SystemPrompt, @UserPrompt, @FewShots)