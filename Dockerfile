# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file csproj của project liên quan
COPY RemoteDesktop.ServerAgent/RemoteDesktop.ServerAgent.csproj RemoteDesktop.ServerAgent/
COPY RemoteDesktop.Shared/RemoteDesktop.Shared.csproj RemoteDesktop.Shared/

# Restore dependencies cho project ServerAgent (nó sẽ kéo cả Shared)
RUN dotnet restore RemoteDesktop.ServerAgent/RemoteDesktop.ServerAgent.csproj

# Copy toàn bộ source code
COPY . .

# Publish project ServerAgent
RUN dotnet publish RemoteDesktop.ServerAgent -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "RemoteDesktop.ServerAgent.dll"]
