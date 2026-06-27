namespace Opencode.Telemetry.Domain;

public class LogEventDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "log_event";
    public string StudentContextKey { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? SeverityText { get; set; }
    public int? SeverityNumber { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
    public Dictionary<string, string> ScopeAttributes { get; set; } = new();
    public string? SourceTraceId { get; set; }
    public string? SourceSpanId { get; set; }
    public DateTime IngestedAtUtc { get; set; }
    public string SchemaVersion { get; set; } = "1.0";
    public string RawIdentityFingerprint { get; set; } = string.Empty;
}
