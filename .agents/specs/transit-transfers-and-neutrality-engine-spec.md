# Especificação Técnica: Engine de Neutralidade, Dinheiro de Trânsito e Pareamento de Repasses

**Documento:** `transit-transfers-and-neutrality-engine-spec.md`  
**Status:** PROPOSED & APPROVED  
**Autor:** Antigravity AI & Jose Henrique  
**Data:** 2026-08-23  

---

## 1. 🎯 Problema de Negócio & Motivação

No modelo financeiro real, nem toda entrada em conta bancária representa **Renda Operacional Efetiva** (como salário ou rendimentos) e nem toda saída representa **Despesa de Vida**.
Identificamos 3 padrões de fluxo transitório que inflam artificialmente o balanço mensal:

1. **Pontes e Repasses com Terceiros (ex: José $\leftrightarrow$ Patrick)**:
   - José envia Pix para Patrick e no mesmo período Patrick repassa/devolve Pix para José.
2. **Dinheiro Transitório / Pagamento por Terceiros (ex: Elmo Dotta $\rightarrow$ Boleto Imóveis)**:
   - Entrada de Pix familiar/terceiro (ex: R$ 4.243,00) que é imediatamente utilizada no mesmo dia para pagar um boleto/fatura espelho (R$ 4.242,01).
3. **Divisão de Despesas / Rachas de Conta (ex: Pix recebido de amigos)**:
   - Entradas de pequenos valores (ex: R$ 53,00) que representam reembolsos diretos de contas divididas.

---

## 2. 🏛️ Arquitetura da Solução

### 2.1 Backend — `TransferPairMatchingEngine` Expandido
* **Fase 1: Pareamento de Transferências Próprias**:
  - Já implementado para contas com titularidade cruzada (`José` $\leftrightarrow$ `José`).
* **Fase 2: Pareamento Recíproco com Terceiros**:
  - Analisa pares de `Pix enviado <Nome>` e `Pix recebido <Nome>`.
  - Se os valores forem idênticos em uma janela de até 3 dias, marca ambos como `is_ignored_in_totals = true` e `nature = TransactionNature.Transfer`.
* **Fase 3: Detecção de Dinheiro de Trânsito / Boleto Espelho**:
  - Detecta entrada de Pix não-salarial seguida de pagamento de boleto ou saída com valor quase idêntico ($\Delta \le \text{R\$ } 2,00$) no mesmo dia ($\le 24\text{h}$).
  - Marca a entrada e a saída como `is_ignored_in_totals = true` (Trânsito).

### 2.2 Endpoint & Comando de Neutralidade Manual
* `PATCH /api/v1/transactions/{id}/neutrality`
  - `ToggleTransactionNeutralityCommand(Guid TransactionId, string UserId, bool IsIgnoredInTotals, string? Reason)`
* BFF: `PATCH /api/v1/gateway/transactions/{id}/neutrality`

### 2.3 Frontend — Controle Visual e Interativo
* **Tabela de Transações**:
  - Badge visual informativo: `Neutro` / `Trânsito`.
* **Gaveta de Detalhes da Transação (`TransactionDetailsDrawer`)**:
  - Switch/Toggle interativo para marcar/desmarcar a transação como dinheiro de trânsito em 1 clique, com recalculo instantâneo dos cards de resumo.
* **Filtros Rápidos**:
  - Opção no filtro para exibir/ocultar transações neutras.

---

## 3. 🧪 Plano de Validação & TDD
1. Criar testes unitários no backend para as 3 fases do `TransferPairMatchingEngine`.
2. Criar testes de integração para o comando `ToggleTransactionNeutralityCommand`.
3. Criar testes no frontend para a ação de toggle e renderização dos badges.
