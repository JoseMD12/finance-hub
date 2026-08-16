import React from 'react';
import { cn } from '@/shared/utils/cn';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading = false, children, disabled, ...props }, ref) => {
    const baseStyles = 'inline-flex items-center justify-center gap-2 font-medium transition-all duration-200 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed outline-none focus-visible:ring-2 focus-visible:ring-offset-2';
    
    const sizeStyles = {
      sm: 'text-xs px-3 py-1.5 rounded-lg',
      md: 'text-sm px-4 py-2.5 rounded-xl',
      lg: 'text-base px-6 py-3 rounded-xl',
    };

    const variantStyles = {
      primary: 'bg-brand text-white hover:bg-brand-dark shadow-sm hover:shadow active:scale-[0.99] focus-visible:ring-brand',
      secondary: 'bg-secondary text-white hover:bg-secondary-dark active:scale-[0.99] focus-visible:ring-secondary',
      outline: 'border border-brand text-brand hover:bg-brand-light active:scale-[0.99] focus-visible:ring-brand',
      ghost: 'bg-surface-muted text-secondary hover:bg-secondary-light active:scale-[0.99] focus-visible:ring-secondary',
      danger: 'bg-status-danger text-white hover:opacity-90 active:scale-[0.99] focus-visible:ring-status-danger',
    };

    return (
      <button
        ref={ref}
        disabled={disabled || isLoading}
        className={cn(baseStyles, sizeStyles[size], variantStyles[variant], className)}
        {...props}
      >
        {isLoading && (
          <svg className="animate-spin h-4 w-4 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        )}
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
