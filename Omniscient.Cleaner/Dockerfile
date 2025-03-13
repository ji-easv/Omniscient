FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 81

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Omniscient.Cleaner/Omniscient.Cleaner.csproj", "Omniscient.Cleaner/"]
COPY ["Omniscient.ServiceDefaults/Omniscient.ServiceDefaults.csproj", "Omniscient.ServiceDefaults/"]
COPY ["Omniscient.RabbitMQClient/Omniscient.RabbitMQClient.csproj", "Omniscient.RabbitMQClient/"]
COPY ["Omniscient.Shared/Omniscient.Shared.csproj", "Omniscient.Shared/"]
RUN dotnet restore "Omniscient.Cleaner/Omniscient.Cleaner.csproj"
COPY . .
WORKDIR "/src/Omniscient.Cleaner"
RUN dotnet build "Omniscient.Cleaner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Omniscient.Cleaner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y netcat-openbsd dos2unix
COPY wait-for-rmq-postgres.sh /app/wait-for-rmq-postgres.sh
RUN chmod +x /app/wait-for-rmq-postgres.sh && dos2unix wait-for-rmq-postgres.sh
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/app/wait-for-rmq-postgres.sh"]
CMD ["dotnet", "Omniscient.Cleaner.dll"]
