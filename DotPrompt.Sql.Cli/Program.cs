// See https://aka.ms/new-console-template for more information

using DotPrompt.Sql;

class Program
{
    public static async Task Main(string[] args)
    {
        var promptFile = args[0];
        var entity = SqlPromptEntity.FromPromptFile(promptFile);

        var config = DatabaseConfigReader.ReadYamlConfig(args[1]);
        var connector = new DatabaseConnector();
        var connection = await connector.ConnectToDatabase(config);
        var loader = new SqlPromptLoader(connection);
        await loader.AddSqlPrompt(entity);

        var prompts = loader.Load();
        foreach (var prompt in prompts)
        {
            Console.WriteLine($"Processed {prompt.PromptName}");
        }
        Console.WriteLine("Press any key to continue ...");
        Console.Read();
    }
}