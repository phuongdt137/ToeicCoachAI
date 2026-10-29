using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class WritingSubmission
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public Guid QuestionId { get; set; }
        public string UserAnswerText { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int TimeSpentSec { get; set; }
        public GradingStatus GradingStatus { get; set; } = GradingStatus.PENDING;
        public decimal? RawScore { get; set; }
        public string? AiEvaluation { get; set; }
        public Guid? CreditTxId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public PracticeSession Session { get; set; } = null!;
        public User User { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public CreditTransaction? CreditTransaction { get; set; }
    }
}
