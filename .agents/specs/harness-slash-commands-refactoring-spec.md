# 📐 Especificação de Refatoração do Harness & Slash Commands

**Status**: Approved / Draft  
**Branch Target**: `feat/harness-slash-commands-refactoring`  
**Escopo**: `FinanceHub` & `AutoReparos` (.NET 10 Microservices)  
**Localização**: `.agents/specs/harness-slash-commands-refactoring-spec.md`

---

## 🎯 1. Objetivo Principal

Melhorar a organização e previsibilidade do Harness de IA (`.agents`), eliminando desvios e perdas de foco da IA (~30% dos cenários) por meio da introdução de **Slash Commands** explícitos e regras reforçadas de desenvolvimento (.NET 10 / C# 13).

---

## ⚡ 2. Matriz de Slash Commands e Triggers de Habilidades

| Slash Command | Habilidade Subjacente (`.agents/skills/`) | Ação Esperada da IA |
| :--- | :--- | :--- |
| `/spec-feature` | `spec-feature` | Conduz o processo iterativo de especificação em **Plan Mode** fazendo 1 pergunta por vez. |
| `/scaffold-slice <Service> <UseCase>` | `scaffold-slice` | Scaffold de Command, Query, Handlers (Interface em arquivo `.cs` separado da Classe) e Endpoints Minimal API. |
| `/run-tdd` | `run-tdd` | Executa obrigatoriamente o ciclo TDD: **1. Red** (Teste falhando) → **2. Green** (Código mínimo) → **3. Refactor**. |
| `/code-review` | `code-review` | Auditoria pré-commit de segurança FAPI/mTLS, LGPD (PII), exceções RFC 7807 e ausência de magic strings. |
| `/git-commit` | `git-commit` | Realiza commit único formatado via Conventional Commits. |
| `/git-commit-many-by <strategy>` | `git-commit-many-by` | Fraciona alterações pendentes em múltiplos commits atômicos estruturados. |
| `/git-pr` | `git-pr` | Abre Pull Request com template padronizado, release notes e checklist de aceitação. |

---

## 🔀 3. Detalhamento do Comando `/git-commit-many-by <strategy>`

O comando aceita três estratégias obrigatórias:

### A. `/git-commit-many-by layer`
Agrupa e realiza commits em sequência respeitando as camadas de Clean Architecture:
1. `feat(domain): ...` -> Modificações em Agregados, Value Objects, Domain Events e Domain Exceptions.
2. `feat(application): ...` -> Modificações em Commands, Queries, DTOs e Interfaces de Handlers.
3. `feat(infrastructure): ...` -> Modificações em DbContext, Mapeamentos EF Core, Repositórios e MassTransit.
4. `feat(api): ...` -> Modificações em Endpoints, Middlewares e `Program.cs`.
5. `test(...): ...` -> Suíte de testes unitários e de integração.

### B. `/git-commit-many-by feature`
Identifica os Casos de Uso ou Slices alterados e cria um commit completo (do Domain aos Testes) para cada funcionalidade.

### C. `/git-commit-many-by service`
Agrupa alterações por projetos de Microsserviços (`PluggyIntegration`, `FileImporter`, `TransactionAggregator`, `ApiGateway`, `FinanceHub.Web`, `Shared.*`).

---

## 🛡️ 4. Regras Anti-Desvio do Harness (Prevenção dos 30% de Falhas)

1. **Inversão de Dependência em Arquivos Isolados (Regra 13)**:
   - É estritamente proibido declarar `public interface I<Name>` e `public class <Name>` no mesmo arquivo `.cs`. A Interface e a Classe Concreta DEVEM residir em arquivos `.cs` dedicados.
2. **Ciclo TDD Obrigatório**:
   - Nenhuma funcionalidade pode ser dada como concluída sem demonstrar o teste unitário que antes falhou e agora passa limpo.
3. **Zero Remendos Superficiais**:
   - Falhas em builds ou testes devem ser investigadas e resolvidas na causa raiz. Proibido engolir exceções ou suprimir asserções.

---

## 📂 5. Plano de Alterações em Arquivos do Repositório

1. **`.agents/specs/harness-slash-commands-refactoring-spec.md`**: Este arquivo de especificação.
2. **`.agents/AGENTS.md`**: Adição do Índice de Slash Commands no topo e reforço da tabela de governança.
3. **`.agents/skills/git-commit-many/SKILL.md`**: Criação da skill de comitagem fracionada.
4. **`.agents/skills/git-commit/SKILL.md`**: Adição do acionador `/git-commit`.
5. **`.agents/skills/dotnet-vertical-slice/SKILL.md`**: Adição do acionador `/scaffold-slice` e instrução de arquivos `.cs` separados.
6. **`.agents/skills/dotnet-testing/SKILL.md`**: Adição do acionador `/run-tdd`.
7. **`.agents/skills/code-review/SKILL.md`**: Adição do acionador `/code-review`.
8. **`.agents/skills/spec-collaboration/SKILL.md`**: Adição do acionador `/spec-feature`.
