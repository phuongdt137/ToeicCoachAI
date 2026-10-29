using ToeicCoachAI.Model.Enums;

namespace ToeicCoachAI.Model.DTOs.Auth
{
    public class AuthUserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public UserTier Tier { get; set; }
        public int CreditBalance { get; set; }
        public int? TargetScoreLr { get; set; }
        public int? TargetScoreW { get; set; }
    }
}
