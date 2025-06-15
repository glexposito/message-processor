FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MessageProcessor.Worker/MessageProcessor.Worker.csproj", "MessageProcessor.Worker/"]
RUN dotnet restore "MessageProcessor.Worker/MessageProcessor.Worker.csproj"
COPY . .
WORKDIR "/src/MessageProcessor.Worker"
RUN dotnet build "./MessageProcessor.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MessageProcessor.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageProcessor.Worker.dll"]
