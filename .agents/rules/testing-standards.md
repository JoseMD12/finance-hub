# Rule: .NET Testing Standards — FinanceHub

## 1. Cobertura Mínima Obrigatória
- **80% de cobertura** é o mínimo aceito por microsserviço, especialmente na camada `Application`.
- PRs sem cobertura suficiente **não devem ser mergeados**.

## 2. Stack de Testes Padronizada
Todos os projetos de teste **devem** usar exclusivamente:
- `xUnit` — framework de testes
- `FluentAssertions` — asserções legíveis
- `NSubstitute` — mocking de interfaces
- `Testcontainers` — testes de infraestrutura (PostgreSQL, RabbitMQ)
- `Bogus` — geração de dados falsos realistas

**Proibido**: Moq, AutoFixture isolado sem Bogus, dados reais de produção.

## 3. Nomenclatura de Testes
Siga o padrão: `Metodo_Cenario_ResultadoEsperado`

```
// ✅ Correto
SyncTransacoes_QuandoBancoRetornaVazio_NaoDevePersistirNada()

// ❌ Errado
TesteSyncTransacoes()
```

## 4. Isolamento por Camada
- **Domain**: sem mocks, sem I/O, apenas lógica pura.
- **Application**: mockar repositórios e serviços externos com NSubstitute.
- **Infrastructure**: usar Testcontainers para banco e mensageria reais.
- **Api**: usar `WebApplicationFactory<Program>` para testar endpoints HTTP.

## 5. Segurança em Testes
- Nunca commitar tokens, senhas, CPFs ou certificados reais em arquivos de teste.
- Mockar `ICertificateProvider` para retornar `null` (modo Dev Fallback) em testes unitários.
- Configurar Serilog em `MinimumLevel.Warning` em ambientes de teste para evitar logs com PII.

## 6. Organização dos Arquivos de Teste
```
tests/FinanceHub.UnitTests/
  ├── <NomeDoServico>/          ← Espelhar estrutura do serviço
  │   ├── Domain/
  │   ├── Application/
  │   └── Infrastructure/
  └── Factories/                ← Dados falsos reutilizáveis (Bogus)
```

## 7. Testes de Deduplicação são Obrigatórios
O `TransactionAggregator` **deve** ter testes cobrindo:
- Hash SHA-256 idêntico para a mesma transação (com payloads diferentes).
- Hash SHA-256 distinto para transações com valores ou datas diferentes.
- Rejeição de inserção duplicada via índice único PostgreSQL.
