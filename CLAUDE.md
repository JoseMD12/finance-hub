# FinanceHub — Claude Code entrypoint

Este repositório guarda suas instruções de agente em `.agents/`, num formato pensado para
múltiplas ferramentas (Codex, Gemini, Cursor, Windsurf, Antigravity). O Claude Code **não** lê
esse diretório sozinho — ele carrega este arquivo. Os imports abaixo trazem as regras do projeto
para o contexto no início de toda sessão.

## Documento canônico

@.agents/AGENTS.md

## Regras de arquitetura e convenções

@.agents/rules/git-and-code-review.md
@.agents/rules/git-commits-user-permission.md
@.agents/rules/spec-collaboration.md
@.agents/rules/clean-arch-vertical-slice.md
@.agents/rules/ddd-aggregate-rich-domain.md
@.agents/rules/csharp-dotnet10.md
@.agents/rules/exception-handling-rfc7807.md
@.agents/rules/tdd-workflow.md
@.agents/rules/testing-standards.md
@.agents/rules/postgres-efcore.md
@.agents/rules/react-frontend-architecture.md
@.agents/rules/react-query-and-state.md
@.agents/rules/frontend-design-system-a11y.md
@.agents/rules/frontend-http-and-rfc7807.md
@.agents/rules/openfinance-security.md

## Convenções que não estão escritas nas regras

- **Mensagens de commit em inglês.** As regras exigem Conventional Commits mas deixam o idioma
  em aberto, dizendo "conforme convenção do projeto". A convenção deste projeto, visível em
  100% do histórico de `develop`, é **inglês** — no assunto e no corpo. A conversa com o
  usuário e a documentação em `.agents/` continuam em português.
- **Escopos usados**: `pluggy`, `fileimporter`, `aggregator`, `gateway`, `web`, `shared`,
  `harness`, `domain`, `application`, `infra`, `api`, `transactions`, `ui`, `core`, `services`.

## Onde encontrar contexto

- `.agents/specs/` — especificações vivas por feature. Ao trabalhar numa feature, ler a spec
  correspondente e **mantê-la sincronizada com o que for realmente implementado**, incluindo
  divergências do plano e descobertas que invalidem seções já escritas.
- `.agents/knowledge/` — modelo de domínio, arquitetura dos serviços e contratos de API.
- `.agents/plans/` — planos de integração por instituição.
- `.agents/skills/` — skills do projeto, acionadas por slash command (`/spec-feature`,
  `/scaffold-slice`, `/git-commit-many-by`, `/git-pr`, `/judge`, …). Para que apareçam como
  slash commands nativos do Claude Code, cada desenvolvedor cria o link local:

  ```bash
  mkdir -p .claude && ln -s ../.agents/skills .claude/skills
  ```

  O caminho `.claude/skills` já está no `.gitignore`, então o link é por máquina.

## Lembrete de segurança

Nunca executar `git commit` ou `git push` sem pedido explícito do usuário no prompt atual
(Regra 24 do `AGENTS.md`). Isso vale inclusive depois de corrigir lints ou apontamentos de
análise estática.
