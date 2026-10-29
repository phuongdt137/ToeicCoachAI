using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class CreditTransaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public CreditTxType TxType { get; set; }
        public CreditFeatureCode? FeatureCode { get; set; }
        public int Amount { get; set; }
        public int BalanceAfter { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? PaymentOrderCode { get; set; }
        public CreditTxStatus Status { get; set; } = CreditTxStatus.PENDING;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public ICollection<WritingSubmission> WritingSubmissions { get; set; } = new List<WritingSubmission>();
        public ICollection<ContextualChatMessage> ChatMessages { get; set; } = new List<ContextualChatMessage>();
        public ICollection<AiCompetencyReport> CompetencyReports { get; set; } = new List<AiCompetencyReport>();
    }
}
