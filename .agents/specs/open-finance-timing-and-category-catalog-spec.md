# Especificação Técnica: Prazos de Liquidação Open Finance e Resolução do Catálogo de Categorias

**Documento:** `open-finance-timing-and-category-catalog-spec.md`  
**Status:** PROPOSAL / REVIEW  
**Autor:** Antigravity AI & Jose Henrique  
**Data:** 2026-08-23  

---

## 1. 🎯 Diagnóstico da Causa Raiz

### 1.1 Por que o frontend exibia "Não categorizado" se as transações estavam com CategoryId no banco?
1. As transações ingeridas são devidamente classificadas pelo `CategoryResolverPipeline` (ex: `Zaffari` $\rightarrow$ `Supermercado`, `Fernanda Climus` $\rightarrow$ `Médicos`, `Amazon` $\rightarrow$ `Streaming`).
2. O `CanonicalTransaction.CategoryId` é gravado no PostgreSQL.
3. No entanto, o frontend (`TransactionsTable` $\rightarrow$ `CategoryTagPopover`) precisa cruzar o `t.categoryId` (GUID) com a lista de categorias retornada por `GET /api/v1/gateway/transactions/categories` para renderizar o nome, ícone e cor da tag.
4. No `TransactionGatewayEndpoints.cs` do `ApiGateway`, o grupo `/api/v1/gateway/transactions` estava configurado com `.RequireAuthorization()`, bloqueando o endpoint público de catálogo `/categories` com `401 Unauthorized`.
5. Com a falha 401, a lista de categorias ficava vazia (`[]`), fazendo com que o `CategoryTag` exibisse o fallback `"Não categorizado"`.

### 1.2 Prazos de Compensação e Visibilidade no Open Finance
* **Pix e Contas Correntes**: Compensação instantânea e reflexo na API do Open Finance em 1 a 5 minutos.
* **Cartão de Crédito (D+1 / D+2)**: O app do banco reduz o limite imediatamente como "Autorização Pendente". O feed do Open Finance regulado pelo Banco Central só disponibiliza a transação após a liquidação formal da adquirente/bandeira (levando de poucas horas até 24h-48h).

---

## 2. 📋 Plano de Implementação em Camadas

### Camada 1: BFF (`FinanceHub.ApiGateway`)
* Liberar a rota pública de catálogo de categorias:
  ```csharp
  group.MapGet("/categories", async (ITransactionAggregatorServiceClient transactionClient, CancellationToken ct) =>
  {
      var categories = await transactionClient.GetCategoriesAsync(ct);
      return Results.Ok(categories);
  })
  .WithName("GetGatewayCategories")
  .AllowAnonymous()
  .Produces<IEnumerable<GatewayCategoryDto>>(StatusCodes.Status200OK);
  ```

### Camada 2: Frontend (`FinanceHub.Web`)
* **Componente `SyncInfoModal.tsx`**:
  * Modal informativo com design system limpo (off-white `#FAFCFB`, sem pure white, sem emojis, ícones Lucide `Clock`, `CreditCard`, `ArrowRightLeft`, `ShieldCheck`).
  * Explica os prazos de compensação de Pix vs Cartão de Crédito e integridade de dados.
* **Componente `TransactionsSummaryCards.tsx`**:
  * Adicionar botão de ajuda `Info` no Card 1 ("Saldo em Contas") abrindo o `SyncInfoModal`.
* **Componente `SyncTimingInfoCard.tsx` / `ConnectionsPage.tsx`**:
  * Seção retrátil e informativa na tela de Conexões bancárias.

---

## 3. 🧪 Plano de Testes e Validação
1. Validar endpoint de categorias com `curl` (garantindo retorno 200 OK sem 401).
2. Executar suíte de testes unitários do frontend (`npm test -- --run`).
3. Executar suíte de testes backend (`dotnet test`).
4. Reconstruir containers e verificar renderização das tags de categorias e do modal informativo no navegador.
