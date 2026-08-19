# FinanceHub Chrome Extension Architecture Plan

**Status:** Proposed
**Scope:** `extensions/financehub-pluggy-extension/`
**Context:** Chrome MV3 extension that captures the Meu.Pluggy access token, displays the session state, lists connected accounts through FinanceHub and opens the FinanceHub web application.

## 1. Objectives

- Make the extension easy to understand, test and evolve.
- Keep token capture isolated from presentation and backend communication.
- Preserve the current user flow: open the Side Panel, copy the token and return to FinanceHub.
- Keep account data real and supplied by the FinanceHub backend.
- Reduce permissions, hardcoded values and duplicated visual rules.
- Detect login, logout and token changes without requiring the user to close and reopen the panel.

## 2. Current Problems

The current extension is functional, but responsibilities are mixed:

- `background.js` contains lifecycle handling, network interception, token persistence, logout cleanup and message routing.
- `content.js` contains main-world fetch/XHR interception, logout detection and page communication.
- `popup.js` contains JWT parsing, backend HTTP calls, account mapping, DOM rendering, storage subscriptions and clipboard behavior.
- `popup.html` contains the complete visual system, inline styles and application behavior markup.
- URLs, storage keys, header names and timing values are spread through JavaScript files.
- The manifest requests permissions without a documented permission matrix.
- There are no automated extension tests, fixtures or a documented manual verification flow.

## 3. Target Structure

```text
extensions/financehub-pluggy-extension/
├── manifest.json
├── README.md
├── src/
│   ├── background/
│   │   ├── service-worker.js
│   │   ├── tokenCapture.js
│   │   ├── sessionState.js
│   │   └── messageRouter.js
│   ├── content/
│   │   ├── content-script.js
│   │   ├── pageAuthObserver.js
│   │   └── logoutDetector.js
│   ├── sidepanel/
│   │   ├── sidepanel.html
│   │   ├── sidepanel.css
│   │   ├── sidepanel.js
│   │   ├── accountList.js
│   │   ├── sessionCard.js
│   │   └── userIdentity.js
│   └── shared/
│       ├── constants.js
│       ├── messages.js
│       ├── storage.js
│       ├── pluggyApi.js
│       ├── jwtClaims.js
│       └── dom.js
├── tests/
│   ├── background/
│   ├── content/
│   ├── sidepanel/
│   └── shared/
└── assets/
    └── icons/
```

`popup.html` may remain the Side Panel entry point during migration, but the target name should be `sidepanel.html` because the UI is no longer an action popup.

## 4. Responsibilities and Boundaries

### Background service worker

The service worker is the only owner of extension-wide session persistence and browser event listeners.

It may:

- listen to `webRequest` and receive messages from content scripts;
- validate the JWT shape without logging or exposing its value;
- persist or remove the token through `storage.js`;
- route typed messages;
- open the Side Panel after an explicit user action.

It must not:

- render HTML;
- parse account display labels;
- contain CSS or UI copy;
- call the FinanceHub accounts endpoint for the Side Panel.

### Content script

The content script owns page-context integration only:

- install the main-world fetch/XHR observer;
- forward captured tokens to the service worker;
- observe login/logout navigation and logout controls;
- send typed authentication-state messages.

It must not persist tokens directly or make backend account requests.

### Side Panel

The Side Panel owns presentation and user interaction:

- subscribe to storage changes;
- request account data when a token becomes available;
- render user identity, token status and connected accounts;
- copy the token;
- open FinanceHub.

It must not intercept network traffic or implement logout detection.

### Shared modules

Shared modules centralize contracts and policies:

- constants for URLs, storage keys, headers and timeouts;
- message names and payload shapes;
- storage read/write/remove operations;
- backend HTTP client and RFC 7807 error normalization;
- safe JWT claim decoding for display-only purposes;
- account type labels and DOM-safe rendering helpers.

## 5. Coding Standards

- Use strict mode and semicolon-based JavaScript consistently.
- Use one responsibility per module and functions small enough to test independently.
- Use named constants instead of inline strings, URLs, header names or timing values.
- Use typed message names and validate message payloads at the boundary.
- Never log raw tokens, authorization headers, account numbers or financial payloads.
- Never use `innerHTML` with backend or token-derived values; use `textContent` and DOM APIs.
- Keep all API calls in `shared/pluggyApi.js`.
- Keep storage access in `shared/storage.js`.
- Keep all design tokens in `sidepanel.css` variables; do not introduce undefined CSS variables.
- Prefer project tokens: `brand`, `brand-dark`, `brand-light`, `secondary`, `secondary-dark`, `secondary-light`, `surface-ground`, `surface-card`, `surface-muted`, `border-subtle`, `shadow-card` and `shadow-elevated`.
- All interactive controls need accessible names, visible focus states and keyboard support.
- Use outline SVG icons only; no emojis and no pure-white surfaces.
- Preserve `prefers-reduced-motion` by disabling lift transitions when requested.

## 6. Security and Privacy Rules

- Keep the Pluggy token only in `chrome.storage.local` while the current architecture requires it; do not place it in URLs, logs or DOM attributes.
- Display only non-sensitive JWT claims such as the user email/name; treat decoded claims as presentation data, not verified authorization.
- Send the Pluggy token only in the dedicated `X-Pluggy-Access-Token` header to the configured local backend endpoint.
- Allow backend requests only to the configured FinanceHub origin.
- Remove the token and cached account state on logout or invalid/expired session responses.
- Avoid broad host permissions; document every remaining permission in `README.md`.
- Add a short-lived logout lock so the authorization request generated by logout cannot immediately repopulate the token.
- Do not add client ID, client secret or Pluggy credentials to the extension.

## 7. Backend Contract

The extension should consume a single documented endpoint client:

```text
GET http://localhost:5056/api/v1/pluggy/accounts
Header: X-Pluggy-Access-Token: <token>
```

The response must be an immutable DTO containing only the fields needed by the Side Panel:

- `itemId`;
- `institutionName`;
- `name`;
- `type`;
- `subtype`;
- `balance`;
- `creditData` when applicable.

Backend failures must render a useful non-sensitive state in the Side Panel without exposing response bodies or tokens.

## 8. Testing Strategy

Add tests before each migration step:

- `shared/storage`: token save, read, remove and logout cleanup.
- `shared/jwtClaims`: valid, malformed and missing claims without throwing.
- `shared/pluggyApi`: success, empty response, 401/403, 5xx and malformed JSON.
- `background/tokenCapture`: accepts valid JWT-shaped values and ignores invalid values.
- `background/sessionState`: logout lock prevents immediate token restoration.
- `content/logoutDetector`: detects text, `aria-label`, `title`, nested icon clicks and login routes.
- `sidepanel`: empty state, token-found state, account loading, account error, clipboard action and FinanceHub navigation.

Use Node's built-in test runner or Vitest with Chrome API fakes. Keep fixtures synthetic and never store real tokens or account data in tests.

Manual smoke test:

1. Reload the unpacked extension.
2. Open Meu.Pluggy and click the extension icon.
3. Login and verify the panel changes without reopening.
4. Verify the user and accounts come from the backend.
5. Copy the token and open FinanceHub.
6. Logout and verify the token, user and accounts disappear.
7. Reload Meu.Pluggy with the panel open and verify state refresh.
8. Resize the Side Panel and verify layout and contrast.

## 9. Migration Phases

### Phase 1 — Stabilize current behavior

- Fix undefined design tokens and contrast regressions.
- Add `README.md` with installation, permissions, backend URL and smoke test.
- Extract constants, storage keys and message names without changing behavior.
- Add tests for logout, storage changes and account rendering.

### Phase 2 — Separate runtime modules

- Move service-worker responsibilities into background modules.
- Move page authentication detection into content modules.
- Move API and JWT handling out of `popup.js`.
- Split CSS and markup from behavior.

### Phase 3 — Harden boundaries

- Validate all runtime messages.
- Normalize backend errors without leaking sensitive details.
- Narrow host permissions and document the remaining ones.
- Add expiration/invalid-session handling and cached-account cleanup.

### Phase 4 — Improve delivery

- Add a lightweight build/package script that copies the MV3 files into a distributable directory.
- Add lint and test commands for the extension.
- Add a CI check for JSON validity, JavaScript syntax, undefined CSS variables and secret patterns.
- Keep each migration as a small English Conventional Commit.

## 10. Definition of Done

- No monolithic `popup.js` or `popup.html` remains as the owner of unrelated concerns.
- No undefined CSS token is referenced.
- No raw token or PII appears in logs, fixtures or generated artifacts.
- Account and session states update without reopening the Side Panel.
- Logout reliably clears token, identity and account data.
- Automated tests cover the critical token and logout flows.
- `README.md` documents loading, permissions, backend dependency and troubleshooting.
- JavaScript syntax, manifest validation, tests and a manual Chrome smoke test pass.
