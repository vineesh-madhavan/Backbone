// Backbone.Core/Interfaces/IUnitOfWork.cs

using Backbone.Core.Entities;

namespace Backbone.Core.Interfaces
{

    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : BaseEntity;
        Task<int> CompleteAsync();
    }
}
