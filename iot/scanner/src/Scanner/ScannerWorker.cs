using System;
using System.IO.Ports;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ICNC.IOT.Scanner {
    public class ScannerWorker : IHostedService {
        private const string dev = "/dev/ttyACM0";
        private readonly ILogger<ScannerWorker> _logger;
        private readonly SerialPort _port;
        private readonly ScannerMessenger.ScannerMessengerClient _client;

        public ScannerWorker(ILogger<ScannerWorker> logger,
                             ScannerMessenger.ScannerMessengerClient client)
        {
            _logger = logger;
            _client = client;
            _port = new SerialPort
            {
                PortName = dev,
                BaudRate = 38400,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _port.DataReceived += OnScan;
            _port.ErrorReceived += OnError;
            _port.Open();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _port?.Close();
            return Task.CompletedTask;
        }

        private Regex MachinePattern { get; init; } = new("(?<=MACHINE )([A-Z]\\d{3})");
        private Regex StaffPattern { get; init; } = new("(?<=STAFF )(\\.*)");
        private static HttpClientHandler HttpClientHandler => new() { AllowAutoRedirect = false };
        private static HttpClient Client => new(HttpClientHandler);
        private void OnError(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            port?.Close();
        }

        private void OnScan(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;
            string data = serialPort.ReadExisting();

            var machineMatch = MachinePattern.Match(data);
            ///< summary>
            /// match url
            ///</summary>
            bool isUrl = Uri.IsWellFormedUriString(data, UriKind.Absolute);

            if (isUrl)
            {
                var uri = new Uri(data);
                var resp = Client.GetAsync(uri.AbsoluteUri).Result;
                var headers = resp.Headers;
                if (headers is not null)
                {
                    if (headers.Location is not null)
                    {
                        var queryDict =
                            Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                                headers.Location.Query);
                        if (queryDict.ContainsKey("id"))
                        {
                            var idStr = queryDict["id"];
                            if (Guid.TryParse(idStr, out var id))
                            {
                                _client.OnScanManufactureOrderQrCode(
                                    new ScannerMessage { Code = id.ToString() });
                                return;
                            }
                        }
                    }
                }
            } else if (machineMatch.Success)
            {
                _client.OnScanMachine(new ScannerMessage { Code = machineMatch.Value });
                _logger.LogInformation($"on scan {machineMatch.Value}");
            } else
            {
                var staffMatch = StaffPattern.Match(data);
                if (staffMatch.Success)
                {
                    _client.OnScanStaff(new ScannerMessage { Code = staffMatch.Value });
                }
            }
        }
    }

}