# 🛑 PLANO ARQUITETURAL: Integração Direta Itaú Open Finance (FAPI / ICP-Brasil)

> **STATUS:** ❌ **REJEITADO / SUBSTITUÍDO (SUPERSEDED)**  
> **Data de Rejeição:** 2026-08-16  
> **Motivo:** Acesso direto via FAPI 1.0/2.0 corporativo requer certificado digital ICP-Brasil e credenciamento PJ de alto custo. Substituído com 100% de sucesso pelo conector online unificado **`FinanceHub.PluggyIntegration`** e pelo fallback offline **`FinanceHub.FileImporter`** para PDFs.  
> **Documento de Referência:** [system-architecture-and-services.md](file:///home/josemd12/Code/FinanceHub/.agents/knowledge/system-architecture-and-services.md)

---

## 📋 Resumo do Plano Original (Histórico Arquivado)
* **Objetivo Original**: Conexão mTLS direta com as APIs Open Finance do Banco Itaú para leitura de extrato e fatura.
* **Complexidade/Impeditivos**:
  1. Necessidade de certificado e-CNPJ ICP-Brasil corporativo emitido por Autoridade Certificadora brasileira.
  2. Ausência de suporte a agregação multisserviço direta sem convênio formal bancário.
  3. Formato da fatura e extrato disponibilizados em PDF na interface do usuário.
* **Decisão e Rota Adotada**:
  * **Online**: Ingestão via `FinanceHub.PluggyIntegration` (Meu.Pluggy).
  * **Offline**: Leitura de Fatura e Extrato PDF via `FinanceHub.FileImporter` com biblioteca `PdfPig`.
