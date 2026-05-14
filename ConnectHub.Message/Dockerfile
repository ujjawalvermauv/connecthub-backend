FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ConnectHub.Message/ConnectHub.Message.csproj", "ConnectHub.Message/"]
RUN dotnet restore "ConnectHub.Message/ConnectHub.Message.csproj"

COPY . .
WORKDIR "/src/ConnectHub.Message"
RUN dotnet publish "ConnectHub.Message.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConnectHub.Message.dll"]