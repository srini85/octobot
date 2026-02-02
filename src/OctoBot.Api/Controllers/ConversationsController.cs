using MediatR;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.DTOs;
using OctoBot.Application.Queries.Conversation;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/bots/{botId:guid}/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetByBot(
        Guid botId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var conversations = await _mediator.Send(new GetConversationsQuery(botId, skip, take));
        return Ok(conversations);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<ConversationWithMessagesDto>> GetMessages(
        Guid botId,
        Guid id,
        [FromQuery] int limit = 50)
    {
        var conversation = await _mediator.Send(new GetConversationMessagesQuery(id, limit));
        if (conversation == null) return NotFound();
        return Ok(conversation);
    }
}
