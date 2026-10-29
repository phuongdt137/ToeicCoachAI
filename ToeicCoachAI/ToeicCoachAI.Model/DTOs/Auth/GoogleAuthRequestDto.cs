namespace ToeicCoachAI.Model.DTOs.Auth
{
    public class GoogleAuthRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AnonymousGuestToken { get; set; }
    }
}
