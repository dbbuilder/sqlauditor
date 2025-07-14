# Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files
COPY src/SqlAnalyzer.Web/package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY src/SqlAnalyzer.Web/ .

# Build for production
RUN npm run build

# Runtime stage
FROM nginx:alpine AS final

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY src/SqlAnalyzer.Web/nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/ || exit 1