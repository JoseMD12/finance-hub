# 🚫 Regra: Proibição de Caminhos Absolutos (Zero Absolute Paths)

Esta regra estabelece uma restrição absoluta em todo o ciclo de desenvolvimento, planejamento, documentação e especificações dentro do repositório **FinanceHub**.

---

## 🛑 Diretriz Principal

**NUNCA utilize caminhos absolutos** (ex: `/home/josemd12/Code/...` ou `/mnt/c/Code/...`) em qualquer tipo de arquivo ou comunicação gerada por agentes e subagents.

Isso se aplica a:
1.  **Código-Fonte**: Arquivos de configuração, strings de conexão, carregamento de certificados e DI.
2.  **Documentação e Especificações**: Arquivos de especificação na pasta `.agents/specs/`, guias e planos de implementação.
3.  **Arquivos de Ambiente**: Declarações e exemplos no `.env.example` ou referências no `.env`.
4.  **Anotações e Logs**: Textos explicativos e comentários de código.

---

## 🔄 Como Substituir Caminhos Absolutos

*   **No Código/Configurações**: Use sempre caminhos relativos ao diretório raiz da aplicação, ou use variáveis de ambiente configuráveis em conjunto com `Path.Combine(AppContext.BaseDirectory, ...)` ou caminhos relativos à raiz do projeto.
*   **Em Links de Markdown/Documentos**: Utilize referências relativas do Git/Repositório (ex: `[ItauConstants.cs](../Domain/Constants/ItauConstants.cs)` ou `[docker-compose.yml](../../docker-compose.yml)`).
*   **Nas Variáveis de Ambiente (`.env`)**: Indique caminhos relativos ou configure o container para montar volumes relativos do Docker Compose.
