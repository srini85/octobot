using Microsoft.AspNetCore.Mvc;
using OctoBot.Agent;
using OctoBot.Core.ValueObjects;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/bots/{botId:guid}/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentManager _agentManager;

    public ChatController(IAgentManager agentManager)
    {
        _agentManager = agentManager;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        Guid botId,
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        try
        {
            var agent = await _agentManager.GetOrCreateAgentAsync(botId, ct);

            var incomingMessage = new IncomingMessage(
                ChannelType: "api",
                ChannelId: "api",
                UserId: request.UserId ?? "anonymous",
                UserName: request.UserName ?? "Anonymous",
                Content: request.Message,
                Timestamp: DateTime.UtcNow
            );

            var response = await agent.ProcessMessageAsync(incomingMessage, ct);

            return Ok(new ChatResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stream")]
    public async Task StreamMessage(
        Guid botId,
        [FromBody] ChatRequest request,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";

        try
        {
            var agent = await _agentManager.GetOrCreateAgentAsync(botId, ct);

            var incomingMessage = new IncomingMessage(
                ChannelType: "api",
                ChannelId: "api",
                UserId: request.UserId ?? "anonymous",
                UserName: request.UserName ?? "Anonymous",
                Content: request.Message,
                Timestamp: DateTime.UtcNow
            );

            await foreach (var chunk in agent.ProcessMessageStreamAsync(incomingMessage, ct))
            {
                await Response.WriteAsync($"data: {chunk}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
        }
        catch (InvalidOperationException ex)
        {
            await Response.WriteAsync($"data: {{\"error\": \"{ex.Message}\"}}\n\n", ct);
        }
    }
}

public record ChatRequest(
    string Message,
    string? UserId = null,
    string? UserName = null
);

public record ChatResponse(string Response);
