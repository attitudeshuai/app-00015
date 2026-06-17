using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Interfaces;
using BalconyFarm.Application.Models;
using BalconyFarm.Domain.Entities;
using BalconyFarm.Domain.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace BalconyFarm.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("用户注册请求: {Username}", dto.Username);

        if (await _unitOfWork.Users.ExistsAsync(u => u.Username == dto.Username, cancellationToken))
        {
            return ApiResponse<LoginResponseDto>.Error("用户名已存在", 400);
        }

        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == dto.Email, cancellationToken))
        {
            return ApiResponse<LoginResponseDto>.Error("邮箱已被注册", 400);
        }

        var user = dto.Adapt<User>();
        user.Id = Guid.NewGuid();
        user.PasswordHash = _passwordHashService.HashPassword(dto.Password);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(user.Id, user.Username);
        var userDto = user.Adapt<UserDto>();

        _logger.LogInformation("用户注册成功: {UserId}", user.Id);

        return ApiResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = token,
            User = userDto
        }, "注册成功");
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("用户登录请求: {UsernameOrEmail}", dto.UsernameOrEmail);

        var user = (await _unitOfWork.Users.FindAsync(
            u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail,
            cancellationToken)).FirstOrDefault();

        if (user == null)
        {
            return ApiResponse<LoginResponseDto>.Error("用户名或密码错误", 401);
        }

        if (!_passwordHashService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return ApiResponse<LoginResponseDto>.Error("用户名或密码错误", 401);
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Username);
        var userDto = user.Adapt<UserDto>();

        _logger.LogInformation("用户登录成功: {UserId}", user.Id);

        return ApiResponse<LoginResponseDto>.Success(new LoginResponseDto
        {
            Token = token,
            User = userDto
        }, "登录成功");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDto>.Error("用户不存在", 404);
        }

        var userDto = user.Adapt<UserDto>();
        return ApiResponse<UserDto>.Success(userDto);
    }

    public async Task<ApiResponse<UserDto>> UpdateCurrentUserAsync(Guid userId, UpdateUserRequestDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("更新用户信息: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserDto>.Error("用户不存在", 404);
        }

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
        {
            if (await _unitOfWork.Users.ExistsAsync(u => u.Username == dto.Username && u.Id != userId, cancellationToken))
            {
                return ApiResponse<UserDto>.Error("用户名已存在", 400);
            }
            user.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            if (await _unitOfWork.Users.ExistsAsync(u => u.Email == dto.Email && u.Id != userId, cancellationToken))
            {
                return ApiResponse<UserDto>.Error("邮箱已被使用", 400);
            }
            user.Email = dto.Email;
        }

        if (dto.Avatar != null)
        {
            user.Avatar = dto.Avatar;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("用户信息更新成功: {UserId}", userId);

        var userDto = user.Adapt<UserDto>();
        return ApiResponse<UserDto>.Success(userDto, "更新成功");
    }
}
