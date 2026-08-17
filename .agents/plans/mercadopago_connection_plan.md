# 🛑 PLANO ARQUITETURAL: Integração Direta Mercado Pago (OAuth2 / Developer API)

> **STATUS:** ❌ **REJEITADO / SUBSTITUÍDO (SUPERSEDED)**  
> **Data de Rejeição:** 2026-08-16  
> **Motivo:** A API oficial do Mercado Pago para desenvolvedores foca em movimentações de conta vendedor/checkout e não expõe itens detalhados de fatura de cartão de crédito. Substituído pelo conector **`FinanceHub.PluggyIntegration`** e pelo fallback offline **`FinanceHub.FileImporter`** (Extrato CSV e Fatura PDF).  
> **Documento de Referência:** [system-architecture-and-services.md](file:///home/josemd12/Code/FinanceHub/.agents/knowledge/system-architecture-and-services.md)

---

## 📋 Resumo do Plano Original (Histórico Arquivado)
* **Objetivo Original**: Autenticação OAuth2 direta com a API do Mercado Pago para consumir saldo e movimentações.
* **Limitações Identificadas**:
  1. A API oficial do MP não fornece o detalhamento analítico da fatura de cartão de crédito para pessoas físicas.
  2. A fatura do cartão é disponibilizada exclusivamente em formato PDF no aplicativo.
* **Decisão e Rota Adotada**:
  * **Online**: Ingestão via `FinanceHub.PluggyIntegration` (Meu.Pluggy).
  * **Offline**: Leitura de Extrato CSV e Fatura PDF via `FinanceHub.FileImporter`.
