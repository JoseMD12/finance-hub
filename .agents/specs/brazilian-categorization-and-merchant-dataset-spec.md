# FinanceHub — Brazilian Categorization & Merchant Dataset Specification

**Status**: 📋 SPECIFICATION READY (Planejamento Completo)  
**Target Microservice**: `FinanceHub.TransactionAggregator` & `FinanceHub.Web`  
**Created**: 2026-08-21  

---

## 🎯 1. Objetivo & Contexto
Substituir o seed estático/hardcoded em C# (`CategorySeedData.cs`) por uma arquitetura escalável e extensível baseada em dados reais do ecossistema financeiro brasileiro.
O sistema reconhece estabelecimentos de alto volume no Brasil (ex: iFood, Zé Delivery, Uber, Drogasil, Carrefour, Pão de Açúcar, Netflix, etc.) e categoriza transações automaticamente com regras determinísticas, versionadas e extensíveis, **preservando integralmente o texto bruto original e o texto sanitizado**.

---

## 🏛️ 2. Arquitetura dos Datasets & Armazenamento

```text
src/Services/TransactionAggregator/FinanceHub.TransactionAggregator.Infrastructure/
└── Persistence/
    ├── Datasets/
    │   ├── categories.default.json         <-- Taxonomia completa, subcategorias, ícones Lucide, cores
    │   └── merchants.brazil.json          <-- Top 150+ marcas BR, padrões de extrato (keywords/regex)
    ├── Schemas/
    │   ├── categories.schema.json         <-- Validação de contrato JSON Schema
    │   └── merchants.schema.json          <-- Validação de integridade referencial
    └── DbInitializer.cs                    <-- Carregador idempotente (UPSERT) no startup
```

---

## 🌐 3. Estratégia de Agregação de Datasets & Fontes de Dados

Para garantir precisão, reprodutibilidade e conformidade com o mercado financeiro brasileiro, a montagem do dataset é dividida em **4 camadas de agregação**:

### Camada 1: Taxonomia Oficial Open Finance Brasil & Pluggy Categories
- **Fonte Primária**: [Open Finance Brasil - API de Dados de Transações](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/17379201/Especifica+es+Open+Finance+Brasil) & [Pluggy Categories Catalog](https://docs.pluggy.ai/docs/categories).
- **Dados Extraídos**:
  - Macro e microcategorias padronizadas (Alimentação, Transporte, Moradia, Saúde, Lazer, Compras, Educação, Serviços Financeiros, Renda, Outros).
  - Identificadores GUIDs determinísticos (`CategorySeedData` compatíveis).
  - Metadados visuais de interface (ícones Lucide vetorizados, paletas de tokens de cores `@theme` sem branco puro `#FFFFFF`).

### Camada 2: Classificação CNAE (IBGE / Receita Federal)
- **Fonte Primária**: [Portal CNAE - IBGE](https://cnae.ibge.gov.br/) e Base Pública de CNPJs em [dados.gov.br](https://dados.gov.br).
- **Dados Extraídos**:
  - Código CNAE de atividade econômica (ex: `56.11-2 - Restaurantes e bares`, `47.11-3 - Supermercados`, `49.23-8 - Transporte por táxi ou aplicativo`).
  - Mapeamento direto de subclasses CNAE para as categorias canônicas do FinanceHub.

### Camada 3: Catálogo de Padrões de Adquirentes e Gateways Brasileiros
- **Fonte Primária**: Base de engenharia reversa de adquirentes e gateways nacionais (Cielo, Rede, Stone, PagSeguro/PagBank, Mercado Pago, Getnet, SumUp) e repositórios de extratos bancários brasileiros.
- **Dados Extraídos**:
  - Prefixos de adquirentes e meios de pagamento: `PAG*`, `PAGARME*`, `STONE*`, `CIELO*`, `REDE*`, `GETNET*`, `MP*`, `COMPRA CARTAO`, `PIX TRANSF`, `DEB AUTO`, `DL*GOOGLE`, `APPLE.COM/BILL`.
  - Padrões de conciliação por regex e keywords para top 150+ empresas de consumo frequente no Brasil.

### Camada 4: Pipeline de Validação e Integridade Referencial
- **JSON Schema**: Validação estrita durante a compilação/testes (`dotnet test`) garantindo que:
  - Todo `merchant` aponte para um `categoryId` e `subcategoryId` válidos existentes em `categories.default.json`.
  - Não existam padrões de regex ou keywords duplicados ou conflitantes.

---

## 📋 4. Decisões Arquiteturais Consolidadas

### Decisão 1: Persistência & Fonte da Verdade
- **Definição**: Datasets padrão versionados em arquivos JSON (`categories.default.json` e `merchants.brazil.json`).
- **Sincronização**: Na inicialização do serviço (`DbInitializer`), os arquivos JSON são lidos e persistidos no PostgreSQL via `UPSERT` (inserindo novas categorias e regras faltantes de forma idempotente, sem sobrescrever customizações do usuário).

### Decisão 2: Motor de Precedência de Categorização (Cadeia de Responsabilidade)
1. **Regra Personalizada do Usuário (`CategorizationSource.UserRule`)**: Prioridade máxima se o usuário tiver criado uma regra específica.
2. **Regras Globais de Empresas Brasileiras (`CategorizationSource.GlobalRule`)**: Casamento de padrões contra o dataset nacional de merchants (ex: `"PAG*IFOOD"`, `"UBER *TRIP"`, `"DROGASIL"`).
3. **Open Finance / Pluggy AI (`CategorizationSource.GlobalRule` ou `Fallback`)**: Mapeamento da categoria sugerida pelo conector bancário.
4. **Fallback Padrão (`CategorizationSource.Fallback`)**: Categoria "Outros" (`11111111-1111-1111-1111-111111111110`).

### Decisão 3: Engine de Normalização e Preservação de Textos
- **Preservação de Dados**: Toda transação armazena o texto bruto original do extrato (`OriginalText`) e o texto limpo para visualização e busca (`CleanText`) dentro de `SanitizedDescription`.
- **Sanitização de Extratos Bancários**: Remoção de ruídos de processadoras/adquirentes brasileiras (`PAG*`, `STONE*`, `CIELO*`, `MP*`, `PIX TRANSF`, etc.).
- **Normalização NFD**: Remoção de acentos e unificação para caixa alta no algoritmo de matching.
- **Match Determinístico**: Verificação por lista de keywords (`contains`) e expressões regulares para marcas populares brasileiras.

### Decisão 4: Recategorização pelo Usuário & Aplicação Retroativa Opcional
- Ao alterar a categoria de uma transação na interface:
  1. A transação atual é atualizada imediatamente (`IsManuallyCategorized = true`, `CategorizationSource = UserManual`).
  2. Cria-se uma `UserRule` para classificar automaticamente transações futuras similares.
  3. **Atualização Retroativa Condicional**: As transações passadas do mesmo estabelecimento **só são atualizadas em lote se o usuário explicitamente marcar a opção/checkbox no modal** (`applyToPastTransactions = true`).

---

## 🛠️ 5. Plano de Implementação (Fases)

### Fase 1: Datasets JSON, Schemas & Sincronização
- [ ] Criar `categories.default.json` com a taxonomia completa (Alimentação, Transporte, Moradia, Saúde, Lazer, Compras, Educação, Finanças, Renda, Outros) e metadados visuais (ícones Lucide, cores de token).
- [ ] Criar `merchants.brazil.json` com top 150+ marcas e empresas brasileiras e regras de correspondência.
- [ ] Criar `DatasetLoader` e integrar ao `DbInitializer.cs` para sincronização idempotente (`UPSERT`) na inicialização do serviço.

### Fase 2: Merchant Categorization Engine
- [ ] Criar entidade/tabela de regras `CategorizationRule` (`Id`, `UserId?`, `Pattern`, `CategoryId`, `Priority`, `IsRegex`, `CreatedAtUtc`).
- [ ] Implementar `ICategorizationEngine` no `TransactionAggregator` executando a cadeia de precedência (UserRule -> GlobalMerchantRule -> Pluggy -> Fallback).
- [ ] Plugar o `ICategorizationEngine` nos consumidores de ingestão (`TransactionIngestedConsumer`, `InvoiceItemIngestedConsumer`).

### Fase 3: Endpoint de Recategorização com Retroatividade
- [ ] Atualizar endpoint `PATCH /api/v1/transactions/{id}/category` para aceitar `applyToFuture` e `applyToPastTransactions`.
- [ ] Adicionar checkbox na interface (`CategoryTagPopover.tsx`) para o usuário decidir se quer aplicar a todas as transações passadas daquele estabelecimento.
