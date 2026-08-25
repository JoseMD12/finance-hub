import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.stubGlobal('__FINANCEHUB_WEB_URL__', 'http://localhost:3000');
vi.stubGlobal('__FINANCEHUB_API_URL__', 'http://localhost:5050');

import { openFinanceHub, openMeuPluggy, scheduleSidePanelClose } from './sidePanelService';
import { RUNTIME_URLS } from '../../shared/constants/runtime';
import { browser } from 'wxt/browser';

vi.mock('wxt/browser', () => ({
  browser: {
    tabs: {
      create: vi.fn(),
      query: vi.fn(),
      update: vi.fn(),
      onUpdated: {
        addListener: vi.fn(),
        removeListener: vi.fn(),
      },
      reload: vi.fn(),
    },
    windows: {
      getCurrent: vi.fn(),
      update: vi.fn(),
    },
    sidePanel: {
      close: vi.fn(),
    },
    scripting: {
      executeScript: vi.fn(),
    },
    storage: {
      local: {
        get: vi.fn(),
      },
    },
  },
}));

describe('sidePanelService navigation and actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (browser.storage.local.get as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({});
    (browser.tabs.query as unknown as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    (browser.tabs.create as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 101 });
    (browser.windows.getCurrent as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 1 });
  });

  it('opens Meu.Pluggy overview in a new browser tab', async () => {
    await openMeuPluggy();
    expect(browser.tabs.create).toHaveBeenCalledWith({ url: RUNTIME_URLS.meuPluggy });
  });

  it('opens FinanceHub /conexoes in a new tab when no existing tab is open', async () => {
    (browser.tabs.query as unknown as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 10, url: 'https://google.com' },
    ]);

    await openFinanceHub(null);
    expect(browser.tabs.create).toHaveBeenCalledWith({ url: RUNTIME_URLS.financeHubWeb });
  });

  it('focuses existing FinanceHub tab when already open', async () => {
    (browser.tabs.query as unknown as ReturnType<typeof vi.fn>).mockResolvedValue([
      { id: 200, url: 'http://localhost:3000/dashboard', windowId: 5 },
    ]);

    await openFinanceHub(null);
    expect(browser.tabs.update).toHaveBeenCalledWith(200, {
      active: true,
      url: RUNTIME_URLS.financeHubWeb,
    });
    expect(browser.windows.update).toHaveBeenCalledWith(5, { focused: true });
    expect(browser.tabs.create).not.toHaveBeenCalled();
  });

  it('schedules side panel close after specified delay', () => {
    vi.useFakeTimers();
    scheduleSidePanelClose();
    expect(browser.sidePanel.close).not.toHaveBeenCalled();

    vi.advanceTimersByTime(2500);
    expect(browser.windows.getCurrent).toHaveBeenCalled();
    vi.useRealTimers();
  });
});
