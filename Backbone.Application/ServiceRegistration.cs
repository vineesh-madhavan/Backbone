//Backbone.Application/ServiceRegistration.cs
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;

namespace Backbone.Application
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(typeof(ServiceRegistration).Assembly);
            return services;
        }

        //public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        //{
        //    // This will register all handlers in the Application assembly
        //    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));
        //    return services;
        //}
    }
}