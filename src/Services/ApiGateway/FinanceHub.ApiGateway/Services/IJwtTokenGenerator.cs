namespace FinanceHub.ApiGateway.Services;

public interface IJwtTokenGenerator
{
    string GenerateDevToken(string userId);
}
