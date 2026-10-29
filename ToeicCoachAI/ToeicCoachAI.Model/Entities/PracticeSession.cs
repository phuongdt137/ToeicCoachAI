using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class PracticeSession
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public PracticeSessionMode SessionMode { get; set; }
        public Guid? BlueprintId { get; set; }
        public string? FilterConfig { get; set; }
        public PracticeSessionStatus Status { get; set; } = PracticeSessionStatus.IN_PROGRESS;
        public string? CurrentStateSnapshot { get; set; }
        public string? ScoreSummary { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? SubmittedAt { get; set; }

        public User? User { get; set; }
        public MockTestBlueprint? Blueprint { get; set; }
        public ICollection<GuestSession> GuestSessions { get; set; } = new List<GuestSession>();
        public ICollection<UserAttempt> Attempts { get; set; } = new List<UserAttempt>();
        public ICollection<WritingSubmission> WritingSubmissions { get; set; } = new List<WritingSubmission>();
        public ICollection<ContextualChatThread> ChatThreads { get; set; } = new List<ContextualChatThread>();
    }
}
