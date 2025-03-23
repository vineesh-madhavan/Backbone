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
    }
}