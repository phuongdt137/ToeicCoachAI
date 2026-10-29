using System.ComponentModel.DataAnnotations;

namespace ToeicCoachAI.Model.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
        public string? AnonymousGuestToken { get; set; }
    }
}
