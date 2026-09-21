using CipherVault.DTOs;

namespace CipherVault.Services.Contracts;

public interface IDashboardService
{
    Task<DashboardStatsDTO> GetStatsAsync(string userId);
}
