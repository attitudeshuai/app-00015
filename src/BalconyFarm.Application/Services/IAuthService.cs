using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;

namespace BalconyFarm.Application.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateCurrentUserAsync(Guid userId, UpdateUserRequestDto dto, CancellationToken cancellationToken = default);
}
