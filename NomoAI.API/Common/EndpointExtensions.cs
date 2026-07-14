using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace NomoAI.API.Common
{
    public static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            var endpointTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(t))
                .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

            services.TryAddEnumerable(endpointTypes);
            return services;
        }

        public static IApplicationBuilder MapEndpoints(this WebApplication app)
        {
            var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();
            foreach (var endpoint in endpoints)
            {
                endpoint.MapEndpoint(app);
            }
            return app;
        }
    }
}
