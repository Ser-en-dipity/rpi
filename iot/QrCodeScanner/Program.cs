using System;
using System.IO;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ICNC.IOT.QrCodeScanner {
  public class Program {
    private static string Env =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private static IConfiguration Configuration =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Env}.json", true)
            .Build();

    public static void Main(string[] args) {
      Log.Logger = new LoggerConfiguration()
                       .ReadFrom.Configuration(Configuration)
                       .CreateLogger();
      CreateHostBuilder(args).Build().Run();
      Log.CloseAndFlush();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, builder) => {
              builder.AddJsonFile("appsettings.json", false, true);
              builder.AddJsonFile($"appsettings.{Env}.json", true, false);
            })
            .ConfigureServices((hostContext, services) => {
              IConfiguration configuration = hostContext.Configuration;
              GrpcOption grpcOption =
                  configuration.GetSection(GrpcOption.GRPC).Get<GrpcOption>();

              services
                  .AddGrpcClient<ScannerMessenger.ScannerMessengerClient>(
                      opt => {
                        opt.Address = new System.Uri(grpcOption.Endpoint);
                      })
                  .ConfigureChannel(copt => {
                    if (!grpcOption.SSL) {
                      copt.Credentials = ChannelCredentials.Insecure;
                    }
                  });
              services.AddHostedService<Worker>();
            })
            .UseSerilog();
  }
}
