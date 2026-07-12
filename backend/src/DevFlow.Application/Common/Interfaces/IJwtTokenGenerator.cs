using DevFlow.Application.Common.Models;

namespace DevFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(UserInfo user);
}