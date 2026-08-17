---
name: pr-analyzer
description: Inspect and audit GitHub Actions CI pipelines, test execution logs, and SonarCloud metrics (quality gate, open issues, code smells, vulnerabilities, and duplicated lines density) for a given Pull Request.
---

# Pull Request & SonarCloud Automated Analyzer Skill

## ⚡ Trigger / Slash Command
```bash
/pr-analyzer [pr-number]
```
*Exemplos:* `/pr-analyzer` (analisa o PR associado à branch atual) ou `/pr-analyzer 14` (analisa o PR específico #14).

---

## 🎯 Primary Purpose

Audita de ponta a ponta o estado de integridade e conformidade de um Pull Request no FinanceHub:
1. **GitHub Actions CI Pipeline**: Verifica status de build (`Build Solution`), execução de testes unitários e de integração (`Run Unit Tests`) e Testcontainers.
2. **SonarCloud Quality Gate**: Consulta a API oficial do SonarCloud para inspecionar métricas do código novo introduzido no PR.
3. **Detecção de Issues & Code Smells**: Lista todas as issues abertas classificadas por severidade (`BLOCKER`, `CRITICAL`, `MAJOR`, `MINOR`), arquivos e linhas afetadas.
4. **Análise de Duplicação de Código**: Valida a densidade de linhas duplicadas (`new_duplicated_lines_density`), blocos duplicados e novas linhas.

---

## 🛠️ Step-by-Step Execution Workflow

### Step 1: Detect Target Pull Request
Obtenha o número do PR a partir do argumento do usuário ou via GitHub CLI:
```bash
gh pr view --json number,title,state,url,headRefName,baseRefName
```

### Step 2: Inspect GitHub Actions CI Pipeline
Verifique os checks ativos e histórico da execução:
```bash
gh pr view <pr-number> --json statusCheckRollup
gh run list --limit 3 --json databaseId,status,conclusion,name,url
```
- Se o status for `IN_PROGRESS` ou `QUEUED`, monitore com:
  ```bash
  gh run watch <run-id>
  ```
- Se falhar, inspecione os logs da falha com:
  ```bash
  gh run view <run-id> --log-failed
  ```

### Step 3: Query SonarCloud Quality Gate & Duplications API
Execute o script utilitário embutido na skill:
```bash
python3 .agents/skills/pr-analyzer/scripts/check_pr_sonar.py <pr-number>
```

Ou execute consultas diretas via API REST do SonarCloud autenticadas com Basic Auth (`SONAR_TOKEN` / `SONARCLOUD_TOKEN`):

1. **Métricas de Duplicação e Cobertura**:
   ```
   GET https://sonarcloud.io/api/measures/component?component=JoseMD12_finance-hub&pullRequest=<PR>&metricKeys=new_duplicated_lines_density,new_duplicated_lines,new_duplicated_blocks,new_lines,new_coverage
   ```
2. **Busca de Issues Abertas**:
   ```
   GET https://sonarcloud.io/api/issues/search?componentKeys=JoseMD12_finance-hub&pullRequest=<PR>&resolved=false
   ```

### Step 4: Structured Diagnostic Report
Apresente um relatório claro contendo:
- 📊 **Status Geral do PR**: Número, título, branches e link.
- 🟢/🔴 **Status da CI**: Cada job executado e tempo decorrido.
- 📉 **Duplicação de Código**: Densidade percentual e blocos detectados.
- 🚨 **Lista de Issues**: Arquivo, linha, regra e link direto para o SonarCloud.
- 💡 **Plano de Remediação**: Passos acionáveis de correção quando houver falhas.
