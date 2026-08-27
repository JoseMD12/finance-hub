# FinanceHub — Spec: Design System Visual e Motion Global

> **Status**: `Concluída & Implementada`  
> **Scope**: `src/Web/FinanceHub.Web` — aplicação completa  
> **Objetivo**: Elevar a identidade visual e adicionar motion com sistema global e consistente, cobrindo todas as telas presentes e futuras sem animações isoladas ou duplicadas.

---

## 0. Princípios desta Spec

1. **Sistema primeiro, tela depois** — Toda animação e estilo vive em um componente ou token reutilizável. Nenhuma propriedade de animação inline em páginas ou features.
2. **Motion com intenção** — Cada animação resolve um problema de feedback, orientação ou hierarquia. Nunca decorativa pura.
3. **Progressividade** — A interface funciona sem nenhuma animação (respeita `prefers-reduced-motion`). Motion é camada adicional.
4. **Light mode como padrão** — Toda a aplicação é light mode, incluindo a tela de Login. O toggle dark/light mode é feature futura, não desta spec.
5. **Consistência tipográfica** — Introdução de `Syne` como fonte display para KPIs e títulos de impacto. `Plus Jakarta Sans` permanece como fonte do corpo (já configurada).

---

## 1. Instalação e Dependências

- `framer-motion@^12` (suporte nativo e oficial ao React 19 sem peer dependency warnings).
- Importação da fonte `Syne` (pesos 700 e 800) no [index.html](file:///home/josemd12/Code/FinanceHub/src/Web/FinanceHub.Web/index.html):
```html
<link
  href="https://fonts.googleapis.com/css2?family=Syne:wght@700;800&display=swap"
  rel="stylesheet"
/>
```
- **Proibições**: Sem bibliotecas extras como GSAP, Three.js, anime.js, etc.

---

## 2. Tokens Globais — Adições ao `index.css`

Adicionar ao bloco `@theme` em [src/index.css](file:///home/josemd12/Code/FinanceHub/src/Web/FinanceHub.Web/src/index.css):
```css
@theme {
  /* Fonte display — para KPIs e títulos de impacto */
  --font-display: 'Syne', sans-serif;

  /* Glow de marca — usado nos componentes de motion */
  --color-brand-glow-rgb: 224, 86, 151;
  --color-secondary-glow-rgb: 29, 85, 90;

  /* Durations de motion */
  --duration-micro: 150ms;
  --duration-default: 250ms;
  --duration-enter: 400ms;

  /* Easings */
  --ease-default: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-spring-out: cubic-bezier(0.34, 1.56, 0.64, 1);
}
```

Classes utilitárias adicionais em [src/index.css](file:///home/josemd12/Code/FinanceHub/src/Web/FinanceHub.Web/src/index.css):
```css
/* Fonte display — usar em KPIs e títulos de seção de alto impacto */
.font-display {
  font-family: var(--font-display);
}

/* Glow card — aplicado pela classe utilitária, não inline */
.glow-card {
  position: relative;
}
.glow-card::before {
  content: '';
  position: absolute;
  inset: -1px;
  border-radius: inherit;
  background: radial-gradient(
    400px circle at var(--mouse-x, 50%) var(--mouse-y, 50%),
    rgba(var(--glow-rgb, 224, 86, 151), 0.1),
    transparent 60%
  );
  opacity: 0;
  transition: opacity var(--duration-default) var(--ease-default);
  pointer-events: none;
  z-index: 0;
}
.glow-card:hover::before {
  opacity: 1;
}

/* Led border canvas — container deve ter position: relative */
.led-border-container {
  position: relative;
}

@keyframes scan-sweep {
  from { left: -60%; }
  to { left: 110%; }
}
.scanner-item {
  position: relative;
  overflow: hidden;
}
.scanner-item::after {
  content: '';
  position: absolute;
  top: 0;
  width: 60%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(224,86,151,0.12), transparent);
  animation: scan-sweep 0.4s ease-out forwards;
}
```

---

## 3. Estrutura de Arquivos de Motion

Localização: `src/shared/components/motion/`
- `index.ts` (barrel export exclusivo)
- `GlowCard.tsx` (efeito cursor/luz com CSS radial-gradient)
- `LogoMark.tsx` (morph de forma quadrado->círculo com Framer Motion)
- `NavActiveIndicator.tsx` (layout animation da Sidebar)
- `MagneticButton.tsx` (botão magnet com useSpring)
- `NumberScramble.tsx` (scramble numérico com `tabular-nums`, prevenção de layout shift e suporte a `formatCurrencyBRL`)
- `AuroraBackground.tsx` (blobs animados em Canvas 2D para Login)
- `ScannerReveal.tsx` (stagger com varredura de luz)

---

## 4. Mapa de Uso Global por Componente

| Componente | Onde usar | Onde NÃO usar |
|---|---|---|
| `<GlowCard>` | Todos os `<Card>` do Dashboard e Conexões | Cards informativos sem interação |
| `<LogoMark>` | Topo do painel direito da LoginPage | Sidebar (perfil do usuário mantido) |
| `<NavActiveIndicator>` | Sidebar navItems | Qualquer outro menu/tab |
| `<MagneticButton>` | CTA principal (Sync em Conexões, Entrar no Login) | Botões secundários/tabelas |
| `<NumberScramble>` | Card de "Saldo Consolidado Total" no Dashboard | Cards estáticos (Receitas/Despesas) |
| `<AuroraBackground>` | Painel esquerdo do Login apenas | Qualquer outra tela |
| `<ScannerReveal>` | Tabela de Transações ao carregar lista | Dashboard, Conexões |

---

## 5. Integração por Tela

1. **LoginPage**:
   - Painel esquerdo: `bg-secondary` com `<AuroraBackground />`, tipografia Syne e decorações geométricas (remover referência à imagem estática `login-hero.jpeg`).
   - Painel direito: `bg-surface-card`, `<LogoMark size="sm" showText />` no topo, campos com stagger motion e submit com `<MagneticButton>`.
2. **Sidebar**:
   - Manter perfil no topo intacto (sem LogoMark).
   - `<LayoutGroup>` e `<NavActiveIndicator>` com `layoutId="fh-nav-active-pill"`.
3. **DashboardPage**:
   - Card Saldo Total com `<GlowCard>` + `<NumberScramble>`.
   - Cards Receitas/Despesas com `<GlowCard>` (verde/vermelho).
   - Stagger de entrada e `<DashboardSkeleton>` em `src/features/dashboard/components/DashboardSkeleton.tsx` quando `isLoading`.
4. **ConnectionsPage**:
   - `<ConnectionCard>` envolto por `<LedBorder active={isConnected}>`.
   - Botão de sincronização com `<MagneticButton>`.
5. **TransactionsPage**:
   - Resumo com `<GlowCard>` + stagger.
   - `<TransactionsTable>` com `<ScannerReveal>`.
6. **Topbar**:
   - Micro-interações táteis no botão de notificações com `whileHover`/`whileTap`.

---

## 6. Acessibilidade & Motion Guidelines

- `useReducedMotion` em todos os 8 componentes de motion para desligar animações e loops de RAF.
- Canvas cleanup obrigatório (`cancelAnimationFrame`, listener de visibilidade `visibilitychange`).
- Tipografia `font-display` restrita aos KPIs, títulos e marca.

---

## 7. Ordem de Implementação

1. **Tokens e CSS**: Adicionar variáveis de `@theme`, Google Fonts `Syne` e utilitários.
2. **Instalar `framer-motion@^12`**: Validar instalação limpa em `FinanceHub.Web`.
3. **Criar componentes de motion em `src/shared/components/motion/`**:
   - `LogoMark.tsx`
   - `GlowCard.tsx`
   - `NavActiveIndicator.tsx`
   - `MagneticButton.tsx`
   - `NumberScramble.tsx`
   - `LedBorder.tsx`
   - `AuroraBackground.tsx`
   - `ScannerReveal.tsx`
   - `index.ts`
4. **Integrações de Telas**:
   - Sidebar (`NavActiveIndicator`, `LayoutGroup`)
   - Topbar (micro-interações)
   - LoginPage (AuroraBackground, LogoMark, MagneticButton, Syne)
   - DashboardPage & DashboardSkeleton (GlowCard, NumberScramble, Stagger, Skeleton)
   - ConnectionsPage (LedBorder, MagneticButton, GlowCard)
   - TransactionsPage (GlowCard, ScannerReveal)
5. **Tipografia Global**: Adicionar `font-display` nos KPIs e títulos especificados.
6. **Testes & Verificação**: Rodar testes unitários do Vitest e garantir 100% de integridade.
