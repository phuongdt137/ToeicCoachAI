using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class UserMistake
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid PrimarySkillTagId { get; set; }
        public int WrongCount { get; set; } = 1;
        public int ConsecutiveCorrectCount { get; set; } = 0;
        public char? LastWrongOption { get; set; }
        public MistakeStatus Status { get; set; } = MistakeStatus.ACTIVE_MISTAKE;
        public DateTimeOffset LastAttemptedAt { get; set; }
        public DateTimeOffset? NextReviewAt { get; set; }

        public User User { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public SkillTag PrimarySkillTag { get; set; } = null!;
    }
}
