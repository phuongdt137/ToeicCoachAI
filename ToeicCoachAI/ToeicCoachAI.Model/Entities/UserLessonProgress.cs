namespace ToeicCoachAI.Model.Entities
{
    public class UserLessonProgress
    {
        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset LastAccessedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public User User { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;
    }
}
