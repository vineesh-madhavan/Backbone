//Backbone.Infrastructure/Data/Repositories/UserRepository.cs
using Backbone.Core.Entities;
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backbone.Core.Specifications;
using Backbone.Core.Interfaces;
using Core.Secutiy;

namespace Backbone.Infrastructure.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();


            return await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetUserByUsernameAsync(string userName, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();


            return await query.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => !u.IsDeleted) // Include soft delete filter if needed
                .ToListAsync(cancellationToken);
        }

        public async Task<User?> GetUserWithRolesAsync(int userId, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();


            query = query
                .Include(u => u.UserRoleMappings)
                    .ThenInclude(urm => urm.Role);


            return await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetUserWithDetailsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();


                query = query
                    .Include(u => u.Status)
                    .Include(u => u.UserDetails)
                    .Include(u => u.UserAddresses)
                    .Include(u => u.UserRoleMappings)
                        .ThenInclude(urm => urm.Role);
            

            return await query.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .Where(u => u.UserName == username);

            if (includeDetails)
            {
                query = query
                    .Include(u => u.Status)
                    .Include(u => u.UserDetails)
                    .Include(u => u.UserAddresses)
                    .Include(u => u.UserRoleMappings)
                        .ThenInclude(urm => urm.Role);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.UserName == username, cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            user.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                // Perform dummy hash to prevent timing attacks
                using (var hmac = new HMACSHA512())
                {
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
                return false;
            }

            return user.PasswordHash.VerifyPasswordHash(user.PasswordSalt, password);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.UserId == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetFirstOrDefaultAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> ListAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            ISpecification<User> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec)
                .CountAsync(cancellationToken);
        }

        private IQueryable<User> ApplySpecification(ISpecification<User> spec)
        {
            return SpecificationEvaluator<User>.GetQuery(
                _context.Users.AsQueryable(),
                spec);
        }
    }
}
