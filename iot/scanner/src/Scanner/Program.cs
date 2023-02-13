using Grpc.Core;
using ICNC.IOT.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ICNC.IOT.Scanner {
    class Program {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostCtx, services) => {
                    IConfiguration configuration = hostCtx.Configuration;

                    GrpcOption grpcOption =
                    configuration.GetSection(GrpcOption.GRPC).Get<GrpcOption>();

                    services
                        .AddGrpcClient<ScannerMessenger.ScannerMessengerClient>(
                            opt => {
                                opt.Address = new System.Uri(grpcOption.Endpoint);
                            })
                        .ConfigureChannel(copt => {
                            if (!grpcOption.SSL)
                            {
                                copt.Credentials = ChannelCredentials.Insecure;
                            }
                        });
                    services.AddHostedService<ScannerWorker>();
                })
                .UseWindowsService();
    }
}
