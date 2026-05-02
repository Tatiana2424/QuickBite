FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/quickbite-web/package.json frontend/quickbite-web/package-lock.json ./
RUN npm ci
COPY frontend/quickbite-web ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY frontend/quickbite-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
