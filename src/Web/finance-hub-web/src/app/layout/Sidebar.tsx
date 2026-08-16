import React from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, ReceiptText, Landmark, LogOut } from 'lucide-react';
import { cn } from '@/shared/utils/cn';

export const Sidebar: React.FC = () => {
  const navItems = [
    { label: 'Dashboard', to: '/', icon: LayoutDashboard },
    { label: 'Transações', to: '/transacoes', icon: ReceiptText },
    { label: 'Conexões', to: '/conexoes', icon: Landmark },
  ];

  return (
    <aside className="w-64 bg-secondary text-white flex flex-col justify-between p-6 shadow-elevated">
      <div>
        {/* Logo / Brand Header */}
        <div className="flex items-center gap-3 mb-10 px-2">
          <div className="w-9 h-9 rounded-xl bg-brand flex items-center justify-center font-black text-white text-lg shadow-sm">
            F
          </div>
          <div>
            <h1 className="font-extrabold text-base tracking-tight text-white leading-none">FinanceHub</h1>
            <span className="text-[11px] text-secondary-light/70 font-medium">Open Finance</span>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex flex-col gap-1.5">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all duration-150',
                    isActive
                      ? 'bg-brand text-white shadow-sm'
                      : 'text-secondary-light/80 hover:bg-secondary-dark hover:text-white'
                  )
                }
              >
                <Icon className="w-4 h-4" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>
      </div>

      {/* User / Session Footer */}
      <div className="pt-6 border-t border-secondary-dark/60 flex items-center justify-between px-2">
        <div className="flex flex-col">
          <span className="text-xs font-bold text-white">Usuário</span>
          <span className="text-[11px] text-secondary-light/60">Sessão Ativa</span>
        </div>
        <NavLink
          to="/login"
          className="p-2 text-secondary-light/70 hover:text-white hover:bg-secondary-dark rounded-lg transition-colors"
          title="Encerrar Sessão"
        >
          <LogOut className="w-4 h-4" />
        </NavLink>
      </div>
    </aside>
  );
};
