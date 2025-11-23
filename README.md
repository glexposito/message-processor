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
  Location: `tools/servicebus-spammer`

---

## Prerequisites

- Docker
- .NET 10+ SDK

---

## Run the full environment

From the folder that contains \`docker-compose.yml\`:

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
...
Calculating the meaning of life for Message ID fc23a7a9-bbca-4af0-bc4b-86d2517b0c13... please wait.
Received: Message 1326 @ 2025-11-23T03:17:05.7212890Z
...
```

Leave this terminal open — it is your **live processing feed**.

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
