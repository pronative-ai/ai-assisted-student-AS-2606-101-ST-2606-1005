namespace Opencode.Telemetry.Contracts;

public class LogEventDocumentDto
{
    public string EventName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? SeverityText { get; set; }
    public int? SeverityNumber { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
}
