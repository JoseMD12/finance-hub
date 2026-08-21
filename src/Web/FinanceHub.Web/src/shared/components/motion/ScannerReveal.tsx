import React, { type ReactNode } from 'react';
import { motion, useReducedMotion, type Variants } from 'framer-motion';

export interface ScannerRevealProps {
  children: ReactNode;
  staggerMs?: number; // default: 80ms
  className?: string;
  as?: 'div' | 'tbody' | 'ul';
}

export const ScannerReveal = ({
  children,
  staggerMs = 80,
  className,
  as = 'div',
}: ScannerRevealProps) => {
  const prefersReduced = useReducedMotion();

  if (prefersReduced) {
    if (as === 'tbody') {
      return <tbody className={className}>{children}</tbody>;
    }
    if (as === 'ul') {
      return <ul className={className}>{children}</ul>;
    }
    return <div className={className}>{children}</div>;
  }

  const containerVariants: Variants = {
    hidden: {},
    visible: {
      transition: {
        staggerChildren: staggerMs / 1000,
      },
    },
  };

  const itemVariants: Variants = {
    hidden: { opacity: 0, y: 8 },
    visible: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] as const },
    },
  };

  const items = React.Children.toArray(children);

  if (as === 'tbody') {
    return (
      <motion.tbody
        variants={containerVariants}
        initial="hidden"
        animate="visible"
        className={className}
      >
        {items.map((child, index) => (
          <motion.tr
            key={index}
            variants={itemVariants}
            className="scanner-item"
            style={{
              animationDelay: `${index * staggerMs}ms`,
            }}
          >
            {React.isValidElement(child) ? (child.props as { children?: ReactNode }).children : child}
          </motion.tr>
        ))}
      </motion.tbody>
    );
  }

  const ContainerComponent = as === 'ul' ? motion.ul : motion.div;

  return (
    <ContainerComponent
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className={className}
    >
      {items.map((child, index) => (
        <motion.div
          key={index}
          variants={itemVariants}
          className="scanner-item"
          style={{
            animationDelay: `${index * staggerMs}ms`,
          }}
        >
          {child}
        </motion.div>
      ))}
    </ContainerComponent>
  );
};
