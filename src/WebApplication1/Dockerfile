#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WebApplication1/WebApplication1.csproj", "WebApplication1/"]
COPY ["Management/src/EndpointCore/Steeltoe.Management.EndpointCore.csproj", "Management/src/EndpointCore/"]
COPY ["Common/src/Abstractions/Steeltoe.Common.Abstractions.csproj", "Common/src/Abstractions/"]
COPY ["Common/src/Common.Http/Steeltoe.Common.Http.csproj", "Common/src/Common.Http/"]
COPY ["Discovery/src/Abstractions/Steeltoe.Discovery.Abstractions.csproj", "Discovery/src/Abstractions/"]
COPY ["Common/src/Common/Steeltoe.Common.csproj", "Common/src/Common/"]
COPY ["Management/src/EndpointBase/Steeltoe.Management.EndpointBase.csproj", "Management/src/EndpointBase/"]
COPY ["Management/src/OpenTelemetryBase/Steeltoe.Management.OpenTelemetryBase.csproj", "Management/src/OpenTelemetryBase/"]
COPY ["Logging/src/DynamicLogger/Steeltoe.Extensions.Logging.DynamicLogger.csproj", "Logging/src/DynamicLogger/"]
COPY ["Logging/src/Abstractions/Steeltoe.Extensions.Logging.Abstractions.csproj", "Logging/src/Abstractions/"]
COPY ["Configuration/src/Abstractions/Steeltoe.Extensions.Configuration.Abstractions.csproj", "Configuration/src/Abstractions/"]
COPY ["Management/src/Abstractions/Steeltoe.Management.Abstractions.csproj", "Management/src/Abstractions/"]
COPY ["Management/src/Diagnostics/Steeltoe.Management.Diagnostics.csproj", "Management/src/Diagnostics/"]
RUN dotnet restore "WebApplication1/WebApplication1.csproj"
COPY . .
WORKDIR "/src/WebApplication1"
RUN dotnet build "WebApplication1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]