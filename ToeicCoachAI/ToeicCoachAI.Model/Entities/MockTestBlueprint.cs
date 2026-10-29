using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class MockTestBlueprint
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public BlueprintTestType TestType { get; set; }
        public int DurationMinutes { get; set; }
        public List<Guid> QuestionGroupIds { get; set; } = new List<Guid>();
        public bool IsActive { get; set; } = true;

        public ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
    }
}
