﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Omniscient.Web/Omniscient.Web.csproj", "Omniscient.Web/"]
COPY ["Omniscient.ServiceDefaults/Omniscient.ServiceDefaults.csproj", "Omniscient.ServiceDefaults/"]
RUN dotnet restore "Omniscient.Web/Omniscient.Web.csproj"
COPY . .
WORKDIR "/src/Omniscient.Web"
RUN dotnet build "Omniscient.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Omniscient.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Omniscient.Web.dll"]
