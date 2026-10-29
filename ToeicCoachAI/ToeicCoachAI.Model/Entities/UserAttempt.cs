namespace ToeicCoachAI.Model.Entities
{
    public class UserAttempt
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid? UserId { get; set; }
        public Guid QuestionId { get; set; }
        public char? SelectedOption { get; set; }
        public bool IsCorrect { get; set; }
        public int TimeSpentSec { get; set; }
        public bool IsFlagged { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public PracticeSession Session { get; set; } = null!;
        public User? User { get; set; }
        public Question Question { get; set; } = null!;
        public ICollection<ContextualChatThread> ChatThreads { get; set; } = new List<ContextualChatThread>();
    }
}
