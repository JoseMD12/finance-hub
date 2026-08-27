# Especificação Técnica: Engine de Neutralidade, Dinheiro de Trânsito e Pareamento de Repasses

**Documento:** `transit-transfers-and-neutrality-engine-spec.md`  
**Status:** 🟢 `Concluída & Implementada`  
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

### 2.1 Desacoplamento Ortogonal: Natureza (Comportamento) vs Propósito (Finalidade)
No modelo financeiro, um lançamento pode ser simultaneamente uma **Transferência Pareada** (comportamento de compensação entre contas) e um **Pagamento de Fatura** (propósito semântico do débito):

* **`Nature: TransactionNature` (Comportamento/Ação Relacional)**:
  * `Operating`: Transação comum de vida / terceiros.
  * `Transfer`: Transação pareada com lançamento espelho (titularidade própria ou repasse).
  * `Investment`: Aporte ou resgate de aplicação/caixinha.
  * `Adjustment`: Ajuste de saldo manual ou compensatório.
* **`IsBillPayment: bool` (Propósito / Finalidade Semântica)**:
  * Indica se o lançamento representa liquidação de fatura de cartão de crédito (baseado em padrão textual `FATURA` ou categoria `Pagamento de Fatura`).
* **`IsIgnoredInTotals: bool` (Expurgo Operacional)**:
  * Verdadeiro sempre que `Nature == Transfer`, `IsBillPayment == true`, ou quando marcado como neutro/trânsito.
* **`PairedTransactionId: Guid?` (Vínculo de Pareamento)**:
  * Chave de identificação da transação simétrica oposta.

### 2.2 Backend — `TransferPairMatchingEngine` Expandido
* **Fase 1: Pareamento de Transferências Próprias**:
  - Reconhece e pareia transferências entre contas de mesma titularidade (`José` $\leftrightarrow$ `José`), mesmo que uma das pernas seja pagamento de fatura (`Nature = Transfer`, preservando `IsBillPayment = true`).
* **Fase 2: Pareamento Recíproco com Terceiros**:
  - Analisa pares de `Pix enviado <Nome>` e `Pix recebido <Nome>`.
  - Se os valores forem idênticos em janela de $\le 72\text{h}$, marca ambos como `is_ignored_in_totals = true` e `nature = TransactionNature.Transfer`.
* **Fase 3: Detecção de Dinheiro de Trânsito / Boleto Espelho**:
  - Detecta entrada de Pix não-salarial seguida de pagamento de boleto ou saída com valor quase idêntico ($\Delta \le \text{R\$ } 2,00$) no mesmo dia ($\le 24\text{h}$).
  - Marca a entrada e a saída como `is_ignored_in_totals = true` (Trânsito).

### 2.3 Endpoint & Comando de Neutralidade Manual
* `PATCH /api/v1/transactions/{id}/neutrality`
  - `ToggleTransactionNeutralityCommand(Guid TransactionId, string UserId, bool IsIgnoredInTotals, string? Reason)`
* BFF: `PATCH /api/v1/gateway/transactions/{id}/neutrality`

### 2.4 Frontend — Badges Cumulativos e Controle Visual
* **Tabela de Transações (`TransactionsTable.tsx`)**:
  - Suporte a **múltiplos badges complementares** na mesma linha:
    * Se `isBillPayment === true` $\rightarrow$ exibe Badge 🧾 **`Fatura`**.
    * Se `nature === 'Transfer'` $\rightarrow$ exibe Badge 🔄 **`Transferência`**.
    * Se `isIgnoredInTotals === true` e não for transferência nem fatura $\rightarrow$ exibe Badge **`Neutro`**.
  - Lançamentos de pagamento de fatura transferidos entre contas exibirão simultaneamente `[ 🔄 Transferência ]` e `[ 🧾 Fatura ]`.
* **Modal de Detalhes da Transação**:
  - Switch interativo para alternar neutralidade nos totais.
  - Exibição destacada das tags de finalidade (Fatura) e relacionamento (Transferência Pareada).

---

## 3. 🧪 Plano de Validação & TDD
1. Criar testes unitários para a entidade `CanonicalTransaction` garantindo coexistência de `Nature = Transfer` e `IsBillPayment = true`.
2. Atualizar migração/coluna no EF Core (`is_bill_payment`) se necessário e DTOs de leitura (`TransactionDto.isBillPayment`).
3. Atualizar `TransferPairMatchingEngineTests` e testes do frontend (`TransactionsTable.test.tsx` e `TransactionsPage.test.tsx`).

