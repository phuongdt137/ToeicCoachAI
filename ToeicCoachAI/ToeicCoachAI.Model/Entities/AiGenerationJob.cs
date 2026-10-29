using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class AiGenerationJob
    {
        public Guid Id { get; set; }
        public JobTriggerType TriggerType { get; set; }
        public Guid TargetSkillTagId { get; set; }
        public TargetDifficulty TargetDifficulty { get; set; }
        public int RequestedCount { get; set; }
        public int PublishedCount { get; set; }
        public int QuarantinedCount { get; set; }
        public JobStatus Status { get; set; } = JobStatus.RUNNING;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public SkillTag TargetSkillTag { get; set; } = null!;
        public ICollection<QuestionGroup> QuestionGroups { get; set; } = new List<QuestionGroup>();
    }
}
