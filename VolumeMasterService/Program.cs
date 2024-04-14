namespace VolumeMasterService;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options => { options.ServiceName = "VolumeMasterService"; })
            .ConfigureServices(services => { services.AddHostedService<Worker>(); })
            .Build();
        host.Run();
    }
}