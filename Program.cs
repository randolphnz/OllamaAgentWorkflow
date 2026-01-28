using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OllamaSharp;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

var configBuilder = new ConfigurationBuilder().AddJsonFile("appconfig.json").Build();
var configSection = configBuilder.GetSection("AppSettings");
var uri = configSection["OllamaURI"] ?? throw new Exception("OllamaURI not found in configuration.");
var modelName = configSection["ModelName"] ?? throw new Exception("ModelName not found in configuration.");
var dateExtractorInstructions = configSection["DateExtractorInstructions"] ?? throw new Exception("DateExtractorInstructions not found in configuration.");
var mcpProjectPath = configSection["MCPProjectPath"] ?? throw new Exception("MCPProjectPath not found in configuration.");
var mcpProjectName = configSection["MCPProjectName"] ?? throw new Exception("MCPProjectName not found in configuration.");
Uri ollamaUri = new Uri(uri);

try
{   
    var ollamaClient = new OllamaApiClient(ollamaUri, modelName);

    // MCP Client Options
    var clientOptions = new McpClientOptions
    {
        ClientInfo = new() { Name = "demo-client", Version = "1.0.0" }
    };

    // Connect to MCP server
    var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = mcpProjectName,
        Command = "dotnet run",
        Arguments = ["--project", mcpProjectPath],
    });

    // Logger
    using var loggerFactory = LoggerFactory.Create(a => a.AddConsole().SetMinimumLevel(LogLevel.Information));

    // Create MCP client
    var mcpClient = await McpClient.CreateAsync(clientTransport, clientOptions, loggerFactory: loggerFactory);
    Console.WriteLine($"\nConnected to server: {mcpClient.ServerInfo.Name}");

    // Build chat client with invoking mcp tool invocation support
    var chatClient = new ChatClientBuilder(ollamaClient).UseLogging(loggerFactory).UseFunctionInvocation().Build();

    // Get available tools from MCP Server
    var mcpTools = await mcpClient.ListToolsAsync();
    foreach (var tool in await mcpClient.ListToolsAsync())
    {
        Console.WriteLine($"\nAvailable Tools: {tool.Name} ({tool.Description})\n");
    }

    Console.WriteLine("Type your message below (type 'exit' to quit):");

    while (true)
    {
        Console.Write("\nYou: ");
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput))
            continue;

        if (userInput.Trim().ToLower() == "exit")
        {
            Console.WriteLine("Exiting chat...");
            break;
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, userInput)
        };

        try
        {
            var response = await chatClient.GetResponseAsync(messages, new ChatOptions { Tools = [.. mcpTools] });
            var assistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);

            if (assistantMessage != null)
            {
                var textOutput = string.Join(" ", assistantMessage.Contents.Select(c => c.ToString()));
                Console.WriteLine("\nAI: " + textOutput);
            }
            else
            {
                Console.WriteLine("\nAI: (no assistant message received)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
    
}
catch(Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
