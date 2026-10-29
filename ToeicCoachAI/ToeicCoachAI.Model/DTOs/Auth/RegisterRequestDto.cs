using System.ComponentModel.DataAnnotations;

namespace ToeicCoachAI.Model.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [MaxLength(200, ErrorMessage = "Họ và tên không vượt quá 200 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
        public string? AnonymousGuestToken { get; set; }
    }
}
