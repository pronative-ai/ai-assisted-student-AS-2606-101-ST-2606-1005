# Telemetry Contracts

## Metric Point Document

Persisted one per ingested `opencode.cost.usage` metric point.

| Field | Type | Description |
|-------|------|-------------|
| id | string | Deterministic ID from signal name + timestamp + value |
| documentType | string | Always `"metric_point"` |
| studentContextKey | string | Single-student deployment context key |
| signalName | string | Always `"opencode.cost.usage"` |
| metricType | string | Always `"Sum"` |
| aggregationTemporality | string | Always `"Cumulative"` |
| valueUsd | double | Cumulative counter value |
| observationTimestampUtc | string (ISO 8601) | When the measurement was taken |
| resourceAttributes | object | Resource-level attributes from the OTLP payload |
| scopeAttributes | object | Instrumentation scope attributes |
| ingestedAtUtc | string (ISO 8601) | When the record was persisted |
| schemaVersion | string | Schema version, currently `"1.0"` |

## Log Event Document

Persisted one per captured `api_request` or `api_error` log event.

| Field | Type | Description |
|-------|------|-------------|
| id | string | Deterministic ID from event name + timestamp + metadata |
| documentType | string | Always `"log_event"` |
| studentContextKey | string | Single-student deployment context key |
| eventName | string | `"api_request"` or `"api_error"` |
| timestampUtc | string (ISO 8601) | When the event occurred |
| severityText | string? | Severity text (e.g., `"error"`, `"info"`) |
| severityNumber | int? | Numeric severity per OTLP SeverityNumber enum |
| body | string? | Log record body |
| resourceAttributes | object | Resource-level attributes |
| ingestedAtUtc | string (ISO 8601) | When the record was persisted |
| schemaVersion | string | Schema version, currently `"1.0"` |
