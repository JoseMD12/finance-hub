import React from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, ReceiptText, Landmark, LogOut } from 'lucide-react';
import { cn } from '@/shared/utils/cn';

export const Sidebar: React.FC = () => {
  const navItems = [
    { label: 'Dashboard', to: '/', icon: LayoutDashboard },
    { label: 'Transações & Extrato', to: '/transacoes', icon: ReceiptText },
    { label: 'Conexões & Importador', to: '/conexoes', icon: Landmark },
  ];

  return (
    <aside className="w-64 bg-secondary text-white flex flex-col justify-between p-6 shadow-elevated border-r border-secondary-dark/40 select-none">
      <div>
        {/* Logo / Brand Header (Aligned with Refs & Design System) */}
        <div className="flex items-center gap-3 mb-10 px-2">
          <div className="w-10 h-10 rounded-xl bg-brand flex items-center justify-center font-black text-white text-xl shadow-brand transform transition-transform hover:scale-105">
            F
          </div>
          <div>
            <h1 className="font-extrabold text-lg tracking-tight text-white leading-none">FinanceHub</h1>
            <span className="text-[11px] text-secondary-light/80 font-semibold tracking-wide">Open Finance Platform</span>
          </div>
        </div>

        {/* Navigation Section */}
        <div className="mb-3 px-2">
          <span className="text-[10px] font-bold uppercase tracking-wider text-secondary-light/50">Menu Principal</span>
        </div>

        <nav className="flex flex-col gap-2">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all duration-200 group',
                    isActive
                      ? 'bg-brand text-white shadow-brand font-bold translate-x-1'
                      : 'text-secondary-light/80 hover:bg-secondary-dark hover:text-white hover:translate-x-1'
                  )
                }
              >
                <Icon className="w-4 h-4 transition-transform group-hover:scale-110" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>
      </div>

      {/* User / Session Footer */}
      <div className="pt-6 border-t border-secondary-dark/80 flex items-center justify-between px-2">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-secondary-light/20 border border-secondary-light/40 flex items-center justify-center font-bold text-xs text-white">
            FH
          </div>
          <div className="flex flex-col">
            <span className="text-xs font-bold text-white leading-tight">Usuário FinanceHub</span>
            <span className="text-[10px] text-secondary-light/70 font-medium">Sessão Autenticada</span>
          </div>
        </div>
        <NavLink
          to="/login"
          className="p-2 text-secondary-light/70 hover:text-white hover:bg-secondary-dark rounded-xl transition-all hover:scale-105"
          aria-label="Encerrar sessão"
          title="Encerrar Sessão"
        >
          <LogOut className="w-4 h-4" />
        </NavLink>
      </div>
    </aside>
  );
};
