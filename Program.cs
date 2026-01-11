using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OllamaSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

IConfigurationRoot configBuilder = new ConfigurationBuilder().AddJsonFile("appconfig.json").Build();
IConfigurationSection configSection = configBuilder.GetSection("AppSettings");
string uri = configSection["OllamaURI"] ?? throw new Exception("OllamaURI not found in configuration.");
string modelName = configSection["ModelName"] ?? throw new Exception("ModelName not found in configuration.");
string writerInstructions = configSection["WriterAgentInstructions"] ?? throw new Exception("WriterInstructions not found in configuration.");
string editorInstructions = configSection["EditorAgentInstructions"] ?? throw new Exception("EditorInstructions not found in configuration.");
Uri ollamaUri = new Uri(uri);

try
{   
    OllamaApiClient ollamaClient = new OllamaApiClient(ollamaUri, modelName);    
    ChatClientAgent writer = new ChatClientAgent(ollamaClient, name: "Writer", instructions: writerInstructions);   
    ChatClientAgent editor = new ChatClientAgent(ollamaClient, name: "Editor", instructions: editorInstructions);
    Workflow workflow = AgentWorkflowBuilder.BuildSequential(writer, editor);
    AIAgent workflowAgent = workflow.AsAgent();

    string topic = "A day in Hangzhou Zhejiang province China.";
    Console.WriteLine($"--- Starting to write a story for topic '{topic}' ---\n");
    AgentRunResponse response = await workflowAgent.RunAsync(topic);
    Console.WriteLine("--- Final Output ---");
    Console.WriteLine(response.Text);
}
catch(Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
