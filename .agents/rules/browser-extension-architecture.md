# FinanceHub Browser Extension Architecture Rules

**Scope:** `src/Web/FinanceHub.Web.Extension/`
**Runtime:** Chrome Manifest V3
**Build:** WXT with Vite
**Language:** TypeScript with strict checking

## 1. Terminology

Use the standard browser-extension terms consistently:

- **Manifest V3:** The extension platform contract declared by `manifest.json` generated from `wxt.config.ts`.
- **Service Worker:** The event-driven background runtime. It has no DOM and may be suspended at any time.
- **Content Script:** Code injected into matching web pages. It can access the page DOM but runs in an isolated extension context.
- **Main World:** The page's own JavaScript context. Code injected here must be minimal and communicate through validated `window.postMessage` events.
- **Extension Page:** An extension-owned document such as the Side Panel, options page or an extension tab.
- **Side Panel:** The persistent extension UI displayed beside the current browser tab.
- **Message Contract:** A typed message name and payload exchanged through `chrome.runtime` or `window.postMessage`.
- **Storage Area:** `chrome.storage.local` or another explicitly chosen Chrome storage area.
- **Entrypoint:** A WXT source entry that produces a browser runtime artifact.

Avoid ambiguous names such as `popup` for the primary UI once the extension uses a Side Panel. Use `sidepanel` for the Side Panel entrypoint.

## 2. Source Layout

All authored extension code must live under `extensions/financehub-pluggy-extension/src/`.

```text
extensions/financehub-pluggy-extension/
├── package.json
├── tsconfig.json
├── wxt.config.ts
├── README.md
├── src/
│   ├── entrypoints/
│   │   ├── background.ts
│   │   ├── content.ts
│   │   └── sidepanel/
│   │       ├── index.html
│   │       ├── main.tsx
│   │       └── styles.css
│   ├── shared/
│   │   ├── constants/
│   │   ├── contracts/
│   │   ├── storage/
│   │   ├── messaging/
│   │   ├── api/
│   │   └── security/
│   ├── background/
│   │   ├── token-capture/
│   │   ├── session-state/
│   │   └── message-routing/
│   ├── content/
│   │   ├── authentication-observer/
│   │   └── page-bridge/
│   └── sidepanel/
│       ├── components/
│       ├── hooks/
│       ├── services/
│       └── view-models/
├── public/
│   └── icons/
└── tests/
```

Generated `.output/` files must never be edited or committed.

## 3. Runtime Boundaries

### Service Worker

The Service Worker owns browser-level events, token persistence and message routing. It must not access the DOM, render UI or contain presentation strings.

### Content Script

The Content Script owns page observation and communication with the Service Worker. It must not call the FinanceHub backend or write tokens directly to storage.

### Main World Bridge

Main World code may only observe the minimum fetch/XHR data required to identify the Pluggy session token. It must not log payloads or expose the token to arbitrary page code. All messages must validate `event.source`, `event.origin`, message type and payload shape.

### Side Panel

The Side Panel owns rendering, clipboard interaction, account queries and navigation to FinanceHub. It consumes storage and message contracts; it must not intercept page traffic or implement browser event listeners.

## 4. WXT Rules

- WXT is the build and entrypoint framework; Chrome APIs remain the runtime API.
- Manifest configuration belongs in `wxt.config.ts`, not handwritten generated output.
- Entrypoints must use explicit names matching their runtime purpose: `background`, `content`, and `sidepanel`.
- Use TypeScript for all new source files.
- Keep the generated Manifest V3 permissions reviewable and minimal.
- Do not use remote code, runtime script downloads or inline executable scripts.
- Use WXT/Vite environment handling only for non-secret build configuration. Never package Pluggy secrets.

## 5. Messaging and State

- Centralize message names and payload schemas in `src/shared/messaging/`.
- Use discriminated message types with a `type` field.
- Validate messages at every runtime boundary.
- Centralize storage keys and serialization in `src/shared/storage/`.
- Token changes must be observable by the Side Panel without reopening it.
- Logout must clear the token, account cache, identity display and last synchronization metadata.
- Service Worker state must not rely only on module-level variables because the runtime can be suspended.

## 6. API Rules

- All backend calls belong in `src/shared/api/` or the Side Panel service layer.
- URLs, headers and timeout values must be constants.
- The Pluggy access token is sent only through `X-Pluggy-Access-Token`.
- Do not place tokens in query strings, URLs, logs, DOM attributes or analytics events.
- Backend errors must be normalized into a safe user-facing state without exposing raw response bodies.
- DTOs must contain only the fields needed by the Side Panel.

## 7. Security and Permissions

- Request only the permissions required by the current runtime behavior.
- Document each permission and host permission in `README.md`.
- Never log Bearer tokens, JWT payloads, account numbers, card data or raw financial responses.
- Decoded JWT claims are display-only data and are never treated as authorization.
- Keep account and identity data in memory or extension storage only as long as necessary.
- Use a short-lived logout lock to prevent the logout request from restoring the previous token.
- Do not add client IDs, client secrets or credentials to the extension package.

## 8. UI and Design System

- Centralize colors, typography, spacing, radii, shadows and transitions in Side Panel design tokens.
- Reuse FinanceHub tokens: `brand`, `brand-dark`, `brand-light`, `secondary`, `secondary-dark`, `secondary-light`, `surface-ground`, `surface-card`, `surface-muted`, `border-subtle`, `shadow-card` and `shadow-elevated`.
- Undefined CSS variables are a build failure.
- Do not use pure white surfaces, emojis, vertical title bars or `&` in titles and menu labels.
- Interactive cards use the shared lift animation and respect `prefers-reduced-motion`.
- All icon-only buttons require an accessible `aria-label`.
- Text and colored surfaces must meet WCAG AA contrast requirements.

## 9. Testing and Quality Gates

- Write a failing test before changing production behavior.
- Unit-test storage, JWT claim decoding, message validation, token capture, logout detection and API error handling.
- Test Side Panel states: no token, token found, loading accounts, accounts loaded, backend error and logout.
- Use synthetic tokens and account fixtures only.
- Add static checks for manifest validity, TypeScript compilation, JavaScript bundles, undefined design tokens and secret patterns.
- The extension README must include a manual Chrome smoke test.

## 10. Migration Rule

Migration commits must preserve behavior at each step. Do not combine WXT migration, UI redesign and security changes in one unreviewable commit. Prefer these boundaries:

1. Add WXT toolchain and TypeScript entrypoints.
2. Move shared constants, storage and message contracts.
3. Migrate Service Worker and Content Script.
4. Migrate Side Panel.
5. Add tests and quality gates.
6. Remove legacy root files after parity validation.
