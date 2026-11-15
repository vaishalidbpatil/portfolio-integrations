FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Portfolio.Mcp.Server/Portfolio.Mcp.Server.csproj", "src/Portfolio.Mcp.Server/"]
RUN dotnet restore "src/Portfolio.Mcp.Server/Portfolio.Mcp.Server.csproj"
COPY . .
WORKDIR "/src/src/Portfolio.Mcp.Server"
RUN dotnet build "Portfolio.Mcp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portfolio.Mcp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Mcp.Server.dll"]
