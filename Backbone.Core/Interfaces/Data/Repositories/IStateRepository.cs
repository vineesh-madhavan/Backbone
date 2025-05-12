//Backbone.Core.Interfaces.Data.Repositories/IStateRepository.cs
using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces.Data.Repositories
{
    public interface IStateRepository : IRepository<State>
    {
        Task<State?> GetByNameAsync(string stateName, CancellationToken cancellationToken = default);
    }
}
