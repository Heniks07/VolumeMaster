namespace VolumeMasterD;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .ConfigureServices((hostContext, services) => { services.AddHostedService<Worker>(); })
            .Build();

        await host.RunAsync();
    }
}