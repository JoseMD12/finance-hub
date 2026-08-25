import { useEffect, useRef } from 'react';
import { useReducedMotion } from 'framer-motion';
import { cn } from '../../utils/cn';

export interface AuroraBackgroundProps {
  className?: string;
}

export const AuroraBackground = ({ className }: AuroraBackgroundProps) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const prefersReduced = useReducedMotion();

  useEffect(() => {
    if (prefersReduced) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let rafId: number;
    let t = 0;

    const handleResize = () => {
      const parent = canvas.parentElement;
      if (!parent) return;
      const rect = parent.getBoundingClientRect();
      const dpr = window.devicePixelRatio || 1;
      canvas.width = rect.width * dpr;
      canvas.height = rect.height * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    handleResize();
    const resizeObserver = new ResizeObserver(handleResize);
    if (canvas.parentElement) {
      resizeObserver.observe(canvas.parentElement);
    }

    const draw = () => {
      const parent = canvas.parentElement;
      if (!parent) return;
      const width = parent.clientWidth;
      const height = parent.clientHeight;

      ctx.clearRect(0, 0, width, height);

      t += 0.006;

      // Blob 1: Brand pink
      const b1X = width * 0.4 + Math.sin(t * 0.8) * width * 0.25;
      const b1Y = height * 0.35 + Math.cos(t * 0.6) * height * 0.2;
      const b1Radius = Math.max(width, height) * 0.55;
      const grad1 = ctx.createRadialGradient(b1X, b1Y, 0, b1X, b1Y, b1Radius);
      grad1.addColorStop(0, 'rgba(224, 86, 151, 0.35)');
      grad1.addColorStop(0.6, 'rgba(224, 86, 151, 0.08)');
      grad1.addColorStop(1, 'transparent');
      ctx.fillStyle = grad1;
      ctx.beginPath();
      ctx.arc(b1X, b1Y, b1Radius, 0, Math.PI * 2);
      ctx.fill();

      // Blob 2: Tertiary coral
      const b2X = width * 0.65 + Math.cos(t * 0.7) * width * 0.2;
      const b2Y = height * 0.65 + Math.sin(t * 0.9) * height * 0.25;
      const b2Radius = Math.max(width, height) * 0.45;
      const grad2 = ctx.createRadialGradient(b2X, b2Y, 0, b2X, b2Y, b2Radius);
      grad2.addColorStop(0, 'rgba(255, 115, 56, 0.20)');
      grad2.addColorStop(0.5, 'rgba(255, 115, 56, 0.05)');
      grad2.addColorStop(1, 'transparent');
      ctx.fillStyle = grad2;
      ctx.beginPath();
      ctx.arc(b2X, b2Y, b2Radius, 0, Math.PI * 2);
      ctx.fill();

      // Blob 3: Subtle white highlight
      const b3X = width * 0.5 + Math.sin(t * 1.2) * width * 0.15;
      const b3Y = height * 0.5 + Math.cos(t * 1.1) * height * 0.15;
      const b3Radius = Math.max(width, height) * 0.3;
      const grad3 = ctx.createRadialGradient(b3X, b3Y, 0, b3X, b3Y, b3Radius);
      grad3.addColorStop(0, 'rgba(255, 255, 255, 0.06)');
      grad3.addColorStop(0.5, 'rgba(255, 255, 255, 0.01)');
      grad3.addColorStop(1, 'transparent');
      ctx.fillStyle = grad3;
      ctx.beginPath();
      ctx.arc(b3X, b3Y, b3Radius, 0, Math.PI * 2);
      ctx.fill();

      // Edge vignette blending
      const vignette = ctx.createLinearGradient(0, 0, 0, height);
      vignette.addColorStop(0, 'rgba(29, 85, 90, 0.2)');
      vignette.addColorStop(0.5, 'transparent');
      vignette.addColorStop(1, 'rgba(29, 85, 90, 0.4)');
      ctx.fillStyle = vignette;
      ctx.fillRect(0, 0, width, height);
    };

    const loop = () => {
      if (!document.hidden) {
        draw();
      }
      rafId = requestAnimationFrame(loop);
    };

    rafId = requestAnimationFrame(loop);

    const handleVisibility = () => {
      if (!document.hidden && !rafId) {
        rafId = requestAnimationFrame(loop);
      }
    };

    document.addEventListener('visibilitychange', handleVisibility);

    return () => {
      cancelAnimationFrame(rafId);
      resizeObserver.disconnect();
      document.removeEventListener('visibilitychange', handleVisibility);
    };
  }, [prefersReduced]);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      tabIndex={-1}
      className={cn('pointer-events-none absolute inset-0 h-full w-full', className)}
    />
  );
};
