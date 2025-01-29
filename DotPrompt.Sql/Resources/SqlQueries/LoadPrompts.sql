SELECT
    pf.PromptId,
    pf.PromptName,
    pf.CreatedAt,
    pf.ModifiedAt,
    pf.Model,
    pf.OutputFormat,
    pf.MaxTokens,
    pf.SystemPrompt,
    pf.UserPrompt,
    pp.ParameterId,
    pp.ParameterName,
    pp.ParameterValue,
    pd.DefaultValue
FROM PromptFile pf
         LEFT JOIN PromptParameters pp ON pf.PromptId = pp.PromptId
         LEFT JOIN ParameterDefaults pd ON pp.ParameterId = pd.ParameterId
ORDER BY pf.PromptId