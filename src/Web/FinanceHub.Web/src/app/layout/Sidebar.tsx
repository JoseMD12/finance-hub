import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import {
  Code2,
  Landmark,
  LayoutGrid,
  LogOut,
  UserRound,
} from 'lucide-react';
import { cn } from '@/shared/utils/cn';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';

export interface SidebarProps {
  initialCollapsed?: boolean;
}

export const Sidebar: React.FC<SidebarProps> = ({ initialCollapsed = false }) => {
  const [isCollapsed, setIsCollapsed] = useState(initialCollapsed);

  const navItems = [
    { label: 'Dashboard', to: '/', icon: LayoutGrid },
    { label: 'Dados', to: '/transacoes', icon: Code2 },
    { label: 'Conexões', to: '/conexoes', icon: Landmark },
  ];

  return (
    <aside
      className={cn(
        'bg-surface-card border-r border-border-subtle flex flex-col justify-between p-4 shadow-card transition-all duration-300 ease-in-out select-none relative z-30',
        isCollapsed ? 'w-20' : 'w-64'
      )}
    >
      {/* Toggle Collapse/Expand Button */}
      <button
        type="button"
        onClick={() => setIsCollapsed((prev) => !prev)}
        aria-label={isCollapsed ? 'Expandir barra lateral' : 'Recolher barra lateral'}
        title={isCollapsed ? 'Expandir' : 'Recolher'}
        className="absolute -right-3 top-7 w-6 h-6 rounded-full bg-white border border-border-subtle shadow-sm flex items-center justify-center text-slate-500 hover:text-brand hover:scale-110 transition-all cursor-pointer z-40"
      >
        {isCollapsed ? <ChevronRight className="w-3.5 h-3.5" /> : <ChevronLeft className="w-3.5 h-3.5" />}
      </button>
      <div>
        {/* User Profile Header (Fidelidade ao Side Bar.pdf) */}
        <div
          className={cn(
            'flex items-center justify-between mb-8 p-2 rounded-2xl transition-all duration-300',
            isCollapsed ? 'flex-col gap-3 justify-center text-center' : 'gap-3'
          )}
        >
          <div className="flex items-center gap-3 min-w-0">
            <IconCircle
              icon={UserRound}
              tone="brand"
              size="lg"
              className="rounded-full border border-brand/25"
              label="Perfil do usuário"
            />

            {/* User Info (Visible in Expanded mode) */}
            {!isCollapsed && (
              <div className="flex flex-col min-w-0 overflow-hidden">
                <h2 className="font-bold text-sm text-slate-800 truncate leading-tight">José Dotta</h2>
                <span className="text-[11px] text-slate-400 truncate font-medium">
                  josehenriquedotta61@gmail.com
                </span>
              </div>
            )}
          </div>

          {/* Logout Button */}
          {!isCollapsed && (
            <NavLink
              to="/login"
              className="p-2 text-slate-600 hover:text-brand hover:bg-brand-light rounded-xl transition-all flex-shrink-0"
              aria-label="Encerrar sessão"
              title="Encerrar Sessão"
            >
              <LogOut className="w-4.5 h-4.5" />
            </NavLink>
          )}
        </div>

        {/* Navigation Items (Fidelidade ao Side Bar.pdf com Conexões preservado) */}
        <nav className="flex flex-col gap-2">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                title={isCollapsed ? item.label : undefined}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 px-4 py-3 rounded-2xl text-sm font-semibold transition-all duration-200 group',
                    isActive
                      ? 'bg-brand text-white shadow-brand font-bold'
                      : 'text-slate-700 hover:bg-surface-ground hover:text-slate-900',
                    isCollapsed && 'justify-center px-0 py-3'
                  )
                }
              >
                <Icon
                  className={cn(
                    'w-5 h-5 flex-shrink-0 transition-transform duration-200 group-hover:scale-110',
                    isCollapsed && 'w-5 h-5'
                  )}
                />
                {!isCollapsed && <span className="truncate">{item.label}</span>}
              </NavLink>
            );
          })}
        </nav>
      </div>

      {/* Footer / Collapsed Logout Option */}
      {isCollapsed && (
        <div className="pt-4 border-t border-border-subtle flex justify-center">
          <NavLink
            to="/login"
            className="p-2.5 text-slate-500 hover:text-brand hover:bg-brand-light rounded-xl transition-all"
            aria-label="Encerrar sessão"
            title="Encerrar Sessão"
          >
            <LogOut className="w-5 h-5" />
          </NavLink>
        </div>
      )}
    </aside>
  );
};
