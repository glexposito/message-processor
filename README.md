# Message Processor

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

- Docker (or Podman with `podman-compose`)
- .NET 10+ SDK

---

## Run the full environment

The stack attaches to a shared network so that SigNoz can be plugged in later without
restarting anything. Create it once — this is not idempotent, so skip it (or ignore the
"already exists" error) if you've already run it before:

```bash
docker network create signoz-network 2>/dev/null || true
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

> The worker and spammer export telemetry to `signoz-ingester:4317`. If SigNoz is not
> running, the OTLP exporter fails quietly in the background and message processing is
> unaffected — observability is genuinely optional here.

### Picking up changes

Plain `docker compose up -d` is idempotent and safe to re-run any time — it diffs the
compose file against what's running and only recreates containers whose config actually
changed, leaving the rest alone.

- Changed a compose file value (env var, port, etc.) but not the code? `docker compose up -d`
  is enough — no rebuild needed.
- Changed source code or a Dockerfile (`MessageProcessor.Worker`,
  `tools/ServiceBus.Spammer`)? Rebuild the image first: `docker compose up --build -d`.
- **Avoid `--force-recreate`.** It skips the diff and recreates every container
  regardless of whether it changed, which — at least under `podman-compose` — can cascade
  into SIGKILLing unrelated healthy containers (`sqlserver`, `servicebus-emulator`, etc.)
  instead of just the service you meant to touch.

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

Traces, metrics, and logs are exported via OpenTelemetry. To view them locally, run
SigNoz alongside this stack.

> **Note:** SigNoz no longer ships `deploy/docker/docker-compose.yaml`, and `install.sh`
> is now a stub that prints a deprecation notice and exits successfully without
> installing anything. Installation goes through **Foundry**.

**1. Install Foundry**

```bash
curl -fsSL https://signoz.io/foundry.sh | bash
```

**2. Write a casting file**

Create `signoz/casting.yaml` (any directory works — Foundry generates its output
relative to this file):

```yaml
apiVersion: v1alpha1
kind: Installation
metadata:
  name: signoz
spec:
  deployment:
    flavor: compose
    mode: docker
```

**3. Generate the manifests**

```bash
cd signoz
foundryctl forge -f casting.yaml
```

This writes `pours/deployment/compose.yaml` without touching any containers, so it is
safe to re-run — but note that it **fully regenerates the file from scratch**, silently
wiping any manual edits (including the network patch below). Redo step 4 every time you
re-run `forge`.

**4. Patch the network to be external**

The generated compose declares `signoz-network` as its own. Since our stack created it
first, mark it external in `pours/deployment/compose.yaml` so either stack can start in
any order:

```yaml
networks:
  signoz-network:
    external: true
    name: signoz-network
```

While you are in there, consider pinning `signoz/signoz-otel-collector:latest` to a
concrete tag — `latest` makes this PoC non-reproducible across rebuilds.

> This patch only sticks until the next `foundryctl forge`. If you ever regenerate the
> manifests, re-apply it before running `docker compose up -d` again — otherwise compose
> will try to (re)create `signoz-network` as its own, which fails or conflicts if this
> stack already created it externally.

**5. Start SigNoz**

```bash
docker compose -f pours/deployment/compose.yaml up -d
```

Open `http://localhost:8080` — your worker will appear under **Services** once it starts
processing messages.

### Collector endpoint

The collector service is named `ingester` and exposes the network alias
**`signoz-ingester`**. That is the hostname to use from other containers on
`signoz-network`:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-ingester:4317
```

Ports `4317` (gRPC) and `4318` (HTTP) are also published to the host, so if you would
rather not share a network at all, point the exporter at the host instead and drop
`signoz-network` from `docker-compose.yaml` entirely:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://host.docker.internal:4317
```

On Podman, use `host.containers.internal` instead of `host.docker.internal`.

### How distributed tracing works here

Each message flows through two services. OpenTelemetry tracks the full journey as a
single **trace**:

```
traceId: abc123  ← shared by all spans
spanId: 111  parentId: (none)  "send message"     ← servicebus-spammer (root)
spanId: 222  parentId: 111     "process message"  ← message-processor  (child)
spanId: 333  parentId: 222     "GET"              ← message-processor  (child)
```

- **traceId** — groups all spans into one trace
- **spanId** — identifies each unit of work
- **parentId** — builds the parent/child tree

The spammer injects the `traceparent` header into the Service Bus message properties.
The worker extracts it and uses it as the parent of its own span — this is how SigNoz
can render the full flow across two separate services in one flamegraph.

---

## Run integration tests

These tests use **Testcontainers** to spin up the required infrastructure automatically.

From the repo root:

```bash
dotnet test MessageProcessor.Tests/MessageProcessor.Tests.csproj
```

Make sure Docker is running before executing the tests.

---

## Running on Podman

The stack works under `podman-compose`, with two caveats:

- Docker and Podman have separate network namespaces. If SigNoz runs under Docker and
  this stack under Podman, they cannot share `signoz-network` — use the
  `host.containers.internal` endpoint instead.
- `depends_on` with `condition: service_healthy` is honoured inconsistently across
  `podman-compose` versions. If the worker starts before the emulator is ready, that is
  the likely cause rather than a config error.
- Rootless Podman needs an explicit unqualified-search registry, or every image pull
  fails with `short-name "..." did not resolve to an alias and no unqualified-search
  registries are defined`. Fix once per machine:

  ```bash
  mkdir -p ~/.config/containers
  echo 'unqualified-search-registries = ["docker.io"]' >> ~/.config/containers/registries.conf
  ```
- If a forced restart (e.g. `--force-recreate` on a subset of services) SIGKILLs other
  containers along the way, `podman-compose` can occasionally fail the first `up` with
  `cannot open exec.fifo` / `unable to start container` for containers whose runtime
  state got left inconsistent. Re-running `docker compose up -d` resolves it — no data or
  config is lost, it is just a rootless-runtime race, not a real failure.

---

## TL;DR — Quick start

```bash
docker network create signoz-network 2>/dev/null || true
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
