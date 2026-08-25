# Plano Arquitetural: Catálogo Dinâmico de Instituições Financeiras via Backend

Este documento estabelece o plano de arquitetura e implementação para transformar a lista de instituições financeiras (bancos e conectores) em uma **fonte única de verdade fornecida pelo Backend**, consumida dinamicamente pelo Frontend via TanStack Query.

---

## 🎯 1. Motivação e Objetivos

Atualmente, o frontend (`FinanceHub.Web`) mantém um arquivo estático `src/shared/constants/institutions.ts` com dados manuais de instituições (nomes, IDs e URLs de logos).

### Problemas do modelo atual:
1. **Acoplamento**: Adicionar ou desativar um conector (ex: novo banco via Open Finance ou leitor de OFX) exige novo build e deploy do frontend.
2. **Duplicação de Domínio**: O backend (`FinanceHub.PluggyIntegration` e `FinanceHub.FileImporter`) já conhece as capacidades de cada conector, mas essas regras estão replicadas no cliente.
3. **Segurança e Resiliência de Assets**: URLs de imagens externas (Wikimedia/SimpleIcons) podem sofrer quebras de link ou bloqueios CORS/CSP.

### Metas:
- Centralizar o catálogo oficial de instituições no **Backend**.
- Expor endpoint RESTful documentado via RFC 7807 e OpenAPI no API Gateway.
- Implementar cache de longa duração no frontend via TanStack Query (`staleTime: 1 hour`).
- Eliminar dependências de dados estáticos hardcoded no React.

---

## 🏛️ 2. Arquitetura da Solução

```
[ Frontend (React 19) ]
       │
       ▼ (GET /api/v1/institutions)
[ FinanceHub.ApiGateway ] (BFF Routing + Rate Limit + Caching)
       │
       ▼ (gRPC / HTTP Interno)
[ FinanceHub.PluggyIntegration ] (Query: GetSupportedInstitutionsQuery)
       │
       ▼
[ Repositório / Dataset de Conectores Ativos ] (Itaú, Inter, Mercado Pago, etc.)
```

---

## 📋 3. Contrato da API (`GET /api/v1/institutions`)

### DTO de Resposta: `InstitutionDto`
```json
[
  {
    "id": "itau",
    "name": "Itaú Unibanco",
    "code": "341",
    "logoUrl": "/assets/institutions/itau.svg",
    "primaryColorHex": "#EC7000",
    "brandTone": "amber",
    "isConnectable": true,
    "supportedCapabilities": ["OpenFinance", "FileImport"]
  },
  {
    "id": "inter",
    "name": "Banco Inter",
    "code": "077",
    "logoUrl": "/assets/institutions/inter.svg",
    "primaryColorHex": "#FF7A00",
    "brandTone": "orange",
    "isConnectable": true,
    "supportedCapabilities": ["OpenFinance", "FileImport"]
  },
  {
    "id": "mercadopago",
    "name": "Mercado Pago",
    "code": "323",
    "logoUrl": "/assets/institutions/mercadopago.svg",
    "primaryColorHex": "#009EE3",
    "brandTone": "sky",
    "isConnectable": true,
    "supportedCapabilities": ["OpenFinance", "FileImport"]
  }
]
```

---

## 🛠️ 4. Roteiro de Implementação (Etapas Futuras)

### Fase 1: Backend (.NET 10 Microservices)
1. **`FinanceHub.PluggyIntegration.Application`**:
   - Criar `InstitutionDto.cs`.
   - Criar Query `GetSupportedInstitutionsQuery.cs`.
   - Criar Interface `IGetSupportedInstitutionsQueryHandler.cs` e Implementação `GetSupportedInstitutionsQueryHandler.cs` (arquivos separados conforme Regra 13).
2. **`FinanceHub.PluggyIntegration.Api`**:
   - Centralizar rota no endpoint extension class `InstitutionEndpoints.cs` (`MapInstitutionEndpoints()`).
3. **`FinanceHub.ApiGateway`**:
   - Adicionar rota proxy para `/api/v1/institutions` no YARP / BFF Gateway.
4. **Testes Backend**:
   - Testes unitários para o Handler com FluentAssertions e NSubstitute.

### Fase 2: Frontend (React 19 + TanStack Query)
1. **API Client & Keys Factory**:
   - `src/features/institutions/api/institutionsApi.ts`
   - `src/features/institutions/api/institutionsKeys.ts`
   - `src/features/institutions/hooks/useInstitutionsQuery.ts`
2. **Refatoração dos Componentes Consumidores**:
   - `TransactionsFilterBar.tsx`: Popular `institutionOptions` dinamicamente a partir de `useInstitutionsQuery()`.
   - `BankLogoTag.tsx`: Renderizar logo dinâmico ou fallback seguro.
   - `ConnectionsPage.tsx`: Renderizar grid de bancos conectáveis via query.
3. **Testes Frontend**:
   - Atualizar mocks do MSW e suítes Vitest.

---

## 📌 Status
- **Estado**: Planejado / Especificado
- **Ação Imediata**: Aguardando acionamento via comando `/scaffold-slice` ou sprint dedicada.
