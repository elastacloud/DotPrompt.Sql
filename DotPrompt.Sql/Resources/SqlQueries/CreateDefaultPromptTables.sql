-- Create the PromptFile table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromptFile')
BEGIN
CREATE TABLE PromptFile (
    PromptId INT IDENTITY(1,1) PRIMARY KEY,
    PromptName VARCHAR(255) NOT NULL,
    VersionNumber INT NOT NULL DEFAULT 1,
    CreatedAt DATETIMEOFFSET NULL,
    ModifiedAt DATETIMEOFFSET NULL,
    Model VARCHAR(255) NULL,
    OutputFormat VARCHAR(255) NOT NULL DEFAULT '',
    MaxTokens INT NOT NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL DEFAULT '',
    UserPrompt NVARCHAR(MAX) NOT NULL DEFAULT '',
    CONSTRAINT UQ_PromptName_Version UNIQUE (PromptName, VersionNumber)
);
END;

-- Add VersionNumber column to PromptFile (if not exists)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PromptFile' AND COLUMN_NAME = 'VersionNumber')
BEGIN
ALTER TABLE PromptFile ADD VersionNumber INT NOT NULL DEFAULT 1;
-- Ensure unique constraint exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = 'PromptFile' AND CONSTRAINT_NAME = 'UQ_PromptName_Version')
BEGIN
ALTER TABLE PromptFile ADD CONSTRAINT UQ_PromptName_Version UNIQUE (PromptName, VersionNumber);
END;
END;

-- Create the PromptParameters table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PromptParameters')
BEGIN
CREATE TABLE PromptParameters (
  ParameterId INT IDENTITY(1,1) PRIMARY KEY,
  PromptId INT NOT NULL,
  VersionNumber INT NOT NULL DEFAULT 1,
  ParameterName VARCHAR(255) NOT NULL,
  ParameterValue VARCHAR(255) NOT NULL,
  CONSTRAINT FK_Parameters_PromptFile FOREIGN KEY (PromptId)
      REFERENCES PromptFile(PromptId) ON DELETE CASCADE
);
END;

-- Add VersionNumber column to PromptParameters (if not exists)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PromptParameters' AND COLUMN_NAME = 'VersionNumber')
BEGIN
ALTER TABLE PromptParameters ADD VersionNumber INT NOT NULL DEFAULT 1;
END;

-- Add Unique Constraint on (ParameterId, VersionNumber) in PromptParameters
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'PromptParameters' AND CONSTRAINT_NAME = 'UQ_ParameterId_Version'
)
BEGIN
ALTER TABLE PromptParameters ADD CONSTRAINT UQ_ParameterId_Version UNIQUE (ParameterId, VersionNumber);
END;

-- Create the ParameterDefaults table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ParameterDefaults')
BEGIN
CREATE TABLE ParameterDefaults (
   DefaultId INT IDENTITY(1,1) PRIMARY KEY,
   ParameterId INT NOT NULL,
   VersionNumber INT NOT NULL DEFAULT 1,
   DefaultValue VARCHAR(255) NOT NULL,
   Description NVARCHAR(500) NULL,
   CONSTRAINT FK_ParameterDefaults_PromptParameters FOREIGN KEY (ParameterId)
       REFERENCES PromptParameters(ParameterId) ON DELETE CASCADE
);
END;

-- Add VersionNumber column to ParameterDefaults (if not exists)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ParameterDefaults' AND COLUMN_NAME = 'VersionNumber')
BEGIN
ALTER TABLE ParameterDefaults ADD VersionNumber INT NOT NULL DEFAULT 1;
END;

-- Add Unique Constraint on (ParameterId, VersionNumber) in ParameterDefaults
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'ParameterDefaults' AND CONSTRAINT_NAME = 'UQ_ParameterDefaults_ParameterId_Version'
)
BEGIN
ALTER TABLE ParameterDefaults ADD CONSTRAINT UQ_ParameterDefaults_ParameterId_Version UNIQUE (ParameterId, VersionNumber);
END;

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

-- Ensure PromptParameterType TVP exists
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'PromptParameterType' AND is_table_type = 1)
BEGIN
CREATE TYPE PromptParameterType AS TABLE
    (
    ParameterName VARCHAR(255),
    ParameterValue VARCHAR(255)
    );
END;

-- Ensure ParameterDefaultType TVP exists
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'ParameterDefaultType' AND is_table_type = 1)
BEGIN
CREATE TYPE ParameterDefaultType AS TABLE
    (
    ParameterName VARCHAR(255),
    DefaultValue VARCHAR(255)
    );
END;