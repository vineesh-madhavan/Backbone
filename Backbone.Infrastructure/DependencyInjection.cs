// Infrastructure/DependencyInjection.cs
//using Backbone.Core.Interfaces;
//using Backbone.Infrastructure.Data;
//using Backbone.Infrastructure.Persistence;
////using Backbone.Infrastructure.Interceptors;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Serilog;
//using Microsoft.EntityFrameworkCore.Diagnostics;
//using Backbone.Infrastructure.Interceptors;
//using Backbone.Infrastructure.Services;
//using Backbone.Core.Interfaces.Data.Repositories;

//namespace Infrastructure
//{
//    public static class DependencyInjection
//    {
//        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
//        {
//            var connectionString = configuration.GetConnectionString("DefaultConnection");

//            services.AddDbContext<ApplicationDbContext>( options =>
//                options.UseNpgsql(connectionString,
//                npgsqlOptions =>
//                {
//                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
//                    npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
//                }));

//            //// MediatR v11 registration (using assembly scanning)
//            //services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

//            //services.AddDbContext<ApplicationDbContext>(options =>
//            //    options.UseNpgsql(connectionString)
//            //    .LogTo(message => Log.Logger.Information(message), LogLevel.Information));

//            //services.AddDbContext<ApplicationDbContext>((sp, options) =>
//            //{
//            //    options.UseNpgsql(connectionString)
//            //           .AddInterceptors(sp.GetRequiredService<SerilogDbContextInterceptor>());
//            //});

//            services.AddDbContext<ApplicationDbContext>((sp, options) =>
//            {
//                options.UseNpgsql(connectionString)
//                       .EnableDetailedErrors()
//                       .EnableSensitiveDataLogging()
//                       .AddInterceptors(
//                           sp.GetRequiredService<MasterSaveChangesInterceptor>(),
//                           sp.GetRequiredService<SerilogDbContextInterceptor>());
//            });

//            services.AddHttpContextAccessor();
//            services.AddScoped<ICurrentUserService, CurrentUserService>();

//            // Repository and UoW registration
//            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
//            services.AddScoped<IUnitOfWork, UnitOfWork>();
//            services.AddScoped<SerilogDbContextInterceptor>();
//            services.AddSingleton<TimeProvider>(TimeProvider.System);
//            services.AddScoped<ICurrentUserService, CurrentUserService>();
//            services.AddScoped<MasterSaveChangesInterceptor>();



//            return services;
//        }
//    }
//}

using Backbone.Core.Interfaces;
using Backbone.Core.Interfaces.Data.Repositories;
using Backbone.Infrastructure.Data;
using Backbone.Infrastructure.Interceptors;
using Backbone.Infrastructure.Persistence;
using Backbone.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        private class LoggerCategory { } // Helper class for logger resolution

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            try
            {
                var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger<LoggerCategory>();
                logger?.LogInformation("Starting infrastructure services registration");

                RegisterDbContext(services, configuration, logger);
                RegisterCoreServices(services, logger);
                RegisterInterceptors(services, logger);

                logger?.LogInformation("Completed infrastructure services registration");
                return services;
            }
            catch (Exception ex)
            {
                // Fallback logging if DI isn't fully set up yet
                Console.WriteLine($"Failed to register infrastructure services: {ex}");
                throw new InfrastructureConfigurationException("Failed to configure infrastructure services", ex);
            }
        }

        private static void RegisterDbContext(IServiceCollection services, IConfiguration configuration, ILogger<LoggerCategory> logger)
        {
            try
            {
                logger?.LogDebug("Configuring database context");

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is missing");
                }

                services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    try
                    {
                        var env = sp.GetRequiredService<IWebHostEnvironment>();
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorCodesToAdd: null);
                            npgsqlOptions.CommandTimeout(30); // 30 seconds timeout
                        })
                        .EnableDetailedErrors(env.IsDevelopment())
                        .EnableSensitiveDataLogging(env.IsDevelopment())
                        .AddInterceptors(
                            sp.GetRequiredService<MasterSaveChangesInterceptor>(),
                            sp.GetRequiredService<SerilogDbContextInterceptor>());

                        logger?.LogInformation("Configured ApplicationDbContext with connection string: {ConnectionString}",
                            MaskConnectionString(connectionString));
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to configure DbContext options");
                        throw;
                    }
                });

                logger?.LogDebug("DbContext registration completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register DbContext");
                throw;
            }
        }

        private static void RegisterCoreServices(IServiceCollection services, ILogger<LoggerCategory> logger)
        {
            try
            {
                logger?.LogDebug("Registering core services");

                services.AddHttpContextAccessor();
                services.AddScoped<ICurrentUserService, CurrentUserService>();
                services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
                services.AddScoped<IUnitOfWork, UnitOfWork>();
                services.AddSingleton<TimeProvider>(TimeProvider.System);

                logger?.LogInformation("Registered core services: ICurrentUserService, IRepository<T>, IUnitOfWork");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register core services");
                throw;
            }
        }

        private static void RegisterInterceptors(IServiceCollection services, ILogger<LoggerCategory> logger)
        {
            try
            {
                logger?.LogDebug("Registering interceptors");

                services.AddScoped<SerilogDbContextInterceptor>();
                services.AddScoped<MasterSaveChangesInterceptor>();

                logger?.LogInformation("Registered interceptors: SerilogDbContextInterceptor, MasterSaveChangesInterceptor");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register interceptors");
                throw;
            }
        }

        private static string MaskConnectionString(string connectionString)
        {
            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "*****";
                }
                return builder.ToString();
            }
            catch
            {
                return "Invalid connection string format";
            }
        }
    }

    public class InfrastructureConfigurationException : Exception
    {
        public InfrastructureConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}