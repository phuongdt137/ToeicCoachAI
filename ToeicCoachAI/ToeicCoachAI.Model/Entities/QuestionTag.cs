namespace ToeicCoachAI.Model.Entities
{
    public class QuestionTag
    {
        public Guid QuestionId { get; set; }
        public Guid SkillTagId { get; set; }

        public Question Question { get; set; } = null!;
        public SkillTag SkillTag { get; set; } = null!;
    }
}
