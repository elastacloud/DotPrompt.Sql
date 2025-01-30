// See https://aka.ms/new-console-template for more information
namespace DotPrompt.Sql.Cli;
using DotPrompt.Sql;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var promptFile = args[0];
        var entity = SqlPromptEntity.FromPromptFile(promptFile);

        var config = DatabaseConfigReader.ReadYamlConfig(args[1]);
        var connector = new DatabaseConnector();
        var connection = await connector.ConnectToDatabase(config);
        IPromptRepository sqlRepository = new SqlPromptRepository(connection); 
        var loader = new SqlPromptLoader(sqlRepository);
        bool upVersioned = await loader.AddSqlPrompt(entity);
        Console.WriteLine($"Done: {upVersioned}");

        var prompts = loader.Load();
        foreach (var prompt in prompts)
        {
            Console.WriteLine($"Processed {prompt.PromptName}");
        }
        Console.WriteLine("Press any key to continue ...");
        Console.Read();
    }
}