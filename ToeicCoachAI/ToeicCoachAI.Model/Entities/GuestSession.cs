namespace ToeicCoachAI.Model.Entities
{
    public class GuestSession
    {
        public string AnonymousToken { get; set; } = string.Empty;
        public Guid TestSessionId { get; set; }
        public int? EstimatedScoreLr { get; set; }
        public string? AiBasicFeedback { get; set; }
        public Guid? ClaimedByUserId { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public PracticeSession TestSession { get; set; } = null!;
        public User? ClaimedByUser { get; set; }
    }
}
