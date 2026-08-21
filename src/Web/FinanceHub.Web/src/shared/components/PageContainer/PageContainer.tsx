import React from 'react';
import { cn } from '@/shared/utils/cn';

export interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  className?: string;
}

export const PageHeader: React.FC<PageHeaderProps> = ({
  title,
  description,
  actions,
  className,
}) => {
  return (
    <header className={cn('flex flex-col md:flex-row md:items-center justify-between gap-4', className)}>
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-extrabold text-secondary tracking-tight">
          {title}
        </h1>
        {description && (
          <p className="text-xs text-slate-500 font-medium">
            {description}
          </p>
        )}
      </div>
      {actions && (
        <div className="flex items-center gap-3 shrink-0">
          {actions}
        </div>
      )}
    </header>
  );
};

export interface PageContainerProps extends React.HTMLAttributes<HTMLDivElement> {
  title?: string;
  description?: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
}

export const PageContainer: React.FC<PageContainerProps> = ({
  title,
  description,
  actions,
  children,
  className,
  ...props
}) => {
  return (
    <div className={cn('flex flex-col gap-6', className)} {...props}>
      {title && (
        <PageHeader
          title={title}
          description={description}
          actions={actions}
        />
      )}
      {children}
    </div>
  );
};
