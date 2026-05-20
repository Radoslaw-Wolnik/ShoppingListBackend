# Shopping List Backend

A pragmatic .NET 8 backend for collaborative shopping lists. It keeps the codebase compact while still separating HTTP endpoints, SignalR, services, repositories, validation, mapping, and persistence clearly enough to test and evolve.

## What It Does

- Registers devices and issues one-time API keys.
- Stores API keys securely using BCrypt, with SHA-256 lookup for efficient validation.
- Manages shopping lists, categories, and items with stable ordering.
- Supports owners and editors for shared lists.
- Supports direct device friendships.
- Broadcasts list changes through SignalR so connected clients update instantly.
- Tracks who is currently editing a list using database-backed presence and broadcasts presence changes.
- Validates request DTOs with FluentValidation before handlers run.
- Returns consistent problem responses through custom error middleware.
- Supports explicit browser CORS configuration and optional Redis SignalR scale-out.

## Architecture

This is intentionally not a full multi-project Clean Architecture setup. It is a pragmatic vertical-slice style API in one deployable project:

```text
src/ShoppingListBackend.Api/
  Data/          EF Core DbContext
  DTOs/          Request, response, and realtime contracts
  Endpoints/     Minimal API route groups
  Exceptions/    App-specific exception types
  Extensions/    DI and endpoint helper extensions
  Hubs/          SignalR hub
  Mappers/       AutoMapper profiles
  Middleware/    API key auth and error handling
  Models/        EF Core entities
  Repositories/  Persistence interfaces and EF implementations
  Services/      Business logic / use cases
  Validators/    FluentValidation validators
```

The tests live under `tests/ShoppingListBackend.Tests/` and cover repositories, services, validators, middleware, HTTP endpoints, database mappings, and SignalR behavior.

## Tech Stack

- .NET 8 Minimal APIs
- Entity Framework Core
- PostgreSQL via Npgsql
- SignalR
- Redis backplane support for multi-instance SignalR
- FluentValidation
- AutoMapper
- BCrypt.Net
- xUnit, FluentAssertions, Moq, WebApplicationFactory

## Getting Started

### Run With Docker Compose

Copy the sample environment file and change the password before using it:

```bash
cp .env.example .env
docker compose up --build
```

The API is exposed at `http://localhost:8080`. Docker Compose starts PostgreSQL and Redis.

The compose setup is intended for local development. It runs the API in `Development`, applies EF Core migrations on startup, and wires SignalR through Redis.

### Run Locally

Start PostgreSQL, then provide the connection string:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ShoppingListApp;Username=ShoppingListApp;Password=change-me"
dotnet run --project src/ShoppingListBackend.Api
```

Optional local settings:

```powershell
$env:ConnectionStrings__SignalRRedis="localhost:6379"
$env:Cors__AllowedOrigins__0="http://localhost:5173"
```

Health check:

```bash
GET /health
```

## Authentication

Register a device:

```http
POST /api/devices/register
```

Response:

```json
{
  "deviceId": "00000000-0000-0000-0000-000000000000",
  "apiKey": "returned-once"
}
```

Send the API key on authenticated HTTP requests:

```http
X-API-Key: returned-once
```

SignalR also accepts the same key. .NET clients can send it as a header; browser clients can use the `apiKey` query string if headers are not available during WebSocket negotiation.

## CORS

Allowed browser origins are configured with:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
```

Development defaults include common localhost app ports. Production should set exact app origins; do not use wildcard origins with credentials.

## Main HTTP Routes

Device routes:

- `POST /api/devices/register`
- `GET /api/devices/me`
- `PUT /api/devices/me/username`
- `PUT /api/devices/me/colour`
- `POST /api/devices/me/friends`
- `GET /api/devices/me/friends`
- `DELETE /api/devices/me/friends/{friendId}`
- `DELETE /api/devices/me`

Shopping list routes:

- `GET /api/shopping-lists`
- `GET /api/shopping-lists/{id}`
- `POST /api/shopping-lists`
- `PUT /api/shopping-lists/{id}/title`
- `DELETE /api/shopping-lists/{id}`
- `POST /api/shopping-lists/{id}/copy`
- `POST /api/shopping-lists/{id}/reset-checked`
- `POST /api/shopping-lists/{id}/editors`
- `DELETE /api/shopping-lists/{id}/editors/{editorId}`
- `POST /api/shopping-lists/{listId}/categories`
- `PUT /api/shopping-lists/categories/{categoryId}`
- `DELETE /api/shopping-lists/categories/{categoryId}`
- `PUT /api/shopping-lists/categories/{categoryId}/reorder`
- `POST /api/shopping-lists/categories/{categoryId}/items`
- `PUT /api/shopping-lists/items/{itemId}`
- `PUT /api/shopping-lists/items/{itemId}/toggle`
- `DELETE /api/shopping-lists/items/{itemId}`
- `PUT /api/shopping-lists/categories/{categoryId}/items/{itemId}/reorder`
- `PUT /api/shopping-lists/items/{itemId}/move`

## SignalR

Hub URL:

```text
/hub/shoppingLists
```

Important hub methods:

- `JoinList(listId)`
- `LeaveList(listId)`
- `CreateList(title)`
- `UpdateListTitle(listId, newTitle)`
- `DeleteList(listId)`
- `AddCategory(listId, categoryName)`
- `UpdateCategory(categoryId, newName)`
- `DeleteCategory(categoryId)`
- `ReorderCategory(categoryId, newPosition)`
- `AddItem(categoryId, description)`
- `UpdateItemDescription(itemId, newDescription)`
- `ToggleItem(itemId, isChecked)`
- `DeleteItem(itemId)`
- `ReorderItem(categoryId, itemId, newPosition)`
- `MoveItem(itemId, newCategoryId)`
- `ResetCheckedItems(listId)`

Realtime messages are sent to list groups as `ShoppingListEvent` payloads with an `eventType`, `listId`, timestamp, and event-specific fields. Presence changes are sent as `CurrentlyEditingChanged`.

Presence is stored in PostgreSQL through `EditingSessions`, so multiple API instances see the same currently-editing state. If `ConnectionStrings:SignalRRedis` is configured, SignalR uses Redis as a backplane so group broadcasts also reach clients connected to other API instances.

## Database

The initial EF Core migration lives in `src/ShoppingListBackend.Api/Data/Migrations`.

Apply migrations manually for production:

```bash
dotnet ef database update --project src/ShoppingListBackend.Api --startup-project src/ShoppingListBackend.Api
```

Development startup applies migrations automatically for relational providers. Tests use EF InMemory and create their schema directly.

## Testing

Run everything:

```bash
dotnet test
```

Current suite coverage includes:

- Unit tests for services, repositories, validators, middleware, mapping, and the SignalR hub.
- Integration tests for device endpoints, shopping list endpoints, EF Core mappings, CORS, authorization boundaries, and SignalR broadcasts.
- Collaboration workflow coverage for sharing, editor revocation, copying, reordering, moving, and reset operations.
- Database-backed presence coverage.

At the time of this README update, the suite contains 187 tests.

## Production Notes

- Run EF Core migrations as part of deployment.
- Keep API keys out of logs and client-visible telemetry.
- Store `.env` outside source control; this repo ignores it by default.
- Configure exact CORS origins before exposing the API to browser clients.
- Configure Redis for SignalR when running more than one API instance.
- Consider adding refresh/rotation flows for device API keys.

## Current Boundaries

The code has the model shape for some future features, but these are not complete yet:

- Friend-code invitations.
- Conflict-resolution workflows for true offline-first sync.
- API key rotation / revocation flows beyond deleting a device.
