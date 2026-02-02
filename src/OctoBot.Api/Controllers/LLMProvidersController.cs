using MediatR;
using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.Commands.LLMConfig;
using OctoBot.Application.DTOs;
using OctoBot.Application.Queries.LLMConfig;
using OctoBot.LLM.Abstractions;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/llm-providers")]
public class LLMProvidersController : ControllerBase
{
    private readonly ILLMProviderRegistry _registry;
    private readonly IMediator _mediator;

    public LLMProvidersController(ILLMProviderRegistry registry, IMediator mediator)
    {
        _registry = registry;
        _mediator = mediator;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<LLMProviderInfo>> GetProviders()
    {
        var providers = _registry.GetAllProviders().Select(p => new LLMProviderInfo(
            p.Name,
            p.DisplayName,
            p.SupportsStreaming,
            p.SupportsFunctionCalling,
            p.SupportedModels
        )).ToList();

        return Ok(providers);
    }

    [HttpGet("configs")]
    public async Task<ActionResult<IReadOnlyList<LLMConfigDto>>> GetConfigs()
    {
        var configs = await _mediator.Send(new GetAllLLMConfigsQuery());
        return Ok(configs);
    }

    [HttpPost("configs")]
    public async Task<ActionResult<LLMConfigDto>> CreateConfig([FromBody] CreateLLMConfigDto dto)
    {
        var config = await _mediator.Send(new CreateLLMConfigCommand(dto));
        return Created($"/api/llm-providers/configs/{config.Id}", config);
    }
}

public record LLMProviderInfo(
    string Name,
    string DisplayName,
    bool SupportsStreaming,
    bool SupportsFunctionCalling,
    IReadOnlyList<string> SupportedModels
);
