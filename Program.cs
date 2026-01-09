using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OllamaSharp;

try
{
    Uri ollamaUri = new Uri("http://localhost:11434");
    string modelName = "llama2:7b";
    OllamaApiClient ollamaClient = new OllamaApiClient(ollamaUri, modelName);

    ChatClientAgent writer = new ChatClientAgent(ollamaClient, name: "Writer", instructions: "you write engaging stories based on a topic. Be creative, positive, and keep it under 200 words.");
    ChatClientAgent editor = new ChatClientAgent(ollamaClient, name: "Editor", instructions: "you review a story for clarity, grammar, and style, then refine it.");
    Workflow workflow = AgentWorkflowBuilder.BuildSequential(writer, editor);
    AIAgent workflowAgent = workflow.AsAgent();

    string topic = "A day in longyou county in Zhejiang province China.";
    Console.WriteLine($"--- Starting workflow for topic {topic} ---\n");
    AgentRunResponse response = await workflowAgent.RunAsync(topic);
    Console.WriteLine("--- Final Output ---");
    Console.WriteLine(response.Text);
}
catch(Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
