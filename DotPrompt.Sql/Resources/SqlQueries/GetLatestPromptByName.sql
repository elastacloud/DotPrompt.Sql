WITH LatestPrompts AS (
    SELECT
        PromptId,
        PromptName,
        VersionNumber,
        CreatedAt,
        ModifiedAt,
        Model,
        OutputFormat,
        MaxTokens,
        SystemPrompt,
        UserPrompt,
        ROW_NUMBER() OVER (PARTITION BY PromptName ORDER BY VersionNumber DESC) AS RowNum
    FROM PromptFile
)
SELECT
    lp.PromptId,
    lp.PromptName,
    lp.VersionNumber,
    lp.CreatedAt,
    lp.ModifiedAt,
    lp.Model,
    lp.OutputFormat,
    lp.MaxTokens,
    lp.SystemPrompt,
    lp.UserPrompt
FROM LatestPrompts lp
WHERE lp.PromptName = @PromptName AND lp.RowNum = 1;