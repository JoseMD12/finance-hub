# FinanceHub — Frontend Design Tokens & Visual Specifications

> **Fonte Primária**: Telas e Paletas em `refs-projeto-controle/` (`Paleta.pdf`, `Orçamento.pdf`, `Calendário.pdf`, `Modal.pdf`, `Login.pdf`)

---

## 🎨 1. Paleta de Cores & Tokens

### Cores Principais da Marca
```css
:root {
  /* Brand Colors */
  --color-brand-primary: #E05697;       /* Botões principais, badges de destaque ativo */
  --color-brand-dark: #941B5C;          /* Hover de botões, títulos de grande relevância */
  --color-brand-light: #FCE7F3;         /* Fundo suave para tags da marca */

  /* Secondary & Tertiary */
  --color-secondary-base: #1D555A;      /* Sidebar, títulos de tabela, cabeçalhos escuros */
  --color-secondary-dark: #164347;      /* Hover e acentos de sidebar */
  --color-secondary-light: #E6F4F1;     /* Fundo suave de cards institucionais */
  --color-tertiary-base: #FF7338;       /* Alertas moderados, acentos de gráficos */
  --color-tertiary-light: #FFF0EA;      /* Fundo de destaque terciário */

  /* Neutros & Superfícies */
  --color-surface-ground: #F4F7F6;      /* Fundo da aplicação (body) */
  --color-surface-card: #FFFFFF;        /* Superfície dos cards e modais */
  --color-surface-muted: #EAEFF0;       /* Bordas e divisores */
  --color-text-primary: #1E293B;        /* Texto principal */
  --color-text-muted: #64748B;          /* Subtítulos, labels, metadados */

  /* Status Financeiros */
  --color-status-success: #2ECC71;      /* Receitas (+), status de banco conectado */
  --color-status-danger: #FF5964;       /* Despesas (-), erros, revogações */
  --color-status-info: #38BDF8;         /* Informações, dicas, logs */
  --color-status-warning: #F59E0B;      /* Contas a vencer, avisos */
}
```

---

## 📐 2. Tipografia & Escala de Espaçamento

### Família Tipográfica
- **Fonte Primária**: `'Plus Jakarta Sans', 'Inter', -apple-system, BlinkMacSystemFont, sans-serif`

### Hierarquia de Títulos e Textos
| Nível | Tamanho | Peso | Letter-Spacing | Line-Height | Uso |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **H1 (Page Title)** | `1.875rem` (30px) | `700` (Bold) | `-0.02em` | `1.2` | Título da página no topo |
| **H2 (Card Title)** | `1.25rem` (20px) | `600` (SemiBold) | `-0.01em` | `1.3` | Título de seções e cartões |
| **H3 (Sub-Card)** | `1.0rem` (16px) | `600` (SemiBold) | `0` | `1.4` | Títulos internos de tabelas/modais |
| **Body (Normal)** | `0.875rem` (14px) | `400` (Regular) | `0` | `1.5` | Textos gerais, linhas de tabela |
| **Caption / Label**| `0.75rem` (12px) | `500` (Medium) | `0.02em` | `1.4` | Badges, datas, legendas |

---

## 🧱 3. Elevação, Bordas e Sombras (Cards & Modais)

- **Border Radius Padrão**:
  - Cards e Painéis: `rounded-2xl` (`16px`)
  - Modais: `rounded-3xl` (`24px`)
  - Botões e Inputs: `rounded-xl` (`12px`)
  - Badges e Pills: `rounded-full` (`9999px`)
- **Sombras (Shadows)**:
  - Card suave: `box-shadow: 0 4px 20px -2px rgba(29, 85, 90, 0.05);`
  - Modal elevado: `box-shadow: 0 20px 40px -8px rgba(0, 0, 0, 0.12);`
