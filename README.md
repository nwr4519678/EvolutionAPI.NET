# Evolution API .NET

A .NET 10 WhatsApp Web gateway built with Clean Architecture, CQRS/MediatR, Entity Framework Core, SignalR, WebSockets, and Baileys.NET.

It provides an Evolution API-style REST interface for managing WhatsApp Web sessions, sending messages, receiving events, delivering webhooks, and integrating with OpenAI, Typebot, Chatwoot, RabbitMQ, Amazon SQS, and NATS.

> This project uses a WhatsApp Web client connection. Use dedicated test numbers, follow applicable WhatsApp policies, and validate account and operational risk before production use.

## Solution structure

```text
src/
  EvolutionApi.Domain/          Entities, enums, exceptions, and interfaces
  EvolutionApi.Application/     CQRS commands/queries, validation, DTOs, mappings
  EvolutionApi.Infrastructure/  EF Core, SQL queues, Baileys engine, webhooks
  EvolutionApi.API/             REST API, middleware, SignalR, WebSockets, OpenAPI
tests/
  EvolutionApi.Tests/           xUnit unit tests
```

`EvolutionApi.DotNet.slnx` is the modern solution file. `EvolutionApi.DotNet.sln` is included for tooling that does not support `.slnx`.

## Features

- Instance creation, QR connection, reconnect, logout, delete, and state tracking
- Text, media, audio, sticker, location, contact, reaction, poll, list, and status messages
- Chats, contacts, groups, labels, profile operations, and calls
- SQL Server persistence for instances, messages, Signal keys, session credentials, webhooks, and scheduled work
- Encrypted session and Signal-key storage through ASP.NET Core Data Protection
- Durable outbound and webhook queues with leases, retries, backoff, and failure status
- HMAC/JWT-style webhook authorization, SignalR events, and WebSocket event streaming
- API-key authorization, tenant resolution, rate limiting, SSRF protection, and media size limits
- Hangfire scheduling and OpenAI, Typebot, Chatwoot, RabbitMQ, SQS, and NATS integrations

## Requirements

- .NET 10 SDK
- SQL Server, Azure SQL, or SQL Server in Docker
- A public HTTPS URL for production webhooks and callbacks
- A dedicated WhatsApp test number for staging validation

## Local development

Configure SQL Server in `src/EvolutionApi.API/appsettings.Development.json` or with environment variables, then apply migrations:

```powershell
dotnet ef database update `
  --project src/EvolutionApi.Infrastructure `
  --startup-project src/EvolutionApi.API
```

Start the API:

```powershell
dotnet run --project src/EvolutionApi.API
```

Development endpoints:

- Scalar: `http://localhost:5051/scalar/v1`
- OpenAPI: `http://localhost:5051/openapi/v1.json`
- Health: `http://localhost:5051/health`
- SignalR: `http://localhost:5051/hubs/events`
- WebSocket: `ws://localhost:5051/ws/{instanceName}`

## Configuration

Use environment variables or a secret store for credentials. Never commit production secrets.

```text
ConnectionStrings__DefaultConnection=Server=...;Database=EvolutionApi;...
Evolution__ApiKey=<global-api-key>
Evolution__HangfireDashboardApiKey=<different-dashboard-key>
Evolution__ServerUrl=https://api.example.com
DataProtection__KeyPath=/var/lib/evolution-api/data-protection-keys
Media__MaxDownloadBytes=33554432
OutboundHttp__AllowedHosts=hooks.example.com,cdn.example.com
ApiDocumentation__Enabled=false
```

Production startup fails fast unless the SQL connection, gateway key, Hangfire dashboard key, and persistent Data Protection key path are configured. Preserve the Data Protection key directory across restarts or session credentials cannot be decrypted.

## API quick start

Create an instance:

```powershell
curl -X POST http://localhost:5051/instance/create `
  -H "apikey: <global-api-key>" `
  -H "Content-Type: application/json" `
  -d '{"instanceName":"my-clinic","token":"clinic-secret-token"}'
```

Request a QR code:

```powershell
curl http://localhost:5051/instance/connect/my-clinic `
  -H "apikey: clinic-secret-token"
```

Send a text message:

```powershell
curl -X POST http://localhost:5051/message/sendText/my-clinic `
  -H "apikey: clinic-secret-token" `
  -H "Content-Type: application/json" `
  -d '{"number":"1234567890","text":{"message":"Hello from Evolution API .NET"}}'
```

All protected HTTP endpoints require `apikey`. Instance routes accept the instance key; global management operations require the global key. SignalR requires the global key. WebSocket clients must provide the key in the `apikey` header; do not put credentials in URLs.

## Testing

The test project is a separate solution layer and does not change production project dependencies.

```powershell
dotnet restore EvolutionApi.DotNet.slnx
dotnet build EvolutionApi.DotNet.slnx -c Release
dotnet test EvolutionApi.DotNet.slnx -c Release
```

The current unit suite covers result/pagination behavior, production configuration validation, API-key authorization, URL/SSRF rules, and Data Protection round trips. Full staging verification must additionally exercise QR pairing, restart recovery, reconnects, inbound/outbound messages, media, webhooks, tenant isolation, and database restore.

## Production deployment

Copy `.env.production.template` to `.env.production`, fill it from a secret manager, and deploy behind a TLS reverse proxy:

```powershell
dotnet ef database update `
  --project src/EvolutionApi.Infrastructure `
  --startup-project src/EvolutionApi.API `
  --configuration Release

docker compose -f docker-compose.production.template.yml up -d --build
```

The container runs as a non-root user, binds the application to localhost, uses a read-only filesystem, and persists Data Protection keys in a named volume. Expose only the reverse proxy publicly. Apply host-level egress controls in addition to the application URL validator.

## Operational checklist

- Use managed SQL Server with backups and tested restore procedures.
- Keep API keys, database credentials, and Data Protection keys in a secret manager.
- Preserve the Data Protection volume on every redeploy.
- Protect `/hangfire` with the dedicated dashboard key and restrict network access.
- Keep Scalar/OpenAPI disabled in production unless explicitly required.
- Configure HTTPS, trusted forwarded headers, DNS, webhooks, monitoring, and alerts.
- Run the staging WhatsApp smoke test before promoting a release.

## License

Apache License 2.0. Review upstream and dependency licenses before redistribution.
