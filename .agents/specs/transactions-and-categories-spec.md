# Spec — Tela de Transações & Sistema de Categorias (Tags)

- **Status**: Em Elaboração (Draft)
- **Data**: 2026-08-20
- **Branch**: `feature/transactions-screen`
- **Serviços Envolvidos**: `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`

---

## 1. Visão Geral & Escopo
Prover ao usuário uma experiência fluida e moderna de controle de fluxo de caixa, listando todas as transações agregadas (Open Finance e Importações manuais), permitindo busca e filtros avançados (por período, instituição, categoria e tipo), resumo do período, e a edição instantânea da categoria de cada transação no formato de **Tag / Badge clicável**.

---

## 2. Decisões Confirmadas
- **Modelo de Categoria**: Cada transação possui exatamente 1 categoria (comportamento de Tag visual interativa).
- **Escopo Integrado**: A especificação engloba tanto a tela de visualização/filtros quanto o catálogo e motor de categorização no backend.
- **Resolução Visual Inteligente de Categorias**: 
  - O Backend expõe `GET /api/v1/categories` retornando o catálogo canônico (`id`, `name`, `slug`, `iconKey`, `colorToken`).
  - As transações retornam apenas o `categoryId` (ou `slug`).
  - O Frontend mapeia visualmente as tags/badges usando Design Tokens semânticos e ícones vetoriais (`lucide-react`).
  - **Estratégia de Cache**: Sem camada de Redis neste momento (o Redis será projetado em uma feature/spec dedicada posterior). O catálogo é consultado de forma direta e padronizada.
- **Autossuficiência e Independência de Dados (Zero Dependência Externa/Licenças)**:
  - O FinanceHub não depende de planos pagos ou APIs de categorização externas.
  - O conector Pluggy envia `category` e `description` via payload padrão do Open Finance (obtido via `access_token` do usuário). O `PluggyCategoryMapper` traduz essas categorias para o vocabulário próprio do FinanceHub.
  - Para arquivos importados (.OFX, .CSV) e transações genéricas, o motor interno (`GlobalPatternCategoryResolver`) utiliza um dicionário brasileiro embutido de marcas/estabelecimentos (ex: iFood, Uber, Mercado Livre, Carrefour) com fallback para regras personalizadas do usuário (`user_category_rules`) e finalmente categoria `Outros`.
- **Experiência de Interação (UX)**:
  - **Tag Popover Inline**: Ao clicar na Tag de categoria na tabela, abre-se um Popover/Dropdown flutuante compacto com busca rápida de categorias, ícones e a opção de checkbox "Aplicar para transações similares futuras" (`CreateCustomRule`). Atualização instantânea com feedback visual optimista.

---

## 3. Pesquisa de Datasets e Padrões de Categorização no Brasil

Abaixo estão as fontes oficiais e referências de datasets disponíveis para categorização financeira no mercado brasileiro:

1. **Pluggy Categories & Connector Taxonomy (Open Finance Brasil)**:
   - Taxonomia padrão de categorias e subcategorias utilizadas por agregadores de Open Finance no Brasil.
   - Link de documentação: [Pluggy API Documentation - Categories](https://docs.pluggy.ai/docs/categories)
   - Guia de conectores e dados bancários: [Pluggy Connectors & Banking Data](https://docs.pluggy.ai/docs/connectors)

2. **Tabela MCC (Merchant Category Code) / Adquirentes & Bacen**:
   - Tabela internacional padronizada ISO 18245 usada por Visa, Mastercard, Elo, Cielo e Stone no Brasil para classificar tipo de estabelecimento.
   - Referência completa MCC (GitHub Gist / Repositório Open Source): [Merchant Category Codes (MCC) JSON/CSV Dataset](https://github.com/greggles/mcc-codes)
   - Especificação Mastercard/Visa MCC: [Citibank / Bacen MCC Reference Guide](https://www.citibank.com/tts/sa/flippingbook/2021/Commercial-Cards-Merchant-Category-Codes/files/assets/basic-html/page-1.html)

3. **Tabela CNAE (Classificação Nacional de Atividades Econômicas - IBGE/Receita Federal)**:
   - Dataset oficial de atividades de todas as empresas e estabelecimentos no Brasil (CNPJs).
   - Link oficial IBGE: [IBGE Concla - Estrutura CNAE](https://cnae.ibge.gov.br/)
   - API de dados abertos de CNPJs e estabelecimentos no Brasil: [Minha Receita API (Open Source CNPJ)](https://minhareceita.org/) / [BrasilAPI](https://brasilapi.com.br/docs#tag/CNPJ)

4. **Taxonomia Padrão de Finanças Pessoais (Anbima / Open Finance Brasil)**:
   - Guia de estrutura de finanças pessoais (Alimentação, Transporte, Moradia, Saúde, Lazer, Educação, Serviços Financeiros, Receitas).
   - Referência de estrutura Open Finance Brasil: [Open Finance Brasil - Especificações de Dados](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/overview)

---

## 4. Catálogo Canônico de Categorias (Soberania do FinanceHub)

O **FinanceHub** é a autoridade única e soberana sobre as categorias. Categorias recebidas via Pluggy ou outros conectores são mapeadas durante a ingestão para a taxonomia própria do FinanceHub através de um conversor (`PluggyCategoryMapper`).

### 4.1. Persistência & Modelo de Dados
- **Tabela**: `categories` gerenciada via EF Core no banco de dados do `TransactionAggregator`.
- **Entidade**: `Category` (Aggregate Root / Rich Domain Model) contendo `Id`, `Name`, `Slug`, `ParentCategoryId`, `IconKey`, `ColorToken`, `IsSystemDefault`, `IsActive`, `CreatedAtUtc`.
- **Seed Inicial**: Executado via migration EF Core ou data seeder na inicialização do serviço, garantindo a carga das 10 categorias principais e 40 subcategorias.
- **Extensibilidade**: Prepara a arquitetura para permitir no futuro a criação e personalização de categorias pelo próprio usuário.

### 4.2. 10 Categorias Principais (Palavra Única):
1. **Alimentação** (`food`): Supermercado, Restaurante, Delivery, Padaria.
2. **Transporte** (`transport`): Aplicativos, Combustível, Passagens, Estacionamento, Manutenção.
3. **Moradia** (`housing`): Aluguel, Condomínio, Energia, Água, Gás, Internet.
4. **Saúde** (`health`): Farmácia, Consultas, Plano, Academia.
5. **Lazer** (`leisure`): Streaming, Viagens, Eventos, Hobbies.
6. **Compras** (`shopping`): Vestuário, Eletrônicos, Cosméticos, Decoração.
7. **Educação** (`education`): Cursos, Livros, Mensalidades.
8. **Finanças** (`finance`): Tarifas, Impostos, Juros, Seguros.
9. **Receitas** (`income`): Salário, Rendimentos, Reembolsos, Freelance.
10. **Outros** (`others`): Ajustes, Transferências, Diversos.

---

## 5. Contratos de API & Endpoints

### 5.1. TransactionAggregator & ApiGateway
- `GET /api/v1/categories`: Retorna o catálogo próprio de categorias e subcategorias ativas no FinanceHub (`id`, `name`, `slug`, `parentId`, `iconKey`, `colorToken`).
- `GET /api/v1/transactions`:
  - **Query Params**: `page`, `pageSize`, `startDate`, `endDate`, `institutionId`, `categoryId`, `type`, `search`.
  - **Response**: `{ items: TransactionDto[], summary: TransactionSummaryDto, page, pageSize, totalItems, totalPages }`.
- `PATCH /api/v1/transactions/{id}/categorize`:
  - **Body**: `{ userId: string, newCategoryId: Guid, createCustomRule: bool }`.
  - **Response**: `204 NoContent`.

---

## 6. Telas e Componentes Frontend (`FinanceHub.Web`)
1. **Página Principal (`TransactionsPage.tsx`)**:
   - Cabeçalho com título limpo e ações de exportação.
   - Cards de Resumo do Período (`TransactionsSummaryCards`): Entradas, Saídas e Saldo Líquido.
   - Barra de Filtros (`TransactionsFilterBar`): Busca livre, Período, Instituição, Categoria e Tipo.
   - Tabela Agrupada de Transações (`TransactionsTable`):
     - Coluna de Data agrupada cronologicamente.
     - Descrição com ícone do banco e merchant.
     - Tag de Categoria com Popover Interativo (`CategoryTagPopover`).
     - Valor formatado em BRL com cores semânticas.
   - **Paginação Numerada Clássica (`TransactionsPagination`)**:
     - Botões de página (1, 2, 3... Anterior / Próximo).
     - Seletor de itens por página (10, 20, 50, 100).
     - Contador de total de itens exibidos (ex: "Exibindo 1–20 de 154 transações").

---

## 7. Critérios de Aceitação & Validação

1. **Backend (TDD & Domain Rules)**:
   - Migration/Seed cria as 10 categorias e 40 subcategorias soberanas do FinanceHub.
   - Endpoint `GET /api/v1/categories` retorna a lista completa em < 50ms.
   - Endpoint `GET /api/v1/transactions` com filtros suporta paginação numerada e retorna `summary` com totais calculados.
   - Endpoint `PATCH /api/v1/transactions/{id}/categorize` atualiza a categoria e grava regra em `user_category_rules` quando requisitado.
   - Cobertura de testes unitários e de integração >= 80%.

2. **Frontend (Design & A11y Rules)**:
   - Zero emojis utilizados na interface (apenas ícones `lucide-react`).
   - Cores e cartões respeitam a paleta off-white (`#FAFCFB` / `bg-surface-card`) sem `#FFFFFF` puro.
   - Sem caractere `&` em títulos, seções e categorias.
   - Categorias renderizadas como tags/badges coloridas com menu Popover inline para alteração com 1 clique.
   - Erros HTTP tratados conforme RFC 7807 com toasts descritivos via `Sonner`.

---

## 8. Plano de Execução em Fases

- **Fase 1 (Backend - Database & Category Domain)**: Criar entidade `Category`, migrations/seed das 10 categorias e 40 subcategorias, endpoint `GET /api/v1/categories`.
- **Fase 2 (Backend - Transactions Query & Filtering)**: Atualizar `GetTransactionsQuery` com suporte a filtros, paginação clássica e sumário do período.
- **Fase 3 (Frontend - Types, API & Categories Hook)**: Criar tipos TypeScript, chamadas Axios e hook de categorias.
- **Fase 4 (Frontend - UI Components & Popover)**: Implementar `CategoryTag`, `CategoryTagPopover`, `TransactionsFilterBar`, `TransactionsSummaryCards` e `TransactionsPagination`.
- **Fase 5 (Frontend - Integration & Page Assembly)**: Montar a `TransactionsPage`, integrar rotas e realizar testes E2E/Vitest.

