using MediatR;
using OctoBot.Application.DTOs;
using OctoBot.Core.Interfaces;

namespace OctoBot.Application.Queries.LLMConfig;

public class GetAllLLMConfigsHandler : IRequestHandler<GetAllLLMConfigsQuery, IReadOnlyList<LLMConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllLLMConfigsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LLMConfigDto>> Handle(GetAllLLMConfigsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _unitOfWork.LLMConfigs.GetAllAsync(cancellationToken);

        return configs.Select(c => new LLMConfigDto(
            c.Id,
            c.Name,
            c.ProviderType,
            c.ModelId,
            c.Endpoint,
            c.CreatedAt,
            c.UpdatedAt
        )).ToList();
    }
}
