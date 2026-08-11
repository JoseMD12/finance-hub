# Rule: Collaborative Specification Protocol — FinanceHub

## 1. Strict Question Scoping
- When aligning on specifications, roadmap phases, or system designs with the user, agents MUST ask **only one question at a time**.
- Asking multiple questions in a single response breaks the collaborative workflow and is strictly prohibited.

## 2. Live Spec File Maintenance
- Every collaborative session MUST have a corresponding draft specification file located in `.agents/specs/` (e.g. `.agents/specs/project-roadmap-spec.md`).
- Agents must mutate the target spec document after every user choice to reflect the latest decisions.

## 3. Interactive Decision Formatting
- Each question must present distinct, clear options, marking the `(Recommended)` option based on FinanceHub microservices rules.
