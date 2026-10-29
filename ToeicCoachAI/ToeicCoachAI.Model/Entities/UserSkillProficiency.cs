namespace ToeicCoachAI.Model.Entities
{
    public class UserSkillProficiency
    {
        public Guid UserId { get; set; }
        public Guid SkillTagId { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectAttempts { get; set; }
        public decimal AccuracyRate { get; set; }
        public decimal AvgTimeSec { get; set; }
        public decimal ProficiencyScore { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public SkillTag SkillTag { get; set; } = null!;
    }
}
