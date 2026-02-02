using Microsoft.AspNetCore.Mvc;
using OctoBot.Application.DTOs;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PluginsController : ControllerBase
{
    private readonly IPluginRegistry _registry;

    public PluginsController(IPluginRegistry registry)
    {
        _registry = registry;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PluginInfoDto>> GetPlugins()
    {
        var plugins = _registry.GetAllPlugins().Select(p => new PluginInfoDto(
            p.Metadata.Id,
            p.Metadata.Name,
            p.Metadata.Description,
            p.Metadata.Version,
            p.Metadata.Author,
            p.Metadata.Dependencies
        )).ToList();

        return Ok(plugins);
    }
}
