namespace Backbone.Core.Interfaces;

public interface IJwtService
{
    string GenerateToken(string username);
}
