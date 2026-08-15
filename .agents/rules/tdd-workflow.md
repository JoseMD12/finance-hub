# Rule: TDD (Test-Driven Development) Mandatory Workflow — FinanceHub

> **Prioridade**: OBRIGATÓRIA (Sempre Enforçada)  
> **Escopo**: Todo o desenvolvimento de código em todos os microsserviços do FinanceHub.

---

## 🎯 1. O Princípio TDD no FinanceHub

No **FinanceHub**, **nenhum código de produção deve ser escrito sem antes existir um teste automatizado falhando**.

O TDD garante que o design das interfaces, entidades e casos de uso seja orientado pelos requisitos e comportamento esperados, prevenindo código desnecessário ou modelos anêmicos.

---

## 🔄 2. O Ciclo TDD: Red → Green → Refactor (Yellow)

Todo desenvolvimento de feature deve seguir rigorosamente os três passos abaixo:

```text
  ┌──────────────────────────────────────────────────────────┐
  │ 🔴 1. RED: Escrever o teste primeiro (Falha Garantida)  │
  └───────────────────────────┬──────────────────────────────┘
                              │
                              ▼
  ┌──────────────────────────────────────────────────────────┐
  │ 🟢 2. GREEN: Escrever o código mínimo para passar        │
  └───────────────────────────┬──────────────────────────────┘
                              │
                              ▼
  ┌──────────────────────────────────────────────────────────┐
  │ 🟡 3. REFACTOR (Yellow): Limpar, aplicar patterns e DDD │
  └──────────────────────────────────────────────────────────┘
```

### 🔴 Passo 1: RED (Teste Falhando Primeiro)
1. Antes de criar a implementação da classe/entidade/handler, crie o arquivo de teste correspondente em `tests/FinanceHub.UnitTests/`.
2. Escreva o teste cobrindo a regra de negócio desejada seguindo o padrão `Metodo_Cenario_ResultadoEsperado`.
3. Execute `dotnet test`. O teste **DEVE FALHAR** (seja por não compilar pela ausência da classe ou por asserção de teste falha).

### 🔗 2.1 Integração TDD + Exceções de Domínio ([`exception-handling-rfc7807.md`](./exception-handling-rfc7807.md))
- Ao testar invalidades ou violações de regras de negócio no passo **RED**, o teste **deve obrigatoriamente validar a exceção de domínio fortemente tipada específica** (ex: `act.Should().Throw<InvalidUserIdDomainException>()`).
- Se a classe da exceção de domínio reutilizável (ex: `InvalidUserIdDomainException`, `ConsentInvalidStateException`) ainda não existir, crie o arquivo da exceção na camada `Domain` junto com o teste **RED**.
- O teste TDD deve validar tanto o tipo exato da `DomainException` quanto a mensagem amigável (default ou parametrizada) e seu `ErrorCode`.

### 🟢 Passo 2: GREEN (Código Mínimo de Sucesso)
1. Escreva **apenas o código estritamente necessário** na camada correspondente (`Domain`, `Application`, `Infrastructure`, `Api`) para fazer o teste passar.
2. Execute `dotnet test`. O teste **DEVE PASSAR**.

### 🟡 Passo 3: REFACTOR / YELLOW (Refatoração & Padrões)
1. Melhore a estrutura do código, aplique encapsulamento estrito (Aggregate Root, Value Objects imutáveis), aplique Design Patterns (Strategy, Factory, Outbox).
2. Execute `dotnet test`. Todos os testes **DEVEM PERMANECER PASSANDO**.


---

## 📋 3. TDD nas Especificações e Planejamento

1. Toda especificação criada em `.agents/specs/` **deve listar explicitamente os casos de teste que serão escritos ANTES da codificação**.
2. Discussões de alinhamento com o usuário devem explicitar os cenários de teste (casos de sucesso, exceções de domínio e falhas de borda) como o primeiro item do planejamento.

---

## ⛔ Violações Proibidas
- ❌ Escrever a implementação inteira e depois criar testes "para cobrir".
- ❌ Commitar código de produção que não possui teste TDD associado.
- ❌ Pular a etapa de verificação do teste falhando (Red).
