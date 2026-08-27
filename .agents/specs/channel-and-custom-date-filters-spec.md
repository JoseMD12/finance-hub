# Especificação Técnica: Filtros por Meio de Pagamento e Mini Calendário de Período Customizado

**Documento:** `.agents/specs/channel-and-custom-date-filters-spec.md`  
**Status:** 🟢 `Aprovada / Pronta para Implementação`  
**Data:** 25/08/2026  
**Branch:** `feature/channel-and-custom-date-filters`  
**Serviços Envolvidos:** `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`  

---

## 🎯 1. Visão Geral & Objetivos

Esta especificação define o design e a arquitetura técnica para a expansão da tela de **Extrato de Transações** (`TransactionsPage`), introduzindo dois novos filtros avançados de controle financeiro:

1. **Filtro por Meio / Canal de Movimentação (`channelGroup`)**:
   - Simplificação em **3 opções estratégicas no dropdown** (`CustomSelect`):
     - 🔄 **Todos os Meios**: Sem filtro aplicado (`channelGroup = undefined`).
     - 🏦 **Conta / Saldo**: Transações diretas de saldo em conta corrente/poupança (Pix, TED, DOC, Débito, Transferências e pagamentos de fatura debitados da conta).
     - 💳 **Cartão de Crédito**: Compras no crédito e lançamentos originados em faturas de cartão.
   - Parâmetro padronizado em todas as camadas como `channelGroup` (`string?` no backend, `'account' | 'credit'` no frontend).

2. **Filtro por Período Customizado & Componente de Mini Calendário (`DateRangePicker`)**:
   - Adição do preset **"Personalizado"** ("Selecionar Datas") ao lado dos presets rápidos de período (`Mês Atual`, `Mês Anterior`, `Últimos 30 Dias`, `Ano Atual`, `Todo o Histórico`).
   - Ao ser acionado, expande um **Popover flutuante com Mini Calendário Interativo** (`DateRangePicker`), contendo:
     - Inputs superiores para digitação ou visualização direta das datas **"De"** e **"Até"** (`DD/MM/AAAA`).
     - Navegação de mês/ano (`<` e `>`).
     - Grade interativa de seleção visual de intervalo (primeiro clique define início, segundo clique define fim, hover destaca o intervalo).
     - Botões de ação **"Cancelar"** e **"Aplicar Intervalo"** (atualizando os filtros e resetando a paginação para a página 1).

---

## 🏛️ 2. Registro de Decisões de Design (Architecture & Design Decisions)

| # | Tópico | Decisão Aprovada | Justificativa Técnica |
| :--- | :--- | :--- | :--- |
| **D1** | **Opções do Filtro de Meio** | 3 opções: "Todos os Meios", "Conta / Saldo" e "Cartão de Crédito". | O Open Finance categoriza transações bancárias em múltiplos canais técnicos (Pix, Ted, Doc, etc.), mas a intenção financeira do usuário é segregar o que impactou o saldo em conta vs compras de fatura de cartão. |
| **D2** | **Regras LINQ no Repositório** | `credit`: `t.BankDetails.Channel == TransactionChannel.CreditCard`.<br>`account`: `t.BankDetails.Channel != TransactionChannel.CreditCard`. | Garante que pagamentos de fatura debitados da conta corrente permaneçam na visão de "Conta / Saldo" como saídas reais de caixa, e a visão de cartão foque nas compras a crédito. |
| **D3** | **Mini Calendário (DateRangePicker)** | Componente reutilizável em `src/shared/components/DateRangePicker/` com Popover via Portal, seleção visual no calendário e botão explícito "Aplicar Intervalo". | Permite reutilização em futuras telas de relatórios/analíticos, evita re-renderizações e disparos desnecessários de query durante a seleção do range. |
| **D4** | **Layout da Barra de Filtros** | Grid fluido de 5 colunas no desktop (`grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3`). | Mantém todos os 5 controles principais (Busca + 4 Selects) perfeitamente alinhados e acessíveis sem rolagem desnecessária. |
| **D5** | **Impacto no Sumário Financeiro** | Entradas, Saídas, Balanço Líquido e Contagem recalculam dinamicamente com base nos filtros ativos; Saldo Instantâneo Consolidado e Faturas Abertas mantêm a posição patrimonial atual. | Precisão contábil e consistência na leitura do fluxo de caixa do período filtrado. |

---

## 🧠 3. Especificação do Backend (.NET 10 / C# 13)

### 3.1 `FinanceHub.TransactionAggregator`

#### 1. CQRS DTOs & Minimal API Parameters
- **`TransactionFilterDto.cs`**:
  ```csharp
  public record TransactionFilterDto(
      string UserId,
      int Page = 1,
      int PageSize = 20,
      DateTime? StartDate = null,
      DateTime? EndDate = null,
      string? InstitutionId = null,
      Guid? CategoryId = null,
      string? Type = null,
      string? Search = null,
      bool IncludeIgnoredInTotals = false,
      string? ChannelGroup = null);
  ```
- **`GetTransactionsParameters.cs`**:
  ```csharp
  public sealed record GetTransactionsParameters(
      string UserId,
      int? Page,
      int? PageSize,
      DateTime? StartDate,
      DateTime? EndDate,
      string? InstitutionId,
      Guid? CategoryId,
      string? Type,
      string? Search,
      bool? IncludeIgnoredInTotals,
      string? ChannelGroup);
  ```

#### 2. Repositório EF Core (`TransactionRepository.cs`)
Adicionar a cláusula condicional de filtro no método `QueryPagedByFilterAsync`:
```csharp
if (!string.IsNullOrWhiteSpace(filter.ChannelGroup))
{
    var channelGroupNormalized = filter.ChannelGroup.Trim().ToLowerInvariant();
    if (channelGroupNormalized == "credit" || channelGroupNormalized == "cartao")
    {
        query = query.Where(t => t.BankDetails.Channel == TransactionChannel.CreditCard);
    }
    else if (channelGroupNormalized == "account" || channelGroupNormalized == "saldo" || channelGroupNormalized == "conta")
    {
        query = query.Where(t => t.BankDetails.Channel != TransactionChannel.CreditCard);
    }
}
```
*Nota: Aplicar antes do `CountAsync`, do cálculo de sumário `rawTotalsQuery` e da paginação `itemsQuery`.*

---

### 3.2 `FinanceHub.ApiGateway`

#### 1. DTOs & Query Parameters
- **`GatewayTransactionFilterDto.cs`**:
  ```csharp
  public record GatewayTransactionFilterDto(
      string UserId,
      int Page = 1,
      int PageSize = 20,
      DateTime? StartDate = null,
      DateTime? EndDate = null,
      string? InstitutionId = null,
      Guid? CategoryId = null,
      string? Type = null,
      string? Search = null,
      bool IncludeIgnoredInTotals = false,
      string? ChannelGroup = null);
  ```
- **`TransactionGatewayQueryParameters.cs`**:
  ```csharp
  public sealed record TransactionGatewayQueryParameters(
      int? Page,
      int? PageSize,
      DateTime? StartDate,
      DateTime? EndDate,
      string? InstitutionId,
      Guid? CategoryId,
      string? Type,
      string? Search,
      bool? IncludeIgnoredInTotals,
      string? ChannelGroup);
  ```

#### 2. Client HTTP (`ITransactionAggregatorServiceClient.cs` & `TransactionAggregatorServiceClient.cs`)
Adicionar propagação do parâmetro `channelGroup` na montagem da query string:
```csharp
if (!string.IsNullOrWhiteSpace(filter.ChannelGroup))
{
    queryParams.Add($"channelGroup={Uri.EscapeDataString(filter.ChannelGroup)}");
}
```

#### 3. Endpoint Registration (`TransactionGatewayEndpoints.cs`)
Passar `query.ChannelGroup` para a instância de `GatewayTransactionFilterDto`.

---

## 🎨 4. Especificação do Frontend (`FinanceHub.Web`)

### 4.1 Tipos & Contratos (`src/features/transactions/types/transactions.types.ts`)

```typescript
export type DatePresetKey = 'current-month' | 'previous-month' | 'last-30' | 'current-year' | 'all-time' | 'custom';
export type ChannelGroupFilter = 'account' | 'credit';

export interface TransactionFilterParams {
  readonly page?: number;
  readonly pageSize?: number;
  readonly startDate?: string;
  readonly endDate?: string;
  readonly datePreset?: DatePresetKey;
  readonly channelGroup?: ChannelGroupFilter;
  readonly institutionId?: string;
  readonly categoryId?: string;
  readonly type?: string;
  readonly search?: string;
  readonly includeIgnoredInTotals?: boolean;
}
```

### 4.2 Novo Componente Reutilizável `DateRangePicker` (`src/shared/components/DateRangePicker/`)

#### Arquivos:
- `DateRangePicker.tsx`: Componente com Popover flutuante via `createPortal`.
- `DateRangePicker.test.tsx`: Suíte de testes unitários com Vitest + RTL.

#### Requisitos de UI/UX e Acessibilidade:
1. **Trigger**: Botão com ícone `Calendar`, rótulo dinâmico (se customizado: ex. `01/08/2026 - 25/08/2026`, se inativo: `Personalizado` ou `Selecionar Datas`).
2. **Inputs Superiores**:
   - Campo "De" (`DD/MM/AAAA`) e Campo "Até" (`DD/MM/AAAA`) com validação de formato e foco dinâmico.
3. **Navegação do Calendário**:
   - Cabeçalho exibindo `Mês Ano` (ex: "Agosto 2026") com botões `<` e `>` para navegar entre os meses.
4. **Grade de Dias (Dom a Sáb)**:
   - Dias fora do mês atual exibidos em tom sutil (`text-slate-300`).
   - **Hover & Range Highlight**: Ao passar o cursor após selecionar a data inicial, os dias intermediários recebem highlight suave (`bg-brand-light/50`).
   - **Data Inicial e Final**: Destaque com `bg-brand text-white font-bold rounded-lg`.
   - **Auto-swap**: Se a segunda data clicada for cronologicamente anterior à primeira, o componente inverte automaticamente `startDate` e `endDate`.
5. **Rodapé com Ações**:
   - Botão **"Cancelar" / "Limpar"**: Fecha o popover sem aplicar alterações.
   - Botão **"Aplicar Intervalo"**: Dispara `onApply({ startDate, endDate })` e fecha o popover.
6. **Tokens do Design System**:
   - Superfície: `bg-surface-card` (Off-white `#FAFCFB`). Pure white `#FFFFFF` é proibido.
   - Bordas: `border-border-subtle`.
   - Sombras: `shadow-elevated`.

### 4.3 Barra de Filtros (`TransactionsFilterBar.tsx`)

1. **Grid Superior de 5 Colunas**:
   ```tsx
   <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3 items-end">
     {/* 1. Busca textual */}
     {/* 2. Instituição (CustomSelect) */}
     {/* 3. Categoria (CategoryFilterSelect) */}
     {/* 4. Tipo de Lançamento (CustomSelect) */}
     {/* 5. Meio de Pagamento (CustomSelect) */}
   </div>
   ```
2. **Opções do Dropdown "Meio de Pagamento"**:
   - `''`: "Todos os Meios" (Ícone `ArrowLeftRight`)
   - `'account'`: "Conta / Saldo" (Ícone `Landmark`)
   - `'credit'`: "Cartão de Crédito" (Ícone `CreditCard`)
3. **Barra de Presets de Data**:
   - Adicionar o botão "Personalizado" integrado ao `DateRangePicker`.
   - Se `datePreset === 'custom'`, o botão exibe o intervalo selecionado ou o estado ativo `bg-brand-light text-brand-dark border-brand`.
4. **Badge de Filtros Ativos & Reset**:
   - `activeFiltersCount` atualizado para incluir `channelGroup` e `datePreset === 'custom'`.
   - Se `channelGroup` estiver ativo, exibe tag de filtro removível com `X`.
   - Se `datePreset === 'custom'`, exibe tag de período customizado com `X`.

---

## 🧪 5. Plano de Testes & TDD (Red -> Green -> Refactor)

### 5.1 Backend (.NET 10 / xUnit / FluentAssertions / NSubstitute)

1. **`TransactionEndpointsTests.cs` & `GetTransactionsQueryHandlerTests.cs`**:
   - Teste validando passagem do parâmetro `channelGroup` de `GetTransactionsParameters` para `TransactionFilterDto` e execução do handler.
2. **`TransactionAggregatorServiceClientTests.cs` (Gateway)**:
   - Teste validando que a requisição HTTP downstream inclui `&channelGroup=account` ou `&channelGroup=credit`.
3. **`TransactionRepositoryTests.cs` / Integration**:
   - Consulta com `channelGroup = "credit"` retorna apenas transações com `Channel == CreditCard`.
   - Consulta com `channelGroup = "account"` retorna apenas transações com `Channel != CreditCard`.
   - Consulta com `StartDate` e `EndDate` customizados filtra estritamente o range UTC esperado.

### 5.2 Frontend (Vitest / React Testing Library / user-event)

1. **`DateRangePicker.test.tsx`**:
   - Deve renderizar o trigger e abrir o popover ao clicar.
   - Deve permitir navegar entre meses (`<` e `>`).
   - Deve selecionar intervalo clicando na data inicial e na data final.
   - Deve permitir digitação manual nos inputs "De" e "Até".
   - Deve chamar `onApply` com as strings ISO/formatadas ao clicar em "Aplicar Intervalo".
   - Deve fechar o popover ao clicar fora ou em "Cancelar".
2. **`TransactionsFilterBar.test.tsx` / `TransactionsPage.test.tsx`**:
   - Deve renderizar o novo dropdown de "Meio de Pagamento" e disparar `onFilterChange({ channelGroup: 'credit', page: 1 })`.
   - Deve abrir o `DateRangePicker`, selecionar datas personalizadas e atualizar os parâmetros de busca.
   - Deve restaurar todos os filtros ao padrão ao clicar em "Restaurar Padrão".

---

## 📋 6. Checklist de Implementação

- [ ] **Fase 1: Backend - DTOs, Handlers & Repositório**
  - [ ] Atualizar `TransactionFilterDto.cs` e `GetTransactionsParameters.cs` em `FinanceHub.TransactionAggregator`.
  - [ ] Implementar cláusula LINQ de `channelGroup` em `TransactionRepository.cs`.
  - [ ] Atualizar `GatewayTransactionFilterDto.cs`, `TransactionGatewayQueryParameters.cs` e `TransactionAggregatorServiceClient.cs` em `FinanceHub.ApiGateway`.
  - [ ] Executar testes backend (xUnit) no ciclo TDD.
- [ ] **Fase 2: Frontend - Componente Compartilhado `DateRangePicker`**
  - [ ] Criar `src/shared/components/DateRangePicker/DateRangePicker.tsx`.
  - [ ] Implementar testes unitários em `src/shared/components/DateRangePicker/DateRangePicker.test.tsx`.
  - [ ] Validar conformidade visual com tokens e acessibilidade WAI-ARIA.
- [ ] **Fase 3: Frontend - Integração na Tela de Extrato**
  - [ ] Atualizar tipos em `src/features/transactions/types/transactions.types.ts`.
  - [ ] Atualizar `TransactionsFilterBar.tsx` (Grid de 5 colunas, dropdown Meio, integração do `DateRangePicker`, tags de filtros ativos).
  - [ ] Atualizar `TransactionsPage.tsx` e `transactionsApi.ts` para enviar `channelGroup`.
  - [ ] Atualizar suíte de testes em `TransactionsPage.test.tsx`.
- [ ] **Fase 4: Validação & Build**
  - [ ] Executar `dotnet test` em todas as suítes backend.
  - [ ] Executar `npm test` no frontend.
  - [ ] Verificar build com `npm run build`.

