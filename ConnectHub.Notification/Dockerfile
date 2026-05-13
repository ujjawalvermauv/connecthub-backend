FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ConnectHub.Notification.csproj", "."]
RUN dotnet restore "ConnectHub.Notification.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet publish "ConnectHub.Notification.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConnectHub.Notification.dll"]