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

## GET /api/events

List traffic events in a paged envelope. Results default to `sort=occurredAt`, `page=1`, `pageSize=50`.

### Query parameters

| Name | Type | Notes |
|------|------|-------|
| `eventType` | string | Optional enum filter: `Debris`, `StoppedVehicle`, `Congestion`, `Accident`, `WrongWayDriver`, `Pedestrian`. |
| `severity` | string | Optional enum filter: `Low`, `Medium`, `High`, `Critical`. |
| `from` | datetime | Optional inclusive UTC lower bound. Non-UTC values return 400. |
| `to` | datetime | Optional inclusive UTC upper bound. Non-UTC values return 400. |
| `cameraId` | string | Optional exact camera filter. |
| `sort` | string | `occurredAt` or `-occurredAt`. Unknown fields return 400. |
| `page` | integer | Optional page number, clamped to at least `1`. |
| `pageSize` | integer | Optional page size, clamped to `1..200`. |

### Response envelope

```json
{
  "items": [
    {
      "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "eventType": "Debris",
      "severity": "High",
      "cameraId": "cam-01",
      "occurredAt": "2026-04-18T12:00:00Z",
      "ingestedAt": "2026-04-18T12:00:05Z",
      "detectionSummary": "1 detection: tire 0.92"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 50
}
```

### Response codes

| Status | Condition |
|--------|-----------|
| 200 OK | Request accepted and page returned. |
| 400 Bad Request | Invalid `sort`, non-UTC `from`/`to`, or query binding/model validation failure. |

Example: `GET /api/events?severity=high&from=2026-04-18T00:00:00Z&sort=-occurredAt&page=1&pageSize=25`
