# Especificação Técnica: Painel de Detalhes da Transação (Drawer Lateral) & Notas do Usuário

**Documento:** `.agents/specs/transaction-details-panel-spec.md`  
**Status:** 🟢 `Aprovada para Implementação`  
**Data:** 25/08/2026  
**Branch:** `feature/transaction-details-panel`  
**Serviços Envolvidos:** `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`  

---

## 🎯 1. Objetivos & Visão Geral

Esta especificação define o refinamento da experiência do usuário no **Livro Razão de Transações**, introduzindo um **Slide-over Drawer Lateral** para inspeção detalhada de lançamentos financeiros, exibição transparente de metadados do **Open Finance Brasil**, visualização de hash de deduplicação/pareamento e um sistema completo de **Observações/Notas do Usuário (End-to-End)**.

---

## 🏛️ 2. Requisitos Arquiteturais & Modificações por Serviço

### 🧠 2.1 Backend — Domain & Aggregator (`FinanceHub.TransactionAggregator`)

1. **Rich Domain Model (`CanonicalTransaction.cs`)**:
   - Propriedade de leitura: `public string? Notes { get; private set; }` (limite máximo de 500 caracteres).
   - Método de domínio: `public void UpdateNotes(string? notes)` que valida o comprimento máximo e invoca a sanitização/trim.
2. **Persistence & EF Core**:
   - Mapeamento no `CanonicalTransactionConfiguration.cs`: Coluna `notes` do tipo `text` / `varchar(500)` opcional.
   - Migration do EF Core: `AddNotesColumnToCanonicalTransactions`.
3. **CQRS Application Layer**:
   - Interface: `IUpdateTransactionNotesCommandHandler.cs`
   - Command: `UpdateTransactionNotesCommand(Guid TransactionId, string UserId, string? Notes)`
   - Handler: `UpdateTransactionNotesCommandHandler.cs`
4. **Minimal API Endpoints**:
   - Endpoint em `TransactionEndpoints.cs`: `PATCH /api/v1/transactions/{id:guid}/notes`
   - RFC 7807 Exception handling e `200 OK` / `204 No Content` / `404 Not Found`.

---

### 🌐 2.2 Backend — BFF API Gateway (`FinanceHub.ApiGateway`)

1. **DTO Update (`GatewayTransactionDto.cs`)**:
   - Incluir campo `notes?: string | null` no DTO retornado para o frontend.
2. **Typed Client (`ITransactionAggregatorServiceClient.cs`)**:
   - Método `UpdateTransactionNotesAsync(Guid id, string? notes, CancellationToken cancellationToken)`.
3. **Gateway Endpoints (`TransactionGatewayEndpoints.cs`)**:
   - Mapear `PATCH /api/v1/gateway/transactions/{id:guid}/notes` encaminhando ao Aggregator downstream com propagação do `UserId` e `traceparent`.

---

### 🎨 2.3 Frontend — Web SPA (`FinanceHub.Web`)

1. **Tipagem (`transactions.types.ts`)**:
   - Adicionar `readonly notes?: string | null;` na interface `TransactionDto`.
2. **Drawer Component (`TransactionDetailsDrawer.tsx`)**:
   - Slide-over lateral deslizando da direita (`animate-in slide-in-from-right duration-200`) com backdrop blur.
   - Exibição de cabeçalho com Merchant, valor formatado, badges de instituição bancária e status (`Fatura`, `Neutro`).
   - Seção de Metadados Open Finance: BankTransactionId, Meio de Pagamento, Conta Mascarada, Data/Hora exata (BR + UTC).
   - Seção de Deduplicação & Pareamento: Hash SHA-256 e link/card de atalho para transação pareada quando existir `pairedTransactionId`.
   - Seção de Origem da Categorização: Badge com fonte (`Usuário`, `Dataset Brasil`, `Padrão`).
   - Painel de Ações Rápidas: Alternar Categoria, Alternar Neutralidade, Alternar Fatura.
   - Área Edição de Notas do Usuário: Textarea com autosave/blur e feedback visual toast (Sonner).
3. **Tabela de Transações (`TransactionsTable.tsx`)**:
   - Disparo do Drawer ao clicar na linha (`tr row click`), exceto interações em botões internos.
   - Exibição de um ícone indicador de notas (💬 `MessageSquare` / `FileText`) na célula de descrição quando a transação possuir notas cadastradas.
4. **React Query Hook (`useUpdateTransactionNotesMutation.ts`)**:
   - Hook de mutação utilizando `transactionsKeys.all` para invalidação automática de cache do TanStack Query.

---

## 🧪 3. Plano de Testes & Cobertura

1. **Testes Unitários de Domínio**:
   - Testar `CanonicalTransaction.UpdateNotes()` com notas válidas, nulas, vazias e acima de 500 caracteres (lançando exceção de domínio).
2. **Testes Unitários de Aplicação**:
   - Testar `UpdateTransactionNotesCommandHandler` com repositório mockado.
3. **Testes de Frontend (Vitest)**:
   - Testar renderização do `TransactionDetailsDrawer` e interações de fechamento/salvamento.

---

## ✅ 4. Critérios de Aceite & Conclusão

- [ ] Todas as 180+ suítes de testes backend continuam passando.
- [ ] Build e Lint frontend executam com 0 erros.
- [ ] O Drawer abre suavemente ao clicar na linha da tabela de transações.
- [ ] A adição/edição de nota atualiza instantaneamente o ícone indicador na tabela e é refletida no banco de dados PostgreSQL.
