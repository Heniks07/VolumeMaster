namespace VolumeMasterService;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        if (args.Contains("--list-all"))
        {
            var audioApi = new AudioApi();
            var applications = audioApi.GetApplications();
            Console.WriteLine("Applications:\n" + string.Join("\n", applications));
        }

        var host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options => { options.ServiceName = "VolumeMasterService"; })
            .ConfigureServices(services => { services.AddHostedService<Worker>(); })
            .Build();
        host.Run();
    }
}