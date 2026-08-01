namespace AtasApi.Middleware;

public interface IJwtService
{
    string GenerateToken(int userId, string username, int alaId, string role);
}