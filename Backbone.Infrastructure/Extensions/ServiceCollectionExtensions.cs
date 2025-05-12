//Backbonme.Infrastructure.Extensions.ServiceCollectionExtensions.cs
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Backbone.Infrastructure.Extensions
{
    public static class RepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            //services.AddScoped<IUserDetailRepository, UserDetailRepository>();
            //services.AddScoped<IUserAddressRepository, UserAddressRepository>();
            //services.AddScoped<IDistrictRepository, DistrictRepository>();
            //services.AddScoped<IStateRepository, StateRepository>();
            //services.AddScoped<IUserRoleMappingRepository, UserRoleMappingRepository>();

            return services;
        }
    }
}
