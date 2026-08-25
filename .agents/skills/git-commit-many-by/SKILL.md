---
name: git-commit-many-by
description: Fractional and atomic Git commit generator skill supporting layered (Clean Architecture), feature (Vertical Slice), or microservice-level commit batching with Conventional Commits.
---

# 🧠 Skill: Git Commit Many (Atomic & Layered Commits)

Use esta habilidade quando o usuário acionar o slash command `/git-commit-many-by <strategy>` ou solicitar que as alterações pendentes no Git sejam fracionadas e commitadas de forma estruturada.

---

## ⚡ Trigger / Activation Command
```bash
/git-commit-many-by <strategy>
```
Onde `<strategy>` deve ser obrigatoriamente um dos seguintes valores:
- `layer` (Padrão: Commits por camada de Clean Architecture)
- `feature` (Commits por caso de uso / Vertical Slice)
- `service` (Commits por microserviço / projeto `.csproj`)

---

## 📋 Instruções de Execução por Estratégia

### 1. Estratégia `layer` (`/git-commit-many-by layer`)
Analise `git status` e `git diff` e agrupe os arquivos modificados realizando commits atômicos em ordem cronológica de dependência:

1. **Camada de Domínio (`Domain/`)**:
   - **Filtro**: Arquivos em `*/Domain/*` (Entities, Value Objects, Domain Events, Domain Exceptions).
   - **Mensagem**: `feat(domain): <descrição concisa das alterações de domínio>`
2. **Camada de Aplicação (`Application/`)**:
   - **Filtro**: Arquivos em `*/Application/*` (Commands, Queries, Handlers, Interfaces, DTOs).
   - **Mensagem**: `feat(application): <descrição concisa dos use cases e handlers>`
3. **Camada de Infraestrutura (`Infrastructure/` ou `Infra/`)**:
   - **Filtro**: Arquivos em `*/Infrastructure/*`, DbContext, Configurations, Migrations, Repositories, MassTransit.
   - **Mensagem**: `feat(infra): <descrição das alterações de infraestrutura e persistência>`
4. **Camada API (`API/` ou `Endpoints/`)**:
   - **Filtro**: Endpoints Minimal API, Controllers, Program.cs, DependencyInjection.cs da API.
   - **Mensagem**: `feat(api): <descrição dos endpoints e configurações de API>`
5. **Testes (`tests/` ou `*.Tests/`)**:
   - **Filtro**: Arquivos de teste unitário ou de integração.
   - **Mensagem**: `test(<scope>): <descrição das suítes de testes adicionadas ou refatoradas>`

---

### 2. Estratégia `feature` (`/git-commit-many-by feature`)
Analise o staging e identifique quais Slices / Features distintas foram alteradas. Para cada funcionalidade identificada:
- Agrupe todos os seus arquivos (do Domain até os Testes).
- Realize 1 commit completo por feature:
  - Exemplo: `feat(itau-consent): implement consent authorization lifecycle`
  - Exemplo: `feat(deduplication): implement transaction canonical deduplication`

---

### 3. Estratégia `service` (`/git-commit-many-by service`)
Agrupe as alterações por projeto/microserviço afetado:
- `PluggyIntegration`: `feat(pluggy): ...`
- `FileImporter`: `feat(fileimporter): ...`
- `TransactionAggregator`: `feat(aggregator): ...`
- `ApiGateway`: `feat(gateway): ...`
- `FinanceHub.Web`: `feat(web): ...`
- `Shared.*`: `feat(shared): ...`

---

## ⚠️ Regras e Restrições Finais
- **Conventional Commits**: Siga rigorosamente o padrão `<type>(<scope>): <summary>` em português ou inglês conforme convenção do projeto.
- **Verificação**: Antes de executar cada `git commit`, faça `git add <arquivos-especificos-da-etapa>` para garantir isolamento limpo. NUNCA faça `git add .` se a intenção for separar por camada/feature.
