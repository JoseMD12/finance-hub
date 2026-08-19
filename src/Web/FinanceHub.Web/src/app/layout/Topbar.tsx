import { Search, Bell, ShieldCheck } from 'lucide-react';

export const Topbar: React.FC = () => {
  return (
    <header className="h-16 bg-surface-card border-b border-border-subtle px-8 flex items-center justify-between shadow-sm select-none">
      {/* Search Input */}
      <div className="relative w-80">
        <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" aria-hidden="true" />
        <input
          type="text"
          placeholder="Pesquisar transações, categorias, extratos..."
          className="w-full pl-10 pr-4 py-2 text-xs font-medium bg-surface-ground border border-border-subtle rounded-xl outline-none form-input-focus transition-all duration-200"
          aria-label="Pesquisar transações e faturas"
        />
      </div>

      {/* Actions & Status */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-status-success-bg text-status-success text-xs font-bold shadow-sm">
          <span className="w-2 h-2 rounded-full bg-status-success animate-pulse" />
          <ShieldCheck className="w-3.5 h-3.5" aria-hidden="true" />
          <span>Open Finance Online</span>
        </div>
        <button
          type="button"
          aria-label="Central de notificações"
          className="p-2 text-slate-500 hover:text-secondary hover:bg-surface-muted rounded-xl transition-all duration-200 hover:scale-105 active:scale-95"
          title="Notificações"
        >
          <Bell className="w-4.5 h-4.5" />
        </button>
      </div>
    </header>
  );
};
