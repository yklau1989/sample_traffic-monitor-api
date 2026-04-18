using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Infrastructure.Persistence;
using TrafficMonitor.Infrastructure.Repositories;

namespace TrafficMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TrafficMonitorDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Postgres"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ITrafficEventRepository, TrafficEventRepository>();

        return services;
    }
}
