FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Omniscient.Indexer/Omniscient.Indexer.csproj", "Omniscient.Indexer/"]
COPY ["Omniscient.ServiceDefaults/Omniscient.ServiceDefaults.csproj", "Omniscient.ServiceDefaults/"]
COPY ["Omniscient.Shared/Omniscient.Shared.csproj", "Omniscient.Shared/"]
COPY ["Omniscient.RabbitMQClient/Omniscient.RabbitMQClient.csproj", "Omniscient.RabbitMQClient/"]
RUN dotnet restore "Omniscient.Indexer/Omniscient.Indexer.csproj"
COPY . .
WORKDIR "/src/Omniscient.Indexer"
RUN dotnet build "Omniscient.Indexer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Omniscient.Indexer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y netcat-openbsd dos2unix
COPY wait-for-rmq-postgres.sh /app/wait-for-rmq-postgres.sh
RUN chmod +x /app/wait-for-rmq-postgres.sh && dos2unix wait-for-rmq-postgres.sh
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/app/wait-for-rmq-postgres.sh"]
CMD ["dotnet", "Omniscient.Indexer.dll"]
