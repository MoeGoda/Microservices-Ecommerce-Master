#!/usr/bin/env bash
# F4 — the "end-to-end smoke test" the roadmap names, meant to run against
# `docker compose up -d` (every service reachable at the exact same ports
# the manual multi-terminal workflow already used). Exercises one real
# cross-service flow end to end: register -> login -> create an item ->
# a bad-registration request (proves F3's Accept-Language localization
# still works through the gateway) -> a POS sale -> checkout -> Warehouse
# stock actually decrements -> Reporting's read model picks the sale up via
# the outbox/event pipeline (C3/D1), not a direct call. Also re-checks
# F1's health checks and F2's security headers/rate limiting, since a
# compose stack is exactly the kind of change that could silently break
# any of those without a human noticing. The rate-limiting check runs LAST
# on purpose — it deliberately exhausts /register's 30-second budget, and
# every earlier check that also calls /register needs that budget intact.
#
# grep -o/cut calls below don't use `set -e` because "no match" is itself
# a meaningful, checked outcome here (an empty field fails its own
# check() call with a clear message) — aborting the whole script on the
# first parse miss would hide every check after it.
set -uo pipefail

GATEWAY="${GATEWAY:-http://localhost:5058}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@12345}"

ok=0
fail=0
check() {
  local desc="$1" cond="$2"
  if [ "$cond" = "1" ]; then
    echo "  [PASS] $desc"
    ok=$((ok + 1))
  else
    echo "  [FAIL] $desc"
    fail=$((fail + 1))
  fi
}

echo "=== Health checks (F1) ==="
for pair in "gateway:$GATEWAY/hc" "identity-api:http://localhost:5218/hc" "warehouse-api:http://localhost:5238/hc" "pos-api:http://localhost:5258/hc" "reporting-api:http://localhost:5278/hc" "notifications-api:http://localhost:5298/hc"; do
  name="${pair%%:*}"
  url="${pair#*:}"
  status=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")
  check "$name /hc returns 200 (got $status)" "$([ "$status" = "200" ] && echo 1 || echo 0)"
done

echo "=== F2: security headers on every gateway response ==="
headers=$(curl -sD - -o /dev/null "$GATEWAY/hc")
check "X-Content-Type-Options present" "$(echo "$headers" | grep -qi 'X-Content-Type-Options: nosniff' && echo 1 || echo 0)"
check "X-Frame-Options present" "$(echo "$headers" | grep -qi 'X-Frame-Options: DENY' && echo 1 || echo 0)"

echo "=== Identity: register is forced to Cashier regardless of the requested role (F2) ==="
suffix=$RANDOM
register_response=$(curl -s -X POST "$GATEWAY/Identity/Auth/register" -H "Content-Type: application/json" \
  -d "{\"userName\":\"smoke$suffix\",\"email\":\"smoke$suffix@example.com\",\"password\":\"Password123\",\"role\":\"Admin\"}")
cashier_role=$(echo "$register_response" | grep -o '"role":"[^"]*"' | head -1 | cut -d'"' -f4)
check "self-registered account is Cashier, not the requested Admin (got '$cashier_role')" "$([ "$cashier_role" = "Cashier" ] && echo 1 || echo 0)"

echo "=== Identity: admin login ==="
login_response=$(curl -s -X POST "$GATEWAY/Identity/Auth/login" -H "Content-Type: application/json" \
  -d "{\"userName\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASSWORD\"}")
ADMIN_TOKEN=$(echo "$login_response" | grep -o '"token":"[^"]*"' | head -1 | cut -d'"' -f4)
check "admin login returned a token" "$([ -n "$ADMIN_TOKEN" ] && echo 1 || echo 0)"

if [ -z "$ADMIN_TOKEN" ]; then
  echo "Cannot continue without an admin token — is the seeded admin ($ADMIN_USER) present?"
  echo "$ok passed, $fail failed"
  exit 1
fi

echo "=== F3: a validation failure localizes through the gateway into Identity.API ==="
ar_response=$(curl -s -X POST "$GATEWAY/Identity/Auth/create-user" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -H "Accept-Language: ar" \
  -d '{"userName":"","email":"bad","password":"short","role":"Cashier"}')
check "Arabic Accept-Language returns an Arabic validation message" "$(echo "$ar_response" | grep -q 'يجب' && echo 1 || echo 0)"

echo "=== Warehouse: create an item ==="
categories=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Warehouse/MasterData/categories")
category_id=$(echo "$categories" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
units=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Warehouse/MasterData/units-of-measure")
unit_id=$(echo "$units" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
locations=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Warehouse/MasterData/locations")
location_id=$(echo "$locations" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
check "master data resolved (category=$category_id, unit=$unit_id, location=$location_id)" \
  "$([ -n "$category_id" ] && [ -n "$unit_id" ] && [ -n "$location_id" ] && echo 1 || echo 0)"

item_response=$(curl -s -X POST "$GATEWAY/Warehouse/Items" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d "{\"sku\":\"SMOKE-$suffix\",\"name\":\"Smoke Test Widget\",\"unitPrice\":9.99,\"categoryId\":$category_id,\"baseUnitOfMeasureId\":$unit_id,\"barcode\":\"SMOKE-$suffix-BC\",\"barcodeType\":0}")
item_id=$(echo "$item_response" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
check "item created (id=$item_id)" "$([ -n "$item_id" ] && echo 1 || echo 0)"

echo "=== Warehouse: receive stock so the item has something to sell ==="
curl -s -X POST "$GATEWAY/Warehouse/Stock/receive" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d "{\"itemId\":$item_id,\"locationId\":$location_id,\"unitOfMeasureId\":$unit_id,\"quantity\":10,\"reference\":\"smoke test\"}" > /dev/null
stock_before=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Warehouse/Stock/$item_id" | grep -o '"quantityOnHand":[0-9]*' | head -1 | grep -o '[0-9]*$')
stock_before="${stock_before:-0}"
check "stock received (on hand: $stock_before)" "$([ "$stock_before" -ge 10 ] && echo 1 || echo 0)"

echo "=== POS: full checkout flow ==="
sale_response=$(curl -s -X POST "$GATEWAY/Pos/Sales" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d "{\"locationId\":$location_id}")
sale_id=$(echo "$sale_response" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*$')
check "sale started (id=$sale_id)" "$([ -n "$sale_id" ] && echo 1 || echo 0)"

curl -s -X POST "$GATEWAY/Pos/Sales/$sale_id/lines" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d "{\"barcode\":\"SMOKE-$suffix-BC\",\"quantity\":2}" > /dev/null

checkout_response=$(curl -s -X POST "$GATEWAY/Pos/Sales/$sale_id/checkout" -H "Authorization: Bearer $ADMIN_TOKEN")
checkout_status=$(echo "$checkout_response" | grep -o '"status":"[^"]*"' | head -1 | cut -d'"' -f4)
check "sale checked out (status: $checkout_status)" "$([ "$checkout_status" = "Completed" ] && echo 1 || echo 0)"

echo "=== Cross-service: Warehouse stock actually decremented via the async outbox (C3) ==="
stock_after="$stock_before"
for i in $(seq 1 15); do
  stock_after=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Warehouse/Stock/$item_id" | grep -o '"quantityOnHand":[0-9]*' | head -1 | grep -o '[0-9]*$')
  stock_after="${stock_after:-$stock_before}"
  [ "$stock_after" -le "$((stock_before - 2))" ] && break
  sleep 1
done
check "stock dropped from $stock_before to $stock_after after checkout" "$([ "$stock_after" -le "$((stock_before - 2))" ] && echo 1 || echo 0)"

echo "=== Cross-service: Reporting's read model picks up the sale via its own event ingestion (D1) ==="
today=$(date -u +%Y-%m-%d)
found_in_report=0
for i in $(seq 1 15); do
  report=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" "$GATEWAY/Reporting/reports/sales-by-day")
  if echo "$report" | grep -q "$today"; then
    found_in_report=1
    break
  fi
  sleep 1
done
check "today's date appears in the sales-by-day report" "$found_in_report"

echo "=== F2: rate limiting on /register (runs last — deliberately exhausts its 30s budget) ==="
last_status="000"
for i in 1 2 3 4 5 6; do
  last_status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY/Identity/Auth/register" -H "Content-Type: application/json" -d '{}')
done
check "6th rapid /register attempt is rate-limited (got $last_status)" "$([ "$last_status" = "429" ] && echo 1 || echo 0)"

echo
echo "$ok passed, $fail failed"
[ "$fail" -eq 0 ]
