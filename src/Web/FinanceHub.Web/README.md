# 🌐 FinanceHub.Web — Frontend Application

Aplicação Web moderna do **FinanceHub** desenvolvida em **React 19**, **Vite**, **TypeScript**, **TailwindCSS (v4 @theme tokens)** e **TanStack Query (v5)**.

---

## 🏛️ Arquitetura do Frontend

A aplicação é estruturada sob **Feature-Driven Vertical Slices** em conformidade com as regras arquiteturais do FinanceHub:

```
src/
├── app/                        # Shell da aplicação, Layout, Providers e Roteamento
│   ├── layout/                 # Sidebar, Topbar, AppLayout
│   ├── providers/              # QueryClientProvider, AuthProvider, Sonner Toaster
│   └── routes/                 # AppRoutes (React Router)
├── features/                   # Slices verticais independentes por domínio
│   ├── auth/                   # Autenticação e tela de login
│   ├── connections/            # Gestão de conexões Open Finance (Pluggy) e arquivos
│   ├── dashboard/              # Visão geral de saldos consolidados e KPIs
│   └── transactions/           # Extrato consolidado, listagem, filtros e categorização (tags)
└── shared/                     # Abstrações reutilizáveis compartilhadas
    ├── api/                    # Axios httpClient com interceptors RFC 7807 e endpoints centralizados
    ├── components/             # Button, Card, Custom Select, StatusBadge, Toast
    └── utils/                  # Formatação de moeda BRL, datas e manipuladores de string
```

---

## 🎨 Diretrizes de Design & Tokens

- **Off-White Standard**: Todas as superfícies utilizam o token `--color-surface-card` (`#FAFCFB` / `bg-surface-card`). O uso de branco puro (`#FFFFFF`) é estritamente proibido.
- **Zero Emojis**: Ícones vetoriais padronizados via `lucide-react`.
- **Formatação BRL**: Formatação mandatória de moedas via utilitários `formatCurrencyBRL`.
- **Erros RFC 7807**: Tratamento de respostas de erro no formato `application/problem+json` com feedback amigável via toasts `Sonner`.

---

## ⚡ Como Executar Localmente

```bash
# Instalar dependências
npm install

# Iniciar servidor de desenvolvimento (Vite)
npm run dev

# Executar testes unitários e de componentes (Vitest)
npm run test

# Executar build de produção
npm run build
```
