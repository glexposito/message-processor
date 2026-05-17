# Message Processor

> **POC:** A fun, practical way to play with the **Azure Service Bus Emulator + Testcontainers for .NET** and see real messages flowing through a local, containerized, event-driven system.  
> Spin it up. Spam it with events. Watch the worker process them. Magic. 🪄

---

## What this project is about

This repository is a proof of concept for building and testing a **message-driven .NET worker** using:

- ✅ Azure Service Bus Emulator (Docker)
- ✅ Testcontainers for .NET
- ✅ Docker Compose
- ✅ .NET Worker Service
- ✅ OpenTelemetry (traces, metrics, logs) with SigNoz

It allows you to:

- Run everything locally
- Send messages into Service Bus
- See the worker process them in real time
- Verify the full pipeline with integration tests

---

## Projects

- **MessageProcessor.Worker**  
  The background worker that listens to Azure Service Bus and processes messages.

- **MessageProcessor.Tests**  
  Integration tests using Testcontainers.

- **ServiceBus.Spammer**  
  A small console tool that sends one message per second.  
  Location: `tools`

---

## Prerequisites

- Docker
- .NET 10+ SDK

---

## Run the full environment

From the folder that contains `docker-compose.yaml`:

```bash
docker network create signoz-network
```

Then start the stack:

```bash
docker compose up -d
```

This starts:
- Azure SQL Edge
- Azure Service Bus Emulator
- Service Bus Sentinel (health checker)
- Node `greetings-api`
- `MessageProcessor.Worker`
- `ServiceBus.Spammer`

Verify everything is running:

```bash
docker ps
```

You should see something like:

```
message-processor-worker   Up ...
servicebus-emulator        Up ...
sqlserver                  Up ...
```

---

## Watch the worker process messages (LIVE)

This is the most important command in the whole project:

```bash
docker logs -f message-processor-worker
```

You should see:

```
info: MessageProcessor.Worker.ServiceBusMessageProcessor[0]
      Calculating the meaning of life for Message ID fc23a7a9-bbca-4af0-bc4b-86d2517b0c13... please wait.
info: MessageProcessor.Worker.ServiceBusMessageProcessor[0]
      Received: Message 1326 @ 2025-11-23T03:17:05.7212890Z
```

Leave this terminal open — it is your **live processing feed**.

---

## Observability with SigNoz (optional)

Traces, metrics, and logs are exported via OpenTelemetry. To view them locally, run SigNoz alongside this stack.

**1. Clone and start SigNoz**

```bash
git clone -b main https://github.com/SigNoz/signoz.git
docker compose -f signoz/deploy/docker/docker-compose.yaml up -d
```

**2. Connect SigNoz's collector to the shared network**

```bash
docker network connect signoz-network signoz-otel-collector
```

**3. Start this stack**

```bash
docker compose up -d
```

Open `http://localhost:8080` — your worker will appear under **Services** once it starts processing messages.

### How distributed tracing works here

Each message flows through two services. OpenTelemetry tracks the full journey as a single **trace**:

```
traceId: abc123  ← shared by all spans

spanId: 111  parentId: (none)  "send message"     ← servicebus-spammer (root)
spanId: 222  parentId: 111     "process message"  ← message-processor  (child)
spanId: 333  parentId: 222     "GET"              ← message-processor  (child)
```

- **traceId** — groups all spans into one trace
- **spanId** — identifies each unit of work
- **parentId** — builds the parent/child tree

The spammer injects the `traceparent` header into the Service Bus message properties. The worker extracts it and uses it as the parent of its own span — this is how SigNoz can render the full flow across two separate services in one flamegraph.

---

## Run integration tests

These tests use **Testcontainers** to spin up the required infrastructure automatically.

From the repo root:

```bash
dotnet test MessageProcessor.Tests/MessageProcessor.Tests.csproj
```

Make sure Docker is running before executing the tests.

---

## TL;DR — Quick start

```bash
docker compose up -d
docker logs -f message-processor-worker
```

You now have a local, containerized playground for:

- Azure Service Bus Emulator
- Azure Service Bus Emulator Spammer
- .NET worker
- Event-driven systems
- Integration testing

Have fun breaking it, fixing it, and making it better 😄
