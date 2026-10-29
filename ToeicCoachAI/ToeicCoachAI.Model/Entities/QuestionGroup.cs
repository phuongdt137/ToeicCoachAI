using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class QuestionGroup
    {
        public Guid Id { get; set; }
        public Guid? GenerationJobId { get; set; }
        public SkillType SkillType { get; set; }
        public short PartNumber { get; set; }
        public short DifficultyLevel { get; set; }
        public QuestionSourceType SourceType { get; set; }
        public QuestionGroupStatus Status { get; set; } = QuestionGroupStatus.DRAFT_VALIDATING;
        public string? PassageContent { get; set; }
        public string? TranscriptData { get; set; }
        public string? MediaAssets { get; set; }
        public string? WritingPromptMeta { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public AiGenerationJob? GenerationJob { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<AiValidationLog> ValidationLogs { get; set; } = new List<AiValidationLog>();
    }
}
