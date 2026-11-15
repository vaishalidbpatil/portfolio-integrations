FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Portfolio.Agent.Worker/Portfolio.Agent.Worker.csproj", "src/Portfolio.Agent.Worker/"]
RUN dotnet restore "src/Portfolio.Agent.Worker/Portfolio.Agent.Worker.csproj"
COPY . .
WORKDIR "/src/src/Portfolio.Agent.Worker"
RUN dotnet build "Portfolio.Agent.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portfolio.Agent.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Agent.Worker.dll"]
