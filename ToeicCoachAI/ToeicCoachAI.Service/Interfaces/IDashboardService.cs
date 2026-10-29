using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Auth;
using ToeicCoachAI.Model.DTOs.Dashboard;

namespace ToeicCoachAI.Service.Interfaces
{
    public interface IDashboardService
    {
        Task<ServiceResult<StudentDashboardSummaryDto>> GetStudentDashboardAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<TestHubPageDto>> GetTestHubPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<VocabHubPageDto>> GetVocabHubPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<SubscriptionAndWalletPageDto>> GetSubscriptionAndWalletPageAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<AuthUserResponseDto>> PurchaseSubscriptionPlanAsync(
            Guid userId,
            Guid planId,
            CancellationToken cancellationToken = default);
    }
}
