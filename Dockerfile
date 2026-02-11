FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RemoteDesktop.ServerAgent/RemoteDesktop.ServerAgent.csproj RemoteDesktop.ServerAgent/
RUN dotnet restore RemoteDesktop.ServerAgent/RemoteDesktop.ServerAgent.csproj

COPY . .
RUN dotnet publish RemoteDesktop.ServerAgent -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "RemoteDesktop.ServerAgent.dll"]
