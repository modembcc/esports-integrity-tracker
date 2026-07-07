# ── Stage 1: node build ──
FROM node:22.22.1-alpine AS build
WORKDIR /src

COPY package*.json ./
# --ignore-scripts per your npm security setup; esbuild binary comes via
# optional deps in the lockfile so the build works without install scripts
RUN npm ci --ignore-scripts

COPY . ./

# CRITICAL: Vite env vars are baked in AT BUILD TIME. This arg must be
# passed by compose — there is no runtime VITE_API_URL in the container.
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ── Stage 2: nginx serves the static bundle ──
FROM nginx:alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80