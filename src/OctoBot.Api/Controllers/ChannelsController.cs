using Microsoft.AspNetCore.Mvc;
using OctoBot.Channels.Abstractions;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly IChannelRegistry _registry;

    public ChannelsController(IChannelRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ChannelInfo>> GetChannels()
    {
        var channels = _registry.GetAllFactories().Select(f => new ChannelInfo(
            f.ChannelType,
            f.GetSettingDefinitions()
        )).ToList();

        return Ok(channels);
    }
}

public record ChannelInfo(
    string ChannelType,
    IReadOnlyList<ChannelSettingDefinition> Settings
);
