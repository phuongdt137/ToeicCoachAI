using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class AiCompetencyReport
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ReportTriggerSource TriggerSource { get; set; }
        public Guid? CreditTxId { get; set; }
        public string? MetricsSnapshot { get; set; }
        public string? EstimatedScores { get; set; }
        public string? AiAnalysisDetail { get; set; }
        public string? SuggestedRoadmap { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public CreditTransaction? CreditTransaction { get; set; }
    }
}
