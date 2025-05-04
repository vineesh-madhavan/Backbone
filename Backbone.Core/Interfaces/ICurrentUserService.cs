// Backbone.Core/Interfaces/ICurrentUserService.cs
namespace Backbone.Core.Interfaces
{
    public interface ICurrentUserService
    {
        string? Username { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
    }
}