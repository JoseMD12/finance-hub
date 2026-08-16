# 📋 Plano de Conexão: Banco Itaú Real Account Integration

Este documento detalha o planejamento técnico da integração direta de conta corrente com o **Banco Itaú** sem o uso de mocks ou simulações para obter dados reais de saldo, Pix e extratos.

---

## 🎯 Objetivo & Escopo
Implementar o microsserviço `FinanceHub.ItauIntegration` seguindo **Clean Architecture + DDD** e os padrões estritos de desenvolvimento do FinanceHub para buscar extrato e saldo real através das APIs do portal de desenvolvedores do Itaú.

---

## 🛡️ Credenciais e Informações Necessárias (.env)

Para conectar à sua conta real do Itaú, precisamos das seguintes variáveis configuradas no arquivo `.env`:

```env
# Credenciais do Portal Itaú Developers
ITAU_CLIENT_ID=seu_client_id_do_aplicativo
ITAU_CLIENT_SECRET=seu_client_secret_do_aplicativo

# Parâmetros de conta correntista
ITAU_AGENCY=sua_agencia_sem_digito
ITAU_ACCOUNT=sua_conta_com_digito

# Certificado mTLS (Segurança Bancária ICP-Brasil / Itaú)
ITAU_CERTIFICATE_PATH=certs/seu_certificado.p12
ITAU_CERTIFICATE_PASSWORD=senha_do_certificado_se_houver
```

### 🔑 Como Obter Essas Informações no Itaú:
1.  **Acesse o Itaú Developers**: Entre no portal oficial de desenvolvedores do Itaú (https://developers.itau.com.br).
2.  **Crie uma Aplicação**: Crie um novo aplicativo no painel, selecionando as APIs de **Extrato e Saldo**.
3.  **Gere o Certificado**: Siga o fluxo do portal para gerar ou enviar sua solicitação de assinatura de certificado (CSR). Salve o certificado emitido `.crt` e a chave privada `.key`.
4.  **Converta para PKCS#12 (.p12)**: Junte o arquivo `.crt` e `.key` em um arquivo `.p12` único usando o OpenSSL (para que o .NET carregue nativamente):
    ```bash
    openssl pkcs12 -export -out certificado.p12 -inkey chave.key -in certificado.crt
    ```

---

## 🏛️ Estrutura de Scaffolding Proposta

Seguindo as regras de **Vertical Slice / Clean Architecture / TDD / DDD / Caminhos Relativos**, a estrutura de arquivos no projeto `FinanceHub.ItauIntegration` será:

### 1. Camada de Domínio (`Domain`)
*   `ItauConstants.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Domain/Constants/`): Constantes e endpoints oficiais do Itaú.
*   `ItauSyncState.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Domain/Entities/`): Entidade de controle de sincronização de histórico (Aggregate Root).
*   **Domain Exceptions**: Uma classe dedicada por mensagem de erro, derivadas de `DomainException`.

### 2. Camada de Aplicação (`Application`)
*   `SyncItauTransactionsCommand.cs` & `ISyncItauTransactionsCommandHandler.cs` & `SyncItauTransactionsCommandHandler.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Application/Commands/SyncTransactions/`): Caso de uso vertical CQRS que orquestra a chamada ao Itaú, publica o evento `TransactionIngested` usando MassTransit Outbox e salva o progresso do cursor.

### 3. Camada de Infraestrutura (`Infrastructure`)
*   `ItauApiClient.cs` & `IItauApiClient.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Infrastructure/Services/`): Cliente HTTP com o certificado mTLS injetado por `FileSystemCertificateProvider` que efetua chamadas ao barramento do Itaú.
*   `ItauDbContext.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Infrastructure/Persistence/`): Persistência em PostgreSQL isolado da sincronização.

### 4. Camada API (`Api`)
*   `SyncEndpoints.cs` (em `src/Services/ItauIntegration/FinanceHub.ItauIntegration.Api/Endpoints/`): Exposição do endpoint `POST /api/v1/itau/sync`.

---

## 🧪 Estratégia de Testes (TDD Red-Green-Refactor)

1.  **Testes de Integração**: Testes no módulo de infraestrutura (`ItauApiClientTests.cs`) simulando comunicação mTLS.
2.  **Testes Unitários**: Testes do Command Handler (`SyncItauTransactionsCommandHandlerTests.cs`) garantindo que as regras de negócio de datas e persistência de cursores funcionam perfeitamente sob diferentes respostas.
3.  **Cobertura**: Mínimo de 80% de cobertura.
