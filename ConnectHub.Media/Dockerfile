FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ConnectHub.Media.csproj", "."]
RUN dotnet restore "ConnectHub.Media.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet publish "ConnectHub.Media.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConnectHub.Media.dll"]