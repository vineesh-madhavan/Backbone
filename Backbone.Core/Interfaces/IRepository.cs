// Backbone.Core/Interfaces/IRepository.cs
using Backbone.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Backbone.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

        // Specification pattern methods
        Task<T?> GetFirstOrDefaultAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<T>> ListAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            ISpecification<T> spec,
            CancellationToken cancellationToken = default);
    }
}