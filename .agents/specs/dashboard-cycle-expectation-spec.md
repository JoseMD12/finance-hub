# Especificação Técnica: Dashboard de Expectativa do Ciclo Financeiro

**Documento:** `.agents/specs/dashboard-cycle-expectation-spec.md`
**Status:** 🟢 `Aprovada / Em Implementação (Fatias A + E)`
**Data:** 27/08/2026
**Branch:** `feature/dashboard-overview`
**Serviços Envolvidos:** `FinanceHub.PluggyIntegration`, `FinanceHub.TransactionAggregator`, `FinanceHub.ApiGateway`, `FinanceHub.Web`

---

## 🎯 1. Visão Geral & Objetivos

O Dashboard atual (`DashboardPage.tsx`) renderiza três cards de KPI, uma lista de saldos por
instituição e um donut de categorias — mas quase tudo é **interface morta**, alimentada por
campos que o backend nunca envia.

| Campo esperado por `dashboard.types.ts` | O que o Gateway realmente envia |
| --- | --- |
| `monthlyIncomeBrl`, `monthlyExpenseBrl` | *(nada)* → cards fixos em `R$ 0,00` |
| `categoryExpenses[]` | *(nada)* → donut sempre no empty state |
| `accountBalances[].institutionName` | apenas `institutionId` → renderiza `undefined` |
| `accountBalances[].balanceBrl` | `amount` → renderiza `R$ 0,00` |

Fontes: `src/Web/FinanceHub.Web/src/features/dashboard/types/dashboard.types.ts` versus
`src/Services/ApiGateway/FinanceHub.ApiGateway/DTOs/DashboardResponseDto.cs`.

Mas o objetivo real desta especificação é maior que corrigir o contrato. A pergunta que o
Dashboard precisa responder é **"quanto ainda posso gastar este mês?"** — num mês que não é o
mês do calendário. A vida financeira do usuário é ancorada no **1º e 2º salário** e nos
**vencimentos da 1ª e 2ª fatura**.

E há um problema estrutural por baixo: **hoje as faturas vêm todas num blob único**. O que a
interface chama de "Faturas" é `sum(saldos negativos)`, sem separar a fatura de agosto
(fechada, a pagar) da de setembro (aberta, ainda acumulando).

### Definição de pronto

O Dashboard responde, de relance:

> "Recebo X dos meus salários. Tenho Y na fatura do Itaú e Z na do Inter. Até hoje gastei A.
> Então ainda tenho **B** para gastar — e no dia 20 meu saldo cai para o mínimo do ciclo."

---

## 📡 2. O que a Meu.Pluggy Gratuita Realmente Entrega

Verificado no código, não presumido:

| Campo | Onde está | Status |
| --- | --- | --- |
| `creditData.balanceDueDate` | `PluggyAccountDto.cs:6` | ✅ vem; `PluggyAccount.ParseDueDate()` já converte |
| `creditData.creditLimit` / `availableCreditLimit` | `PluggyAccountDto.cs:4-5` | ✅ vem; usado em `GetPluggyItemsQueryHandler.cs:70` |
| `balance` da conta de crédito | `PluggyAccountDto.cs:13` | ✅ equivale à fatura atual em aberto |
| `type=CREDIT` / `subtype=CREDIT_CARD` | `AccountType.cs` | ✅ `IsCreditCard` já existe |
| **data de fechamento** | — | ❌ não mapeado |
| **parcelas** (`CurrentInstallment` / `TotalInstallments`) | `PluggyTransactionMapper.cs:50-51` | ❌ `null` hardcoded |

### 2.1 Três bloqueios que precisam cair antes de qualquer gráfico

1. **`InvoiceDueDate` é descartado.** O evento `InvoiceItemIngested` carrega a data de
   vencimento (`PluggyTransactionMapper.cs:53`), mas `InvoiceItemIngestedConsumer.cs:33` monta
   o `IngestTransactionCommand` **sem ela**. O Aggregator hoje não sabe que faturas vencem.

2. **Limite e vencimento só existem atrás do token da Pluggy.** `creditData` nunca é
   persistido; é lido ao vivo em chamadas que exigem o header `X-Pluggy-Access-Token`
   (`PluggyGatewayEndpoints.cs:116`). O Dashboard não possui esse token. Enquanto o sync não
   persistir esses campos, nenhum widget de fatura é possível.

3. **`balanceCloseDate` exige confirmação empírica.** O `PluggyAccountDto` mapeia apenas três
   campos de `creditData`. O recurso de conta da Pluggy costuma trazer mais, mas **isto não
   deve ser afirmado sem observação direta** — a tarefa A.1 loga o payload cru de um sync de
   cartão e registra o resultado nesta seção. O desenho **não depende** desse campo: havendo,
   é usado; não havendo, cai no fallback configurável.

### 2.2 Resultado de A.1 — observação direta da API (27/08/2026)

Consulta real a `https://my-api.pluggy.ai` com token do usuário, sobre três conexões ativas
(Itaú, Banco Inter e Mercado Pago), 4 contas de crédito e 500 transações de cartão.

#### `creditData` por conta de cartão

Campos realmente devolvidos: `level`, `brand`, `brandAdditionalInfo`, `balanceCloseDate`,
`balanceDueDate`, `availableCreditLimit`, `balanceForeignCurrency`, `minimumPayment`,
`creditLimit`, `isLimitFlexible`, `holderType`, `status`, `disaggregatedCreditLimits`,
`additionalCards`.

| Achado | Consequência |
| --- | --- |
| **`balanceCloseDate` veio `null` nos três cartões** | A cascata de fallback do fechamento é **obrigatória**, não opcional. A decisão híbrida (seção 4.1) estava certa. |
| **`balanceDueDate` vem preenchido, mas no passado**: Itaú `2026-08-25`, MP `2026-08-13`, Inter `2026-08-12`, contra hoje `2026-08-27` | A Pluggy reporta o vencimento da **última fatura fechada**, não da próxima. É preciso rolar a data para frente pelo dia do mês, nunca usar o campo cru como "próximo vencimento". |
| `minimumPayment` disponível (`61,96` / `250,67` / `168,25`) | Permite mostrar o mínimo ao lado do total da fatura. Mapeado. |
| `creditLimit` e `availableCreditLimit` sempre presentes e coerentes | A barra de uso do limite é viável de imediato. |
| `disaggregatedCreditLimits[].customizedLimitAmount` (Itaú: `3.872,37` contra `creditLimit` `12.150`) | O usuário tem limite personalizado menor que o do banco. A barra de uso deve considerar o personalizado, senão subestima o consumo. |

#### `creditCardMetadata` por transação

Campos devolvidos: `billForecastDate`, `billId`, `cardNumber`, `installmentNumber`,
`payeeMCC`, `purchaseDate`, `totalInstallments`.

| Achado | Consequência |
| --- | --- |
| **`billForecastDate` (`"YYYY-MM"`) é a competência da fatura, dada diretamente pela Pluggy** | **Muda a Fatia B**: na maioria dos casos não é preciso derivar o fechamento para saber a que fatura a compra pertence — a Pluggy já diz. |
| **Mas vem `null` em 166 de 500 transações (33%)** | A regra de fechamento continua necessária como fallback para o terço restante. As duas estratégias coexistem: `billForecastDate` quando existe, regra derivada quando não. |
| **`billId` é um UUID estável por fatura** (8 distintos na amostra) | Identidade de fatura melhor que `(conta, competência)`. `CreditCardInvoice` deve chavear por `billId` quando disponível. |
| `installmentNumber` / `totalInstallments` presentes em 27 transações | Parcelamento é real e utilizável para projetar faturas futuras. |
| **`purchaseDate` difere de `date` em parcelamentos** — parcela `2/2` com compra em `2026-07-06` e lançamento em `2026-08-19` | A ingestão hoje usa a data de lançamento. Para "quando eu comprei" a data certa é `purchaseDate`; para "em que fatura cai" é `billForecastDate`. São três datas distintas e o modelo precisa guardar as três. |

> **Conclusão de A.1**: a fundação é viável com o plano gratuito. Os dois ajustes de rota são
> tratar `balanceDueDate` como data passada a ser rolada, e usar `billForecastDate`/`billId`
> como fonte primária de competência, com a regra de fechamento apenas como fallback.

### 2.3 O que o usuário ainda precisa informar

Levantamento fechado após a análise do histórico real (seções 3.4.1 e 4.1.1). Quase tudo é
detectável — o que sobra para digitar é o que **não existe nos dados**, por definição.

| Dado | Origem | Digitar? |
| --- | --- | --- |
| Valor da fatura atual | `balance` da conta de crédito | Não |
| Competência de cada compra | `billForecastDate` (67%) + regra derivada | Não |
| Vencimento da fatura | `balanceDueDate`, rolado para frente | Não |
| **Dia de fechamento** | **derivado das fronteiras de `billForecastDate`** | Não — só confirmar |
| Limite e limite personalizado | `creditLimit` / `customizedLimitAmount` | Não |
| Pagamento mínimo | `minimumPayment` | Não |
| Parcelamento | `installmentNumber` / `totalInstallments` | Não |
| **Dia do 1º e 2º salário** | **detectado por cluster de dia do mês** | Não — só confirmar |
| **Valor esperado dos salários** | mediana da janela recente | Não — só confirmar |
| **Quais créditos são receita fixa** | ambíguo nos dados: aluguel recebido, reembolso de amigo e salário têm a mesma forma | **Sim** — marcar uma vez |
| **Reserva mensal desejada** | não existe em dado bancário | **Sim** |

Ou seja: o `CycleSettingsModal` nasce **todo preenchido**, e o trabalho do usuário é revisar e
marcar quais receitas recorrentes ele considera fixas. Nada de configuração em branco.

---

## 🧾 3. Fatura como Entidade de Primeira Classe

Esta é a mudança que resolve a separação entre "a fatura de agosto" e "a de setembro".

> **Revisado após A.1.** O desenho original derivava a competência da data de fechamento. A
> observação direta da API mostrou que a Meu.Pluggy **já informa a competência** por transação.
> A regra derivada continua necessária, mas rebaixada a fallback.

### 3.1 O modelo

Nova agregação `CreditCardInvoice` no `TransactionAggregator`:

```
ExternalBillId   7aad06fa-1b36-4af3-a404-22e91fe61883   (billId da Pluggy, quando houver)
ReferenceMonth   2026-09                                 (competência)
ClosingDateUtc   2026-09-25                              (derivado — a API devolve null)
DueDateUtc       2026-10-05                              (rolado para frente, ver 3.3)
TotalAmountBrl   (soma das transações atribuídas)
MinimumPaymentBrl
Status           Futura | Aberta | Fechada | Paga
```

**Identidade**: `ExternalBillId` quando a Pluggy o fornece — é um UUID estável por fatura, mais
confiável que qualquer chave que possamos montar. Quando ausente, cai para
`(UserId, InstitutionId, AccountNumber, ReferenceMonth)`.

### 3.2 A regra de atribuição — duas fontes, nesta ordem

**1ª — a competência dada pela Pluggy** (`creditCardMetadata.billForecastDate`, formato
`"YYYY-MM"`). Cobre ~67% das transações observadas. É autoritativa: veio do próprio emissor.

**2ª — a regra derivada do fechamento**, para o terço restante em que `billForecastDate` é nulo:

```
compra.Data <= fechamento(M)  →  fatura M
compra.Data >  fechamento(M)  →  fatura M+1
```

Com fechamento no dia 25, em 27/08/2026 existem simultaneamente:

- **fatura de agosto** — compras de 26/07 a 25/08 — *fechada, vence 05/09*
- **fatura de setembro** — compras de 26/08 em diante — *aberta, ainda acumulando*

### 3.3 As três datas de uma compra no cartão

A.1 revelou que uma compra parcelada carrega **três datas distintas**, e confundi-las produz
números errados. Exemplo real observado — parcela `2/2` da SUPERLEGAL:

| Data | Valor no exemplo | Responde |
| --- | --- | --- |
| `purchaseDate` | `2026-07-06` | "quando eu comprei" |
| `date` (lançamento) | `2026-08-19` | "quando entrou no extrato" |
| `billForecastDate` | `2026-08` | "em qual fatura vai ser cobrado" |

A ingestão atual usa apenas a data de lançamento. O modelo precisa guardar as três.

### 3.4 Origem do dia de fechamento (cascata de fallback)

1. ~~`creditData.balanceCloseDate`~~ — **descartado**: A.1 confirmou que a API expõe o campo mas
   devolve `null` nos três cartões. Mantido no DTO apenas para o caso de algum connector passar
   a preenchê-lo.
2. **Derivar das fronteiras de `billForecastDate`** — ver 3.4.1. É a fonte boa, descoberta em
   A.1-b, e dispensa o usuário digitar.
3. `balanceDueDate − DueOffsetDays` (padrão 10) quando não há histórico suficiente.
4. `ClosingDay` configurado pelo usuário por cartão — override sempre disponível.

#### 3.4.1 O fechamento é derivável do próprio histórico

Agrupando as compras por `billForecastDate`, a fronteira entre duas competências consecutivas
**é** a data de fechamento. Observado no cartão Itaú (`0c62a72e`) em 27/08/2026:

| Competência | Compras de … até |
| --- | --- |
| 2026-01 | 2025-12-30 → 2026-01-28 |
| 2026-02 | 2026-01-29 → 2026-02-26 |
| 2026-03 | 2026-02-27 → 2026-03-29 |
| 2026-04 | 2026-03-30 → 2026-04-29 |

As fronteiras são contíguas — dia 28 fecha, dia 29 já cai na competência seguinte. **Fechamento
= dia 28**, derivado sem nenhuma configuração. O vencimento da API é dia 25 do mês seguinte,
o que dá um intervalo fechamento→vencimento de ~27 dias, coerente.

Qualidade da derivação por cartão, na amostra:

| Cartão | Competências com dados | Fechamento derivado | Confiança |
| --- | --- | --- | --- |
| Itaú | 8 | dia 28 | **Alta** — fronteiras contíguas e estáveis por 5 meses |
| Inter | 3 | ~dia 30/31 | Média — fronteiras com lacuna (31/07 → 03/08) |
| Mercado Pago | 3 | indeterminado | Baixa — poucas compras, lacunas grandes |

Por isso a UI mostra a **confiança** junto do valor derivado: alta é apresentada como fato
confirmável, baixa pede confirmação ativa do usuário.

### 3.5 O vencimento precisa ser rolado para frente

A.1 mostrou que `balanceDueDate` traz o vencimento da **última fatura fechada**, não da próxima:
em 27/08/2026 os três cartões devolveram `2026-08-25`, `2026-08-13` e `2026-08-12` — todos no
passado.

A ingestão grava o valor **cru, como veio**, sem interpretar. Quem rola para frente é o domínio
do ciclo, extraindo o dia do mês e projetando a próxima ocorrência a partir da data de
referência. Tratar o campo cru como "próximo vencimento" produziria uma linha do tempo inteira
com datas vencidas.

### 3.6 Parcelas

`installmentNumber` e `totalInstallments` são reais e utilizáveis — 27 das 500 transações
observadas são parceladas. Cada parcela futura cria linhas nas faturas seguintes, permitindo
responder *"a fatura de outubro já nasce com R$ 400 comprometidos"*.

Cuidado de borda já implementado: connectors chegam a devolver `0` para compras à vista. Zero e
negativos são normalizados para `null` na borda do `PluggyIntegration`, para que o domínio do
Aggregator — que rejeita numeração de parcela não positiva — nunca receba valor inválido.

---

## 🔁 4. O Ciclo Financeiro

O mês do calendário é a unidade errada. O **Ciclo** vai do 1º salário ao dia anterior ao
próximo 1º salário.

### 4.1 Configuração híbrida (decisão do usuário)

Nova entidade `UserFinancialCycleSettings` — precedente no repositório: `UserCategoryRule` já
é configuração por usuário. Cada campo guarda **valor e origem**
(`Detectado` | `Configurado` | `Padrão`):

- `PrimaryIncomeDay` / `SecondaryIncomeDay`
- `CycleAnchorDay` — dia que abre o ciclo (padrão: `PrimaryIncomeDay`)
- por cartão: `ClosingDay`, `DueDay`
- `ReserveAmountBrl` — reserva opcional descontada do disponível

#### 4.1.1 A detecção de receita fixa foi validada contra o histórico real

Análise dos créditos das três contas correntes em 27/08/2026, agrupados por descrição
normalizada e por dia do mês:

```
SALÁRIO INSTITUTO DE PESQUISAS ELDORADO   (Itaú)
  2026-06-25   R$ 3.466,73
  2026-07-10   R$ 2.960,00      ← 1º salário
  2026-07-24   R$ 2.630,11      ← 2º salário
  2026-08-08   R$ 2.960,00      ← 1º salário
  2026-08-25   R$ 2.674,68      ← 2º salário
```

O padrão do usuário aparece sozinho nos dados: **dois créditos por mês**, um por volta do dia
8–10 com **valor fixo de R$ 2.960,00**, e outro por volta do dia 24–25 com valor variável entre
R$ 2.630 e R$ 3.466. É exatamente o modelo de adiantamento e pagamento que motiva o ciclo.

**Algoritmo**: agrupar créditos por descrição normalizada (removendo dígitos), exigir ocorrência
em ≥3 meses distintos, e então separar os lançamentos em *clusters* por dia do mês. Dois
clusters bem separados significam duas receitas fixas no ciclo, não uma.

#### 4.1.2 A armadilha: receita descontinuada

```
PIX RECEBIDO - PRETO NO BRANCO TECNOLOGIA   (Inter)  — sempre no dia 1
  set/25 a mar/26   R$ 5.000,00
  abr/26 a jun/26   R$ 2.000,00
  jul/26 em diante  (nada)
```

Oito ocorrências, sempre no dia 1, absolutamente regular — e **encerrada em junho**. A mediana
de todo o histórico devolveria R$ 5.000 e inflaria o "disponível" em milhares de reais todo mês.

Duas defesas obrigatórias no estimador:

1. **Janela recente**: estimar sobre os últimos 3–6 ciclos, nunca sobre todo o histórico.
2. **Detecção de descontinuidade**: se a receita não aparece nos 2 ciclos mais recentes, ela é
   marcada como encerrada e sai da previsão — sem apagar o histórico, e avisando o usuário.

A mediana (e não a média) continua sendo a estatística certa dentro da janela, para que um 13º
ou uma PLR não distorça a projeção.

Regras da estratégia híbrida:

- O sistema estima e apresenta o valor como **sugestão editável**, nunca como fato silencioso.
  A interface exibe a origem — o usuário precisa saber que `R$ 4.200` é um palpite do
  histórico, e não um valor que ele confirmou.
- Uma vez que o usuário edita um campo, a detecção **nunca mais o sobrescreve**.
- O Dashboard funciona no primeiro acesso, sem tela de configuração bloqueante.
- `CycleSettingsModal` é onde ele corrige, com os valores detectados pré-preenchidos.

### 4.2 A fórmula

```
Disponível = ReceitaPrevista
           − FaturasQueVencemNesteCiclo
           − GastoEmContaRealizado
           − CompromissosFixosPendentes
           − Reserva
```

Dois acertos que o código **já possui** e que impedem a conta de mentir:

- **`IsBillPayment`** — sem ele, pagar a fatura contaria duas vezes: uma como fatura e outra
  como débito em conta.
- **`IsIgnoredInTotals`** — transferências internas e lançamentos neutros já ficam de fora.

`ReceitaPrevista` = o que já caiu somado ao que falta cair, estimado pela **mediana** dos
últimos 3 a 6 ciclos na categoria `Receitas > Salário` (já semeada em
`categories.default.json`). Mediana, não média: um 13º ou uma PLR distorce a média e infla
artificialmente o disponível.

---

## 🧠 4-A. Motor de Detecção de Padrões (genérico, sem conhecimento do usuário)

> **Princípio inegociável**: a aplicação não conhece nenhum usuário em particular. Toda detecção
> é estatística e precisa funcionar numa conta criada hoje, de outra pessoa, sem histórico. Os
> exemplos das seções 3.4.1 e 4.1.1 são **validação** do motor contra dados reais, nunca
> entrada dele.

### 4-A.0 Dívida paga: o dado pessoal saiu do código `✅ Implementado`

> Concluído em 27/08/2026, antes da Fatia E. 238/238 testes verdes.

**O que foi removido de `IngestTransactionCommandHandler`:** o bloco que classificava
neutralidade casando o texto da descrição contra o nome completo do usuário e contra nomes de
produto de banco, além de três `Guid.Parse` inline.

**O que entrou no lugar** — neutralidade derivada da natureza econômica da categoria, alinhada
a `transit-transfers-and-neutrality-engine-spec.md` §2.1:

- `Category.Nature` — nova propriedade, declarada no dataset `categories.default.json`. Só as
  exceções se declaram: `Outros > Transferências` é `Transfer`, `Finanças > Investimentos` é
  `Investment`, `Outros > Ajustes` é `Adjustment`. Subcategoria herda do pai; ausência é
  `Operating`.
- `CanonicalTransaction.ApplyNature(nature)` — aplica a natureza e deriva a neutralidade.
  Dinheiro que só muda de lugar não entra nos totais.
- `ICategoryRepository.GetNatureByCategoryIdAsync` — devolve `Operating` para categoria
  inexistente, para que catálogo incompleto nunca quebre a ingestão.
- `SystemCategoryIds` — constantes do catálogo, elimina os `Guid.Parse` inline (Regra 10).
- `TransactionBillPaymentDetector` — vocabulário bancário brasileiro de pagamento de fatura,
  em domain service. Conhecimento de mercado, legítimo em código.
- Migration `20260827164240_AddNatureToCategories`, com `UPDATE`s que corrigem o catálogo em
  bases já semeadas — o seed só roda em base vazia, então sem isso uma instalação existente
  ficaria com tudo como `Operating` e transferências voltariam a contar como gasto.

**Dois bugs reais encontrados no caminho:**

1. **Tarifa bancária virava pagamento de fatura.** O código usava
   `11111111-...-110801` como "categoria de pagamento de fatura", mas esse id é
   `Finanças > Tarifas`. Toda tarifa era marcada `IsBillPayment` e sumia dos totais.
2. **Resgate de cofrinho virava receita.** `merchants.brazil.json` mapeava
   `DINHEIRO RETIRADO` para `Receitas > Rendimentos`. Dinheiro voltando de aplicação era somado
   à renda — inflando tanto as entradas quanto o "disponível para gastar", justamente o número
   central do Dashboard. Remapeado para `Finanças > Investimentos`, que é neutro por natureza.

**Lacuna assumida e o caminho genérico:** a regra que dependia do nome do titular cobria
*transferências entre contas do próprio usuário*. Ela sai agora sem substituto imediato. O
substituto genérico é detecção de mesma titularidade — via `/identity` da Pluggy ou por
pareamento de débito e crédito de mesmo valor em contas diferentes do mesmo usuário numa janela
curta, ampliando o `TransferPairMatchingEngine` que já existe. Fica na fatia **P**.

Enquanto isso, o usuário tem o caminho de dado já pronto: categorizar uma dessas transações
como `Transferências` com `createCustomRule: true` e `applyToPastTransactions: true` gera um
`UserCategoryRule` que cobre o histórico e as futuras — que é exatamente a migração de código
para dado.

### 4-A.0.1 Referência: como era a dívida

`IngestTransactionCommandHandler.cs:105-118` classifica transferências como neutras usando o
**nome completo do usuário** escrito num `if`, junto de GUIDs de categoria via `Guid.Parse`
inline (viola também a Regra 10, zero magic strings).

Distinção que orienta a limpeza:

| Camada | Onde vive | Exemplo | Legítimo? |
| --- | --- | --- | --- |
| Vocabulário de mercado | dataset versionado | `merchants.brazil.json` com "COFRINHO", "PIX RECEBIDO" | ✅ Sim — vale para qualquer brasileiro |
| Regra pessoal | **banco de dados** | "transferências para meu próprio nome são neutras" | ✅ Sim, como `UserCategoryRule` |
| Regra pessoal | **código** | `descUpper.Contains("JOSE HENRIQUE...")` | ❌ Nunca |

O mecanismo certo **já existe**: `UserCategoryRule` é config por usuário no banco. A migração é
código → dado: as regras hoje hardcoded viram linhas de `UserCategoryRule` semeadas na primeira
sincronização, e o `if` sai do handler.

Caso genérico equivalente, que substitui a regra do nome próprio: **transferência entre contas
do mesmo titular**. Detectável sem saber o nome de ninguém — o titular vem de
`/identity` da Pluggy, ou por pareamento de débito e crédito de mesmo valor em contas diferentes
do mesmo usuário dentro de uma janela curta. O `TransferPairMatchingEngine` já faz metade disso.

### 4-A.1 Onde mora: camada desacoplada, não serviço separado

Recomendação: **módulo com fronteira explícita dentro do `TransactionAggregator`**, em
`Application/Services/PatternDetection/`, atrás de `IPatternDetectionEngine`.

Um microsserviço próprio foi considerado e **descartado por ora**: a Regra 1 proíbe acesso
cruzado a banco entre serviços, então um serviço separado teria que **replicar o ledger
canônico inteiro** para poder analisá-lo. O custo de consistência não se paga enquanto a
detecção for batch e leve.

O desacoplamento que importa é o de **dependência**, não o de processo:

- o motor lê o ledger por uma porta de leitura própria e **nunca escreve nele**;
- a saída é `DetectedPattern`, tabela separada, que é **proposta**, não fato;
- nenhuma outra parte do sistema depende do motor para funcionar — sem detecção, o Dashboard
  cai no modo realizado (4-A.4).

Isso mantém a opção aberta: quando a detecção passar a exigir janela de execução própria ou
escalar diferente do Aggregator, ela vira serviço com o mesmo contrato, sem reescrita.

### 4-A.2 Os quatro detectores, todos estatísticos

Rodam em sequência, cada um consumindo a saída do anterior. Nenhum conhece categorias
específicas, nomes ou instituições.

**1 · Assinatura estável** — agrupa lançamentos que são "a mesma coisa acontecendo de novo".
A chave **não é** a descrição crua. Ordem de preferência, do mais forte ao mais fraco:

1. contraparte estruturada (`paymentData.payer` / `receiver` da Pluggy) — é identidade real;
2. `merchantName` já normalizado pelo pipeline existente;
3. descrição normalizada: sem acento, sem dígito, caixa alta, espaços colapsados.

Os afixos a remover (`PIX RECEBIDO -`, `TRANSFERÊNCIA RECEBIDA`) **não são lista fixa em
código**: são os prefixos e sufixos mais frequentes no corpus do próprio usuário, extraídos por
frequência. Assim o motor se adapta ao vocabulário de qualquer banco, inclusive um que ainda
não integramos.

**2 · Recorrência** — para cada grupo, calcula os intervalos entre ocorrências consecutivas e
testa se a distribuição se concentra em torno de uma cadência conhecida (~7, ~14/15, ~30 dias).
Critério de robustez: **mediana** do intervalo e **desvio absoluto mediano** (MAD) — não média e
desvio padrão, que uma única ocorrência atrasada distorce. Exige presença em ≥3 períodos
distintos.

**3 · Cadência por dia do mês** — separa um grupo em N eventos por mês. O agrupamento é
**circular**: dia 31 e dia 1 são vizinhos, não opostos. Um agrupamento linear ingênuo quebraria
justamente em quem recebe na virada do mês. Dois clusters bem separados significam duas
receitas fixas no ciclo — é o que faz o modelo de 1º e 2º salário emergir sozinho, sem nada
codificado sobre adiantamento.

**4 · Continuidade e valor** — projeta a próxima ocorrência pela cadência. Se o padrão perdeu
≥2 ocorrências esperadas, é marcado `Descontinuado` e sai da previsão sem apagar o histórico.
O valor esperado é a **mediana da janela recente** (3–6 ciclos), com o coeficiente de variação
distinguindo receita fixa de variável.

O mesmo motor de fronteira serve para o **fechamento do cartão** (3.4.1): agrupar por
competência e achar a descontinuidade entre grupos consecutivos é o mesmo problema de detecção
de fronteira, sem nada específico de bandeira ou banco.

### 4-A.3 A saída é proposta, nunca fato

```
DetectedPattern
  Kind             ReceitaFixa | DespesaFixa | FechamentoCartao | AncoraDeCiclo
  SignatureKey     (assinatura estável, não a descrição crua)
  Cadence          Mensal | Quinzenal | Semanal
  DayOfMonth       8            (do cluster circular)
  ExpectedAmount   2960.00      (mediana da janela)
  AmountVariability Fixo | Variavel
  Confidence       0.0–1.0      (derivada de nº de ocorrências, MAD e recência)
  Status           Ativo | Descontinuado
  UserDecision     Pendente | Confirmado | Rejeitado | Editado
```

`UserDecision` é o que fecha o ciclo com a decisão híbrida da seção 4.1: uma vez `Editado` ou
`Rejeitado`, **nenhuma re-detecção sobrescreve**. O usuário sempre ganha do estimador.

### 4-A.4 O problema de partida a frio — comportamento definido por profundidade

A pergunta certa não é "como detectar do zero", é "o que a tela mostra enquanto não dá para
detectar". Sem isso definido, uma conta nova vê um dashboard quebrado.

| Histórico | O que o motor entrega | O que o Dashboard mostra |
| --- | --- | --- |
| **0 ciclos** | nada | **Modo realizado**: saldos, faturas, gastos por categoria. Sem projeção, sem "disponível". Convite explícito para informar receitas. |
| **1–2 ciclos** | candidatos com confiança baixa | Projeção marcada como estimativa, com pedido ativo de confirmação. |
| **3+ ciclos** | padrões com confiança alta | Ciclo completo: Fôlego, linha do tempo e burn-down. |

Dois atenuantes importantes que reduzem o problema de partida a frio:

- **A parte de cartão não precisa de histórico nenhum.** Fatura, vencimento, limite e
  competência vêm prontos da Pluggy no primeiro sync. Metade do Dashboard funciona no dia zero.
- **A Pluggy devolve histórico retroativo** — na amostra observada, de 8 a 10 meses na primeira
  sincronização. Na prática, uma conta "nova" no FinanceHub costuma já nascer com histórico
  suficiente para o motor. O modo realizado é a rede de segurança para quem não tem.

### 4-A.5 Como testar sem tautologia

Testar detecção contra o dado real de um usuário é teste que se auto-aprova. O motor é testado
com **séries sintéticas construídas para o caso**, cobrindo:

- salário quinzenal com dia variando por dia útil (dia 30 caindo em sábado → pago dia 29);
- receita que atravessa a virada do ano e do mês (o caso circular);
- receita descontinuada — o motor precisa parar de projetá-la;
- outlier de 13º — a mediana não pode se mover;
- histórico de um único mês — precisa devolver confiança baixa, não um palpite confiante;
- corpus vazio — precisa devolver vazio sem lançar exceção.

Os dados reais das seções 3.4.1 e 4.1.1 servem como **teste de aceitação de fim a fim**, rodado
manualmente, jamais como fixture versionada — são dados financeiros pessoais e não entram no
repositório.

---

## 📊 5. Os Gráficos

Método aplicado: **a forma vem da pergunta**, e a cor vem por último.

### Nível 1 — A resposta · "Fôlego do Ciclo"

**Forma:** número-herói somado a **uma barra empilhada horizontal**. Não é gráfico de análise,
é um placar — a decomposição de uma grandeza única em partes pede exatamente isso.

```
Receita do ciclo · R$ 8.500
├─ Fatura Itaú 2.100 ─┤├─ Fatura Inter 1.400 ─┤├─ Gasto em conta 1.900 ─┤├─ LIVRE 3.100 ─┤
                                                              ▲ hoje, dia 12 de 30
```

Herói: **`R$ 3.100 livres` · `R$ 172/dia até dia 14`**. A divisão por dia restante é o que
transforma o número em decisão.

**Cor:** os segmentos são *estados*, não entidades — neutros de peso decrescente para o
comprometido, e o acento da marca apenas no "LIVRE". Disponível negativo vira o polo negativo
do par divergente, com ícone e rótulo, nunca só cor.

### Nível 2 — O quando · "Linha do Tempo do Ciclo"

O widget mais valioso para o dia a dia, e o que hoje não existe em lugar nenhum.

**Forma:** `ComposedChart` — área de **saldo projetado dia a dia** ao longo do ciclo, com
marcadores de evento:

- ▲ 1º salário · ▲ 2º salário — entradas
- ▼ vencimento Itaú · ▼ vencimento Inter — saídas
- linha vertical "hoje"; sólido até hoje, tracejado depois (realizado versus projetado)

Callout: **"menor saldo previsto: R$ 340 no dia 20"**. É a pergunta real — *sobrevivo até o
próximo salário?* — e ela só aparece num gráfico com eixo de tempo e eventos marcados.

**Cor:** divergente com zero neutro; status com ícone nos marcadores.

### Nível 3 — O ritmo · "Burn-down do Ciclo"

**Forma:** linha de **gasto acumulado** (sólida, até hoje) contra **ritmo ideal** (tracejada,
orçamento dividido pelos dias) e **projeção de fechamento** (pontilhada).

Responde "no ritmo atual fecho o ciclo em R$ N" — antes de estourar, não depois.

> **Um eixo apenas.** A tentação é plotar "R$ acumulado" e "% do orçamento" em dois eixos y.
> É o erro nº 1 do catálogo de anti-padrões de visualização. Tudo indexado a uma escala.

### Nível 4 — As faturas · "Faturas por Competência"

**Forma:** **colunas empilhadas, uma por competência** (ago · set · out · nov), empilhadas por
cartão. O estado da fatura entra como **textura, não como cor**:

- **fechada / a pagar** → preenchimento sólido
- **aberta / acumulando** → sólido com topo mais claro indicando o que ainda vai entrar
- **futura / só parcelas** → hachurada

Leitura de relance: *"agosto fechou em 3.500, setembro já está em 1.200 no dia 12, outubro já
nasce com 400 de parcelas"*.

Abaixo, um card por cartão: valor, **pagamento mínimo** (`minimumPayment`, disponível),
vencimento em D-N, **barra de uso do limite** e variação percentual contra o ciclo anterior.

> **Correção vinda de A.1 — a barra de limite não pode usar `creditLimit`.** O Itaú devolve
> `creditLimit` de `R$ 12.150`, mas `disaggregatedCreditLimits[].customizedLimitAmount` de
> `R$ 3.872,37` — o limite que o usuário mesmo definiu. Usar o limite do banco mostraria 23% de
> consumo onde o real é 71%, que é justamente o número que importa. A barra usa
> `customizedLimitAmount` quando existe, e `creditLimit` só como fallback.

**Cor:** categórica **por instituição**, em ordem fixa. A mesma cor do cartão em todo o
aplicativo — a cor segue a entidade, nunca a posição na pilha.

### Nível 5 — Contexto

- **Onde gastei** — barras horizontais ranqueadas, com variação contra o ciclo anterior. Donut
  com oito categorias é anti-padrão: ninguém compara ângulos. A barra ordenada responde "meu
  maior gasto foi X" em um olhar.
- **Evolução por ciclo** — barras dos últimos seis ciclos: receita, despesa, resultado.

### O que não colocar

- Donut como herói — não responde "quanto posso gastar".
- Velocímetro ou gauge — ocupa muito, informa pouco.
- Pizza de receitas com duas fatias.
- Qualquer eixo duplo.

---

## 🎨 6. Achado de Cor que Exige Ação

`shared/constants/institutions.ts` define as cores por instituição como **classes de tag**:
Itaú `amber`, Inter `orange`, Mercado Pago `sky`, Nubank `purple`.

Amber e orange são hues adjacentes. Funcionam como tags separadas por espaço em branco, mas
**empilhadas e encostadas numa mesma coluna provavelmente reprovam** na separação por
deficiência de visão de cor, e possivelmente até no piso de visão normal.

Ação obrigatória antes de fechar a fatia E: rodar o validador de paleta em modo claro e
escuro. Se reprovar, derivar uma **rampa de gráfico** separada das cores de tag — mantendo a
associação instituição→hue, porém re-degrauzada até passar. Tokens novos vão para
`src/index.css` (Regra 17), nunca hex inline.

---

## 🗂️ 7. Fatiamento

| Fatia | Entrega | Depende de | Status |
| --- | --- | --- | --- |
| **A** | Fundação: descoberta do payload, persistir vencimento/limite/fechamento no sync | — | ✅ Implementada |
| **E** | Contrato real do dashboard atual, barras ranqueadas, evolução, filtro de período | — | 🔵 Backend pronto (E.1–E.3); frontend (E.4) pendente |
| **B** | `CreditCardInvoice` por `billId`/`billForecastDate`, gráfico "Faturas por Competência" | A | ⚪ Planejada |
| **C** | `UserFinancialCycleSettings` híbrido, "Fôlego do Ciclo", burn-down | B | ⚪ Planejada |
| **D** | "Linha do Tempo do Ciclo" | C | ⚪ Planejada |
| **P0** | Remoção do dado pessoal do código; neutralidade por natureza da categoria | — | ✅ Implementada |
| **P** | Motor de Detecção de Padrões (seção 4-A) + titularidade própria genérica | P0 | ⚪ Planejada — **pré-requisito de C** |

> A fatia **P** foi criada depois que o usuário reforçou que a aplicação precisa ser genérica.
> Ela absorve a detecção que a fatia C pressupunha, e paga a dívida de
> `IngestTransactionCommandHandler.cs:105-118`. Como C depende de estimar receita, **P vem antes
> de C**. A limpeza do `if` com nome pessoal pode ser antecipada isoladamente — é pequena e
> independente do resto.

**A branch `feature/dashboard-overview` entrega A + E.** A é pré-requisito de tudo e é pequena;
E é o que o usuário vê quebrado hoje. Juntas entregam um Dashboard que funciona de verdade e
destravam B, C e D sem inflar a revisão.

---

## 🏗️ 8. Arquitetura da Implementação (A + E)

### 8.1 Fatia A — Fundação `✅ Implementada`

#### A.1 — Descoberta

Resolvida por duas vias complementares:

1. **Permanente**: `[JsonExtensionData] AdditionalFields` em `PluggyCreditDataDto` e
   `PluggyCreditCardMetadataDto` captura todo campo que a API manda e nós ainda não mapeamos.
   `SyncAllPluggyAccountsCommandHandler.LogCreditDataDiscovery` registra em log quais campos
   vieram preenchidos e quais chegaram sem mapeamento — só nomes de campo e valores de
   limite/data, sem PII (LGPD).
2. **Pontual**: consulta direta à API com token do usuário em 27/08/2026. Resultados na
   seção 2.2.

Escolhido mecanismo permanente em vez de log descartável porque o contrato do plano gratuito
varia por connector e vai continuar mudando.

#### A.2 — PluggyIntegration

- `PluggyCreditDataDto` ganhou `BalanceCloseDate` e `MinimumPayment`.
- `PluggyCreditCardMetadataDto` criado com `InstallmentNumber`, `TotalInstallments`, `BillId`,
  `BillForecastDate`, `PurchaseDate` e `CardNumber`; `PluggyTransactionDto` passou a expô-lo.
- `PluggyTransactionMapper` deixou de mandar `CurrentInstallment: null, TotalInstallments: null`
  hardcoded e passou a propagar os valores reais.
- `PluggyAccount` ganhou `RawBalanceCloseDate`, `CreditLimit`, `AvailableCreditLimit`,
  `ParseCloseDate()` e o utilitário público `ParseUtcDate(string?)`.
- `PluggyTransaction` normaliza parcela não positiva para `null` na borda.
- `AccountBalanceSnapshotItem` — **contrato compartilhado entre serviços** — ganhou
  `IsCreditCard`, `CreditLimit`, `AvailableCreditLimit`, `InvoiceDueDateUtc` e
  `InvoiceClosingDateUtc`, todos opcionais com default, para não quebrar publishers existentes.

#### A.3 — TransactionAggregator

- **Dois consumers descartavam os dados, não um.** Além do `InvoiceItemIngestedConsumer`
  identificado no plano, o `TransactionsBatchIngestedConsumer` — que é o **caminho quente** do
  sync, usado por `PublishBatchEventsAsync` — também montava o comando sem vencimento nem
  parcelas. Os dois foram corrigidos e há teste travando a regressão em ambos.
- **Divergência deliberada do plano quanto ao modelo.** O plano dizia estender
  `BankTransactionDetails` com "os campos de crédito". Na implementação os dados foram
  separados por dono real:
  - **por transação** → `BankTransactionDetails`: `InvoiceDueDateUtc`, `CurrentInstallment`,
    `TotalInstallments`, mais o computado `IsInstallment`.
  - **por conta** → `AccountBalance.CreditInfo`, novo value object `CreditAccountInfo` com
    `IsCreditCard`, `CreditLimit`, `AvailableCreditLimit`, `InvoiceDueDateUtc`,
    `InvoiceClosingDateUtc` e o computado `UsedCreditLimit`.

    Limite e limite disponível são fatos **da conta**. Replicá-los em cada linha de transação
    seria desnormalização que ficaria obsoleta a cada sync.
- `AccountBalance.SynchronizeCreditData()` e `AccountBalanceSnapshotSynchronizedConsumer`
  passaram a gravar os dados de crédito no snapshot oficial.
- `InvalidInstallmentDomainException` novo, para parcela não positiva ou atual maior que o total.
- Datas normalizadas para UTC na construção: `Local` converte de fato, `Unspecified` é rotulado.
- Migration `20260827160311_AddCreditInvoiceDataToTransactionsAndBalances` — inteiramente
  aditiva, todas as colunas nullable exceto `is_credit_card`, que tem default `false`. Segura
  para aplicar sobre base existente.

#### Cobertura de testes de A

`CreditInvoiceDetailsTests` (domínio), `CreditInvoiceDataPropagationTests` (os dois consumers),
`IngestTransactionCreditDataTests` (handler) e `PluggyCreditDataMappingTests` (mapeamento e
desserialização do payload real). Todos verdes, sem regressão nos 207 testes existentes.

### 8.2 Fatia E — Contrato real

- **E.1** Slice `Queries/GetDashboardSummary/` com interface e implementação em arquivos
  separados (Regra 13).

  **Reaproveitamento que evita divergência numérica:**
  `TransactionRepository.QueryPagedByFilterAsync:101` já calcula `TotalIncome`,
  `TotalExpense`, `NetBalance`, `RealConsolidatedBalanceBrl`, `TotalOpenCreditCardsBrl`,
  `ProjectedAvailableBalanceBrl` e `LastSyncAtUtc`, com as regras de neutralidade aplicadas. O
  handler chama esse mesmo método com `PageSize = 5` e recebe de uma vez os KPIs **e** o feed
  de recentes — garantindo que Dashboard e Transações nunca mostrem números diferentes para o
  mesmo período.

  > As chamadas do handler são **sequenciais**, não `Task.WhenAll`: compartilham o mesmo
  > `DbContext`, e o EF Core não permite operações concorrentes num único contexto.

- **E.2** `DashboardSummaryDto` e demais DTOs; `IDashboardReadRepository` agregando **no
  Postgres** (`GroupBy` por categoria com cauda colapsada em "Outras", e por mês nos últimos
  seis). O índice `(UserId, TransactionDateUtc, Type)` já existe — E não precisa de migration.

  Nome e logo da instituição são resolvidos no frontend por `getInstitutionInfo()`, evitando
  duplicar catálogo de bancos no .NET. `IsCreditCard = balance < 0`, mesma heurística de
  `TransactionRepository.cs:141`.

- **E.3** Endpoints centralizados em classes de extensão dedicadas (Regra 23):
  `MapDashboardEndpoints()` no Aggregator, e `/api/v1/gateway/dashboard` aceitando os filtros
  no Gateway. `DashboardResponseDto` e `/balances/consolidated` permanecem intactos — possuem
  outros consumidores. `userId` continua vindo da claim, nunca da query string.

#### Estado de E.1 a E.3 `✅ Implementado`

> Concluído em 27/08/2026. 250/250 testes verdes.

Arquivos entregues:

| Camada | Arquivo |
| --- | --- |
| Application | `Queries/GetDashboardSummary/{GetDashboardSummaryQuery,IGetDashboardSummaryQueryHandler,GetDashboardSummaryQueryHandler}.cs` |
| Application | `DTOs/DashboardSummaryDtos.cs`, `Interfaces/IDashboardReadRepository.cs` |
| Infrastructure | `Persistence/Repositories/DashboardReadRepository.cs` |
| Api | `Endpoints/DashboardEndpoints.cs`, `Endpoints/GetDashboardSummaryParameters.cs` |
| Gateway | `DTOs/GatewayDashboardSummaryDto.cs`, `GetDashboardSummaryAsync` no cliente tipado |

Decisões tomadas durante a implementação, que o desenho não previa:

- **`IsCreditCard` deixou de ser heurística.** O plano usava `balance < 0`, mas a Fatia A
  passou a persistir `AccountBalance.CreditInfo.IsCreditCard` vindo do snapshot oficial. O
  repositório de leitura usa o campo real; a heurística de sinal fica só no cálculo antigo de
  `QueryPagedByFilterAsync`, que não foi tocado.
- **`InstitutionBalanceDto` carrega os limites e o vencimento**, além do saldo. São dados que a
  Fatia A tornou disponíveis e que o card por instituição precisa; buscá-los à parte exigiria
  uma segunda consulta.
- **Percentual calculado no servidor**, arredondado a 2 casas com `MidpointRounding.AwayFromZero`.
  Evita que cada cliente refaça a conta e chegue a totais ligeiramente diferentes.
- **Degradação em vez de exceção no Gateway.** Corpo vazio do downstream devolve um Dashboard
  zerado em vez de `NullReferenceException`, para a interface renderizar o estado sem dados.
  Erro HTTP real continua virando `GatewayDownstreamException`, como nas demais rotas.
- **Flag falsa não entra na query string**, mantendo a URL limpa e o cache do downstream
  previsível.

Cobertura: `GetDashboardSummaryQueryHandlerTests` (8 casos, incluindo período vazio,
propagação de filtros e reaproveitamento do sumário) e `DashboardGatewayClientTests` (4 casos,
incluindo a degradação para Dashboard vazio).

- **E.4** Frontend. `datePresets.ts` e os tipos de filtro são **movidos** para `shared/`
  (Regra 14 proíbe import cross-feature), re-exportados de `transactions.types.ts` para não
  quebrar imports existentes. Componentes novos: `DashboardFilterBar`,
  `DashboardSummaryCards`, `InstitutionBalanceList`, `CategoryRankedBars`,
  `MonthlyCashFlowChart`, `RecentTransactionsCard`.

  Regras aplicáveis: `React.memo`, `useCallback` e `useMemo`, com skeleton apenas em
  `isLoading && !data` (Regra 25); sem `whileHover` ou `whileTap` em card acima de área
  rolável, usando CSS puro com `motion-reduce:` (Regra 26); tokens de design, zero emoji,
  ícones outline e `bg-surface-card` (Regras 17, 18, 20, 21 e 22).

---

## 🧪 9. Estratégia de Testes (Regra 8 — TDD)

Escritos antes da implementação. O que realmente pode quebrar:

**Fatia A**
- Consumer preserva vencimento, limite e fechamento ponta a ponta.
- Conta de crédito sem `creditData` não quebra a ingestão — campos nulos são válidos.
- `ParseDueDate()` com formato inesperado devolve `null` em vez de lançar exceção.

**Fatia E**
- Período vazio devolve zeros sem lançar exceção.
- `IsIgnoredInTotals` fica fora dos totais; `IsBillPayment` não conta duas vezes.
- Percentuais de categoria somam 100; a cauda agrega corretamente em "Outras".
- Agrupamento por categoria e por mês no read repository.
- Endpoint devolve 401 sem claim de usuário e repassa os filtros corretamente.
- Frontend com MSW: render com dados, empty state, erro RFC 7807, e troca de preset refazendo
  a query sem piscar skeleton.

**Fatias futuras (B, C, D)**
- `FinancialCycle` — ciclo cruzando virada de ano; âncora no dia 31 em mês de 30 dias;
  fevereiro.
- `InvoiceAssignmentService` — compra exatamente **no** dia do fechamento (fronteira); compra
  no dia seguinte; fechamento no dia 31.
- Estimativa de receita — a mediana ignora o outlier do 13º.

---

## ✅ 10. Verificação

```bash
dotnet build FinanceHub.slnx && dotnet test tests/FinanceHub.Tests
cd src/Web/FinanceHub.Web && npm run lint && npm run test && npm run build
docker compose up -d
```

No navegador, após sincronizar um cartão: (a) os KPIs do Dashboard batem com os de
`/transacoes` no mesmo período; (b) as barras de categoria mostram categorias reais; (c) os
cards por instituição mostram nome e saldo corretos, não mais `undefined` e `R$ 0,00`;
(d) trocar o preset de período atualiza tudo sem piscar skeleton.

Para a fatia A, cuja mudança não é visível na interface: consultar o banco do Aggregator após
um sync de cartão e confirmar que vencimento, limite e fechamento chegaram persistidos.

Por fim, abrir os gráficos e **olhar** — o validador de paleta checa cor, não colisão de
rótulo nem overflow.

---

## 🚫 11. Fora de Escopo

- Seletor global de mês/ano no `Topbar` e unificação do período com a tela de Transações,
  previstos em `.agents/specs/phase-6-frontend-web-spec.md:165`.
- Busca global do `Topbar` — hoje é um input decorativo, sem handler.
- Orçamentos por categoria e visão de calendário (`features/budgets` e `features/calendar` da
  Phase 6, ainda inexistentes).
