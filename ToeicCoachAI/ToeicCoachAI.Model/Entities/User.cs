using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.USER;
        public UserTier Tier { get; set; } = UserTier.FREE;
        public int CreditBalance { get; set; } = 0;
        public int? TargetScoreLr { get; set; }
        public int? TargetScoreW { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
        public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
        public ICollection<GuestSession> ClaimedGuestSessions { get; set; } = new List<GuestSession>();
        public ICollection<UserLessonProgress> LessonProgresses { get; set; } = new List<UserLessonProgress>();
        public ICollection<UserVocabProgress> VocabProgresses { get; set; } = new List<UserVocabProgress>();
        public ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
        public ICollection<UserAttempt> Attempts { get; set; } = new List<UserAttempt>();
        public ICollection<WritingSubmission> WritingSubmissions { get; set; } = new List<WritingSubmission>();
        public ICollection<ContextualChatThread> ChatThreads { get; set; } = new List<ContextualChatThread>();
        public ICollection<UserMistake> Mistakes { get; set; } = new List<UserMistake>();
        public ICollection<UserSkillProficiency> SkillProficiencies { get; set; } = new List<UserSkillProficiency>();
        public ICollection<AiCompetencyReport> CompetencyReports { get; set; } = new List<AiCompetencyReport>();
    }
}
