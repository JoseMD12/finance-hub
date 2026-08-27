# Especificação Técnica: Sistema de Notas/Observações Inline na Tabela de Transações

**Documento:** `.agents/specs/transaction-details-panel-spec.md`  
**Status:** 🟢 `Aprovada para Implementação`  
**Data:** 25/08/2026  
**Branch:** `feature/transaction-details-panel`  
**Serviços Envolvidos:** `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`  

---

## 🎯 1. Objetivos & Visão Geral

Esta especificação substitui o Drawer lateral por uma solução **Inline e Enxuta** para gerenciamento de observações no **Livro Razão de Transações**. A leitura e edição de notas passa a ocorrer diretamente na tabela de transações via **Popover Flutuante**, ativado por um ícone de mensagem posicionado na coluna de **Ações**.

---

## 🏛️ 2. Arquitetura & Componentes

### 🧠 2.1 Backend (.NET 10 & API Gateway)
- **Mantido 100% Intacto**:
  - Propriedade `Notes` e método `UpdateNotes(string? notes)` na entidade `CanonicalTransaction`.
  - Mapeamento EF Core e Migration `AddNotesColumnToCanonicalTransactions`.
  - Command `UpdateTransactionNotesCommand` e Handler `UpdateTransactionNotesCommandHandler`.
  - Endpoints `PATCH /api/v1/transactions/{id}/notes` e `PATCH /api/v1/gateway/transactions/{id}/notes`.

---

### 🎨 2.2 Frontend — Web SPA (`FinanceHub.Web`)

1. **Remoção do Drawer**:
   - Eliminar `TransactionDetailsDrawer.tsx` e o acionamento por clique na linha inteira (`tr row click`).
2. **Ícone na Coluna de Ações (`TransactionsTable.tsx`)**:
   - Posicionado na célula de Ações ao lado do botão de 3 pontinhos (`TransactionActionDropdown`).
   - **Com Nota**: Ícone de balão preenchido/destacado (`text-purple-600 bg-purple-50 border-purple-200 shadow-2xs`), exibindo tooltip com o texto da nota.
   - **Sem Nota**: Ícone discreto em cinza/transparente (`text-slate-300 hover:text-purple-600 hover:bg-purple-50`), permitindo inclusão rápida com 1 clique.
3. **Popover Inline de Edição (`TransactionNotePopover.tsx`)**:
   - Popover flutuante com suporte a `dialog` nativo/portal.
   - Textarea com limite de 500 caracteres, autosave no blur/salvar, botão de limpeza e feedback via Sonner Toast.
4. **React Query Hook (`useUpdateTransactionNotesMutation.ts`)**:
   - Atualização otimista e invalidação da chave `transactionsKeys.all`.

---

## ✅ 3. Critérios de Aceite & Conclusão

- [ ] A coluna de Ações exibe o ícone de mensagem para todas as linhas da tabela.
- [ ] O ícone fica destacado/colorido quando a transação já possui observação cadastrada.
- [ ] Ao clicar no ícone, abre o Popover flutuante de notas sem abrir drawers ou modais.
- [ ] Salvar ou remover a nota atualiza instantaneamente a UI e persiste no banco de dados PostgreSQL.
