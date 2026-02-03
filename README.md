# OctoBot

An AI agent platform with support for multiple LLM providers and communication channels.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18 or later)

## Getting Started

Run both the API and web client in separate terminals:

### 1. Start the API

```bash
cd src/OctoBot.Api
dotnet run
```

The API will start at `http://localhost:62888`.

### 2. Start the Web Client

```bash
cd web-client
npm install
npm run dev
```

The web client will start at `http://localhost:5173`. API requests to `/api/*` are automatically proxied to the backend.

### Changing the API Port

If the API runs on a different port, update the proxy target in `web-client/vite.config.ts`:

```ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:YOUR_PORT',
      changeOrigin: true,
    },
  },
},
```

## Project Structure

- `src/OctoBot.Api` - ASP.NET Core Web API
- `src/OctoBot.Agent` - AI agent orchestration
- `src/OctoBot.LLM.*` - LLM provider integrations (OpenAI, Anthropic, Ollama)
- `src/OctoBot.Plugins.*` - Plugin system
- `src/OctoBot.Channels.*` - Communication channels (Telegram)
- `web-client` - React frontend
