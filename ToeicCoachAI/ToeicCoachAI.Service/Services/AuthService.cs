using ToeicCoachAI.Model.Common;
using ToeicCoachAI.Model.DTOs.Auth;
using ToeicCoachAI.Model.Entities;
using ToeicCoachAI.Model.Enums;
using ToeicCoachAI.Repository.Interfaces;
using ToeicCoachAI.Service.Interfaces;
using ToeicCoachAI.Service.Security;

namespace ToeicCoachAI.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<AuthUserResponseDto>> RegisterAsync(
            RegisterRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var existingUser = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existingUser != null)
            {
                return ServiceResult<AuthUserResponseDto>.Failure(
                    "Email này đã được đăng ký. Vui lòng đăng nhập hoặc sử dụng Email khác.",
                    "EMAIL_ALREADY_EXISTS");
            }

            var now = DateTimeOffset.UtcNow;
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),
                PasswordHash = PasswordHasherHelper.HashPassword(request.Password),
                Role = UserRole.USER,
                Tier = UserTier.FREE,
                CreditBalance = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Users.AddAsync(newUser, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // UC-02: Sync Guest Placement Test Session if anonymous token exists
                if (!string.IsNullOrWhiteSpace(request.AnonymousGuestToken))
                {
                    await _unitOfWork.Users.SyncGuestSessionToUserAsync(
                        request.AnonymousGuestToken,
                        newUser.Id,
                        cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ServiceResult<AuthUserResponseDto>.Success(
                    MapToDto(newUser),
                    "Đăng ký tài khoản thành công.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ServiceResult<AuthUserResponseDto>> LoginAsync(
            LoginRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);

            if (user == null || !PasswordHasherHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                return ServiceResult<AuthUserResponseDto>.Failure(
                    "Email hoặc mật khẩu không chính xác.",
                    "INVALID_CREDENTIALS");
            }

            if (!string.IsNullOrWhiteSpace(request.AnonymousGuestToken))
            {
                var synced = await _unitOfWork.Users.SyncGuestSessionToUserAsync(
                    request.AnonymousGuestToken,
                    user.Id,
                    cancellationToken);

                if (synced)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            return ServiceResult<AuthUserResponseDto>.Success(
                MapToDto(user),
                "Đăng nhập thành công.");
        }

        public async Task<ServiceResult<AuthUserResponseDto>> AuthenticateGoogleAsync(
            GoogleAuthRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return ServiceResult<AuthUserResponseDto>.Failure(
                    "Không lấy được thông tin Email từ tài khoản Google.",
                    "GOOGLE_EMAIL_MISSING");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail, cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                if (user == null)
                {
                    var now = DateTimeOffset.UtcNow;
                    var displayName = string.IsNullOrWhiteSpace(request.FullName)
                        ? normalizedEmail.Split('@')[0]
                        : request.FullName.Trim();

                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = normalizedEmail,
                        FullName = displayName,
                        // Random unguessable password hash for Google-created account
                        PasswordHash = PasswordHasherHelper.HashPassword(Guid.NewGuid().ToString("N")),
                        Role = UserRole.USER,
                        Tier = UserTier.FREE,
                        CreditBalance = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _unitOfWork.Users.AddAsync(user, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // UC-02: Sync Guest Placement Test Session if present
                if (!string.IsNullOrWhiteSpace(request.AnonymousGuestToken))
                {
                    await _unitOfWork.Users.SyncGuestSessionToUserAsync(
                        request.AnonymousGuestToken,
                        user.Id,
                        cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ServiceResult<AuthUserResponseDto>.Success(
                    MapToDto(user),
                    "Đăng nhập bằng Google thành công.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ServiceResult<AuthUserResponseDto>> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<AuthUserResponseDto>.Failure(
                    "Không tìm thấy người dùng.",
                    "USER_NOT_FOUND");
            }

            return ServiceResult<AuthUserResponseDto>.Success(MapToDto(user));
        }

        private static AuthUserResponseDto MapToDto(User user)
        {
            return new AuthUserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Tier = user.Tier,
                CreditBalance = user.CreditBalance,
                TargetScoreLr = user.TargetScoreLr,
                TargetScoreW = user.TargetScoreW
            };
        }
    }
}
