using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Admin;
using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Service.Interfaces
{
    public interface IAdminService
    {
        // Phase 1: Overview & SkillTag Management (UC-16)
        Task<ServiceResult<AdminDashboardSummaryDto>> GetDashboardSummaryAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult<SkillTagManagementPageDto>> GetSkillTagManagementPageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> SaveSkillTagAsync(
            SaveSkillTagRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> DeleteSkillTagAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        // Phase 2: Dual-AI Generation & Blind Validation Pipeline (UC-17, UC-18)
        Task<ServiceResult<AiPipelinePageDto>> GetAiPipelinePageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> TriggerAiGenerationJobAsync(
            TriggerAiJobRequestDto request,
            JobTriggerType triggerType = JobTriggerType.ADMIN_MANUAL,
            CancellationToken cancellationToken = default);

        // Phase 3: Quarantine Review Queue & Question Bank (UC-19, UC-20)
        Task<ServiceResult<QuarantinePageDto>> GetQuarantinePageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> ApproveAndPublishQuarantineGroupAsync(
            ReviewQuarantineRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> ChangeQuestionGroupStatusAsync(
            Guid groupId,
            QuestionGroupStatus newStatus,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<QuestionBankPageDto>> GetQuestionBankPageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> CreateCuratedQuestionGroupAsync(
            CreateCuratedQuestionRequestDto request,
            CancellationToken cancellationToken = default);

        // Phase 4: MockTestBlueprint Packaging & Credit/Subscription Rules (UC-21, UC-24)
        Task<ServiceResult<BlueprintManagementPageDto>> GetBlueprintManagementPageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> SaveBlueprintAsync(
            SaveBlueprintRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> AutoAssembleBlueprintAsync(
            Guid blueprintId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<AdminSettingsPageDto>> GetAdminSettingsPageAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResult> UpdateCreditRuleAsync(
            CreditRuleAdminDto request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> SaveSubscriptionPlanAsync(
            SubscriptionPlanAdminDto request,
            CancellationToken cancellationToken = default);
    }
}
