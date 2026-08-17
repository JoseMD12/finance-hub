import React from 'react';
import { Search, Bell } from 'lucide-react';

export const Topbar: React.FC = () => {
  return (
    <header className="h-18 bg-white border-b border-border-subtle px-8 flex items-center justify-between">
      {/* Search Input */}
      <div className="relative w-80">
        <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
        <input
          type="text"
          placeholder="Pesquisar transações, categorias..."
          className="w-full pl-10 pr-4 py-2 text-xs font-medium bg-surface-ground border border-border-subtle rounded-xl outline-none focus:border-brand transition-colors"
        />
      </div>

      {/* Actions & Status */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-full bg-status-success-bg text-status-success text-xs font-bold">
          <span className="w-2 h-2 rounded-full bg-status-success animate-pulse" />
          <span>Gateway Online</span>
        </div>
        <button
          type="button"
          aria-label="Notificações"
          className="p-2 text-slate-500 hover:text-secondary hover:bg-slate-100 rounded-xl transition-colors"
        >
          <Bell className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
};
