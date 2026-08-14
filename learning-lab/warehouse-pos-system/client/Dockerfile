# Build context is client/ itself (unlike the .NET services, the Angular
# app has no sibling project references to pull in, so there's no need to
# widen the context to the repo root).
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

# environment.ts's apiBaseUrl/notificationsHubUrl (http://localhost:5058 /
# :5298) are baked in at THIS build step, not read at container runtime —
# see that file's own comment. That's fine specifically because the
# consumer of this container is a BROWSER running on the developer's own
# host, not another container: docker-compose publishes the gateway and
# Notifications.API on those exact same host ports, so "localhost:5058"
# typed into a browser on the host machine reaches them correctly whether
# they were started via docker-compose or five separate `dotnet run`
# terminals. A container-to-container deployment (no published host
# ports) would need the runtime-config.json approach that comment
# describes instead.
FROM nginx:alpine AS final
COPY --from=build /app/dist/client/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
