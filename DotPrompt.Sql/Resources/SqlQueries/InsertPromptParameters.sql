 INSERT INTO PromptParameters (PromptId, ParameterName, ParameterValue)
                    OUTPUT INSERTED.ParameterId
                    VALUES (@PromptId, @ParameterName, @ParameterValue)