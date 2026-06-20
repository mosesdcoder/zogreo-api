# ── Stage 1: build ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first (layer cache for restore)
COPY Zogreo.sln ./
COPY src/Zogreo.Api/Zogreo.Api.csproj             src/Zogreo.Api/
COPY src/Zogreo.Application/Zogreo.Application.csproj  src/Zogreo.Application/
COPY src/Zogreo.Domain/Zogreo.Domain.csproj        src/Zogreo.Domain/
COPY src/Zogreo.Infrastructure/Zogreo.Infrastructure.csproj src/Zogreo.Infrastructure/

RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish src/Zogreo.Api/Zogreo.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runner
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish ./
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "Zogreo.Api.dll"]
