# Especificação Técnica: Filtros por Meio de Pagamento (Saldo vs Conta) e Período Customizado

**Documento:** `.agents/specs/channel-and-custom-date-filters-spec.md`  
**Status:** 🟡 `Em Elaboração / Planejamento`  
**Data:** 25/08/2026  
**Branch:** `feature/channel-and-custom-date-filters`  
**Serviços Envolvidos:** `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`  

---

## 🎯 1. Visão Geral & Objetivos

Esta funcionalidade expande o controle e a usabilidade da tela de **Extrato de Transações** (`TransactionsPage`), adicionando dois novos filtros de alta relevância no consumo de finanças pessoais:

1. **Filtro por Meio / Canal de Movimentação**:
   - Atualmente o backend possui enum `TransactionChannel` (Pix, CreditCard, DebitCard, Ted, Doc, BankTransfer, Other), porém os conectores de Open Finance e extratos bancários brutos fornecem metadados limitados onde muitas transações são genéricas.
   - **Solução proposta pelo usuário**: Simplificar a visão do filtro em **2 opções estratégicas**:
     - 💳 **Saldo / Cartão de Crédito**: Transações originadas em faturas ou linhas de crédito (ex: `CreditCard`, compras parceladas/faturas).
     - 🏦 **Conta / Conta Corrente (PIX, Débito, TED, Extrato)**: Transações diretas de saldo em conta corrente/poupança (ex: `Pix`, `DebitCard`, `Ted`, `Doc`, `BankTransfer`).
     - 🔄 **Todos os Meios**: Opção padrão sem filtro.

2. **Filtro por Período de Data Customizado**:
   - Atualmente o filtro de data oferece botões rápidos de preset (`Mês Atual`, `Mês Anterior`, `Últimos 30 Dias`, `Ano Atual`, `Todo o Histórico`).
   - **Solução proposta**: Adicionar uma opção de **Preset Customizado ("Personalizado")** que expande/exibe seletores de data de início (`startDate`) e fim (`endDate`), permitindo ao usuário selecionar intervalos específicos mantendo o funcionamento dos presets rápidos.

---

## 🏛️ 2. Arquitetura & Impactos por Camada

### 🧠 2.1 Backend (.NET 10 — `FinanceHub.TransactionAggregator`)

1. **CQRS DTO (`TransactionFilterDto.cs` & `GetTransactionsParameters.cs`)**:
   - Adicionar o parâmetro opcional `string? Channel` ou `string? ChannelGroup` em `TransactionFilterDto`.
   - Adicionar o parâmetro em `GetTransactionsParameters` da Minimal API e na interface `ITransactionAggregatorServiceClient`.
2. **Repositório (`TransactionRepository.cs`)**:
   - Implementar a cláusula `WHERE` LINQ no EF Core para filtrar por canal:
     - Se filtro for `Account` (Conta): `t.BankDetails.Channel != TransactionChannel.CreditCard`.
     - Se filtro for `Credit` (Saldo/Cartão): `t.BankDetails.Channel == TransactionChannel.CreditCard` ou `t.IsBillPayment == true`.

---

### 🎨 2.2 Frontend (`FinanceHub.Web`)

1. **Tipos (`transactions.types.ts`)**:
   - Estender `DatePresetKey` para incluir `'custom'`.
   - Estender `TransactionFilterParams` com a propriedade opcional `channelGroup?: 'account' | 'credit'`.
2. **Barra de Filtros (`TransactionsFilterBar.tsx`)**:
   - Adicionar o dropdown `CustomSelect` para **Meio** (Todos, Conta / Saldo, Cartão / Crédito).
   - Ao selecionar o preset **Personalizado**, exibir dois campos de data (`input type="date"`) nativos/componentizados elegantes com suporte a `startDate` e `endDate`.
3. **Contador de Filtros Ativos & Reset**:
   - Atualizar a contagem `activeFiltersCount` para considerar `channelGroup` e datas customizadas.
