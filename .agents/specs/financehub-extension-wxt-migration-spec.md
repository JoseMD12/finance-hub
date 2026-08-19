# FinanceHub Extension WXT Migration Specification

**Status:** In Progress
**Owner:** FinanceHub
**Scope:** `src/Web/FinanceHub.Web.Extension/` with legacy reference at `extensions/financehub-pluggy-extension/`
**Related rules:** `.agents/rules/browser-extension-architecture.md`

## 1. Goal

Migrate the FinanceHub Chrome extension from manually maintained JavaScript, HTML and CSS files to a maintainable WXT + TypeScript codebase under `src/`, while preserving token capture, logout detection, Side Panel behavior, account loading and navigation to FinanceHub.

## 2. Confirmed Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Browser platform | Chrome Manifest V3 | Required runtime for the current Side Panel and Service Worker implementation. |
| Build framework | WXT | File-based entrypoints, Vite workflow, TypeScript support and future browser targets without hiding browser APIs. |
| Source location | `src/Web/FinanceHub.Web.Extension/src/` | Keeps the extension beside the web application while separating authored code from generated output. |
| Language | TypeScript strict | Makes message, storage and backend contracts explicit. |
| Primary UI surface | Side Panel | The current user experience is persistent beside Meu.Pluggy, not an action popup. |
| Backend data | FinanceHub PluggyIntegration accounts endpoint | Account identity and institution data must remain real backend data. |
| Shared package | `src/Web/FinanceHub.Web.Shared/` | Independently versioned local package for contracts, tokens and reusable extension code. |
| Environment model | One build configuration from `.env` | The extension does not need separate development, staging and production environments. |
| Legacy migration | Keep the legacy extension until parity validation | Allows rollback and side-by-side comparison before removing the old implementation. |

## 3. Runtime Design

```text
Meu.Pluggy page
    │
    ├── Content Script observes authentication state
    │       │
    │       └── typed runtime message
    │
    ├── Main World bridge observes the minimum token response data
    │       │
    │       └── typed runtime message
    │
    └── Service Worker
            ├── persists/removes token in chrome.storage.local
            ├── applies logout lock
            └── publishes storage changes

Side Panel
    ├── observes storage changes
    ├── decodes display-only identity claims
    ├── calls GET /api/v1/pluggy/accounts
    ├── renders accounts and session state
    └── opens FinanceHub
```

## 4. Target Entrypoints

- `src/Web/FinanceHub.Web.Extension/src/entrypoints/background.ts`: WXT background Service Worker.
- `src/Web/FinanceHub.Web.Extension/src/entrypoints/content.ts`: Meu.Pluggy Content Script.
- `src/Web/FinanceHub.Web.Extension/src/entrypoints/sidepanel/`: Side Panel document and UI entrypoint.

## 5. Open Decision 1 — Side Panel UI Technology

### Decision: React + TypeScript

The Side Panel already has multiple independently changing states (session, identity, account loading, API error, logout and clipboard feedback). React gives those states explicit components and makes future UI changes safer. WXT supports React-based extension entrypoints while keeping Chrome APIs available.

### Alternative: TypeScript with DOM modules

This has fewer dependencies and a smaller output, but state transitions and DOM cleanup remain manual. It is suitable for a very small static panel, but the current panel has already outgrown that shape.

The migrated WXT Side Panel uses React + TypeScript. The extension project remains independent and consumes `FinanceHub.Web.Shared` through a local `file:` dependency.

## 6. Planned API Contract

```text
GET http://localhost:5056/api/v1/pluggy/accounts
X-Pluggy-Access-Token: <Pluggy access token>
```

The extension consumes institution name, account name, account type/subtype, balance and credit data. It does not call Pluggy directly for account details.

## 7. Migration Acceptance Criteria

- The unpacked WXT build loads successfully in `chrome://extensions`.
- Token capture continues to work after Meu.Pluggy login and page reload.
- Logout clears token, identity and accounts while the Side Panel remains open.
- Account data is loaded from FinanceHub and errors are shown without sensitive details.
- The “Voltar para o FinanceHub” action opens the FinanceHub application.
- No raw token, secret or financial payload is logged or committed.
- TypeScript, manifest, tests and manual smoke checks pass.
