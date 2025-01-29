-- Create the PromptFile table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromptFile')
BEGIN
CREATE TABLE PromptFile (
                            PromptId INT IDENTITY(1,1) PRIMARY KEY,
                            PromptName VARCHAR(255) NOT NULL UNIQUE,
                            CreatedAt DATETIMEOFFSET NULL,
                            ModifiedAt DATETIMEOFFSET NULL,
                            Model VARCHAR(255) NULL,
                            OutputFormat VARCHAR(255) NOT NULL DEFAULT '',
                            MaxTokens INT NOT NULL,
                            SystemPrompt NVARCHAR(MAX) NOT NULL DEFAULT '',
                            UserPrompt NVARCHAR(MAX) NOT NULL DEFAULT ''
);
END;

-- Create the PromptParameters table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromptParameters')
BEGIN
CREATE TABLE PromptParameters (
                                  ParameterId INT IDENTITY(1,1) PRIMARY KEY,
                                  PromptId INT NOT NULL,
                                  ParameterName VARCHAR(255) NOT NULL,
                                  ParameterValue VARCHAR(255) NOT NULL,
                                  CONSTRAINT FK_Parameters_PromptFile FOREIGN KEY (PromptId)
                                      REFERENCES PromptFile(PromptId) ON DELETE CASCADE
);
END;

-- Create the ParameterDefaults table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ParameterDefaults')
BEGIN
CREATE TABLE ParameterDefaults (
                                   DefaultId INT IDENTITY(1,1) PRIMARY KEY,
                                   ParameterId INT NOT NULL,
                                   DefaultValue VARCHAR(255) NOT NULL,
                                   Description NVARCHAR(500) NULL,
                                   CONSTRAINT FK_ParameterDefaults_PromptParameters FOREIGN KEY (ParameterId)
                                       REFERENCES PromptParameters(ParameterId) ON DELETE CASCADE
);
END;

-- Create the index for PromptParameters.PromptId if it doesn't exist
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_PromptParameters_PromptId' 
    AND object_id = OBJECT_ID('PromptParameters')
)
BEGIN
CREATE INDEX IX_PromptParameters_PromptId ON PromptParameters(PromptId);
END;

-- Create the index for ParameterDefaults.ParameterId if it doesn't exist
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ParameterDefaults_ParameterId' 
    AND object_id = OBJECT_ID('ParameterDefaults')
)
BEGIN
CREATE INDEX IX_ParameterDefaults_ParameterId ON ParameterDefaults(ParameterId);
END;
