// Backbone.Core/Interfaces/ICurrentUserService.cs
namespace Backbone.Core.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string? Username { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
    }
}