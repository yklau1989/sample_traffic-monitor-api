# API Reference

## POST /api/events

Ingest a new traffic event from the detection system. Idempotent - duplicate `eventId` returns 200 instead of 201.

### Request

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "Debris",
  "severity": "High",
  "cameraId": "cam-01",
  "occurredAt": "2026-04-18T12:00:00Z",
  "detections": [
    {
      "label": "tire",
      "confidence": 0.92,
      "boundingBox": { "x": 120, "y": 340, "width": 80, "height": 60 }
    }
  ]
}
```

### Response codes

| Status | Condition |
|--------|-----------|
| 201 Created | New event inserted. `Location` header set to `/api/events/{eventId}`. |
| 200 OK | Duplicate `eventId` - event already exists. `Location` header set. |
| 400 Bad Request | Model validation failure (missing required field, string too long, etc.). Body is RFC 7807 Problem Details with `errors` map. |
| 422 Unprocessable Entity | Domain rule violation (`eventId` is empty GUID, `occurredAt` is not UTC). Body is RFC 7807 Problem Details. |

### 201 Created response example

```json
{ "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

Header: `Location: /api/events/3fa85f64-5717-4562-b3fc-2c963f66afa6`

### 400 Bad Request response example

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "traceId": "...",
  "errors": {
    "cameraId": ["The CameraId field is required."]
  }
}
```
