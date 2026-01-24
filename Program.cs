using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OllamaSharp;
using OllamaSharp.Models.Chat;

IConfigurationRoot configBuilder = new ConfigurationBuilder().AddJsonFile("appconfig.json").Build();
IConfigurationSection configSection = configBuilder.GetSection("AppSettings");
string uri = configSection["OllamaURI"] ?? throw new Exception("OllamaURI not found in configuration.");
string modelName = configSection["ModelName"] ?? throw new Exception("ModelName not found in configuration.");
string dateExtractorInstructions = configSection["DateExtractorInstructions"] ?? throw new Exception("DateExtractorInstructions not found in configuration.");
string mcpProjectPath = configSection["MCPProjectPath"] ?? throw new Exception("MCPProjectPath not found in configuration.");
string mcpProjectName = configSection["MCPProjectName"] ?? throw new Exception("MCPProjectName not found in configuration.");
Uri ollamaUri = new Uri(uri);

try
{   
    OllamaApiClient ollamaClient = new OllamaApiClient(ollamaUri, modelName);
    ChatClientAgent dateExtractorAgent = new ChatClientAgent(ollamaClient, name: "DateExtractor", instructions: dateExtractorInstructions);    
    Console.Write("Prompt: ");
    string chatMessage = Console.ReadLine() ?? throw new Exception("Prompt is empty.");
    AgentRunResponse response = await dateExtractorAgent.RunAsync(chatMessage);
    Console.WriteLine($"The date to be booked: {response.Text.Trim()}");
    string extractedDate = response.Text.Trim();

    //Connect to MCP server
    StdioClientTransport clientTransport = new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = mcpProjectName,
        Command = "dotnet run",
        Arguments = ["--project", mcpProjectPath],
    });

    McpClient mcpClient = await McpClient.CreateAsync(clientTransport);
    Console.WriteLine($"Connected to server: {mcpClient.ServerInfo.Name}");
    CallToolResult result = await mcpClient.CallToolAsync("book_calendar", new Dictionary<string, object?>() {["message"] = extractedDate}, cancellationToken: CancellationToken.None);
    Console.WriteLine($"{result.Content.OfType<TextContentBlock>().First().Text})");
}
catch(Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
