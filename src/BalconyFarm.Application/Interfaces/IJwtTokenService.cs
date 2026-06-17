namespace BalconyFarm.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string username);
}
