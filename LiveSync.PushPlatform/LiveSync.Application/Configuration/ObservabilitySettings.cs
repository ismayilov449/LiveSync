namespace LiveSync.Application.Configuration;

public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    public bool EnablePrometheus { get; set; } = true;

    public OtlpSettings Otlp { get; set; } = new();

    public sealed class OtlpSettings
    {
        public string? Endpoint { get; set; }
    }
}
