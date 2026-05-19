# Shopping List Backend

A pragmatic .NET 8 backend for collaborative shopping lists. It keeps the codebase compact while still separating HTTP endpoints, SignalR, services, repositories, validation, mapping, and persistence clearly enough to test and evolve.

## What It Does

- Registers devices and issues one-time API keys.
- Stores API keys securely using BCrypt, with SHA-256 lookup for efficient validation.
- Manages shopping lists, categories, and items with stable ordering.
- Supports owners and editors for shared lists.
- Supports direct device friendships.
- Broadcasts list changes through SignalR so connected clients update instantly.
- Tracks who is currently editing a list and broadcasts presence changes.
- Validates request DTOs with FluentValidation before handlers run.
- Returns consistent problem responses through custom error middleware.

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

The API is exposed at `http://localhost:8080`.

The compose setup is intended for local development. It runs the API in `Development` so EF Core can create the schema with `EnsureCreated`. For production, use migrations instead of automatic schema creation.

### Run Locally

Start PostgreSQL, then provide the connection string:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ShoppingListApp;Username=ShoppingListApp;Password=change-me"
dotnet run --project src/ShoppingListBackend.Api
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

## Testing

Run everything:

```bash
dotnet test
```

Current suite coverage includes:

- Unit tests for services, repositories, validators, middleware, mapping, and the SignalR hub.
- Integration tests for device endpoints, shopping list endpoints, EF Core mappings, and SignalR broadcasts.
- Validation filter coverage for real HTTP requests.

## Production Notes

- Add EF Core migrations before production deployment.
- Keep API keys out of logs and client-visible telemetry.
- Store `.env` outside source control; this repo ignores it by default.
- Configure CORS explicitly before exposing the API to browser clients.
- Consider a distributed presence tracker if the API will run on multiple instances.
- Consider adding refresh/rotation flows for device API keys.

## Current Boundaries

The code has the model shape for some future features, but these are not complete yet:

- Friend-code invitations.
- Conflict-resolution workflows for true offline-first sync.
- EF migration files.
- Multi-instance SignalR backplane/presence storage.
