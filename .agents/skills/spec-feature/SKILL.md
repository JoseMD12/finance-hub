---
name: spec-feature
description: Interactive and collaborative specification drafting skill for FinanceHub in Plan Mode. Enforces one-question-at-a-time decision making, progressive spec document updates, and architectural validation gates.
---

# Feature Specification & Planning Skill — FinanceHub

## ⚡ Trigger / Slash Command
```bash
/spec-feature
```

Use esta habilidade para planejar e criar especificações técnicas detalhadas em `.agents/specs/` em Plan Mode (1 pergunta por vez).

## Core Principles

1. **One Question at a Time**:
   - Never flood the user with multiple architectural decisions at once.
   - Ask precisely **one question per interaction** to refine choices.

2. **Progressive Spec Maintenance**:
   - Maintain a live document in `.agents/specs/<spec-name>.md`.
   - Update the draft spec immediately after each decision is confirmed by the user.

3. **Structured Options**:
   - Provide recommended default options with technical rationale (e.g. performance, security, complexity).
   - Use interactive selection tooling (`ask_question`) or clear markdown choice menus.

4. **Validation & Sign-Off**:
   - Once all sections of a spec are decided, run an empirical check or architectural alignment check before closing the spec draft.

## Spec Workflow Steps

```text
[Identify Goal] ──> [Create Spec Draft in .agents/specs/] ──> [Ask 1 Question] ──> [Update Spec Document] ──> [Repeat until Spec Complete] ──> [Sign-off]
```
