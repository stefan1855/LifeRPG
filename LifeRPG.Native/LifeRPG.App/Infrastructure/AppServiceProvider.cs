using Microsoft.Extensions.DependencyInjection;

namespace LifeRPG.App.Infrastructure;

public static class AppServiceProvider
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static void Configure()
    {
        var services = new ServiceCollection();
        services.AddLifeRpgServices();
        Services = services.BuildServiceProvider();
    }
}
