CREATE OR ALTER PROCEDURE sp_AddSqlPrompt
    @PromptName VARCHAR(255),
    @Model VARCHAR(255),
    @OutputFormat VARCHAR(255),
    @MaxTokens INT,
    @SystemPrompt NVARCHAR(MAX),
    @UserPrompt NVARCHAR(MAX),
    @Parameters PromptParameterType READONLY, -- Table-Valued Parameter
    @Defaults ParameterDefaultType READONLY, -- Table-Valued Parameter
    @IsNewVersion BIT OUTPUT -- New Output Parameter
    AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingPromptId INT, @ExistingVersion INT, @NewVersion INT;
    DECLARE @NewPromptId INT;
    SET @IsNewVersion = 0; -- Default to false

    -- Get latest version of the prompt
SELECT TOP 1 
        @ExistingPromptId = PromptId,
        @ExistingVersion = VersionNumber
FROM PromptFile
WHERE PromptName = @PromptName
ORDER BY VersionNumber DESC;

-- If no existing prompt, set version to 1, otherwise increment
SET @NewVersion = ISNULL(@ExistingVersion, 0) + 1;

    -- Check if any values have changed
    IF NOT EXISTS (
        SELECT 1 
        FROM PromptFile
        WHERE PromptId = @ExistingPromptId
        AND Model = @Model
        AND OutputFormat = @OutputFormat
        AND MaxTokens = @MaxTokens
        AND SystemPrompt = @SystemPrompt
        AND UserPrompt = @UserPrompt
    )
    OR EXISTS (
        -- Parameters changed?
        SELECT 1 FROM @Parameters p
        WHERE NOT EXISTS (
            SELECT 1 FROM PromptParameters pp
            WHERE pp.PromptId = @ExistingPromptId
            AND pp.VersionNumber = @ExistingVersion
            AND pp.ParameterName = p.ParameterName
            AND pp.ParameterValue = p.ParameterValue
        )
    )
    OR EXISTS (
        -- Defaults changed?
        SELECT 1 FROM @Defaults d
        WHERE NOT EXISTS (
            SELECT 1 FROM ParameterDefaults pd
            JOIN PromptParameters pp ON pd.ParameterId = pp.ParameterId
            WHERE pp.PromptId = @ExistingPromptId
            AND pp.VersionNumber = @ExistingVersion
            AND pd.VersionNumber = @ExistingVersion
            AND pp.ParameterName = d.ParameterName
            AND pd.DefaultValue = d.DefaultValue
        )
    )
BEGIN
        -- Insert new version of the prompt
INSERT INTO PromptFile (PromptName, VersionNumber, CreatedAt, ModifiedAt, Model, OutputFormat, MaxTokens, SystemPrompt, UserPrompt)
VALUES (@PromptName, @NewVersion, GETUTCDATE(), GETUTCDATE(), @Model, @OutputFormat, @MaxTokens, @SystemPrompt, @UserPrompt);

SET @NewPromptId = SCOPE_IDENTITY();

        -- Insert new parameters
INSERT INTO PromptParameters (PromptId, VersionNumber, ParameterName, ParameterValue)
SELECT @NewPromptId, @NewVersion, p.ParameterName, p.ParameterValue
FROM @Parameters p;

-- Insert new defaults
INSERT INTO ParameterDefaults (ParameterId, VersionNumber, DefaultValue)
SELECT pp.ParameterId, @NewVersion, d.DefaultValue
FROM @Defaults d
         JOIN PromptParameters pp ON pp.PromptId = @NewPromptId AND pp.ParameterName = d.ParameterName;

-- Set output flag to indicate a new version was inserted
SET @IsNewVersion = 1;
END;
END;