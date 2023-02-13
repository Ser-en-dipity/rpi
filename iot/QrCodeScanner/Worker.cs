using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ICNC.IOT.QrCodeScanner {
  public class Worker : IHostedService {
    private const string dev = "/dev/ttyACM0";
    private readonly ILogger<Worker> _logger;
    private readonly SerialPort _port;
    private readonly ScannerMessenger.ScannerMessengerClient _client;

    public Worker(ILogger<Worker> logger,
                  ScannerMessenger.ScannerMessengerClient client) {
      _logger = logger;
      _client = client;
      _port = new SerialPort { PortName = dev, BaudRate = 38400,
                               Parity = Parity.None, StopBits = StopBits.One,
                               DataBits = 8 };
    }

    public Task StartAsync(CancellationToken cancellationToken) {
      _port.DataReceived += OnScan;
      _port.ErrorReceived += OnError;
      _port.Open();
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      _port?.Close();
      return Task.CompletedTask;
    }
    private static readonly Regex _codePattern = new(@"^([^>]+)");
    private static readonly Regex _snPattern = new(@"([^>]+)$");
    private void OnError(object sender, SerialErrorReceivedEventArgs e) {
      SerialPort port = (SerialPort)sender;
      port?.Close();
    }
    private void OnScan(object sender, SerialDataReceivedEventArgs e) {
      SerialPort port = (SerialPort)sender;
      string data = port.ReadExisting();
      var codeMatch = _codePattern.Match(data);
      var snMatch = _snPattern.Match(data);
      _logger.LogInformation(
          "On Scan CODE:{@code} codeMatch is {@cm}, snMatch is {@sm}", data,
          codeMatch.Value, snMatch.Value);
      if (codeMatch.Success && snMatch.Success) {
        if (string.IsNullOrWhiteSpace(codeMatch.Value) ||
            string.IsNullOrWhiteSpace(snMatch.Value))
          return;

        _logger.LogInformation("On Scan CODE:{@code} SN:{@sn}", codeMatch.Value,
                               snMatch.Value);
        try {
          _client.OnScanZhaosQrCode(new ScannerMessage() {
            Code = $"{codeMatch.Value}>>{snMatch.Success}"
          });
        } catch (Grpc.Core.RpcException ex) {
          _logger.LogWarning(default, ex, "grpc call failed");
        }
      }
    }
  }
}
