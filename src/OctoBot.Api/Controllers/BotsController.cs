using MediatR;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.Commands.BotInstance;
using OctoBot.Application.DTOs;
using OctoBot.Application.Queries.BotInstance;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BotsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BotInstanceDto>>> GetAll()
    {
        var bots = await _mediator.Send(new GetAllBotInstancesQuery());
        return Ok(bots);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BotInstanceDto>> GetById(Guid id)
    {
        var bot = await _mediator.Send(new GetBotInstanceQuery(id));
        if (bot == null) return NotFound();
        return Ok(bot);
    }

    [HttpPost]
    public async Task<ActionResult<BotInstanceDto>> Create([FromBody] CreateBotInstanceDto dto)
    {
        var bot = await _mediator.Send(new CreateBotInstanceCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = bot.Id }, bot);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BotInstanceDto>> Update(Guid id, [FromBody] UpdateBotInstanceDto dto)
    {
        var bot = await _mediator.Send(new UpdateBotInstanceCommand(id, dto));
        if (bot == null) return NotFound();
        return Ok(bot);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteBotInstanceCommand(id));
        if (!result) return NotFound();
        return NoContent();
    }
}
