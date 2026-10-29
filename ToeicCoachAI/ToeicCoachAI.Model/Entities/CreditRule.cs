using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.Entities
{
    public class CreditRule
    {
        public CreditFeatureCode FeatureCode { get; set; }
        public int CreditCost { get; set; }
        public int RateLimitPerMinute { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
