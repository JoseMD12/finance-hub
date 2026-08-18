# FinanceHub — Comportamento de Atualização das Conexões Pluggy

> **Tipo:** Knowledge / registro de comportamento e decisão futura
> **Última atualização:** 2026-08-18
> **Status:** Fluxo manual pelo Meu.Pluggy adotado; atualização embutida não utilizada

Este documento registra o comportamento atual da tela de conexões do FinanceHub, o comportamento esperado para uma atualização real de instituição financeira, as limitações encontradas no modelo atual do Meu.Pluggy e as alternativas possíveis. Ele não autoriza nem representa uma mudança de código.

## 1. Comportamento observado no Meu.Pluggy

Ao entrar no portal `meu.pluggy.ai`, selecionar uma instituição — por exemplo, Mercado Pago — e escolher a opção de atualização:

1. o portal abre uma etapa de Open Finance;
2. a instituição aparece carregando os dados;
3. em alguns casos não é solicitada novamente a senha ou qualquer credencial;
4. após o carregamento, os dados da instituição são atualizados.

Esse comportamento não é apenas uma nova leitura dos dados já disponíveis. A instituição financeira é acionada novamente pelo fluxo de atualização da conexão, inclusive com tratamento de autenticação adicional quando o banco exigir.

## 2. Comportamento atual do FinanceHub

### 2.1 Listagem das instituições

`GET /api/v1/gateway/pluggy/items`:

1. recebe o `X-Pluggy-Access-Token` enviado pelo frontend;
2. o `ApiGateway` encaminha a chamada ao `FinanceHub.PluggyIntegration`;
3. o serviço chama a API do Meu.Pluggy para `GET /items`;
4. para cada item, consulta `GET /accounts?itemId=...`;
5. calcula `Saldo Total` e `Crédito Total` a partir das contas retornadas;
6. devolve os cards ao frontend.

Portanto, a rota de items atualmente consulta a API do Meu.Pluggy. Ela não lê diretamente as instituições no PostgreSQL do FinanceHub. O PostgreSQL recebe os eventos financeiros posteriormente, durante a ingestão/sincronização.

### 2.2 Sincronização global

`POST /api/v1/gateway/pluggy/sync`:

1. obtém os items disponíveis na sessão atual;
2. consulta as contas de cada item;
3. consulta as transações de cada conta;
4. publica eventos de transação e de itens de fatura no RabbitMQ;
5. o Transaction Aggregator processa esses eventos e persiste os dados canônicos no PostgreSQL;
6. retorna um resumo da operação.

Essa operação pode reprocessar dados que o Meu.Pluggy já tenha disponibilizado, mas não força, por si só, uma nova autenticação ou coleta no banco.

### 2.3 Reprocessamento individual legado

`POST /api/v1/gateway/pluggy/items/{itemId}/sync`:

1. localiza o item na sessão atual;
2. consulta novamente as contas e transações já disponíveis para aquele item;
3. publica os mesmos eventos de ingestão da sincronização global, mas somente para a instituição escolhida;
4. retorna um resumo individual.

O endpoint individual legado significava **reprocessar os dados disponíveis**, e não **iniciar o fluxo de atualização do consentimento Open Finance**. Ele deixou de ser exposto na tela; a atualização da instituição é feita manualmente no Meu.Pluggy.

### 2.4 Data exibida no card

O campo `Atualizado: data/hora` é baseado no `lastUpdatedAt` retornado pelo item da Pluggy. Esse campo representa a informação de atualização fornecida pela Pluggy e não necessariamente o instante em que o FinanceHub publicou ou persistiu os últimos eventos no próprio banco.

## 3. Comportamento esperado

O botão individual deve oferecer uma atualização equivalente ao fluxo observado no Meu.Pluggy:

1. iniciar o processo de atualização do item específico;
2. abrir o Connect Widget da Pluggy em modo de atualização;
3. deixar a Pluggy conduzir o fluxo Open Finance;
4. não pedir senha novamente quando o banco não exigir isso;
5. mostrar a etapa de carregamento enquanto a instituição atualiza os dados;
6. tratar casos em que o banco exige nova autenticação, MFA ou consentimento;
7. após a conclusão, atualizar os cards, o saldo, o crédito, a data de atualização e as transações do FinanceHub;
8. manter a sincronização global como alternativa para atualizar todas as instituições.

O comportamento esperado deve preservar a separação entre:

- **atualização externa da conexão:** Pluggy/banco recupera dados novos;
- **ingestão interna:** FinanceHub lê os dados disponíveis, publica eventos e persiste o resultado.

## 4. Limitações atuais

### 4.1 Token usado pelo frontend

O FinanceHub atual trabalha com o token de sessão do Meu.Pluggy fornecido pelo frontend. Esse token é suficiente para as leituras limitadas utilizadas na tela, mas não fornece as credenciais de aplicação necessárias para administrar o ciclo completo de uma conexão.

Segundo a documentação oficial, o Connect Token é limitado ao item gerado e ao acesso reduzido às contas; operações administrativas e demais recursos devem ser feitas no servidor com uma API Key. A API Key é criada usando `CLIENT_ID` e `CLIENT_SECRET`, que devem permanecer no backend.

### 4.2 Tentativa de atualização direta por PATCH

Foi tentada a atualização direta de `PATCH /items/{itemId}` usando o token pessoal/sessão. A chamada retornou `405 Method Not Allowed` e acabou se manifestando como erro `500` no gateway.

O motivo prático é que essa rota não deve ser tratada como equivalente ao fluxo visual de atualização do Connect Widget, especialmente quando o token não possui a autorização de aplicação necessária.

### 4.3 O endpoint individual atual não atualiza o banco

O endpoint individual atual não inicia a atualização no banco. Se o item ainda estiver com dados antigos na Pluggy, o endpoint apenas lerá e reprocessará esses dados antigos.

### 4.4 Dependência de credenciais de aplicação

Para incorporar oficialmente o fluxo embutido do Connect Widget, a integração precisa de credenciais de aplicação da Pluggy. A obtenção dessas credenciais depende do Dashboard e das condições comerciais/plano da conta. O repositório não deve assumir que trial, preço ou disponibilidade permanecerão iguais sem confirmação direta da Pluggy.

### 4.5 Expiração e escopo dos tokens

- API Key: token de backend, com expiração documentada de duas horas.
- Connect Token: token destinado ao frontend/widget, com expiração documentada de 30 minutos.
- `CLIENT_ID` e `CLIENT_SECRET`: credenciais sensíveis, nunca devem ser enviados ao navegador ou commitados no repositório.
- O token pessoal do usuário não deve ser registrado em logs, documentação de exemplo ou chamados de suporte.

### 4.6 Atualização assíncrona e consistência

Mesmo com o Widget, a conclusão visual do fluxo não garante que todos os eventos já tenham sido processados pelo RabbitMQ e pelo Transaction Aggregator. A tela deverá considerar estados como `iniciando`, `aguardando Pluggy`, `processando FinanceHub`, `concluído` e `erro`.

## 5. Possíveis soluções

### Solução A — Connect Widget em modo de atualização (solução oficial/preferida)

Fluxo previsto:

1. backend autentica na Pluggy usando `CLIENT_ID` e `CLIENT_SECRET` para obter uma API Key;
2. backend cria um Connect Token informando o `itemId` da instituição;
3. frontend abre o Pluggy Connect Widget com esse Connect Token;
4. widget executa a atualização da instituição;
5. frontend recebe o resultado do widget;
6. backend consulta/ingere os dados atualizados e invalida os caches da tela.

A documentação da Pluggy exige que o `itemId` seja informado ao criar o Connect Token para permitir a atualização daquele item específico. Essa é a solução mais próxima do comportamento observado no Meu.Pluggy.

### Solução B — Manter o endpoint individual de reprocessamento

Continuar usando `POST /api/v1/gateway/pluggy/items/{itemId}/sync` para reprocessar dados já disponíveis.

Vantagens:

- não exige Dashboard nem `CLIENT_ID`/`CLIENT_SECRET`;
- aproveita o fluxo já implementado;
- permite atualizar uma instituição sem processar todas.

Limitação principal: não força o banco ou a Pluggy a coletar dados novos. Deve ser apresentado ao usuário como reprocessamento/sincronização dos dados disponíveis, não como atualização completa da conexão.

### Solução C — Redirecionar o usuário para o Meu.Pluggy

Manter um botão para abrir o portal externo, como fallback manual. O usuário realiza a atualização no portal e depois retorna ao FinanceHub para executar a sincronização.

Vantagens:

- não requer credenciais de aplicação no FinanceHub;
- usa o fluxo que já funciona para o usuário.

Limitações:

- experiência fragmentada entre dois sistemas;
- não há garantia de deep link para abrir diretamente a instituição específica;
- o FinanceHub não controla o estado final do fluxo externo.

### Solução D — Consultar a Pluggy sobre plano de desenvolvimento/sandbox

Antes de assumir um custo ou alterar a arquitetura, confirmar com a Pluggy:

- disponibilidade de sandbox/desenvolvimento;
- necessidade de plano pago para produção;
- limite de itens, usuários e chamadas;
- possibilidade de usar o Connect Widget em conta pessoal/trial;
- política de expiração e renovação das credenciais;
- suporte ao fluxo de atualização para os conectores utilizados.

## 6. Decisão registrada por enquanto

Até nova decisão, o comportamento conhecido é:

- sincronização global: lê e reprocessa todos os items disponíveis;
- atualização individual: lê e reprocessa somente um item disponível;
- atualização individual: realizada manualmente no portal Meu.Pluggy;
- listagem: consulta a Pluggy, não o PostgreSQL;
- consultas e sincronização: exigem somente o token da sessão Meu.Pluggy;
- portal Meu.Pluggy: permanece como alternativa manual;
- Connect Widget: não utilizado nesta versão, pois os itens atuais pertencem ao Meu.Pluggy e não à aplicação do Dashboard.

## 7. Referências oficiais

- [Pluggy — Authentication](https://docs.pluggy.ai/docs/authentication): diferença entre API Key e Connect Token e escopos de acesso.
- [Pluggy — Auth](https://docs.pluggy.ai/reference/auth): capacidades de API Key e limitações do Connect Token.
- [Pluggy — Setup PluggyConnect Widget](https://docs.pluggy.ai/docs/setup-pluggyconnect-widget-on-your-app): criação server-side do Connect Token e integração do Widget.
- [Pluggy — Create Connect Token](https://docs.pluggy.ai/reference/connect-token-create): uso de `itemId` para permitir atualização de um item específico.
- [Pluggy — Get your API keys](https://docs.pluggy.ai/docs/get-your-api-keys): criação de `CLIENT_ID` e `CLIENT_SECRET` no Dashboard.
