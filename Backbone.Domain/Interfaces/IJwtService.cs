namespace Backbone.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(string username);
}
