# 📋 Plano de Conexão e Comparativo: Mercado Pago Real Account Integration

Este documento detalha o mapeamento técnico das três estratégias de conexão reais com o Mercado Pago para fins de extrato consolidado no **FinanceHub**, eliminando qualquer uso de mocks, simulações ou dados fictícios.

---

## 🔍 Tabela Comparativa de Métodos Reais

| Critério | 1. API Developer | 2. Pluggy Open Finance | 3. Exportação Manual |
| :--- | :---: | :---: | :---: |
| **Custo Financeiro** | **Gratuito** | **Pago** (após 15 dias de teste) | **Gratuito** |
| **Custo de Esforço/Tempo** | Zero (Automático) | Zero (Automático) | Alto (Manual toda vez) |
| **Dados Pessoais (Pix P2P)** | Não suportado (apenas Checkout) | **Sim (100% de cobertura)** | **Sim (100% de cobertura)** |
| **Saldo em Tempo Real** | Não | **Sim** | Não |
| **Burocracia de Acesso** | Nenhuma | Nenhuma (Plano Dev) | Nenhuma |

---

## 🏛️ Detalhamento Técnico das 3 Opções

### 1. API Developer Oficial (REST)
Acesso direto às credenciais geradas no painel do Mercado Pago Developers (`v1/payments/search`).

*   **Vantagens**:
    *   100% Gratuito e oficial.
    *   Sincronização automática em segundo plano pelo FinanceHub.
*   **Desvantagens**:
    *   **Incompleto para uso pessoal**: Não registra Pix enviados/recebidos entre pessoas físicas (P2P), TEDs e transferências normais de saldo. O Mercado Pago restringe o uso desse endpoint apenas a pagamentos de e-commerce e assinaturas.
*   **Ideal para**: Lojas online ou para consolidar apenas rendimentos automáticos da conta e compras Mercado Livre.

### 2. Provedor de Agregação Regulado (Pluggy)
Acesso via API do motor Open Finance homologado no Banco Central do Brasil, utilizando o widget nativo no celular para biometria.

*   **Vantagens**:
    *   **Extrato 100% completo**: Lê todo e qualquer Pix P2P, transferências de saldo, pagamentos e saldos reais instantâneos.
    *   Fluxo nativo com aplicativo oficial do celular.
*   **Desvantagens**:
    *   **Serviço Pago**: Requer assinatura/mensalidade após o período inicial de teste de 15 dias para manter o fluxo ativo.
*   **Ideal para**: Automação sem atrito com dados financeiros absolutos e consolidados.

### 3. Importação de Arquivos Locais (OFX / CSV / XLS)
Baixar o extrato consolidado manualmente pelo Internet Banking/Aplicativo do Mercado Pago e realizar o upload no FinanceHub.

*   **Vantagens**:
    *   100% Gratuito.
    *   **Extrato completo**: Contém 100% de todas as movimentações reais e Pix.
*   **Desvantagens**:
    *   **Experiência cansativa**: O usuário precisa entrar no aplicativo do Mercado Pago, exportar a planilha e anexar no FinanceHub toda vez que desejar atualizar seu saldo consolidado.
*   **Ideal para**: Atualizações semanais ou mensais gratuitas de histórico financeiro pessoal completo.

---

## 🚀 Próximas Ações do Projeto (Fases Futuras)

1.  **Stand-by da Integração Automatizada**:
    *   A branch `feature/mercadopago-integration` mantém o scaffolding da API de Gateway e Consumidor do Aggregator limpo e pronto, sem nenhuma chamada HTTP ativa para a Pluggy.
2.  **Desenvolvimento do Parser Manual (Foco Futuro MP)**:
    *   Criar na camada de infraestrutura um parser de planilha Excel/CSV do Mercado Pago para permitir a importação gratuita e offline como alternativa à automação paga.
3.  **Desenvolvimento de Itaú e Inter**:
    *   Focar no desenvolvimento das APIs diretas e gratuitas com mTLS e certificados locais das suas contas do **Itaú** e **Banco Inter**.
