# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Serilog.MemoryMapped.Sink.Console/Serilog.MemoryMapped.Sink.Console.csproj", "Serilog.MemoryMapped.Sink.Console/"]
COPY ["Serilog.MemoryMapped.Queue/Serilog.MemoryMapped.Queue.csproj", "Serilog.MemoryMapped.Queue/"]
COPY ["Serilog.MemoryMapped.Repository.MsSql/Serilog.MemoryMapped.Repository.MsSql.csproj", "Serilog.MemoryMapped.Repository.MsSql/"]
COPY ["Serilog.MemoryMapped.Sink.Forwarder/Serilog.MemoryMapped.Sink.Forwarder.csproj", "Serilog.MemoryMapped.Sink.Forwarder/"]
COPY ["Serilog.MemoryMapped.Sink/Serilog.MemoryMapped.Sink.csproj", "Serilog.MemoryMapped.Sink/"]
COPY ["Serilog.MemoryMapped.Repository.PostgreSql/Serilog.MemoryMapped.Repository.PostgreSql.csproj", "Serilog.MemoryMapped.Repository.PostgreSql/"]
COPY ["Serilog.MemoryMapped.Repository.SqLite/Serilog.MemoryMapped.Repository.SqLite.csproj", "Serilog.MemoryMapped.Repository.SqLite/"]
RUN dotnet restore "./Serilog.MemoryMapped.Sink.Console/Serilog.MemoryMapped.Sink.Console.csproj"
COPY . .
WORKDIR "/src/Serilog.MemoryMapped.Sink.Console"
RUN dotnet build "./Serilog.MemoryMapped.Sink.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Serilog.MemoryMapped.Sink.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Serilog.MemoryMapped.Sink.Console.dll"]