#!/usr/bin/env bash
set -euo pipefail

# Emits 30 POST-ready event bodies to samples/events.json.
# occurredAt is rendered relative to the current clock so the domain
# invariant IngestedAt >= OccurredAt always holds.
# macOS `date -u -v-NM` — will need adjustment on Linux.

OUT="$(dirname "$0")/events.json"

EVENT_TYPES=(Debris StoppedVehicle Congestion Accident WrongWayDriver Pedestrian)
SEVERITIES=(Low Medium High Critical)
CAMERAS=(cam-101 cam-102 cam-103 cam-104 cam-105)
LABELS=(debris car car car car person)

COUNT=30

{
  echo "["
  for i in $(seq 1 "$COUNT"); do
    idx=$((i - 1))
    etype=${EVENT_TYPES[$((idx % 6))]}
    sev=${SEVERITIES[$((idx % 4))]}
    cam=${CAMERAS[$((idx % 5))]}
    label=${LABELS[$((idx % 6))]}

    # space events across ~24h, ending ~48min ago
    minutes=$(( (31 - i) * 48 ))
    occurredAt=$(date -u -v-${minutes}M +"%Y-%m-%dT%H:%M:%SZ")

    uuid=$(printf "b2222222-0000-0000-0000-%012d" "$i")
    conf=$(awk -v i="$i" 'BEGIN{c = 0.70 + (i * 0.01); if (c > 0.98) c = 0.98; printf "%.2f", c}')

    comma=","
    [ "$i" -eq "$COUNT" ] && comma=""

    cat <<EOF
  {
    "eventId": "$uuid",
    "eventType": "$etype",
    "severity": "$sev",
    "cameraId": "$cam",
    "occurredAt": "$occurredAt",
    "detections": [
      { "label": "$label", "confidence": $conf, "boundingBox": { "x": 200, "y": 380, "width": 180, "height": 130 } }
    ]
  }$comma
EOF
  done
  echo "]"
} > "$OUT"

echo "wrote $OUT"
