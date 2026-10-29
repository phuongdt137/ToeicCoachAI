namespace ToeicCoachAI.Model.Entities
{
    public class UserVocabProgress
    {
        public Guid UserId { get; set; }
        public Guid VocabId { get; set; }
        public short SrsLevel { get; set; } = 0;
        public double EaseFactor { get; set; } = 2.5;
        public int IntervalDays { get; set; } = 0;
        public DateTimeOffset NextReviewAt { get; set; }
        public int WrongCount { get; set; } = 0;

        public User User { get; set; } = null!;
        public Vocabulary Vocabulary { get; set; } = null!;
    }
}
