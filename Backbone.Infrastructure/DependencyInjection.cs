// Infrastructure/DependencyInjection.cs
using Backbone.Core.Interfaces;
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Persistence;
//using Backbone.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Backbone.Infrastructure.Interceptors;
using Backbone.Infrastructure.Services;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>( options =>
                options.UseNpgsql(connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));

            //// MediatR v11 registration (using assembly scanning)
            //services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseNpgsql(connectionString)
            //    .LogTo(message => Log.Logger.Information(message), LogLevel.Information));

            //services.AddDbContext<ApplicationDbContext>((sp, options) =>
            //{
            //    options.UseNpgsql(connectionString)
            //           .AddInterceptors(sp.GetRequiredService<SerilogDbContextInterceptor>());
            //});

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(connectionString)
                       .EnableDetailedErrors()
                       .EnableSensitiveDataLogging()
                       .AddInterceptors(
                           sp.GetRequiredService<MasterSaveChangesInterceptor>(),
                           sp.GetRequiredService<SerilogDbContextInterceptor>());
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Repository and UoW registration
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<SerilogDbContextInterceptor>();
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<MasterSaveChangesInterceptor>();



            return services;
        }
    }
}