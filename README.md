# Evolution API — .NET 10 (Clean Architecture)

A production-grade, high-performance .NET 10 implementation of the Evolution API WhatsApp gateway built following **Clean Architecture** principles with **CQRS (MediatR)**, **EF Core**, **SignalR**, and **Scalar**.

Connects natively to WhatsApp Web via [Baileys.NET](https://www.nuget.org/packages/Baileys.NET) and implements the complete WhatsApp Signal protocol stack with End-to-End Encryption (E2EE).

> **Build Status**: ✅ `net10.0` — Clean Architecture (4 Layers) — 0 Errors, 0 Warnings

---

## 🏛️ Clean Architecture Overview

The solution is divided into 4 loosely-coupled layers:

```
                            ┌─────────────────────────────────┐
                            │        EvolutionApi.API         │  (Web API, Controllers, SignalR Hub,
                            │                                 │   WebSockets, Scalar Docs, Middleware)
                            └───────────────┬─────────────────┘
                                            │
                                            ▼
                            ┌─────────────────────────────────┐
                            │     EvolutionApi.Application    │  (CQRS Commands, Queries, Handlers,
                            │                                 │   MediatR, FluentValidation, DTOs)
                            └───────────────┬─────────────────┘
                                            │
                                            ▼
                            ┌─────────────────────────────────┐
                            │       EvolutionApi.Domain       │  (Entities, Value Objects, Exceptions,
                            │                                 │   Repository & Service Contracts)
                            └───────────────▲─────────────────┘
                                            │
                                            │ implements contracts
                            ┌───────────────┴─────────────────┐
                            │   EvolutionApi.Infrastructure   │  (Baileys.NET WhatsApp Engine, E2EE Signal,
                            │                                 │   EF Core AppDbContext, Outbox, Event Transports)
                            └─────────────────────────────────┘
```

### 1. `EvolutionApi.Domain`
- **Pure Domain Models**: Entities with zero framework dependencies (`Instance`, `Message`, `Chat`, `Contact`, `WebhookConfig`, `WebhookDelivery`, `SessionAuth`, `SignalKey`, `TypebotSetting`, `ChatwootSetting`, `OpenAiSetting`, etc.).
- **Contracts**: Interfaces for repositories (`IInstanceRepository`, `IChatRepository`, `ISettingsRepository`, `IIntegrationRepository`, `IUnitOfWork`) and core services (`IWhatsAppEngine`, `IWebhookDispatcher`, `ICacheService`, `IEventNotificationService`).
- **Domain Exceptions**: Strongly-typed exceptions such as `WhatsAppEngineUnavailableException`.

### 2. `EvolutionApi.Application`
- **CQRS Pattern via MediatR**: Segregated Command and Query pipelines across all business features (`Instances`, `Messages`, `Chat`, `Groups`, `Labels`, `Calls`, `Proxy`, `Settings`, `Webhooks`, `Integrations`).
- **FluentValidation**: Automatic request validation behaviors.
- **DTOs & Mappings**: Strictly typed request and response contracts.

### 3. `EvolutionApi.Infrastructure`
- **WhatsApp Engine (`EvolutionApi.Infrastructure.WhatsApp.Engine`)**:
  - `BaileysNetWhatsAppEngine`: Production multi-session engine managing concurrent WhatsApp clients.
  - `WaSocket`: Raw binary node and IQ stanza exchange.
  - `WaSignalCrypto`: Cryptographic primitives for Signal Protocol (X3DH, HKDF, Curve25519, AES-256-CBC, HMAC-SHA256).
  - `WaMessageEncryptor` & `WaInboundDecryptor`: Full outbound and inbound Signal E2E encryption/decryption (`pkmsg`, `msg`, `skmsg`).
  - `WaMediaUploader`: Secure CDN upload/download for media attachments.
  - `WaProto`: Protobuf encoding and decoding for all WhatsApp message types.
- **Persistence & Storage**:
  - `AppDbContext`: Entity Framework Core with SQL Server, explicit Fluent API mapping, and retry execution strategies.
  - `SqlSignalKeyStore`: Secure encrypted storage for all Signal session keys, pre-keys, and identity keys.
  - `SessionKeyStore`: AES-256-GCM encrypted persistence of authentication credentials.
  - `MessageStoreService`: High-throughput local storage for chat, contact, and message history.
- **Durable Outbox & Event Transports**:
  - `OutboundMessageQueue` & `OutboundMessageWorker`: SQL-backed transactional outbox with retry backoff.
  - `WebhookDispatcher` & `WebhookDeliveryWorker`: Reliable webhook delivery pipeline with exponential backoff and HMAC-SHA256 JWT signing.
  - `WebSocketTransport`: Real-time WebSocket fan-out to connected subscribers.
  - `SignalRWebhookDispatcherBridge`: Real-time event bridge pushing WhatsApp events directly to SignalR clients.
  - Third-party message brokers via official NuGet SDKs: MassTransit RabbitMQ, AWS SQS, and NATS.Net.
  - OpenAI Processor using the official `OpenAI` SDK for automated response workflows.

### 4. `EvolutionApi.API`
- **REST Endpoints**: Thin controllers invoking MediatR handlers with standardized ProblemDetails error responses.
- **Real-Time Streaming**:
  - **SignalR Hub**: Accessible at `/hubs/events` for real-time typed events (QR updates, connection state, messages).
  - **WebSockets**: Native WebSocket streaming endpoint at `/ws/{instanceName}`.
- **Security & Resilience**:
  - `ApiKeyFilter`: API key authentication and authorization per instance and globally.
  - `RateLimiter`: ASP.NET Core sliding-window and fixed-window rate limiters.
- **Scalar API Documentation**: Beautiful, modern OpenAPI interactive documentation at `/scalar/v1`.

---

## ⚡ Features & Parity

| Feature Domain | Capabilities | Status |
|---|---|---|
| **Instance Lifecycle** | Create, Connect, Restart, Logout, Delete, State tracking | ✅ Supported |
| **Messaging (13 types)** | Text, Media (Image, Audio, Video, Document), Location, Contact, Reaction, Poll, List, Status, ephemerals | ✅ E2EE Signal Encrypted |
| **Chats & Contacts** | Fetch chats, messages, contacts, mark read, archive, delete messages | ✅ Supported |
| **Group Operations** | Create, update participants, promote/demote, subject, description, invite codes, group pictures | ✅ Supported |
| **Labels & Business** | Create, update, delete labels, assign/remove chat labels, business profile retrieval | ✅ Supported |
| **Calls** | Audio and video call offer IQs (JINGLE protocol) | ✅ Supported |
| **Profile Management** | Profile pictures (upload/remove), privacy settings, status, name | ✅ Supported |
| **Webhooks** | Durable SQL queue, retry policies, HMAC-SHA256 signing, global & local webhooks | ✅ Supported |
| **Real-time Push** | SignalR hub (`/hubs/events`) and WebSocket streaming (`/ws/{instance}`) | ✅ Supported |
| **Integrations** | Typebot, Chatwoot, OpenAI, RabbitMQ, AWS SQS, NATS | ✅ Supported |

---

## 🚀 Getting Started

## Production deployment

The service deliberately refuses to start in `Production` unless the SQL connection,
gateway key, Hangfire dashboard key, and a persistent Data Protection key path are
configured. Keep every secret outside source control.

1. Copy `.env.production.template` to `.env.production` and replace every placeholder
   with unique secrets from your secret store.
2. Apply EF migrations against the production database from your CI/CD release step:
   `dotnet ef database update --project src/EvolutionApi.Infrastructure --startup-project src/EvolutionApi.API --configuration Release`.
3. Build and start behind a TLS reverse proxy: `docker compose -f docker-compose.production.template.yml up -d --build`.
4. Expose only the reverse proxy publicly. The template binds the application to
   `127.0.0.1:8080`; preserve the named Data Protection volume when redeploying.

Use a dedicated, restricted SQL account and enforce outbound network egress at the
host/firewall level. `OutboundHttp__AllowedHosts` is an optional comma-separated
allow-list for webhook and media domains; private, loopback, and link-local addresses
are always rejected by the application.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB, Express, Standard, or Azure SQL)

### Configuration

Update `src/EvolutionApi.API/appsettings.json` (or set environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EvolutionApi;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Evolution": {
    "ApiKey": "your-global-api-key",
    "SessionEncryptionKey": "<base64-encoded-32-byte-aes-key>",
    "ServerUrl": "http://localhost:5000"
  }
}
```

Generate a secure 32-byte session encryption key:
```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Or Node.js
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

### Apply EF Core Migrations

```bash
dotnet ef database update --project src/EvolutionApi.Infrastructure --startup-project src/EvolutionApi.API
```

### Run the API

```bash
dotnet run --project src/EvolutionApi.API
```

Once running:
- **Scalar API Documentation**: Navigate to `http://localhost:5000/scalar/v1`
- **OpenAPI Schema**: `http://localhost:5000/openapi/v1.json`
- **SignalR Hub**: `http://localhost:5000/hubs/events`
- **WebSocket Stream**: `ws://localhost:5000/ws/{instanceName}`

---

## 📖 API Usage Examples

### 1. Create an Instance
```bash
curl -X POST http://localhost:5000/instance/create \
  -H "apikey: your-global-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "instanceName": "my-clinic",
    "token": "clinic-secret-token"
  }'
```

### 2. Connect & Scan QR Code
```bash
curl http://localhost:5000/instance/connect/my-clinic \
  -H "apikey: clinic-secret-token"
```
*(Returns base64 QR code to scan with WhatsApp on your phone)*

### 3. Send a Text Message
```bash
curl -X POST http://localhost:5000/message/sendText/my-clinic \
  -H "apikey: clinic-secret-token" \
  -H "Content-Type: application/json" \
  -d '{
    "number": "1234567890",
    "text": { "message": "Hello from Evolution API .NET!" }
  }'
```

### 4. Connect via WebSocket
```javascript
const ws = new WebSocket('wss://your-host/ws/my-clinic');

// Browser WebSockets cannot send custom headers. Use a server-side WebSocket
// client/proxy that sends the `apikey` header; never put API keys in a URL.

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log('WhatsApp event received:', data);
  // Example: { event: "MESSAGES_UPSERT", instance: "my-clinic", data: { ... } }
};
```

### 5. Connect via SignalR
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://your-host/hubs/events", {
    headers: { apikey: "your-global-api-key" }
  })
  .withAutomaticReconnect()
  .build();

// Subscribe to real-time events for an instance
connection.on("InstanceEvent", (instanceName, eventName, payload) => {
  console.log(`[${instanceName}] ${eventName}:`, payload);
});

await connection.start();
await connection.invoke("SubscribeToInstance", "my-clinic");
```

---

## 🔒 Signal E2EE Protocol Details

The internal engine handles end-to-end encryption compliant with WhatsApp 3.3 specifications:
- **Outbound Encryption**:
  1. Computes recipient pre-key bundle using WhatsApp USync IQ query.
  2. Executes X3DH shared-secret agreement and Double Ratchet key derivation.
  3. Constructs Signal Protocol `PreKeyWhisperMessage` and `WhisperMessage` nodes.
  4. Encrypts group broadcasts via Sender Keys (`SenderKeyMessage`).
- **Inbound Decryption**:
  1. Decrypts `pkmsg` using identity keys, signed pre-keys, and one-time pre-keys.
  2. Advances Double Ratchet state to decrypt incoming message payload (`msg`).
  3. Tracks and prunes consumed one-time keys to prevent replay attacks.
- **Encrypted Storage**:
  1. All Signal keys, auth credentials, and tokens are protected via `ISecretProtector` using AES-256-GCM.

---

## 🛠️ Tech Stack & Packages

- **Language / Runtime**: C# 14 / .NET 10
- **Architecture**: Clean Architecture + CQRS
- **Mediator**: [MediatR](https://www.nuget.org/packages/MediatR)
- **Validation**: [FluentValidation](https://www.nuget.org/packages/FluentValidation.AspNetCore)
- **Database / ORM**: EF Core 10, SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- **API Documentation**: [Scalar.AspNetCore](https://www.nuget.org/packages/Scalar.AspNetCore)
- **Real-Time**: Microsoft ASP.NET Core SignalR & native WebSockets
- **WhatsApp Engine**: [Baileys.NET](https://www.nuget.org/packages/Baileys.NET)
- **Messaging / Integration SDKs**:
  - [MassTransit.RabbitMQ](https://www.nuget.org/packages/MassTransit.RabbitMQ)
  - [MassTransit.AmazonSQS](https://www.nuget.org/packages/MassTransit.AmazonSQS)
  - [NATS.Net](https://www.nuget.org/packages/NATS.Net)
  - [OpenAI](https://www.nuget.org/packages/OpenAI)
  - [QRCoder](https://www.nuget.org/packages/QRCoder)

---

## 📄 License

Apache License 2.0 — compatible with the original [Evolution API](https://github.com/EvolutionAPI/evolution-api).
