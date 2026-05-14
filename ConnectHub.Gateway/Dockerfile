FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ConnectHub.Gateway/ConnectHub.Gateway/ConnectHub.Gateway.csproj", "ConnectHub.Gateway/"]
RUN dotnet restore "ConnectHub.Gateway/ConnectHub.Gateway.csproj"

COPY . .
WORKDIR "/src/ConnectHub.Gateway/ConnectHub.Gateway"
RUN dotnet publish "ConnectHub.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConnectHub.Gateway.dll"]