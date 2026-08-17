#!/usr/bin/env python3
"""
PR & SonarCloud Automation Analyzer for FinanceHub
Usage:
    python3 check_pr_sonar.py [pr_number]
"""

import sys
import os
import json
import base64
import subprocess
import urllib.request
import urllib.error

PROJECT_KEY = "JoseMD12_finance-hub"
SONAR_API_BASE = "https://sonarcloud.io/api"

def get_env_variable(var_name, default=None):
    if var_name in os.environ:
        return os.environ[var_name]
    
    env_file = os.path.join(os.getcwd(), ".env")
    if not os.path.exists(env_file):
        return default

    with open(env_file, "r", encoding="utf-8") as f:
        for line in f:
            stripped = line.strip()
            if stripped and not stripped.startswith("#") and "=" in stripped:
                k, v = stripped.split("=", 1)
                if k.strip() == var_name:
                    return v.strip().strip('"').strip("'")
    return default

def get_pr_number():
    if len(sys.argv) > 1 and sys.argv[1].isdigit():
        return int(sys.argv[1])
    
    try:
        res = subprocess.run(
            ["gh", "pr", "view", "--json", "number"],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        data = json.loads(res.stdout)
        return data.get("number")
    except Exception:
        return None

def resolve_check_icon(status, conclusion):
    if conclusion == "SUCCESS" or status == "SUCCESS":
        return "🟢"
    if status in ("IN_PROGRESS", "QUEUED"):
        return "🟡"
    return "🔴"

def check_github_ci(pr_number):
    print(f"\n🔍 [1/3] Verificando Pipelines do GitHub Actions para PR #{pr_number}...")
    try:
        res = subprocess.run(
            ["gh", "pr", "view", str(pr_number), "--json", "title,state,url,headRefName,baseRefName,statusCheckRollup"],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            check=True
        )
        pr_data = json.loads(res.stdout)
        print(f"📌 PR #{pr_number}: {pr_data.get('title')}")
        print(f"🔗 URL: {pr_data.get('url')}")
        print(f"🌿 Branch: {pr_data.get('headRefName')} -> {pr_data.get('baseRefName')} (Status: {pr_data.get('state')})")
        
        checks = pr_data.get("statusCheckRollup", [])
        if not checks:
            print("ℹ️ Nenhum status check reportado ainda no PR.")
            return True

        print("\n📋 Status dos Checks:")
        all_passed = True
        for c in checks:
            name = c.get("name") or c.get("context", "Check")
            status = c.get("status") or c.get("state", "UNKNOWN")
            conclusion = c.get("conclusion", "")
            icon = resolve_check_icon(status, conclusion)
            if conclusion != "SUCCESS" and status != "SUCCESS":
                all_passed = False
            suffix = f" ({conclusion})" if conclusion else ""
            print(f"  {icon} {name}: {status}{suffix}")
        return all_passed
    except Exception as e:
        print(f"⚠️ Erro ao consultar GitHub CLI: {e}")
        return False

def resolve_duplication_icon(dup_density):
    if dup_density <= 0.001:
        return "🟢"
    if dup_density < 3.0:
        return "🟡"
    return "🔴"

def print_measures(measures):
    print("\n📊 Métricas do SonarCloud (Código Novo no PR):")
    dup_density = float(measures.get("new_duplicated_lines_density", 0.0))
    dup_icon = resolve_duplication_icon(dup_density)
    print(f"  {dup_icon} Densidade de Código Duplicado: {dup_density}%")
    print(f"  📦 Blocos Duplicados: {measures.get('new_duplicated_blocks', '0')}")
    print(f"  📝 Linhas Duplicadas: {measures.get('new_duplicated_lines', '0')}")
    print(f"  📈 Novas Linhas Adicionadas: {measures.get('new_lines', '0')}")
    if "new_coverage" in measures:
        print(f"  🧪 Cobertura de Testes: {measures.get('new_coverage')}%")

def print_issues(issues, total, pr_number):
    if total == 0:
        print("🎉 0 Issues abertas no SonarCloud! Qualidade Máxima (Clean Code).")
        return

    print(f"⚠️ Total de {total} issue(s) aberta(s) encontradas:")
    for idx, iss in enumerate(issues, 1):
        key = iss.get("key")
        rule = iss.get("rule")
        severity = iss.get("severity")
        msg = iss.get("message")
        component = iss.get("component", "").replace(f"{PROJECT_KEY}:", "")
        line = iss.get("line", "N/A")
        print(f"\n  [{idx}] [{severity}] {rule}")
        print(f"      Arquivo: {component}:{line}")
        print(f"      Mensagem: {msg}")
        print(f"      Link: https://sonarcloud.io/project/issues?id={PROJECT_KEY}&pullRequest={pr_number}&open={key}")

def check_sonarcloud(pr_number):
    print(f"\n🔍 [2/3] Acessando API do SonarCloud para PR #{pr_number}...")
    
    token = get_env_variable("SONAR_TOKEN") or get_env_variable("SONARCLOUD_TOKEN") or "934de92660c2df9d46c25af32982c5d67b7e8496"
    if not token:
        print("❌ Token do SonarCloud não encontrado (SONAR_TOKEN ou SONARCLOUD_TOKEN).")
        return
    
    auth_str = f"{token}:"
    b64_auth = base64.b64encode(auth_str.encode()).decode()
    headers = {
        "Authorization": f"Basic {b64_auth}",
        "Accept": "application/json"
    }
    
    metrics = "new_duplicated_lines_density,new_duplicated_lines,new_duplicated_blocks,new_lines,new_coverage,new_maintainability_rating,new_reliability_rating,new_security_rating"
    url_measures = f"{SONAR_API_BASE}/measures/component?component={PROJECT_KEY}&pullRequest={pr_number}&metricKeys={metrics}"
    
    req_m = urllib.request.Request(url_measures, headers=headers)
    try:
        with urllib.request.urlopen(req_m) as resp:
            data_m = json.loads(resp.read().decode())
            measures = {m["metric"]: m.get("periods", [{}])[0].get("value", m.get("value", "N/A")) 
                        for m in data_m.get("component", {}).get("measures", [])}
            print_measures(measures)
    except Exception as e:
        print(f"⚠️ Erro ao consultar medidas no SonarCloud: {e}")
    
    print(f"\n🔍 [3/3] Buscando Issues Abertas no SonarCloud para PR #{pr_number}...")
    url_issues = f"{SONAR_API_BASE}/issues/search?componentKeys={PROJECT_KEY}&pullRequest={pr_number}&resolved=false"
    req_i = urllib.request.Request(url_issues, headers=headers)
    try:
        with urllib.request.urlopen(req_i) as resp:
            data_i = json.loads(resp.read().decode())
            total = data_i.get("total", 0)
            issues = data_i.get("issues", [])
            print_issues(issues, total, pr_number)
    except Exception as e:
        print(f"⚠️ Erro ao buscar issues no SonarCloud: {e}")

def main():
    pr = get_pr_number()
    if not pr:
        print("❌ Nenhum número de PR informado ou detectado no contexto git.")
        print("Uso: python3 check_pr_sonar.py <numero_do_pr>")
        sys.exit(1)
    
    check_github_ci(pr)
    check_sonarcloud(pr)
    print("\n✅ Análise de PR e SonarCloud concluída com sucesso.\n")

if __name__ == "__main__":
    main()
