using Hangfire;
using Microsoft.Extensions.Logging;
using ToeicCoachAI.Model.DTOs.Admin;
using ToeicCoachAI.Model.Enums;
using ToeicCoachAI.Service.Interfaces;

namespace ToeicCoachAI.Service.BackgroundJobs
{
    public class AiQuestionPipelineJob : IAiQuestionPipelineJob
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AiQuestionPipelineJob> _logger;

        public AiQuestionPipelineJob(
            IAdminService adminService,
            ILogger<AiQuestionPipelineJob> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task ExecuteAutoRefillAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[AI Pipeline] Starting scheduled question pool inventory check (02:00 AM)...");

            var summary = await _adminService.GetDashboardSummaryAsync(cancellationToken);
            if (!summary.IsSuccess || summary.Data == null)
            {
                return;
            }

            var lowTags = summary.Data.SkillTagInventories
                .Where(t => t.Category != SkillCategory.VOCAB_TOPIC && t.IsLowInventory)
                .OrderByDescending(t => t.DeficitCount)
                .Take(3)
                .ToList();

            foreach (var tag in lowTags)
            {
                int batchCount = Math.Min(5, tag.DeficitCount);
                _logger.LogInformation("[AI Pipeline] Auto-refilling {BatchCount} groups for low-inventory SkillTag {Code}...", batchCount, tag.Code);

                await _adminService.TriggerAiGenerationJobAsync(
                    new TriggerAiJobRequestDto
                    {
                        TargetSkillTagId = tag.Id,
                        TargetDifficulty = TargetDifficulty.MEDIUM,
                        RequestedCount = batchCount
                    },
                    JobTriggerType.SCHEDULED_REFILL,
                    cancellationToken);
            }
        }
    }
}
