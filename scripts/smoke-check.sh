#!/usr/bin/env bash
set -euo pipefail

API_URL="${API_URL:-http://localhost:5075}"
WEB_URL="${WEB_URL:-http://localhost:5001}"
WORKER_URL="${WORKER_URL:-http://localhost:5076}"

echo "Checking EnhancementHub health endpoints..."

check() {
  local name="$1"
  local url="$2"
  local path="$3"
  echo -n "  $name $path ... "
  if curl -sf "$url$path" > /dev/null; then
    echo "OK"
  else
    echo "FAILED"
    exit 1
  fi
}

check "API" "$API_URL" "/health"
check "API" "$API_URL" "/health/ready"
check "Web" "$WEB_URL" "/health"
check "Web" "$WEB_URL" "/health/ready"
check "Worker" "$WORKER_URL" "/health"
check "Worker" "$WORKER_URL" "/health/ready"

echo "Smoke checks passed."
