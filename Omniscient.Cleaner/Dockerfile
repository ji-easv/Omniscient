FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Omniscient.Cleaner/Omniscient.Cleaner.csproj", "Omniscient.Cleaner/"]
RUN dotnet restore "Omniscient.Cleaner/Omniscient.Cleaner.csproj"
COPY . .
WORKDIR "/src/Omniscient.Cleaner"
RUN dotnet build "Omniscient.Cleaner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Omniscient.Cleaner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Omniscient.Cleaner.dll"]
