# Design System, Tailwind & Accessibility Rules — FinanceHub

> **Target**: `src/shared/components/`, `src/shared/styles/` & `tailwind.config.ts`  
> **Source**: Identidade Visual do Figma (`refs-projeto-controle/`)  
> **Standard**: `WAI-ARIA Accessibility + BRL Financial Formatting`

---

## 🎨 1. Paleta de Cores, Tokens Tailwind & Centralização de Estilos

> **REGRA IMPERATIVA DE ARQUITETURA DE STYLES**: É **ESTRITAMENTE OBRIGATÓRIO** centralizar todas as variáveis de tema, cores, sombras (`shadow-card`, `shadow-elevated`, `shadow-brand`), raios de borda, transições, animações de hover (`translateY`, glowing effects) e estilos base em `src/index.css` / tokens globais ou classes de utilitários reutilizáveis de componentes pai (`src/shared/components/`).
> 
> **PROIBIÇÕES**:
> - Proibido utilizar códigos hexadecimais brutos ou arbitrários inline em arquivos JSX/TSX.
> - Proibido duplicar regras de transição, hover ou sombras em múltiplos arquivos de feature. Toda elevação e animação deve vir de tokens/utilitários globais.

Todas as classes de cores DEVEM utilizar estritamente os tokens configurados na paleta do projeto (extraída dos designs originais):

| Token Tailwind | Cor Hex | Uso Obrigatório |
| :--- | :--- | :--- |
| `bg-brand`, `text-brand` | `#E05697` | Botões de ação primária, destaques ativos, tabs selecionadas |
| `bg-brand-dark`, `text-brand-dark` | `#941B5C` | Hover em botões primários, títulos principais de destaque |
| `bg-secondary`, `text-secondary` | `#1D555A` | Fundo da Sidebar, cabeçalhos de tabela, ícones institucionais |
| `bg-tertiary`, `text-tertiary` | `#FF7338` | Badges de atenção moderada, acentos visuais |
| `bg-surface-ground` | `#F4F7F6` | Fundo geral da aplicação (clean, alto contraste) |
| `bg-surface-card` | `#FAFCFB` | Cards elevados, modais, containers de dados (Off-white, nunca branco puro #FFFFFF) |
| `text-status-success` | `#2ECC71` | Receitas, entradas de dinheiro, status de sincronização OK |
| `text-status-danger` | `#FF5964` | Despesas, saídas financeiras, alertas de erro |
| `text-status-info` | `#38BDF8` | Informações de transações, dicas, registros |
| `text-status-warning` | `#F59E0B` | Contas a vencer, avisos de limite |

---

## 🛠️ 2. Utilitário de Fusão de Classes `cn(...)`

Todo componente reutilizável em `src/shared/components/` DEVE aceitar `className` opcional e utilizar `cn()` para fusão correta de classes Tailwind:

```typescript
// src/shared/utils/cn.ts
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

---

## 🚫 3. Proibição Estrita de Emojis, Ícones Outline & Proibição de Barras Verticais em Títulos

1. **Zero Emojis**:
   - É **estritamente proibido** o uso de emojis em qualquer parte da interface (títulos, botões, modais, toasts, badges, tooltips ou tabelas).
2. **Ícones Outline**:
   - Toda indicação visual deve utilizar ícones vetoriais outline limpos (`lucide-react` ou SVG puro com `fill: none` e `stroke: currentColor`).
   - Preferência por layout clean: se um ícone não agregar clareza semântica, utilize apenas tipografia limpa.
3. **Proibição de Barras Verticais em Títulos**:
   - É **estritamente proibido** utilizar marcadores ou destaques de barra vertical (`::before` vertical bar, `|` ou retângulos verticais) antes de títulos ou cabeçalhos. Títulos devem ser renderizados puramente com tipografia limpa, peso e hierarquia visual.
4. **Proibição de Branco Puro (`#FFFFFF`)**:
   - É **estritamente proibido** utilizar branco puro (`#FFFFFF`) em superfícies de cartões, modais, campos de formulário e containers de fundo. Toda superfície que seria branca DEVE utilizar obrigatoriamente um tom **off-white** suave (ex: `#FAFCFB` / `bg-surface-card`).
5. **Proibição do Caractere '&' em Títulos e Menus**:
   - É **estritamente proibido** utilizar o caractere '&' em títulos de seção, cabeçalhos, modais ou itens de menu da aplicação. Utilize sempre apenas nomes diretos (ex: 'Conexões' em vez de 'Conexões & Ingestão').

---

## 📦 4. Componentização Obrigatória de Controles de Formulário (Custom Select)

1. **Custom Select / Dropdown Padronizado**:
   - É **proibido** utilizar `<select>` nativo do browser sem estilização customizada de menu de opções.
   - Todo dropdown de seleção DEVE ser um componente reutilizável (`shared/components/Select/Select.tsx` ou padrão Radix UI / Headless), compartilhando:
     - Trigger estilizado com foco na cor `brand` (`#E05697`).
     - Menu flutuante elevado (`shadow-dropdown` e `bg-surface-card`).
     - Opções com hover suave em `secondary-light` (`#E6F4F1`) e estado selecionado em `brand-light` (`#FCE7F3`).
     - Suporte a navegação por teclado (`ArrowDown`, `ArrowUp`, `Enter`, `Escape`).

---

## ♿ 5. Acessibilidade (WAI-ARIA) & Usabilidade


1. **Botões e Links com Ícones**:
   - Botões que contêm apenas ícones DEVEM incluir `aria-label` descritivo (ex: `<button aria-label="Fechar modal"><X /></button>`).
2. **Modais e Diálogos**:
   - Devem possuir `role="dialog"`, `aria-modal="true"`, e vincular `aria-labelledby` ao título do modal.
   - Devem fechar com a tecla `Escape` e prender o foco (*focus trap*).
3. **Contraste de Cores**:
   - Textos sobre fundos coloridos devem atender ao contraste mínimo WCAG AA (4.5:1).
   - Textos sobre fundo `brand` (`#E05697`) ou `secondary` (`#1D555A`) DEVEM utilizar texto branco (`text-white`).

---

## 💰 4. Formatação Canônica de Valores Financeiros & LGPD

Todo valor exibido na UI DEVE ser formatado com os utilitários centrais de `src/shared/utils/formatters.ts` utilizando **instâncias singleton** para máxima performance:

```typescript
// src/shared/utils/formatters.ts

// Singletons de alta performance (evita recriação em loops e tabelas grandes)
const brlFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

const dateFormatter = new Intl.DateTimeFormat('pt-BR');

/**
 * Formata um valor numérico para o padrão de moeda brasileiro (BRL).
 * Exemplo: 1234.56 -> "R$ 1.234,56"
 */
export function formatCurrencyBRL(value: number | string | null | undefined): string {
  if (value === null || value === undefined || isNaN(Number(value))) {
    return 'R$ 0,00';
  }
  return brlFormatter.format(Number(value));
}

/**
 * Formata uma data ISO ou string para exibição DD/MM/YYYY.
 */
export function formatDateBR(dateString: string | Date | null | undefined): string {
  if (!dateString) return '-';
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  return isNaN(date.getTime()) ? '-' : dateFormatter.format(date);
}

/**
 * Mascara CPF de acordo com a LGPD (ex: "123.456.789-00" -> "***.456.789-**").
 */
export function maskSensitiveCpf(cpf: string | null | undefined): string {
  if (!cpf) return '***.***.***-**';
  const clean = cpf.replace(/\D/g, '');
  if (clean.length !== 11) return '***.***.***-**';
  return `***.${clean.substring(3, 6)}.${clean.substring(6, 9)}-**`;
}

/**
 * Mascara conta bancária (ex: "12345-6" -> "***45-6").
 */
export function maskSensitiveAccount(account: string | null | undefined): string {
  if (!account) return '****-*';
  const clean = account.trim();
  if (clean.length <= 4) return '****';
  return `***${clean.slice(-4)}`;
}

/**
 * Mascara chave PIX (detecta e-mail ou telefone).
 */
export function maskSensitivePixKey(key: string | null | undefined): string {
  if (!key) return '***';
  if (key.includes('@')) {
    const [user, domain] = key.split('@');
    return `${user.slice(0, 2)}***@${domain}`;
  }
  const clean = key.replace(/\D/g, '');
  if (clean.length === 11) return maskSensitiveCpf(clean); // CPF
  return `${key.slice(0, 3)}*****${key.slice(-2)}`;
}
```

