---
name: manual-api-curl-testing
description: Autonomous API & Microservices test suite runner for FinanceHub. Executes all health checks, RFC 7807 problem details validation, and Open Finance endpoints in a single command using an object parameter payload.
---

# 🧪 Autonomous API Test Suite Runner (`manual-api-curl-testing`)

## ⚡ Trigger / Slash Commands
```bash
/curl-test                 # Runs autonomous API test suite runner
/test-api-curl             # Alias for /curl-test
/manual-api-curl-testing   # Alias for /curl-test
```

---

## 🎯 Purpose & Autonomous Single-Permission Protocol

This skill provides a **single-command autonomous test runner** script ([`test-runner.js`](file:///home/josemd12/Code/FinanceHub/.agents/skills/manual-api-curl-testing/scripts/test-runner.js)) that receives configuration URLs, token, and user ID as a JSON object parameter.

It allows the agent and user to execute **all 8 health checks, domain validation, and Open Finance sync tests in a single command permission request**.

---

## 🚀 Execution Method (Single Command JSON Object)

Pass the configuration JSON object as a single command argument to `.agents/skills/manual-api-curl-testing/scripts/test-runner.js`:

```bash
node .agents/skills/manual-api-curl-testing/scripts/test-runner.js '{
  "token": "<PLUGGY_ACCESS_TOKEN>",
  "userId": "<USER_ID>",
  "urls": {
    "pluggy": "http://localhost:5056",
    "gateway": "http://localhost:5050",
    "aggregator": "http://localhost:5002"
  }
}'
```

---

## 📋 What the Autonomous Runner Tests (8 Total Tests)

1. **`PluggyIntegration Health`**: `GET http://localhost:5056/health` $\rightarrow$ `200 OK`
2. **`ApiGateway Health`**: `GET http://localhost:5050/health` $\rightarrow$ `200 OK`
3. **`TransactionAggregator Health`**: `GET http://localhost:5002/health` $\rightarrow$ `200 OK`
4. **`Missing Token RFC 7807 Validation`**: `GET http://localhost:5056/api/v1/pluggy/items` $\rightarrow$ `400 Bad Request`
5. **`GET /items (Connected Institutions)`**: `GET http://localhost:5056/api/v1/pluggy/items` $\rightarrow$ `200 OK`
6. **`GET /accounts (Bank & Credit Accounts)`**: `GET http://localhost:5056/api/v1/pluggy/accounts` $\rightarrow$ `200 OK`
7. **`POST /sync (Full Portfolio Ingestion)`**: `POST http://localhost:5056/api/v1/pluggy/sync` $\rightarrow$ `200 OK`
8. **`POST /items/{id}/sync (Single Item Sync)`**: `POST http://localhost:5056/api/v1/pluggy/items/<ID>/sync` $\rightarrow$ `200 OK`

---

## 📊 Sample JSON Output

Upon execution, the runner outputs a clean human-readable log and structured JSON result block between `RESULT_JSON_START` and `RESULT_JSON_END`:

```json
{
  "timestamp": "2026-08-19T22:54:23.477Z",
  "summary": {
    "total": 8,
    "passed": 8,
    "failed": 0
  },
  "results": [...]
}
```
