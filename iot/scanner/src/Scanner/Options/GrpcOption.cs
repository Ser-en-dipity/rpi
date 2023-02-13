namespace ICNC.IOT.Options {
  public record GrpcOption {
    public const string GRPC = "Grpc";
    public string Endpoint { get; init; }
    public bool SSL { get; init; }
  }
}