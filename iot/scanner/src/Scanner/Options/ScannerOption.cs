namespace ICNC.IOT.Options {
  public record ScannerOption {
    public const string SCANNER = "Scanner";
    public int VenderId { get; init; }
    public int ProductId { get; init; }
    public int BaudRate { get; init; }
  }
}