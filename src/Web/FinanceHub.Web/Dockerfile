FROM node:24-alpine AS build
WORKDIR /app
COPY . .
RUN npm ci --ignore-scripts && npm run build

FROM nginx:stable-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Configura permissões e executa como usuário não-root (nginx)
RUN touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid /var/cache/nginx /usr/share/nginx/html /etc/nginx/conf.d

USER nginx

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
