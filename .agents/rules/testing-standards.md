# Rule: .NET Testing Standards — FinanceHub

## 0. Fluxo TDD Obrigatório (Red → Green → Refactor)
- **TDD (Test-Driven Development) é MANDATÓRIO**: Nenhum código de produção deve ser escrito sem antes ter o teste unitário/integração escrito e falhando (**Red**).
- **Green**: Escrever o código mínimo de produção para passar o teste.
- **Refactor (Yellow)**: Refatorar o código aplicando DDD (Aggregate Root, Rich Domain Model) e Design Patterns, mantendo 100% dos testes verdes.
- Veja detalhes em [`.agents/rules/tdd-workflow.md`](./tdd-workflow.md).

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
- Nunca usar valores observados na API real (saldos, limites, salários, dígitos de cartão, IDs de fatura ou de conexão, nomes de contrapartes) em fixtures. Use Bogus ou números redondos fictícios. Fixture "recortada do payload real" é dado pessoal versionado.
- Teste que relê uma entidade depois de gravá-la deve ler do banco, não do change tracker: use `ChangeTracker.Clear()` ou um `DbContext` novo antes da asserção. Reler pelo mesmo contexto devolve a instância em memória e passa mesmo sem `SaveChanges`.
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
