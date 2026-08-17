# 🛑 PLANO ARQUITETURAL: Integração Direta Banco Inter Open Finance

> **STATUS:** ❌ **REJEITADO / SUBSTITUÍDO (SUPERSEDED)**  
> **Data de Rejeição:** 2026-08-16  
> **Motivo:** A API de desenvolvedores do Banco Inter destina-se a pessoas jurídicas (PJ) para emissão de Pix e cobrança, sem suporte a leitura de faturas PF via Open Finance sem certificação de software parceiro. Substituído pelo conector **`FinanceHub.PluggyIntegration`** e pelo fallback offline **`FinanceHub.FileImporter`** (Extrato OFX e Fatura CSV).  
> **Documento de Referência:** [system-architecture-and-services.md](file:///home/josemd12/Code/FinanceHub/.agents/knowledge/system-architecture-and-services.md)

---

## 📋 Resumo do Plano Original (Histórico Arquivado)
* **Objetivo Original**: Integração direta via mTLS/OAuth com a API do Banco Inter.
* **Limitações Identificadas**:
  1. API de desenvolvedor restrita a emissão e recebimento de cobranças PJ.
  2. Extrato bancário de conta corrente é gerado em formato OFX e fatura de cartão em CSV no Internet Banking.
* **Decisão e Rota Adotada**:
  * **Online**: Ingestão via `FinanceHub.PluggyIntegration` (Meu.Pluggy).
  * **Offline**: Leitura de Extrato OFX e Fatura CSV via `FinanceHub.FileImporter`.
