FROM node:24-bookworm-slim AS build
WORKDIR /src
RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN corepack prepare pnpm@11.22.0 --activate && pnpm install --frozen-lockfile
COPY . .
RUN pnpm build          # tsc && vite build → build/ (apiUrl is relative, no build ARG needed)

FROM nginx:alpine AS runtime
COPY --from=build /src/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
