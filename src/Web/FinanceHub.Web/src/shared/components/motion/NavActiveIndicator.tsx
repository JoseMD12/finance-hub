import { motion, useReducedMotion } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface NavActiveIndicatorProps {
  className?: string;
  layoutId?: string;
}

export const NavActiveIndicator = ({
  className,
  layoutId = 'fh-nav-active-pill',
}: NavActiveIndicatorProps) => {
  const prefersReduced = useReducedMotion();

  return (
    <motion.span
      layoutId={layoutId}
      className={cn('absolute inset-0 rounded-2xl bg-brand shadow-brand', className)}
      transition={
        prefersReduced
          ? { duration: 0 }
          : { type: 'spring', stiffness: 300, damping: 30 }
      }
    />
  );
};
