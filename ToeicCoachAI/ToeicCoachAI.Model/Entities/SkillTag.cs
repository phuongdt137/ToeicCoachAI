using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class SkillTag
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public SkillCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public int MinPoolThreshold { get; set; } = 50;
        public string? AiGenerationRules { get; set; }

        public SkillTag? Parent { get; set; }
        public ICollection<SkillTag> Children { get; set; } = new List<SkillTag>();
        public Lesson? Lesson { get; set; }
        public ICollection<Vocabulary> Vocabularies { get; set; } = new List<Vocabulary>();
        public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
        public ICollection<AiGenerationJob> GenerationJobs { get; set; } = new List<AiGenerationJob>();
        public ICollection<UserMistake> PrimaryMistakes { get; set; } = new List<UserMistake>();
        public ICollection<UserSkillProficiency> UserProficiencies { get; set; } = new List<UserSkillProficiency>();
    }
}
