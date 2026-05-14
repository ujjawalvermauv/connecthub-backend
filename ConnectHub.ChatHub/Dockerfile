FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ConnectHub.ChatHub/ConnectHub.ChatHub.csproj", "ConnectHub.ChatHub/"]
RUN dotnet restore "ConnectHub.ChatHub/ConnectHub.ChatHub.csproj"

COPY . .
WORKDIR "/src/ConnectHub.ChatHub"
RUN dotnet publish "ConnectHub.ChatHub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConnectHub.ChatHub.dll"]