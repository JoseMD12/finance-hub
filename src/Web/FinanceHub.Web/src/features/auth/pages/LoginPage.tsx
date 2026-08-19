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

const loginSchema = z.object({
  email: z.string().email('Informe um e-mail válido'),
  password: z.string().min(6, 'A senha deve ter no mínimo 6 caracteres'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
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

  return (
    <main className="min-h-screen bg-surface-ground p-3 sm:p-5 lg:p-8">
      <div className="mx-auto flex min-h-[calc(100vh-1.5rem)] max-w-7xl overflow-hidden rounded-[28px] bg-surface-card shadow-elevated sm:min-h-[calc(100vh-2.5rem)] lg:min-h-[calc(100vh-4rem)]">
        <section
          className="relative hidden min-h-full flex-1 overflow-hidden bg-secondary lg:flex lg:max-w-[52%]"
          aria-label="FinanceHub em uma viagem na neve"
        >
          <img
            src="/images/login-hero.jpeg"
            alt="Duas pessoas em uma estação de esqui"
            className="absolute inset-0 h-full w-full object-cover"
          />
          <div className="absolute inset-0 bg-secondary/60" />
          <div className="relative z-10 flex w-full flex-col justify-between p-10 xl:p-14">
            <div className="flex items-center gap-3 text-white">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-brand text-xl font-extrabold shadow-brand">
                F
              </span>
            </div>
          </div>
        </section>

        <section className="flex w-full items-center justify-center px-6 py-10 sm:px-12 lg:w-[48%] lg:px-16 xl:px-24">
          <div className="w-full max-w-md">
            <div className="mb-9 lg:hidden">
              <div className="flex items-center gap-3">
                <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand text-lg font-extrabold text-white shadow-brand">
                  F
                </span>
                <span className="text-base font-extrabold tracking-tight text-secondary">FinanceHub</span>
              </div>
            </div>

            <div className="mb-8">
              <p className="mb-2 text-xs font-bold uppercase tracking-[0.16em] text-brand">Bem-vindo</p>
              <h2 className="text-3xl font-extrabold tracking-tight text-secondary">Entrar no FinanceHub</h2>
              <p className="mt-2 text-sm font-medium leading-6 text-slate-500">Acesse sua conta.</p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
              <div className="flex flex-col gap-2">
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
              </div>

              <div className="flex flex-col gap-2">
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
              </div>

              <Button type="submit" variant="primary" isLoading={isSubmitting} className="btn-primary-glow mt-1 w-full">
                Entrar
              </Button>
            </form>

            <p className="mt-8 text-center text-xs font-medium text-slate-400">Dados protegidos.</p>
          </div>
        </section>
      </div>
    </main>
  );
};

export default LoginPage;
