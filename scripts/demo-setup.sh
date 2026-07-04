#!/usr/bin/env bash
set -euo pipefail

API="http://localhost:5075"
TOKEN=$(curl -s -X POST "$API/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@enhancementhub.dev","password":"password123"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

AUTH="Authorization: Bearer $TOKEN"
APP_ID="33333333-3333-3333-3333-333333333333"
CONN_ID="55555555-5555-5555-5555-555555555555"
REPO_ID="44444444-4444-4444-4444-444444444444"

echo "Triggering database schema scan..."
curl -s -X POST "$API/api/database-connections/$CONN_ID/scan" -H "$AUTH" -H "Content-Type: application/json" | head -c 200
echo

echo "Indexing repository..."
curl -s -X POST "$API/api/repositories/$REPO_ID/index" -H "$AUTH" -H "Content-Type: application/json" | head -c 200
echo

echo "Building system graph..."
curl -s -X POST "$API/api/system-map/$APP_ID/build" -H "$AUTH" -H "Content-Type: application/json" | head -c 200
echo

echo "Demo data ready."
