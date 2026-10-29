using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class AiValidationLog
    {
        public Guid Id { get; set; }
        public Guid QuestionGroupId { get; set; }
        public string ValidatorModel { get; set; } = string.Empty;
        public string? BlindSolvedAnswers { get; set; }
        public bool IsAnswerMatched { get; set; }
        public int EtsComplianceScore { get; set; }
        public int DistractorQualityScore { get; set; }
        public ValidationDecision FinalDecision { get; set; }
        public string? RejectionReasons { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public QuestionGroup QuestionGroup { get; set; } = null!;
    }
}
