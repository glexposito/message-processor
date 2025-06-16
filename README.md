# MessageProcessor

A .NET solution for processing messages, including a worker service and integration tests.

> **Note:** This project is a proof of concept (POC) for using [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet) with Azure Service Bus.  

## Projects

- **MessageProcessor.Worker**  
  The main worker service that processes messages.  
  Location: [`MessageProcessor.Worker/`](MessageProcessor.Worker/)

- **MessageProcessor.Tests**  
  Integration tests for the worker service.  
  Location: [`MessageProcessor.Tests/`](MessageProcessor.Tests/)

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/)
- Docker (required for running integration tests with Testcontainers)

### Running Tests

> **Docker must be running to execute integration tests.**

```sh
dotnet test MessageProcessor.Tests/MessageProcessor.Tests.csproj
```