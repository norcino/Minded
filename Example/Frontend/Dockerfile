# Multi-stage build for the Minded Example frontend.
# Build context is this directory:
#
#   docker build -t minded-example-frontend Example/Frontend
#
# The runtime stage serves the static Vite build with nginx, proxying /api and /hubs
# to the `api` compose service (the production mirror of the Vite dev-server proxy).

FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 80
