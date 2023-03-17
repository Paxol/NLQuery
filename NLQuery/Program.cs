using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NLQuery;
using OpenAI_API;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("default") ??
                       throw new Exception("Missing connection string \"default\"");

var db = new DatabaseSchemaParser
{
    ConnectionString = connectionString
};

var dbStructure = await db.GetDatabaseSchemaAsync();

var sysMessage = $"""
### Microsoft SQL Server tables, with their properties:
#
${dbStructure}
#

Respond only with a valid SQL Server query 
""";

var question = string.Empty;
while (string.IsNullOrEmpty(question))
{
    Console.Write("Insert your question: ");
    question = Console.ReadLine() ?? string.Empty;
}

var api = new OpenAIAPI(config["OpenAI_ApiKey"]);
var chat = api.Chat.CreateConversation();

if (chat is null) throw new Exception("OpenAI conversation is null");

chat.AppendSystemMessage(sysMessage);

chat.AppendUserInput(question);
var response = await chat.GetResponseFromChatbot();

if (string.IsNullOrEmpty(response)) throw new Exception("Empty response");

Console.WriteLine($"\nQuery:\n{response}\n");

await using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();

    var command = new SqlCommand(response[response.IndexOf("SELECT", StringComparison.Ordinal)..], conn);
    var reader = await command.ExecuteReaderAsync();
    
    while (reader.Read())
    {
        Console.WriteLine($"{reader[0]}");
    }
}

return 0;