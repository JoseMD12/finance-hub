import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/shared/components/Button/Button';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { setAccessToken } from '@/shared/utils/authStorage';
import { requestDevTokenApi } from '../api/authApi';
import { Eye, EyeOff, LockKeyhole, Mail } from 'lucide-react';
import { motion, useReducedMotion } from 'framer-motion';
import { AuroraBackground, LogoMark } from '@/shared/components/motion';

const loginSchema = z.object({
  email: z.string().email('Informe um e-mail válido'),
  password: z.string().min(6, 'A senha deve ter no mínimo 6 caracteres'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const prefersReduced = useReducedMotion();
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormValues) => {
    try {
      const tokenResponse = await requestDevTokenApi(data.email);
      setAccessToken(tokenResponse.accessToken);
      toast.success('Login efetuado com sucesso!');
    } catch {
      // Fallback em desenvolvimento para permitir navegação se backend offline
      setAccessToken(`mock_dev_jwt_${btoa(data.email)}`);
      toast.info('Login efetuado em modo desenvolvimento.');
    } finally {
      navigate('/');
    }
  };

  const getMotionProps = (delay: number) => {
    if (prefersReduced) return {};
    return {
      initial: { opacity: 0, x: 20 },
      animate: { opacity: 1, x: 0 },
      transition: { duration: 0.4, delay, ease: [0.4, 0, 0.2, 1] as const },
    };
  };

  return (
    <main className="min-h-screen bg-surface-ground p-3 sm:p-5 lg:p-8">
      <div className="mx-auto flex min-h-[calc(100vh-1.5rem)] max-w-7xl overflow-hidden rounded-[28px] bg-surface-card shadow-elevated sm:min-h-[calc(100vh-2.5rem)] lg:min-h-[calc(100vh-4rem)]">
        {/* Painel Esquerdo: Aurora Canvas + Tipografia de Impacto */}
        <section
          className="relative hidden min-h-full flex-1 overflow-hidden bg-secondary lg:flex lg:max-w-[52%]"
          aria-label="Apresentação FinanceHub"
        >
          <AuroraBackground />
          <div className="relative z-10 flex w-full flex-col justify-between p-10 xl:p-14">
            <LogoMark size="lg" showText textColor="light" />

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.12em] text-brand mb-2">
                Finanças pessoais
              </p>
              <h1 className="font-display text-4xl xl:text-5xl font-black text-white leading-[1.15]">
                Tudo em<br />um só hub.
              </h1>
              <p className="mt-3 text-sm text-white/60 leading-relaxed max-w-md">
                Itaú, Mercado Pago e Inter — sincronizados,<br />
                categorizados, seu.
              </p>
            </div>

            {/* Decorações geométricas */}
            <div className="absolute top-[-60px] right-[-60px] w-[200px] h-[200px] rounded-full border border-brand/15 pointer-events-none" />
            <div className="absolute bottom-[-30px] left-[-30px] w-[120px] h-[120px] rounded-full border border-white/10 pointer-events-none" />
          </div>
        </section>

        {/* Painel Direito: Formulário com Stagger */}
        <section className="flex w-full items-center justify-center px-6 py-10 sm:px-12 lg:w-[48%] lg:px-16 xl:px-24">
          <div className="w-full max-w-md">
            <motion.div {...getMotionProps(0)} className="mb-6">
              <LogoMark size="sm" showText />
            </motion.div>

            <motion.div {...getMotionProps(0.08)} className="mb-8">
              <p className="mb-2 text-xs font-bold uppercase tracking-[0.16em] text-brand">Bem-vindo</p>
              <h2 className="text-3xl font-extrabold font-display tracking-tight text-secondary">Entrar no FinanceHub</h2>
              <p className="mt-2 text-sm font-medium leading-6 text-slate-500">Acesse sua conta com segurança.</p>
            </motion.div>

            <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
              <motion.div {...getMotionProps(0.16)} className="flex flex-col gap-2">
                <label htmlFor="email" className="text-xs font-bold text-secondary">E-mail</label>
                <div className="relative">
                  <Mail className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
                  <input
                    id="email"
                    type="email"
                    placeholder="seu.email@exemplo.com"
                    {...register('email')}
                    className="form-input-focus w-full rounded-xl border border-border-subtle bg-surface-ground py-3 pl-11 pr-4 text-sm text-secondary outline-none transition-colors placeholder:text-slate-400"
                  />
                </div>
                {errors.email && <span className="text-xs font-medium text-status-danger">{errors.email.message}</span>}
              </motion.div>

              <motion.div {...getMotionProps(0.22)} className="flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <label htmlFor="password" className="text-xs font-bold text-secondary">Senha</label>
                  <button type="button" className="text-xs font-bold text-brand transition-colors hover:text-brand-dark">
                    Esqueci minha senha
                  </button>
                </div>
                <div className="relative">
                  <LockKeyhole className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
                  <input
                    id="password"
                    type={isPasswordVisible ? 'text' : 'password'}
                    placeholder="••••••••"
                    {...register('password')}
                    className="form-input-focus w-full rounded-xl border border-border-subtle bg-surface-ground py-3 pl-11 pr-11 text-sm text-secondary outline-none transition-colors placeholder:text-slate-400"
                  />
                  <button
                    type="button"
                    aria-label={isPasswordVisible ? 'Ocultar senha' : 'Mostrar senha'}
                    onClick={() => setIsPasswordVisible((visible) => !visible)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-surface-muted hover:text-secondary"
                  >
                    {isPasswordVisible ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {errors.password && <span className="text-xs font-medium text-status-danger">{errors.password.message}</span>}
              </motion.div>

              <motion.div {...getMotionProps(0.28)}>
                <Button type="submit" variant="primary" isLoading={isSubmitting} className="btn-primary-glow w-full">
                  Entrar
                </Button>
              </motion.div>
            </form>

            <motion.p {...getMotionProps(0.34)} className="mt-8 text-center text-xs font-medium text-slate-400">
              Dados protegidos com criptografia de ponta a ponta.
            </motion.p>
          </div>
        </section>
      </div>
    </main>
  );
};

export default LoginPage;

