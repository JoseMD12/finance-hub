---
name: git-commit
description: Standardized Git commit generator skill with Conventional Commits rules, scope tagging (e.g. feat(itau):, fix(security):), and change summary rules.
---

# Git Commit Generator Skill

## ⚡ Trigger / Slash Commands
- `/git-commit` -> Commit único com todas as alterações no staging.
- `/git-commit-many-by <layer|feature|service>` -> Para comitagem fracionada por camada ou funcionalidade, ative a skill `git-commit-many`.

Use esta habilidade para gerar ou executar commits no projeto **FinanceHub**. Ela garante o padrão Conventional Commits 1.0.0.

## 1. Commit Structure Rules

Every commit message MUST strictly adhere to the following schema:

```text
<type>(<scope>): <short summary in imperative mood>

[optional body explaining WHAT and WHY]

[optional footer(s)]
```

### Commit Types (`<type>`)
- `feat`: A new feature for FinanceHub (e.g., new bank connector, endpoint, domain rule).
- `fix`: A bug fix in existing microservice logic or API.
- `perf`: A code change that improves performance (e.g., EF Core query optimization).
- `sec` / `security`: Security improvements or vulnerability patches.
- `arch`: Architecture or microservice boundary adjustments.
- `refactor`: A code change that neither fixes a bug nor adds a feature.
- `test`: Adding missing tests or correcting existing unit/integration tests.
- `docs`: Documentation changes only.
- `style`: Formatting, missing semi-colons, white-space changes (no logic change).
- `build`: Changes affecting the build system, project dependencies (`.csproj`), or NuGet packages.
- `ci`: Changes to CI/CD workflows and configuration scripts.
- `chore`: Maintenance tasks, repo updates, or minor chores.

### FinanceHub Scope Tagging (`<scope>`)
Select the primary microservice or shared library affected:
- Integration scopes: `auth`, `itau`, `mercadopago`, `inter`
- Core & BFF scopes: `aggregator`, `gateway`
- Shared library scopes: `shared`, `certificates`, `messaging`, `observability`
- Database & Migrations: `migrations`, `efcore`
- Devops & Config: `deps`, `config`, `docker`

### Short Summary Rules
- Write in the **imperative mood** ("add transaction endpoint", NOT "added transaction endpoint" or "adds transaction endpoint").
- Use all **lowercase** letters for the subject line.
- **No trailing period (`.`)** at the end of the subject line.
- Maximum length: **72 characters** for the first line.

### Commit Body Rules
- Provide a clear explanation of **WHAT** changed and **WHY** the change was made.
- Do NOT focus on step-by-step code details (*how*); focus on intent, trade-offs, and domain impact.
- Use bullet points for multi-part changes.

### Breaking Changes
- Append `!` after the type/scope (e.g., `feat(itau)!: change authentication signature`) or include `BREAKING CHANGE: <description>` in the commit footer.

---

## 2. Security & Compliance Rules

Before generating or executing any commit, verify:
1. **No Secrets/Tokens**: NEVER commit API secrets, OAuth access tokens, JWT tokens, private keys, client certificates, DB connection strings, or passwords.
2. **No PII**: Ensure no customer bank account numbers, CPF/CNPJ, or raw user data are hardcoded in test fixtures or mock data.
3. **No Unintended Files**: Ensure `.env`, `appsettings.Local.json`, user secrets, or build artifacts (`bin/`, `obj/`) are not staged.

---

## 3. Step-by-Step Commit Generator Workflow

When asked to generate or create a commit:

### Step 1: Inspect Staged & Unstaged Changes
Run git commands to analyze the repository state:
```bash
git status --short
git diff --staged
```
If nothing is staged, inspect unstaged changes: `git diff`.

### Step 2: Analyze & Categorize
- Identify the primary intent (`feat`, `fix`, `perf`, `sec`, etc.).
- Identify the affected FinanceHub microservice or shared library scope (`itau`, `aggregator`, `auth`, `certificates`, etc.).
- Scan diffs for sensitive data or unintended file inclusions.

### Step 3: Draft Commit Message
Construct the message adhering to Conventional Commits:
```text
feat(itau): implement OAuth token refresh handler

Replace static API key authentication with dynamic OAuth2 client credentials flow
for Itaú Open Banking endpoints. Automatically handles token caching and refresh 
failures.

- Add ItauOAuthTokenHandler backoff retry strategy
- Register memory cache registration in DependencyInjection
```

### Step 4: Verify & Execute
- Verify line lengths and format.
- Execute git commit if requested:
```bash
git commit -m "feat(itau): implement OAuth token refresh handler" -m "Replace static API key authentication with dynamic OAuth2 client credentials flow for Itaú Open Banking endpoints..."
```

