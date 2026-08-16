# 📋 Plano de Conexão: Banco Inter Real Account Integration

Este documento detalha o planejamento técnico da integração direta de conta corrente com o **Banco Inter** para obter saldo, extrato completo e Pix reais de forma 100% gratuita.

---

## 🎯 Por que o Banco Inter é Diferente?
Diferente do Itaú e do Mercado Pago, o **Banco Inter disponibiliza gratuitamente uma API de Banking completa para Pessoa Física (PF)**. Isso significa que você pode consumir diretamente o seu extrato real (incluindo todos os Pix P2P, compras de débito/crédito, transferências e pagamentos) sem intermediários pagos.

---

## 🛡️ Credenciais e Informações Necessárias (.env)

Para conectar o FinanceHub à sua conta real do Banco Inter, precisamos das seguintes variáveis configuradas no arquivo `.env`:

```env
# Credenciais obtidas no Internet Banking do Inter
INTER_CLIENT_ID=seu_client_id_da_api
INTER_CLIENT_SECRET=seu_client_secret_da_api

# Certificado mTLS (Exportado do painel do Inter)
INTER_CERTIFICATE_PATH=certs/inter_certificado.p12
INTER_CERTIFICATE_PASSWORD=senha_do_certificado_p12
```

### 🔑 Como Obter Essas Informações no Banco Inter:
1.  **Acesse o Internet Banking**: Faça login na sua conta do Inter pelo computador ([https://web.bancointer.com.br](https://web.bancointer.com.br)).
2.  **Acesse o menu de APIs**: Vá em **Configurações > Gerenciamento de APIs > Nova Aplicação**.
3.  **Selecione os Escopos**: Crie uma nova aplicação e selecione os escopos de **Banking** (Consulta de Saldo e Extrato) e/of **Pix**.
4.  **Baixe o Certificado**: O Inter gerará dois arquivos: o certificado público (`.crt`) e a chave privada (`.key`).
5.  **Converta para PKCS#12 (.p12)**: Para carregar o par de chaves nativamente no .NET, empacote-os em um arquivo `.p12` usando o OpenSSL:
    ```bash
    openssl pkcs12 -export -out inter_certificado.p12 -inkey chave_privada.key -in certificado.crt
    ```

---

## 🏛️ Estrutura de Scaffolding Proposta

Seguindo as regras de **Vertical Slice / Clean Architecture / TDD / DDD / Caminhos Relativos**, a estrutura de arquivos no projeto `FinanceHub.InterIntegration` será:

### 1. Camada de Domínio (`Domain`)
*   `InterConstants.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Domain/Constants/`): Constantes com os endpoints oficiais de produção do Inter (`https://api.bancointer.com.br`).
*   `InterSyncState.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Domain/Entities/`): Entidade Aggregate Root que guarda os cursores de data da última sincronização bem-sucedida.
*   **Domain Exceptions**: Uma classe dedicada por mensagem de erro, herdando de `DomainException`.

### 2. Camada de Aplicação (`Application`)
*   `SyncInterTransactionsCommand.cs` & `ISyncInterTransactionsCommandHandler.cs` & `SyncInterTransactionsCommandHandler.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Application/Commands/SyncTransactions/`): Orquestrador CQRS que:
    1.  Chama a infraestrutura para obter o token de acesso via mTLS.
    2.  Busca as transações do extrato completo no período correspondente.
    3.  Dispara o evento `TransactionIngested` para cada transação via MassTransit Outbox.
    4.  Atualiza e persiste o cursor de data.

### 3. Camada de Infraestrutura (`Infrastructure`)
*   `InterApiClient.cs` & `IInterApiClient.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Infrastructure/Services/`): Cliente HTTP com handler mTLS acoplado (usando o certificado `.p12` injetado pelo `FileSystemCertificateProvider`). Efetua a autenticação OAuth2 (`/oauth/v2/token`) e a busca de saldo e extrato completo (`/banking/v2/extrato/completo`).
*   `InterDbContext.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Infrastructure/Persistence/`): Banco PostgreSQL isolado do microsserviço.

### 4. Camada API (`Api`)
*   `SyncEndpoints.cs` (em `src/Services/InterIntegration/FinanceHub.InterIntegration.Api/Endpoints/`): Exposição do endpoint `POST /api/v1/inter/sync`.

---

## 🧪 Estratégia de Testes (TDD Red-Green-Refactor)

1.  **Testes de Integração**: Testar o HttpClient e a autenticação mTLS contra servidores mock locais usando `FakeHttpHandler` no projeto de testes (`InterApiClientTests.cs`).
2.  **Testes de Negócio**: Testar os fluxos do Handler (`SyncInterTransactionsCommandHandlerTests.cs`) para garantir tratamento de cursores e idempotência de transações.
3.  **Cobertura**: Mínimo de 80% de cobertura.
