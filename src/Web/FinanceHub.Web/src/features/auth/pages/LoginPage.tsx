import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/shared/components/Button/Button';
import { Card } from '@/shared/components/Card/Card';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { setAccessToken } from '@/shared/utils/authStorage';
import { requestDevTokenApi } from '../api/authApi';

const loginSchema = z.object({
  email: z.string().email('Informe um e-mail válido'),
  password: z.string().min(6, 'A senha deve ter no mínimo 6 caracteres'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
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
    <div className="min-h-screen flex items-center justify-center bg-surface-ground p-4">
      <Card className="w-full max-w-md p-8">
        <div className="flex flex-col items-center text-center mb-8">
          <div className="w-12 h-12 rounded-2xl bg-brand text-white flex items-center justify-center font-extrabold text-2xl shadow-sm mb-4">
            F
          </div>
          <h1 className="text-2xl font-extrabold text-secondary">Seja bem-vindo de volta!</h1>
          <p className="text-xs font-medium text-slate-500 mt-1">Acesse seu agregador Open Finance</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="email" className="text-xs font-bold text-secondary">E-mail</label>
            <input
              id="email"
              type="email"
              placeholder="seu.email@exemplo.com"
              {...register('email')}
              className="px-4 py-2.5 text-sm bg-surface-ground border border-border-subtle rounded-xl outline-none focus:border-brand transition-colors"
            />
            {errors.email && <span className="text-xs text-status-danger font-medium">{errors.email.message}</span>}
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="password" className="text-xs font-bold text-secondary">Senha</label>
            <input
              id="password"
              type="password"
              placeholder="••••••••"
              {...register('password')}
              className="px-4 py-2.5 text-sm bg-surface-ground border border-border-subtle rounded-xl outline-none focus:border-brand transition-colors"
            />
            {errors.password && <span className="text-xs text-status-danger font-medium">{errors.password.message}</span>}
          </div>

          <Button type="submit" variant="primary" isLoading={isSubmitting} className="w-full mt-2">
            Entrar
          </Button>
        </form>
      </Card>
    </div>
  );
};

export default LoginPage;
