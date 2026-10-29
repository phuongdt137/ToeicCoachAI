namespace ToeicCoachAI.Model.Entities
{
    public class ContextualChatThread
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? SessionId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid? AttemptId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public PracticeSession? Session { get; set; }
        public Question Question { get; set; } = null!;
        public UserAttempt? Attempt { get; set; }
        public ICollection<ContextualChatMessage> Messages { get; set; } = new List<ContextualChatMessage>();
    }
}
