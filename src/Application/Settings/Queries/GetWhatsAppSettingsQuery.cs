using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;

namespace Tempest.Application.Settings.Queries;

/// <summary>Public on purpose: the number and message are visible in every catalog link anyway.</summary>
public sealed record GetWhatsAppSettingsQuery : IQuery<WhatsAppSettingsDto>, IAllowAnonymous;

internal sealed class GetWhatsAppSettingsQueryHandler(IStoreSettingsRepository settings)
    : IQueryHandler<GetWhatsAppSettingsQuery, WhatsAppSettingsDto>
{
    public async Task<Result<WhatsAppSettingsDto>> Handle(
        GetWhatsAppSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var current = await settings.GetAsync(cancellationToken) ?? StoreSettings.CreateDefault();
        return current.ToDto();
    }
}
