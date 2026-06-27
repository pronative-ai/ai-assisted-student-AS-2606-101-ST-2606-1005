using System.ComponentModel.DataAnnotations;

namespace Opencode.Telemetry.Contracts;

public class CostAggregationRequest
{
    [Required]
    public DateTime? Start { get; set; }

    [Required]
    public DateTime? End { get; set; }
}
