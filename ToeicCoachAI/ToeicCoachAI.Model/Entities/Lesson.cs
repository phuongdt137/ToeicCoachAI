namespace ToeicCoachAI.Model.Entities
{
    public class Lesson
    {
        public Guid Id { get; set; }
        public Guid SkillTagId { get; set; }
        public short? PartNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string TheoryMarkdown { get; set; } = string.Empty;
        public string? CommonTraps { get; set; }
        public string? SampleQuestions { get; set; }
        public int OrderIndex { get; set; }
        public bool IsFreePreview { get; set; }

        public SkillTag SkillTag { get; set; } = null!;
        public ICollection<UserLessonProgress> UserProgresses { get; set; } = new List<UserLessonProgress>();
    }
}
