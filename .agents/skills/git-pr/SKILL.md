---
name: git-pr
description: Pull Request creator skill with automated template, testing checklist, security audit checklist, and release notes generator.
---

# Pull Request Creator & Automation Skill

Use this skill when preparing, drafting, or creating Pull Requests for the **FinanceHub** project (.NET 10). It ensures PR descriptions follow standardized templates, includes rigorous testing & security audit checklists, and automatically generates release notes.

## 1. Automated Pull Request Template

Every Pull Request description MUST adhere to the following standard template structure:

```markdown
## 📌 Objective & Summary
<!-- Clearly describe the goal of this PR and what problem it solves -->

## 📑 Type of Change
- [ ] 🚀 `feat`: New feature
- [ ] 🐛 `fix`: Bug fix
- [ ] ⚡ `perf`: Performance improvement
- [ ] 🔒 `sec`: Security patch
- [ ] 🏛️ `arch`: Architecture or microservice boundary adjustments
- [ ] 🛠️ `refactor`: Refactoring / Architectural cleanup
- [ ] 🧱 `build` / `deps`: Dependency or build updates

## 🎯 Affected Microservices & Shared Libraries
- **Services**: `[ ] AuthConsent [ ] ItauIntegration [ ] MercadoPagoIntegration [ ] InterIntegration [ ] TransactionAggregator [ ] ApiGateway`
- **Shared Libraries**: `[ ] Shared.Certificates [ ] Shared.Messaging [ ] Shared.Observability`
- **Infrastructure & DB**: `[ ] PostgreSQL per service [ ] MassTransit Outbox [ ] mTLS Certificates`

## 🧪 Testing Checklist
- [ ] Unit tests added/updated and passing (`dotnet test`)
- [ ] Integration tests executed for Open Finance / bank connector APIs
- [ ] EF Core migration tested against target microservice PostgreSQL database
- [ ] No regression in financial calculation decimal precision
- [ ] Concurrency/Race condition scenarios tested (if applicable)

## 🔒 Financial Security & Compliance Audit
- [ ] FAPI 1.0/2.0 security profile compliance verified (PKCE, PAR, private_key_jwt, DPoP)
- [ ] No hardcoded secrets, connection strings, or API private keys
- [ ] Sensitive financial data (PAN, account numbers, tokens) masked in logs (`ILogger`)
- [ ] Monetary operations strictly use `decimal` type with explicit rounding
- [ ] Idempotency key and SHA256 HashUnico deduplication verified
- [ ] Authorization policies enforced on API Gateway endpoints

## ⚠️ Breaking Changes & Database Migrations
- **Breaking Changes**: `[None / Description]`
- **DB Migrations Required**: `[Yes / No]` (Microservice: `...`, Migration Name: `...`)

## 📝 Release Notes Summary
<!-- Bullet points for inclusion in user-facing release notes -->
- 
```

---

## 2. Release Notes Generator Rules

The Release Notes Generator section must synthesize the branch commits into clean, user-friendly categories:

- **🚀 Features**: High-level new capabilities (e.g. Itaú Pix payment integration).
- **🐛 Bug Fixes**: Specific issue resolutions (e.g. Fixed floating point rounding error in interest calculations).
- **⚡ Performance Improvements**: Measured speedups or query optimizations (e.g. EF Core query split for ledger fetch).
- **🔒 Security & Compliance**: Patches, authorization fixes, or secret rotation updates.
- **⚠️ Breaking Changes & Migration Steps**: Any breaking API changes or required EF Core migrations (`dotnet ef database update`).

---

## 3. Step-by-Step PR Creation Workflow

When executing a PR creation task:

### Step 1: Branch Analysis & Validation
Check current branch status and commits against base branch (e.g., `main` or `develop`):
```bash
git status
git log main..HEAD --oneline
git diff main...HEAD --stat
```

### Step 2: Validate Prerequisites
1. Ensure all unit & integration tests pass:
   ```bash
   dotnet test --configuration Release
   ```
2. Verify project builds cleanly with zero warnings/errors:
   ```bash
   dotnet build --configuration Release
   ```

### Step 3: Generate PR Title & Body
- **Title**: Formatted as `<type>(<scope>): <summary>` (e.g., `feat(itau): add webhooks signature validation`).
- **Body**: Fill out all sections of the Standardized PR Template with concrete details from the branch commits and diff analysis.

### Step 4: Execute PR Creation
If using GitHub CLI (`gh`):
```bash
gh pr create \
  --title "feat(itau): add webhooks signature validation" \
  --body-file pr_description.md \
  --base main
```
Alternatively, save the generated markdown description to `.github/PULL_REQUEST_TEMPLATE.md` or a local file for the user to review.

