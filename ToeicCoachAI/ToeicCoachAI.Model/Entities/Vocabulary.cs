namespace ToeicCoachAI.Model.Entities
{
    public class Vocabulary
    {
        public Guid Id { get; set; }
        public Guid TopicTagId { get; set; }
        public string Word { get; set; } = string.Empty;
        public string? Phonetic { get; set; }
        public string Pos { get; set; } = string.Empty;
        public string MeaningVi { get; set; } = string.Empty;
        public string? DefinitionEn { get; set; }
        public string? ExampleEn { get; set; }
        public string? ExampleVi { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? WordFamily { get; set; }
        public string? Collocations { get; set; }

        public SkillTag TopicTag { get; set; } = null!;
        public ICollection<UserVocabProgress> UserProgresses { get; set; } = new List<UserVocabProgress>();
    }
}
