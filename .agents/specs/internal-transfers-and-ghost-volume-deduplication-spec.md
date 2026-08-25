# Spec — Deduplicação de Volume Fantasma, Transferências Internas e Métricas Financeiras Reais

- **Status**: 🟢 `Concluída & Implementada`
- **Data**: 2026-08-21
- **Branch**: `feature/transfer-matching-and-ghost-volume-dedup`
- **Serviços Envolvidos**: `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`
- **Referência ADR / DDD**: Domain-Driven Design Rich Aggregate Root & Transactional Outbox Pattern

---

## 1. Visão Geral do Problema (Ghost Volume & Double Counting)

Ao integrar múltiplas contas bancárias e cartões de crédito via Open Finance (Itaú, Inter, Mercado Pago, Nubank, etc.), a agregação ingênua (soma simples de todos os `Créditos` e `Débitos`) gera distorções matemáticas graves no fluxo de caixa do usuário, conhecidas na literatura financeira como **Volume Fantasma** (*Ghost Volume*) e **Dupla Contagem** (*Double Counting*).

### Sintoma Real Observado no FinanceHub:
- **Total de Entradas Exibido**: `+ R$ 286.270,90`
- **Total de Saídas Exibido**: `- R$ 283.508,10`
- **Saldo Líquido**: `+ R$ 2.762,80`

Embora a variação líquida patrimonial real seja de apenas R$ 2.762,80, o usuário não movimentou R$ 569.000 em receitas/despesas operacionais. A maior parte do volume decorre de transferências entre suas próprias contas (ex: Pix Itaú $\rightarrow$ Inter), aportes/resgates em cofrinhos e pagamentos de fatura de cartão de crédito.

---

## 2. Tipologia de Movimentações Anulatórias (Regras de Negócio Detalhadas)

Existem **5 categorias fundamentais** de movimentações que não constituem despesa nem receita operacional:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                            TAXONOMIA DE MOVIMENTAÇÕES FINANCEIRAS                           │
├──────────────────────────────────────────────────────────┬──────────────────────────────────┤
│           OPERACIONAIS (Afetam Orçamento Real)           │   NÃO OPERACIONAIS / NEUTRAS     │
├──────────────────────────────────────────────────────────┼──────────────────────────────────┤
│ 1. Receitas Reais (Salário, Vendas, Rendimentos Líquidos) │ 1. Transferências Contas Próprias │
│ 2. Despesas de Consumo (Alimentação, Transporte, Moradia) │ 2. Pagamentos de Fatura Cartão   │
│                                                          │ 3. Aportes/Resgates Cofrinho/CDB │
│                                                          │ 4. Estornos Mútuos / Reembolsos  │
│                                                          │ 5. Ajustes de Saldo / Compensação│
└──────────────────────────────────────────────────────────┴──────────────────────────────────┘
```

### 2.1. Transferências entre Contas Próprias (Same-Ownership Transfers)
- **Definição**: Movimentação de numerário entre duas contas de titularidade do mesmo `UserId` (ex: Pix enviado do Itaú para o Banco Inter).
- **Impacto no Extrato Bancário**:
  - Conta de Origem: Débito de R$ $X$ (`Pix Enviado` / `TED Enviada`).
  - Conta de Destino: Crédito de R$ $X$ (`Pix Recebido` / `TED Recebida`).
- **Regra de Negócio**:
  - Impacto na Receita Operacional: **R$ 0,00**.
  - Impacto na Despesa Operacional: **R$ 0,00**.
  - Impacto no Saldo Líquido Operacional: **R$ 0,00**.
  - Tratamento: As duas transações devem ser marcadas como `Nature = Transfer`, vinculadas via `PairedTransactionId` e excluídas da soma de Entradas e Saídas (`IsIgnoredInTotals = true`).

### 2.2. Pagamento de Fatura de Cartão de Crédito (Credit Card Settlement Double-Counting)
- **Definição**: Quitação do saldo devedor acumulado no cartão de crédito através de débito em conta corrente.
- **Causa da Duplicidade**:
  1. O usuário realiza compras individuais no cartão de crédito ao longo do mês, já contabilizadas item a item nas categorias de consumo (Supermercado, Farmácia, Restaurante).
  2. No dia do vencimento da fatura, ocorre um débito na conta corrente ("Pagamento de Fatura").
- **Regra de Negócio**:
  - O pagamento da fatura é uma **liquidação de passivo financeiro**, e não um novo consumo de produtos/serviços.
  - A despesa real já foi contabilizada em cada transação individual do cartão.
  - Tratamento: A transação de débito bancário "Pagamento de Fatura" recebe `Nature = BillPayment` e `IsIgnoredInTotals = true`.

### 2.3. Aportes, Cofrinhos e Resgates de Investimentos (Asset Relocation)
- **Definição**: Transferência de saldo da conta corrente para uma conta de investimento, CDB com liquidez diária, Poupança ou "Cofrinho" do mesmo titular.
- **Regra de Negócio**:
  - Guardar dinheiro não é despesa de consumo; resgatar o valor aplicado não é receita salarial.
  - Apenas o rendimento real dos juros (`IncomeYieldId`) constitui receita líquida.
  - Tratamento: O aporte e o resgate do principal recebem `Nature = Investment` e `IsIgnoredInTotals = true`.

### 2.4. Estornos, Cancelamentos e Reembolsos (Refunds & Chargebacks)
- **Definição**: Devolução de um valor pago em compra anterior que foi cancelada ou contestada.
- **Regra de Negócio**:
  - Um estorno não deve figurar como nova receita/entrada, pois o usuário apenas cancelou a despesa original.
  - Tratamento: O estorno recebe `Nature = Adjustment` e anula o impacto no fluxo operacional.

---

## 3. Formulação Matemática do Fluxo de Caixa Real

Seja $\mathcal{T} = \{t_1, t_2, \dots, t_n\}$ o conjunto de transações do usuário no período selecionado:

$$\mathcal{T}_{op} = \{t \in \mathcal{T} \mid \text{t.IsIgnoredInTotals} = \text{false}\}$$

$$\mathcal{T}_{neutral} = \{t \in \mathcal{T} \mid \text{t.IsIgnoredInTotals} = \text{true}\}$$

### Métricas Exibidas no Dashboard:
1. **Total de Entradas Operacionais ($I_{op}$)**:
   $$I_{op} = \sum_{t \in \mathcal{T}_{op}, \text{t.Type} = \text{Credit}} \text{t.Amount}$$

2. **Total de Saídas Operacionais ($E_{op}$)**:
   $$E_{op} = \sum_{t \in \mathcal{T}_{op}, \text{t.Type} = \text{Debit}} \text{t.Amount}$$

3. **Saldo Líquido Operacional ($B_{net}$)**:
   $$B_{net} = I_{op} - E_{op}$$

4. **Volume Neutro / Transferências Ocultadas ($V_{neutral}$)**:
   $$V_{neutral} = \sum_{t \in \mathcal{T}_{neutral}} \text{t.Amount}$$

---

## 4. Arquitetura de Domínio & DDD (`FinanceHub.TransactionAggregator.Domain`)

### 4.1. Enum `TransactionNature`
```csharp
namespace FinanceHub.TransactionAggregator.Domain.Entities;

public enum TransactionNature
{
    Operating = 0,     // Receitas e Despesas normais de orçamento
    Transfer = 1,      // Transferência entre contas próprias do mesmo titular
    BillPayment = 2,   // Liquidação de fatura de cartão de crédito
    Investment = 3,    // Aporte ou resgate de investimento/cofrinho
    Adjustment = 4     // Estorno, reembolso ou compensação técnica
}
```

### 4.2. Entidade `CanonicalTransaction` (Rich Aggregate Root)
```csharp
public class CanonicalTransaction
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public AccountIdentifier AccountInfo { get; private set; }
    public TransactionHash Hash { get; private set; }
    public Money Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public SanitizedDescription Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public CategorizationSource CategorizationSource { get; private set; }
    public bool IsManuallyCategorized { get; private set; }
    public DateTime TransactionDateUtc { get; private set; }
    public BankTransactionDetails BankDetails { get; private set; }
    public TransactionAuditInfo AuditInfo { get; private set; }

    // Propriedades DDD para Neutralidade e Pareamento
    public TransactionNature Nature { get; private set; }
    public bool IsIgnoredInTotals { get; private set; }
    public Guid? PairedTransactionId { get; private set; }

    public void MarkAsInternalTransfer(Guid pairedTransactionId)
    {
        Nature = TransactionNature.Transfer;
        IsIgnoredInTotals = true;
        PairedTransactionId = pairedTransactionId;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void MarkAsBillPayment()
    {
        Nature = TransactionNature.BillPayment;
        IsIgnoredInTotals = true;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }

    public void ToggleIgnoreInTotals(bool ignore)
    {
        IsIgnoredInTotals = ignore;
        AuditInfo = new TransactionAuditInfo(AuditInfo.CreatedAtUtc, DateTime.UtcNow);
    }
}
```

---

## 5. Motor de Pareamento Automático (`TransferPairMatchingEngine`)

O motor de pareamento é acionado assincronamente ao término de cada batch de ingestão de transações (`TransactionsBatchIngestedConsumer` / `TransactionIngestedConsumer`).

### 5.1. Resiliência a Chegadas Assíncronas em Momentos Diferentes (Symmetric Incremental Matching)

> **Cenário Real de Open Finance**: O usuário faz um Pix do Itaú para o Inter. O webhook/sincronização do Itaú chega na sexta-feira às 20h00, mas a sincronização do Inter ou arquivo OFX só é processada na segunda-feira ou terça-feira.

#### Como o Algoritmo Trata Chegadas Descasadas no Tempo:
1. **Chegada da Transação A (Débito - Itaú)**:
   - O motor busca no banco uma transação $B$ (Crédito) compatível dentro da janela de $\pm 96\text{h}$ (4 dias, cobrindo finais de semana e feriados).
   - Não encontrando, persiste $A$ com `PairedTransactionId = null`. (Se a transação foi categorizada como `others-transfers`, ela já é marcada com `IsIgnoredInTotals = true` de forma preventiva).
2. **Chegada da Transação B (Crédito - Inter)**:
   - Ao ingerir $B$, o motor executa a busca retroativa no banco por débitos não pareados de mesmo valor e mesmo `UserId` dentro da janela de $\pm 96\text{h}$.
   - O motor encontra $A$ no banco.
   - Executa a vinculação mútua atômica:
     - Atualiza $B.\text{PairedTransactionId} = A.Id$, $B.\text{Nature} = \text{Transfer}$ e $B.\text{IsIgnoredInTotals} = \text{true}$
     - Atualiza $A.\text{PairedTransactionId} = B.Id$, $A.\text{Nature} = \text{Transfer}$ e $A.\text{IsIgnoredInTotals} = \text{true}$
   - Salva a reconciliação no banco de dados.

### 5.2. Algoritmo de Detecção de Pares:
Para uma transação $T_A$ de Débito, busca-se uma transação $T_B$ de Crédito que satisfaça cumulativamente:
1. **Mesmo Usuário**: $T_A.UserId == T_B.UserId$.
2. **Mesmo Valor Monetário**: $T_A.Amount.Amount == T_B.Amount.Amount$ e $T_A.Amount.Currency == T_B.Amount.Currency$.
3. **Sentidos Opostos**: $T_A.Type == Debit$ e $T_B.Type == Credit$.
4. **Contas Distintas**: $(T_A.AccountInfo.AccountId \ne T_B.AccountInfo.AccountId)$ ou $(T_A.AccountInfo.InstitutionId \ne T_B.AccountInfo.InstitutionId)$.
5. **Janela Temporal Estrita**: $|T_A.TransactionDateUtc - T_B.TransactionDateUtc| \le 96 \text{ horas}$ (4 dias).
6. **Não Pareadas Anteriormente**: $T_A.PairedTransactionId == \text{null}$ e $T_B.PairedTransactionId == \text{null}$.
7. **Padrões de Descrição**: Ambas contêm indícios de transferência (`PIX`, `TED`, `DOC`, `TRANSF`, `TRANSFERENCIA`, ou nomes cruzados nos detalhes bancários).

Ao encontrar a correspondência perfeita:
- $T_A.\text{MarkAsInternalTransfer}(T_B.Id)$
- $T_B.\text{MarkAsInternalTransfer}(T_A.Id)$
- Persiste a atualização no `TransactionDbContext` de forma atômica.

---

## 6. Estratégia de Migração e Backfill Automático de Dados Históricos

Para corrigir o histórico existente que gerou os números distorcidos:
1. **Script de Migração EF Core (`AddTransactionNatureAndGhostVolumeColumns`)**:
   - Cria as novas colunas `Nature` (integer, default 0), `IsIgnoredInTotals` (boolean, default false), e `PairedTransactionId` (uuid, nullable).
   - Executa SQL de backfill direto para atualizar transações conhecidas:
     ```sql
     -- 1. Marcar transferências e faturas conhecidas como ignoradas
     UPDATE "Transactions"
     SET "IsIgnoredInTotals" = TRUE,
         "Nature" = CASE 
             WHEN "CategoryId" = '11111111-1111-1111-1111-111111111002' THEN 1 -- Transfer
             WHEN "CategoryId" = '11111111-1111-1111-1111-111111110801' AND "Description_CleanText" ILIKE '%FATURA%' THEN 2 -- BillPayment
             WHEN "CategoryId" = '11111111-1111-1111-1111-111111110805' THEN 3 -- Investment
             ELSE 0
         END
     WHERE "CategoryId" IN (
         '11111111-1111-1111-1111-111111111002', -- Outros / Transferências
         '11111111-1111-1111-1111-111111110805'  -- Finanças / Investimentos
     ) OR ("Description_CleanText" ILIKE '%FATURA%');
     ```
2. **Rotina de Inicialização / Seed**:
   - No startup da API (`Program.cs`), caso existam transações de transferência não pareadas, roda uma passagem única do `TransferPairMatchingEngine` para vincular os pares históricos.

---

## 7. Otimização de Performance e Índices no PostgreSQL

Para garantir consultas instantâneas mesmo em bases com milhões de transações, será criado um **Índice Parcial** no PostgreSQL:

```sql
-- Índice composto com filtro parcial para o cálculo de métricas operacionais
CREATE INDEX IX_Transactions_UserId_Date_Operating 
ON "Transactions" ("UserId", "TransactionDateUtc", "Type") 
INCLUDE ("Amount")
WHERE "IsIgnoredInTotals" = FALSE;
```

---

## 8. Experiência de Usuário no Frontend (UI/UX)

1. **Cards de Resumo**:
   - Exibem apenas valores operacionais reais calculados pelo backend (`totalIncome`, `totalExpense`, `netBalance`).
   - Adicionado contador/aviso sutil de transferências desconsideradas quando houver.
2. **Listagem na Tabela de Transações**:
   - Transações com `IsIgnoredInTotals = true` continuam visíveis na listagem para auditoria completa do extrato, porém com estilo atenuado (opacidade reduzida) e badge informativa discreta (`⇄ Transferência` ou `📄 Fatura`).
3. **Controle Manual do Usuário (Override)**:
   - No popover/modal da transação, o usuário pode alternar se deseja incluir ou não aquela movimentação nos totais.

---

## 9. Plano de Validação e Testes Unitários

- [ ] Teste de Domínio: `CanonicalTransaction_ShouldCorrectlyToggleIgnoreInTotals`.
- [ ] Teste de Domínio: `CanonicalTransaction_ShouldSetPairedIdWhenMarkedAsTransfer`.
- [ ] Teste do Motor de Pareamento: `TransferPairMatchingEngine_ShouldPairOppositeTransactionsWithin96Hours`.
- [ ] Teste do Motor de Pareamento: `TransferPairMatchingEngine_ShouldHandleAsymmetricArrivalTimes`.
- [ ] Teste do Repositório: `QueryPagedByFilterAsync_ShouldExcludeIgnoredTransactionsFromSummaryTotals`.
- [ ] Teste do Endpoint: `GetTransactionsSummary_ShouldReturnOnlyOperatingTotals`.
- [ ] Teste Frontend: `TransactionsSummaryCards_ShouldDisplayAccurateOperatingBalance`.
