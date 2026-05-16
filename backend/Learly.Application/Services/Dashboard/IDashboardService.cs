using Learly.Application.Contracts.Dashboard;
using Learly.Application.Services.Common;

namespace Learly.Application.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardGeralResponse?> ObterGeralAsync(
        AppUserContext uc,
        DateOnly? dataReferencia = null,
        CancellationToken cancellationToken = default);
}
