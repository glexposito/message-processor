# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Context is MessageProcessor.Worker/, so this copies the project
COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app ./

ENTRYPOINT ["dotnet", "MessageProcessor.Worker.dll"]
