namespace ToeicCoachAI.Model.Entities
{
    public class Question
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public short QuestionOrder { get; set; }
        public string? QuestionStem { get; set; }
        public string? Options { get; set; }
        public char? CorrectOption { get; set; }
        public string? ExplanationGeneral { get; set; }
        public string? OptionExplanations { get; set; }
        public string? WritingRubric { get; set; }
        public string? UsageStats { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public QuestionGroup Group { get; set; } = null!;
        public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
        public ICollection<UserAttempt> Attempts { get; set; } = new List<UserAttempt>();
        public ICollection<WritingSubmission> WritingSubmissions { get; set; } = new List<WritingSubmission>();
        public ICollection<ContextualChatThread> ChatThreads { get; set; } = new List<ContextualChatThread>();
        public ICollection<UserMistake> Mistakes { get; set; } = new List<UserMistake>();
    }
}
