using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Auth;

namespace ToeicCoachAI.Service.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthUserResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<ServiceResult<AuthUserResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<ServiceResult<AuthUserResponseDto>> AuthenticateGoogleAsync(GoogleAuthRequestDto request, CancellationToken cancellationToken = default);
        Task<ServiceResult<AuthUserResponseDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
