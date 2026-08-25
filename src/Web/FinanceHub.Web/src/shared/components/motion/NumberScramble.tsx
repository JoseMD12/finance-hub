import { useEffect, useState } from 'react';
import { useReducedMotion } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface NumberScrambleProps {
  value: number;
  format: (n: number) => string;
  duration?: number; // ms, default: 900
  className?: string;
}

export const NumberScramble = ({
  value,
  format,
  duration = 900,
  className,
}: NumberScrambleProps) => {
  const prefersReduced = useReducedMotion();
  const [displayValue, setDisplayValue] = useState<string>(() => format(value));

  useEffect(() => {
    if (prefersReduced) {
      setDisplayValue(format(value));
      return;
    }

    let rafId: number;
    const startTime = performance.now();
    const targetValue = value;
    const baseMagnitude = Math.abs(targetValue) || 1000;

    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);

      if (progress < 0.85) {
        // Scramble phase with cryptographically secure random value for animation
        const randomArray = new Uint32Array(2);
        crypto.getRandomValues(randomArray);
        const rand1 = randomArray[0] / (0xffffffff + 1);
        const rand2 = randomArray[1] / (0xffffffff + 1);

        const randomMultiplier = 0.5 + rand1 * 1.5;
        const randomSign = targetValue < 0 ? -1 : 1;
        const scrambled = randomSign * (rand2 * baseMagnitude * randomMultiplier);
        setDisplayValue(format(scrambled));
      } else if (progress < 1) {
        // Interpolation phase with cubic ease out
        const subProgress = (progress - 0.85) / 0.15;
        const ease = 1 - Math.pow(1 - subProgress, 3);
        const interpolated = targetValue * ease;
        setDisplayValue(format(interpolated));
      } else {
        // Final exact target value
        setDisplayValue(format(targetValue));
        return;
      }

      rafId = requestAnimationFrame(animate);
    };

    rafId = requestAnimationFrame(animate);

    return () => {
      cancelAnimationFrame(rafId);
    };
  }, [value, format, duration, prefersReduced]);

  return (
    <span className={cn('font-display tabular-nums inline-block', className)}>
      {displayValue}
    </span>
  );
};
