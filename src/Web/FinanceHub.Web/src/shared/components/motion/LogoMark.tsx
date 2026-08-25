import { motion, useReducedMotion } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface LogoMarkProps {
  size?: 'sm' | 'md' | 'lg';
  showText?: boolean;
  textColor?: 'dark' | 'light';
  className?: string;
}

const sizeClasses = {
  sm: 'h-7 w-7 text-sm',
  md: 'h-9 w-9 text-base',
  lg: 'h-11 w-11 text-xl',
};

const textSizeClasses = {
  sm: 'text-sm',
  md: 'text-base',
  lg: 'text-2xl',
};

export const LogoMark = ({
  size = 'md',
  showText = false,
  textColor = 'dark',
  className,
}: LogoMarkProps) => {
  const prefersReduced = useReducedMotion();

  return (
    <div className={cn('inline-flex items-center gap-3 select-none', className)}>
      <motion.div
        whileHover={
          prefersReduced
            ? undefined
            : { borderRadius: '50%', rotate: 12, scale: 1.08 }
        }
        transition={{ type: 'spring', stiffness: 300, damping: 20 }}
        style={{ borderRadius: '25%' }}
        className={cn(
          'flex items-center justify-center bg-brand font-display font-black text-white shadow-brand cursor-pointer',
          sizeClasses[size]
        )}
      >
        F
      </motion.div>

      {showText && (
        <span
          className={cn(
            'font-bold tracking-tight flex items-center',
            textColor === 'light' ? 'text-white' : 'text-slate-800',
            textSizeClasses[size]
          )}
        >
          <span>Finance</span>
          <span className="font-display font-black text-brand ml-0.5">Hub</span>
        </span>
      )}
    </div>
  );
};

