# FinanceHub — Gemini & AI Agent Context Guide

This document serves as the primary system context and instruction set for Gemini and AI subagents operating within the **FinanceHub** (.NET 10) microservices repository.

---

## 🚀 System Architecture & Microservices Boundaries

FinanceHub is an enterprise personal finance aggregator engineered on **.NET 10** and **C# 13**. It connects directly to Brazil's **Open Finance** infrastructure via isolated microservices:

1. **`FinanceHub.ApiGateway`**: Single entrypoint BFF for the frontend application, handling auth, rate limiting and dashboard queries.
2. **`FinanceHub.PluggyIntegration`**: Unified Open Finance connector for Brazilian banks (Itaú, Inter, Mercado Pago). Emits `TransactionIngested` and `InvoiceItemIngested`.
3. **`FinanceHub.FileImporter`**: Offline financial file ingestion engine for `.ofx`, `.csv`, and `.pdf` bank/card statements.
4. **`FinanceHub.TransactionAggregator`**: Consumes ingested events, normalizes to canonical transaction model, deduplicates (SHA-256), auto-categorizes, and persists ledger history.
5. **`FinanceHub.Shared.*`**: `Messaging` (MassTransit/Outbox/Events), `Observability` (OpenTelemetry/Serilog), `Certificates` (mTLS).

---

## 🔒 Security Guardrails
1. **Database per Service**: Never access another service's PostgreSQL DB directly.
2. **Outbox Pattern**: Never publish messages directly inside `DbContext.SaveChanges()` without Outbox protection.
3. **OpenTelemetry Context**: Propagate `traceparent` context across all HTTP and RabbitMQ calls.
4. **Zero Secrets in Code**: Encryption keys, mTLS certificates, and API secrets must be loaded dynamically.
