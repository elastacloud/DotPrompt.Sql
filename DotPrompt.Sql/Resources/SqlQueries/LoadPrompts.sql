WITH LatestPrompts AS (
    SELECT
        PromptId,
        PromptName,
        CreatedAt,
        ModifiedAt,
        Model,
        OutputFormat,
        MaxTokens,
        SystemPrompt,
        UserPrompt,
        VersionNumber,
        ROW_NUMBER() OVER (PARTITION BY PromptName ORDER BY VersionNumber DESC) AS RowNum
    FROM PromptFile
)
SELECT
    lp.PromptId,
    lp.PromptName,
    lp.CreatedAt,
    lp.ModifiedAt,
    lp.Model,
    lp.OutputFormat,
    lp.MaxTokens,
    lp.SystemPrompt,
    lp.UserPrompt,
    pp.ParameterId,
    pp.ParameterName,
    pp.ParameterValue,
    pd.DefaultValue
FROM LatestPrompts lp
         LEFT JOIN PromptParameters pp ON lp.PromptId = pp.PromptId AND lp.VersionNumber = pp.VersionNumber
         LEFT JOIN ParameterDefaults pd ON pp.ParameterId = pd.ParameterId AND pp.VersionNumber = pd.VersionNumber
WHERE lp.RowNum = 1
ORDER BY lp.PromptId;
