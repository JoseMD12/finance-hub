import React, { useRef, type ReactNode } from 'react';
import { motion, useInView, useReducedMotion, type Variants } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface ScannerRevealProps {
  children: ReactNode;
  staggerMs?: number; // default: 60ms
  className?: string;
  as?: 'div' | 'tbody' | 'ul';
}

interface ScannerItemProps {
  children: ReactNode;
  index: number;
  staggerMs: number;
  as: 'div' | 'tbody' | 'ul';
  className?: string;
  itemProps?: Record<string, unknown>;
}

const tableRowVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.3,
      ease: [0.25, 1, 0.5, 1],
    },
  },
};

const blockVariants: Variants = {
  hidden: { opacity: 0, y: 6 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.35,
      ease: [0.25, 1, 0.5, 1],
    },
  },
};

const ScannerItem = ({
  children,
  index,
  staggerMs,
  as,
  className,
  itemProps = {},
}: ScannerItemProps) => {
  const ref = useRef<HTMLTableRowElement | HTMLLIElement | HTMLDivElement>(null);
  const isInView = useInView(ref, {
    once: true,
    amount: 0.05,
    margin: '0px 0px -20px 0px',
  });

  const delayMs = Math.min(index * staggerMs, 360);
  const itemStyle = (itemProps.style as React.CSSProperties) || {};

  if (as === 'tbody') {
    return (
      <motion.tr
        ref={ref as React.RefObject<HTMLTableRowElement>}
        variants={tableRowVariants}
        initial="hidden"
        animate={isInView ? 'visible' : 'hidden'}
        className={cn('scanner-item', isInView && 'scanner-active', className)}
        style={{
          ...itemStyle,
          animationDelay: `${delayMs}ms`,
        }}
        {...itemProps}
      >
        {children}
      </motion.tr>
    );
  }

  if (as === 'ul') {
    return (
      <motion.li
        ref={ref as React.RefObject<HTMLLIElement>}
        variants={blockVariants}
        initial="hidden"
        animate={isInView ? 'visible' : 'hidden'}
        className={cn('scanner-item', isInView && 'scanner-active', className)}
        style={{
          ...itemStyle,
          animationDelay: `${delayMs}ms`,
        }}
        {...itemProps}
      >
        {children}
      </motion.li>
    );
  }

  return (
    <motion.div
      ref={ref as React.RefObject<HTMLDivElement>}
      variants={blockVariants}
      initial="hidden"
      animate={isInView ? 'visible' : 'hidden'}
      className={cn('scanner-item', isInView && 'scanner-active', className)}
      style={{
        ...itemStyle,
        animationDelay: `${delayMs}ms`,
      }}
      {...itemProps}
    >
      {children}
    </motion.div>
  );
};

export const ScannerReveal = ({
  children,
  staggerMs = 60,
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

  const items = React.Children.toArray(children);

  if (as === 'tbody') {
    return (
      <tbody className={className}>
        {items.map((child, index) => {
          if (!React.isValidElement(child)) {
            return child;
          }

          const {
            children: childContent,
            className: childClassName,
            ...restProps
          } = (child.props as {
            children?: ReactNode;
            className?: string;
            [key: string]: unknown;
          }) || {};

          return (
            <ScannerItem
              key={child.key ?? index}
              index={index}
              staggerMs={staggerMs}
              as="tbody"
              className={childClassName}
              itemProps={restProps}
            >
              {childContent}
            </ScannerItem>
          );
        })}
      </tbody>
    );
  }

  if (as === 'ul') {
    return (
      <ul className={className}>
        {items.map((child, index) => {
          if (!React.isValidElement(child)) {
            return child;
          }

          const {
            children: childContent,
            className: childClassName,
            ...restProps
          } = (child.props as {
            children?: ReactNode;
            className?: string;
            [key: string]: unknown;
          }) || {};

          return (
            <ScannerItem
              key={child.key ?? index}
              index={index}
              staggerMs={staggerMs}
              as="ul"
              className={childClassName}
              itemProps={restProps}
            >
              {childContent}
            </ScannerItem>
          );
        })}
      </ul>
    );
  }

  return (
    <div className={className}>
      {items.map((child, index) => {
        if (!React.isValidElement(child)) {
          return child;
        }

        const {
          children: childContent,
          className: childClassName,
          ...restProps
        } = (child.props as {
          children?: ReactNode;
          className?: string;
          [key: string]: unknown;
        }) || {};

        return (
          <ScannerItem
            key={child.key ?? index}
            index={index}
            staggerMs={staggerMs}
            as="div"
            className={childClassName}
            itemProps={restProps}
          >
            {childContent}
          </ScannerItem>
        );
      })}
    </div>
  );
};
