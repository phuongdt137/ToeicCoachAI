using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class ContextualChatMessage
    {
        public Guid Id { get; set; }
        public Guid ThreadId { get; set; }
        public ChatSenderRole SenderRole { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public Guid? CreditTxId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ContextualChatThread Thread { get; set; } = null!;
        public CreditTransaction? CreditTransaction { get; set; }
    }
}
