# Cost Aggregation API

## Endpoint

`GET /api/opencode/cost-usage?start={iso8601}&end={iso8601}`

## Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| start | string (ISO 8601) | Yes | Start of the time window (UTC) |
| end | string (ISO 8601) | Yes | End of the time window (UTC) |

### Validation Rules

- Both `start` and `end` are required
- `start` must be earlier than `end`
- Invalid requests return `400 Bad Request`

## Response

```json
{
  "startUtc": "2026-01-01T10:00:00Z",
  "endUtc": "2026-01-01T11:00:00Z",
  "usageUsd": 3.50,
  "currency": "USD",
  "metricName": "opencode.cost.usage",
  "aggregationMethod": "window_delta_from_cumulative_counter",
  "pointCountConsidered": 2
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| startUtc | string (ISO 8601) | Requested start time |
| endUtc | string (ISO 8601) | Requested end time |
| usageUsd | double | Computed cost usage in USD |
| currency | string | Always `"USD"` |
| metricName | string | Always `"opencode.cost.usage"` |
| aggregationMethod | string | Always `"window_delta_from_cumulative_counter"` |
| pointCountConsidered | int | Number of deduplicated points used in the calculation |

### Aggregation Semantics

- Usage is computed as the delta between the last and first cumulative counter value within the window
- Fewer than 2 points returns `usageUsd: 0`
- Counter resets (negative delta) return `usageUsd: 0`
- Duplicate points (same timestamp + same value + same fingerprint) are deduplicated
- The endpoint is read-only and does not mutate any state
