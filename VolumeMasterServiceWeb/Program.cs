using System.Text;
using Newtonsoft.Json;

namespace VolumeMasterServiceWeb;

public class Program
{
    public static async Task Main(string[] args)
    {
        //Task.Run(() => WorkerMain(args));

        await WebMain(args);
        //await WorkerMain(args);
    }


    private static async Task WebMain(string[] args)
    {
        if (args.Contains("--list-all"))
        {
            var audioApi = new AudioApi();
            var applications = audioApi.GetApplications();
            Console.WriteLine("Applications:\n" + string.Join("\n", applications));
        }

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<Worker>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(5195); });


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
        app.UseSwagger();
        app.UseSwaggerUI();
        // }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        var worker = app.Services.GetRequiredService<Worker>();


        app.MapPost("/PlayPause", MultiMediaApi.Pause);
        app.MapPost("/Next", MultiMediaApi.Next);
        app.MapPost("/Previous", MultiMediaApi.Previous);
        app.MapPost("/Stop", MultiMediaApi.Stop);
        app.MapPost("/SetVolume", (int sliderIndex, int volume) => { worker.SetVolume(sliderIndex, volume); });
        app.MapGet("/GetAllVolumes", () =>
        {
            SseData data = new()
            {
                Volume = worker.LastVolume,
                ActualVolume = worker.LastActualVolume,
                OverrideActive = worker.LastOverrideActive
            };
            return JsonConvert.SerializeObject(data);
        });
        app.MapGet("/GetApplications", () => worker.AudioApi.GetApplications());
        app.MapGet("/GetConfig", () => worker.VolumeMasterCom.Config);
        app.MapGet("/volumeUpdate", async context =>
        {
            context.Response.ContentType = "text/event-stream";

            var completionSource = new TaskCompletionSource<bool>();

            // Subscribe to the VolumeChanged event
            worker.VolumeChanged += OnWorkerOnVolumeChanged;

            try
            {
                // Keep the connection open until the client disconnects
                await completionSource.Task;
            }
            finally
            {
                // Unsubscribe from the VolumeChanged event when the client disconnects
                worker.VolumeChanged -= OnWorkerOnVolumeChanged;
            }

            return;

            void OnWorkerOnVolumeChanged(object? sender, VolumeChangedEventArgs volumeChangedEventArgs)
            {
                // Serialize the event arguments to JSON and send them to the client
                try
                {
                    SseData data = new()
                    {
                        Volume = volumeChangedEventArgs.Volume,
                        ActualVolume = volumeChangedEventArgs.ActualVolume,
                        OverrideActive = volumeChangedEventArgs.OverrideActive
                    };

                    var jsonData = JsonConvert.SerializeObject(data);
                    var payload = $"{jsonData}\n";
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e);
                }
            }
        });

        _ = Task.Run(async () => await worker.StartAsync(new CancellationToken()));

        await app.RunAsync();
    }

    class SseData
    {
        public List<int>? Volume { get; set; }
        public List<int>? ActualVolume { get; set; }
        public List<bool>? OverrideActive { get; set; }
    }

    // private static async Task WorkerMain(string[] args)
    // {
    //     var hostBuilder = Host.CreateApplicationBuilder(args);
    //     hostBuilder.Services.AddHostedService<Worker>();
    //
    //     if (args.Contains("--list-all"))
    //     {
    //         var audioApi = new AudioApi();
    //         var applications = audioApi.GetApplications();
    //         Console.WriteLine("Applications:\n" + string.Join("\n", applications));
    //     }
    //
    //     var host = Host.CreateDefaultBuilder(args)
    //         .UseWindowsService(options => { options.ServiceName = "VolumeMasterService"; })
    //         .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    //         .Build();
    //     await host.RunAsync();
    // }
}