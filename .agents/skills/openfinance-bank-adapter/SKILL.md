---
name: openfinance-bank-adapter
description: Technical guide for adding a new Open Finance bank integration adapter (e.g., Itaú, Banco Inter, Mercado Pago) in FinanceHub (.NET 10), implementing IBankConnector, OAuth2/mTLS authentication, rate limiting, and publishing TransactionIngested events via MassTransit Outbox.
---

> **Histórico Arquitetural & Status**: Este guia representa a especificação histórica para conectores bancários diretos individuais com mTLS ICP-Brasil (`FinanceHub.ItauIntegration`, etc.). Conforme decidido no ADR de Arquitetura ([`.agents/knowledge/system-architecture-and-services.md`](../../knowledge/system-architecture-and-services.md)), a conectividade Open Finance online foi unificada no microsserviço **`FinanceHub.PluggyIntegration`** via Meu.Pluggy.

# Open Finance Bank Adapter Implementation Guide (Histórico)

Este documento registra as diretrizes arquiteturais para referência caso novos conectores proprietários sejam implementados.

---

## 1. Overview & Architecture

Conectores bancários no FinanceHub encapsulam a comunicação com provedores externos dentro de microsserviços dedicados (`FinanceHub.PluggyIntegration` para Open Finance online e `FinanceHub.FileImporter` para arquivos offline). Eles traduzem payloads proprietários ou Open Finance em eventos padronizados `TransactionIngested` e `InvoiceItemIngested` despachados via MassTransit / RabbitMQ usando o Transactional Outbox Pattern.

### Core Architecture Rules:
- **Location**: Adapters reside in their dedicated microservice (e.g., `src/Services/FinanceHub.ItauIntegration/`).
- **Abstraction**: Adapters implement `IBankConnector` and emit standard integration events (`FinanceHub.Shared.Messaging`).
- **Isolation**: Domain models never reference bank-specific SDKs, DTOs, or HTTP clients. Microservices enforce database-per-service isolation.
- **Certificates**: Client mTLS X.509 certificates are loaded via `FinanceHub.Shared.Certificates`.
- **Precision**: Monetary amounts must strictly use `decimal` with explicit ISO 4217 currency codes (`BRL`).

---

## 2. Standard `IBankConnector` Interface

Every adapter must implement the standard connector interface:

```csharp
namespace FinanceHub.Shared.Connectors;

public interface IBankConnector
{
    string BankIdentifier { get; }
    
    Task<AuthTokenResponse> AuthenticateAsync(
        BankCredentials credentials, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankAccountDto>> GetAccountsAsync(
        AuthTokenResponse token, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsAsync(
        AuthTokenResponse token, 
        string accountId, 
        DateTimeOffset from, 
        DateTimeOffset to, 
        CancellationToken cancellationToken = default);

    Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

---

## 3. Adapter Directory Structure

Scaffold the following folder structure inside the target bank integration service (e.g., `src/Services/FinanceHub.ItauIntegration/`):

```text
src/Services/FinanceHub.<BankName>Integration/
├── Configuration/
│   └── <BankName>Options.cs
├── Security/
│   └── <BankName>AuthHandler.cs
├── Dtos/
│   ├── <BankName>AccountResponseDto.cs
│   ├── <BankName>TransactionResponseDto.cs
│   └── <BankName>TokenResponseDto.cs
├── Services/
│   ├── <BankName>MappingProfile.cs
│   └── <BankName>Connector.cs
├── Handlers/
│   └── FetchTransactionsCommandHandler.cs
└── Program.cs
```

---

## 4. Step-by-Step Implementation

### Step 1: Define Configuration Options
Create `<BankName>Options.cs` to hold API keys, client credentials, endpoints, and mTLS cert details:

```csharp
namespace FinanceHub.<BankName>Integration.Configuration;

public sealed class <BankName>Options
{
    public const string SectionName = "BankAdapters:<BankName>";

    public string BaseUrl { get; set; } = string.Empty;
    public string AuthEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string Scope { get; set; } = "accounts transactions openid";
    public int RateLimitPerMinute { get; set; } = 120;
}
```

### Step 2: Configure Security & Authentication (mTLS via `FinanceHub.Shared.Certificates`)
Open Finance integrations require mTLS with ICP-Brasil certificates (`X509Certificate2`) and OAuth2 Client Credentials (`private_key_jwt`).

1. **Certificate Management**:
Utilize `FinanceHub.Shared.Certificates.CertificateLoader` for retrieving and validating e-CNPJ client certificates.

2. **Delegating Auth Handler (`<BankName>AuthHandler.cs`)**:
Implement a `DelegatingHandler` to automatically attach OAuth2 bearer tokens and manage auto-refresh before expiration.

```csharp
public sealed class <BankName>AuthHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<<BankName>Options> _options;

    public <BankName>AuthHandler(IMemoryCache cache, IOptions<<BankName>Options> options)
    {
        _cache = cache;
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _cache.GetOrCreateAsync($"token:<BankName>", async entry =>
        {
            var newToken = await FetchTokenAsync(cancellationToken);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(newToken.ExpiresIn - 60);
            return newToken.AccessToken;
        });

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Step 3: Implement Rate Limiting & Polly Resilience Pipeline
Use .NET 10 standard resilience handler with `Microsoft.Extensions.Http.Resilience`:

```csharp
services.AddHttpClient<<BankName>Connector>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    var cert = CertificateLoader.LoadClientCertificate(options.CertificateThumbprint);
    handler.ClientCertificates.Add(cert);
    return handler;
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
});
```

### Step 4: Implement Data Mapping & Event Emission
Map external bank DTOs to `TransactionIngested` integration events (`FinanceHub.Shared.Messaging`):

```csharp
public static class <BankName>MappingProfile
{
    public static TransactionIngested ToIntegrationEvent(this <BankName>TransactionDto dto, string bankCode, string accountId)
    {
        return new TransactionIngested(
            ExternalId: dto.TransactionId,
            AccountId: accountId,
            BankCode: bankCode,
            Amount: decimal.Parse(dto.Amount, CultureInfo.InvariantCulture),
            Currency: dto.CurrencyCode ?? "BRL",
            TransactionDate: DateTimeOffset.Parse(dto.BookingDateTime),
            Description: dto.TransactionInformation,
            Type: dto.CreditDebitIndicator == "CRDT" ? "CREDIT" : "DEBIT",
            IngestedAt: DateTimeOffset.UtcNow
        );
    }
}
```

### Step 5: Implement Connector Class
Create `<BankName>Connector.cs` implementing `IBankConnector`:

```csharp
public sealed class <BankName>Connector : IBankConnector
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<<BankName>Connector> _logger;

    public string BankIdentifier => "<BankName>";

    public <BankName>Connector(HttpClient httpClient, ILogger<<BankName>Connector> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<BankTransactionDto>> GetTransactionsAsync(
        AuthTokenResponse token, 
        string accountId, 
        DateTimeOffset from, 
        DateTimeOffset to, 
        CancellationToken cancellationToken = default)
    {
        var url = $"open-banking/v1/accounts/{accountId}/transactions?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await _httpClient.GetFromJsonAsync<<BankName>TransactionResponseDto>(url, cancellationToken);

        if (response?.Data == null) return Array.Empty<BankTransactionDto>();

        return response.Data.Select(t => t.ToDomain()).ToList().AsReadOnly();
    }
}
```

---

## 5. Verification Checklist

1. **Configuration**: Ensure `appsettings.json` contains `BankAdapters:<BankName>` section with valid secrets from Key Vault / KMS.
2. **mTLS Test**: Verify certificate thumbprint loading via `FinanceHub.Shared.Certificates`.
3. **Unit Tests**: Add tests under `tests/FinanceHub.<BankName>Integration.Tests/` asserting correct transaction DTO mapping, decimal precision, and resilience behavior.
4. **Build & Test**: Run `dotnet build` and `dotnet test`.

