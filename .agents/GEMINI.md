# FinanceHub — Gemini & AI Agent Context Guide

This document serves as the primary system context and instruction set for Gemini and AI subagents operating within the **FinanceHub** (.NET 10) microservices repository.

---

## 🚀 System Architecture & Microservices Boundaries

FinanceHub is an enterprise personal finance aggregator engineered on **.NET 10** and **C# 13**. It connects directly to Brazil's **Open Finance** infrastructure via isolated microservices:

1. **`FinanceHub.AuthConsent`**: OAuth2/OIDC + FAPI consent manager. Handles token lifecycle (`access_token`, `refresh_token`).
2. **`FinanceHub.ItauIntegration`**: Itaú Open Finance API connector. Translates Itaú payloads to `TransactionIngested` event.
3. **`FinanceHub.MercadoPagoIntegration`**: Mercado Pago API connector. Translates MP payloads to `TransactionIngested` event.
4. **`FinanceHub.InterIntegration`**: Banco Inter API connector (to be implemented after Inter Open Finance phase confirmation).
5. **`FinanceHub.TransactionAggregator`**: Consumes `TransactionIngested` events, normalizes to canonical transaction model, deduplicates, and persists history. Emits `TransactionNormalized`.
6. **`FinanceHub.ApiGateway`**: Single entrypoint BFF for the frontend application.
7. **`FinanceHub.Shared.*`**: `Certificates` (mTLS), `Messaging` (MassTransit/Outbox/Events), `Observability` (OpenTelemetry).

---

## 🔒 Security Guardrails
1. **Database per Service**: Never access another service's PostgreSQL DB directly.
2. **Outbox Pattern**: Never publish messages directly inside `DbContext.SaveChanges()` without Outbox protection.
3. **OpenTelemetry Context**: Propagate `traceparent` context across all HTTP and RabbitMQ calls.
4. **Zero Secrets in Code**: Encryption keys, mTLS certificates, and API secrets must be loaded dynamically.
