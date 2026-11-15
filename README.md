# Portfolio Integrations — MCP Server

ASP.NET Core MCP (Model Context Protocol) server exposing trade order tools to AI agents.

## Services

| Service | Port | Description |
|---|---|---|
| `mcp-server` | 5002 | MCP tool server |

## Prerequisites

- Docker Desktop
- `portfolio-service` stack running (for live order data)

## Running locally

```powershell
docker compose up --build
```

Health check: http://localhost:5002/health/live

## Tools

| Endpoint | Description |
|---|---|
| `GET /tools/weather-forecast` | Placeholder — replace with order tools |

## Connecting to portfolio-service

The server expects the Orders API at `Orders__BaseUrl` (default: `http://host.docker.internal:8080`).
Start the main stack first with `setup.ps1` from the `portfolio-service` repo.
