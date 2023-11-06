var services = ConfigureServices();

var serviceProvider = services.BuildServiceProvider();

await serviceProvider.GetService<App>().Run(args);

IServiceCollection ConfigureServices()
{
    var services = new ServiceCollection();

    services.AddTransient<App>();

    return services;
}