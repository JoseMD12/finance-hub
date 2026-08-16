FROM node:24-alpine AS build

WORKDIR /app

# Copia arquivos de dependência
COPY package*.json ./

# Instala dependências
RUN npm ci

# Copia o código fonte e arquivos de configuração
COPY . .

# Compila o projeto frontend para produção
RUN npm run build

# Etapa final: nginx para servir os arquivos estáticos compilados
FROM nginx:stable-alpine

COPY --from=build /app/dist /usr/share/nginx/html

# Copia arquivo de configuração do nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
