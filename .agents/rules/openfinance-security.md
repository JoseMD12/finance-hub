# Segurança de Dados Financeiros, Meu.Pluggy e LGPD — FinanceHub

> **Escopo**: todos os serviços (`src/Services/*`), o frontend (`src/Web/*`) e a documentação.
> **Decisão de base**: ADR-001 em `.agents/knowledge/system-architecture-and-services.md` — o
> acesso ao Open Finance é feito pelo Meu.Pluggy, não por conectores diretos com os bancos.

Esta regra substitui a versão que descrevia FAPI 1.0/2.0, mTLS com certificado ICP-Brasil,
JAR/JWE e ciclo de vida de consentimento próprio. Nada disso existe no código, e nada disso deve
ser implementado sem um novo ADR que reverta o ADR-001.

---

## 1. Quem é responsável por quê

| Camada | Responsável | O que faz |
| --- | --- | --- |
| Consentimento Open Finance, FAPI, mTLS e tokens com cada banco | **Pluggy** | O usuário conecta e revoga instituições no `meu.pluggy.ai`. |
| Autenticação do usuário no FinanceHub | **ApiGateway** | JWT Bearer; o `userId` sai da claim (`NameIdentifier` / `sub`). |
| Token pessoal da Meu.Pluggy | **Repassado, nunca guardado no backend** | Chega por header, segue até o `PluggyIntegration`, é descartado ao fim da requisição. |
| Ledger, saldos e categorias | **TransactionAggregator** | Serviço interno, sem autenticação própria — confia no `userId` que o Gateway envia. |

---

## 2. Token pessoal da Meu.Pluggy

Obrigatório:

1. **Trafega apenas no header `X-Pluggy-Access-Token`**, do cliente para o Gateway e do Gateway
   para o `PluggyIntegration`. Nunca em query string, corpo de log ou URL.
2. **Não é persistido no backend** — nem banco, nem cache, nem fila.
3. **Nunca aparece em log, mensagem de erro, spec, fixture de teste ou chamado de suporte.**
4. **No frontend web, fica no `sessionStorage`** (`features/connections/hooks/usePluggyToken.ts`),
   nunca no `localStorage`: expira com a aba e não sobrevive a reinício do navegador. Na extensão,
   fica no storage da própria extensão, inacessível às páginas.
5. **Token ausente ou inválido**: a API `meu.pluggy.ai` responde **403** (não 401). O Gateway
   devolve RFC 7807 com `errorCode` específico e o usuário é orientado a informar um token novo.

Credenciais de aplicação da Pluggy (`CLIENT_ID` / `CLIENT_SECRET`, necessárias só para o Connect
Widget descrito em `.agents/knowledge/pluggy-connection-refresh-behavior.md`): exclusivamente no
backend, via variável de ambiente (Regra 12), nunca no navegador nem no repositório.

---

## 3. Segredos em repouso

- **Estado atual**: o FinanceHub não persiste nenhum segredo bancário. Não há token de
  consentimento, refresh token de banco ou credencial para cifrar.
- **Se uma feature futura precisar persistir um segredo**, ele deve ser cifrado com AES-256-GCM.
  `FinanceHub.Shared.Observability/Security/EnvelopeEncryptionService` já implementa o algoritmo,
  mas não está registrado em DI nem é usado. A chave mestra vem de variável de ambiente ou cofre de
  segredos, com fail-fast se ausente; adotar KMS gerenciado só quando houver ambiente implantado e
  um segredo real a proteger.

---

## 4. LGPD

1. **Consentimento**: é concedido pelo usuário ao conectar a instituição no Meu.Pluggy. O
   FinanceHub sincroniza apenas o que o usuário conectou e não amplia escopo por conta própria.
2. **Minimização**: buscar e guardar só o necessário para as features. Campos que a Pluggy envia e
   não usamos ficam em `AdditionalFields` apenas em memória, para descoberta de contrato, e só têm
   os **nomes** registrados em log.
3. **Mascaramento na interface**: CPF, conta e chave PIX passam pelos utilitários de
   `src/shared/utils/formatters.ts` (`maskSensitiveCpf`, `maskSensitiveAccount`,
   `maskSensitivePixKey`).
4. **Logs sem PII**: proibido registrar token, CPF, CNPJ, número de conta, número de cartão, nome
   de contraparte ou descrição bruta de transação. Use propriedades estruturadas com identificadores
   técnicos (`UserId`, `AccountId` interno, contagens).
5. **Dado real nunca entra no repositório**: saldos, limites, salários, dígitos de cartão, IDs de
   fatura ou de conexão e nomes observados na API real servem só para aceitação manual. Fixtures,
   specs e exemplos usam valores fictícios (Bogus ou números redondos).

---

## 5. OWASP API Security Top 10 aplicado

| Risco | Controle | Onde está |
| --- | --- | --- |
| **API1 — BOLA / IDOR** | O `userId` vem sempre da claim do JWT, nunca da query string do cliente. Todo acesso a recurso filtra pelo `userId` do contexto. | `ApiGateway/Endpoints/*Endpoints.cs` |
| **API1 — serviços internos** | `TransactionAggregator` e `PluggyIntegration` não têm autenticação própria; o Aggregator recebe `userId` na requisição. Por isso **não publicam porta no host** em ambientes compartilhados — só o Gateway é exposto. | `docker-compose.yml` (`expose` no Aggregator) |
| **API3 — vazamento de propriedades** | Entidades de domínio nunca são serializadas; só DTOs imutáveis. | DTOs por serviço |
| **API4 — consumo de recursos** | Rate limiting no Gateway: 30 req/min por janela fixa para anônimos e 120 req/min por janela deslizante por usuário autenticado. | `ApiGateway/DependencyInjection.cs`, `GatewayConstants.RateLimiting` |
| **API8 — configuração** | Variáveis obrigatórias com fail-fast (Regra 12); erros em RFC 7807 sem stack trace. | `Program.cs` / `DependencyInjection.cs` de cada serviço |

---

## 6. Lacunas conhecidas

Requisitos desta regra que **ainda não estão implementados**. Não presuma que existem.

| Lacuna | Situação |
| --- | --- |
| Redação automática de PII nos logs | Não há enricher nem destructuring policy no Serilog (`ObservabilityExtensions.UseFinanceHubSerilog`). Hoje a proteção depende de não registrar PII na origem (seção 4.4). |
| JWT do FinanceHub no `localStorage` | `src/shared/utils/authStorage.ts` guarda access e refresh token no `localStorage`, contrariando `frontend-http-and-rfc7807.md` §1 (access token em memória, refresh em cookie `HttpOnly`). |
| `PluggyIntegration` publicado na porta 5056 | Exige o token do usuário em toda chamada, mas segue alcançável fora do Gateway. A skill `manual-api-curl-testing` depende dessa porta. |
| Direito ao esquecimento | Não há endpoint para apagar os dados de um usuário. |
