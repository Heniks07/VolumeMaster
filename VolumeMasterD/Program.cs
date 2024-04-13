namespace VolumeMasterD;

public class Program
{
    public static void Main(string[] args)
    {
        //var builder = Host.CreateApplicationBuilder(args);
        //builder.Services.AddHostedService<Worker>();
        //var host = builder.Build();

        var host = Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .ConfigureServices((hostContext, services) => { services.AddHostedService<Worker>(); })
            .Build();

        host.RunAsync().Wait();
    }
}