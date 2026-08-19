# 📋 Plano Arquitetural: Módulo de Autenticação e Gestão de Usuários (Auth & Identity)

**Documento:** `.agents/plans/auth-identity-service-plan.md`  
**Status:** 🟡 Planejado  
**Data:** 18/08/2026  

---

## 1. 🎯 Objetivo

Implementar a camada definitiva de **Autenticação, Cadastro e Gestão de Usuários** no FinanceHub, substituindo o token estático de desenvolvimento por um fluxo seguro com persistência em banco de dados, hash de senha criptográfico (Argon2id / BCrypt), emissão de JWTs assinados com RSA (FAPI Profile) e rotação de Refresh Tokens.

---

## 2. 🏛️ Decisão Arquitetural

- **Localização do Serviço**:
  - Opção recomendada: Integrar a gestão de usuários e credenciais diretamente no **`FinanceHub.ApiGateway`** (BFF) com base de dados isolada (`financehub_identity`) ou microsserviço dedicado `FinanceHub.Identity`.
- **Criptografia & Segurança**:
  - Senhas hasheadas com **Argon2id** (ou BCrypt com fator de custo 12).
  - Assinatura JWT com **RSA-SHA256 (RS256)** de 2048 bits.
  - Refresh Tokens armazenados com hash SHA-256 e expiração de 7 dias com revogação na rotação.
  - Proteção contra Brute Force via Rate Limiter.

---

## 3. 💾 Modelo de Domínio (Database Schema)

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(64) NOT NULL UNIQUE,
    expires_at_utc TIMESTAMP WITH TIME ZONE NOT NULL,
    revoked_at_utc TIMESTAMP WITH TIME ZONE,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_refresh_tokens_user ON refresh_tokens(user_id);
```

---

## 4. 🌐 Endpoints de API (BFF Gateway)

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `POST` | `/api/v1/auth/register` | Cria novo usuário (e-mail, senha, nome). |
| `POST` | `/api/v1/auth/login` | Valida credenciais e retorna `{ accessToken, refreshToken, user }`. |
| `POST` | `/api/v1/auth/refresh` | Rotaciona Refresh Token e emite novo Access Token. |
| `POST` | `/api/v1/auth/logout` | Revoga o Refresh Token ativo. |
| `GET` | `/api/v1/auth/me` | Retorna dados do usuário autenticado a partir do token. |

---

## 5. 🚀 Etapas de Implementação

1. **Camada de Domínio & Segurança**:
   - Criar Aggregate `User` com invariantes de e-mail e hash de senha.
   - Criar `IPasswordHasher` com implementação segura (Argon2id/BCrypt).
2. **Camada de Persistência (EF Core)**:
   - Configuração de `IdentityDbContext` e migration inicial.
3. **Casos de Uso (Commands/Queries CQRS)**:
   - `RegisterUserCommand` & `RegisterUserCommandHandler`.
   - `LoginUserCommand` & `LoginUserCommandHandler`.
   - `RefreshTokenCommand` & `RefreshTokenCommandHandler`.
4. **Endpoints & Middlewares**:
   - `AuthEndpoints.cs` mapeando `/api/v1/auth/*`.
5. **Frontend (React)**:
   - Criação da tela/aba de Cadastro (`RegisterPage.tsx`).
   - Integração com React Hook Form + Zod (`registerSchema`).
   - Suporte completo ao fluxo de Refresh automático no `httpClient.ts`.
