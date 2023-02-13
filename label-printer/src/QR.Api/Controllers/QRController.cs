using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;

namespace QR.Api.Controllers {
  [ApiController]
  [Route("[controller]")]
  public class QRController : ControllerBase {
    private readonly ILogger<QRController> _logger;
    private readonly MinioOptions _minioOptions;
    private MinioClient Client =>
        new MinioClient(_minioOptions.EndPoint, _minioOptions.AccessKey,
                        _minioOptions.SecretKey)
            .WithSSL();

    public QRController(ILogger<QRController> logger,
                        IOptions<MinioOptions> minioOptions) {
      _logger = logger;
      _minioOptions = minioOptions.Value;
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Print([FromBody] PrintCmd cmd) {
      if (!await Client.BucketExistsAsync(cmd.Bucket)) {
        return BadRequest("no bucket !");
      }
      string tmpFileName = $"/tmp/{cmd.FileName}";

      using var fs =
          new FileStream(tmpFileName, FileMode.Create, FileAccess.ReadWrite);
      await Client.GetObjectAsync(cmd.Bucket, cmd.FileName,
                                  (s) => { s.CopyTo(fs); });
      fs.Close();
      using var process = new System.Diagnostics.Process() {
        StartInfo =
            new System.Diagnostics.ProcessStartInfo() {
              FileName = "lp", Arguments = $"-n {cmd.Times} {tmpFileName}",
              UseShellExecute = false, RedirectStandardError = true,
              RedirectStandardInput = true, RedirectStandardOutput = true
            }
      };
      process.Start();
      process.WaitForExit(4200);
      while (!process.StandardOutput.EndOfStream)
        _logger.LogInformation(process.StandardOutput.ReadLine());
      while (!process.StandardError.EndOfStream)
        _logger.LogError(process.StandardError.ReadLine());

      return Ok();
    }
  }
}
