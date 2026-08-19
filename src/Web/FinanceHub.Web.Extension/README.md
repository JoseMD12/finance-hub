# FinanceHub Web Extension

WXT + React + TypeScript implementation of the FinanceHub Pluggy Side Panel.

## Development

1. Copy `.env.example` to `.env` and adjust the FinanceHub URLs.
2. Install dependencies with `npm install`.
3. Run `npm run dev` to generate a development extension.
4. Load the generated `.output/chrome-mv3-dev` directory in `chrome://extensions`.

## Production package

`npm run build` creates the unpacked production extension. `npm run zip` creates a ZIP artifact for distribution.

The extension reads connected accounts from the FinanceHub API. It never packages Pluggy client credentials or secrets.

## Legacy implementation

The previous implementation remains in `extensions/financehub-pluggy-extension` until the WXT version passes the manual parity checklist in the migration spec.
