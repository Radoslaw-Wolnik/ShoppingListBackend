# Shopping List Backend

A pragmatic .NET 8 backend for the [ShoppingListApp](https://github.com/Radoslaw-Wolnik/ShoppingListApp). It provides device identity, shared shopping lists, list/category/item management, and realtime collaboration through SignalR.

The project follows a lightweight vertical-slice style: the API stays in one deployable service, but HTTP endpoints, validation, services, repositories, mapping, persistence, middleware, and realtime contracts are kept separate enough to test and evolve cleanly.

## Core Capabilities

- Device registration with one-time API key issuance.
- API key authentication for HTTP endpoints and SignalR connections.
- Secure API key storage using BCrypt plus SHA-256 lookup.
- Shopping list, category, and item CRUD.
- Stable category and item ordering with reorder and move operations.
- Owner/editor access control for collaborative lists.
- Device friendship support for sharing flows.
- SignalR broadcasts for instant list updates.
- Database-backed editing presence for multi-instance deployments.
- PostgreSQL persistence through Entity Framework Core migrations.
- Explicit CORS configuration for browser clients.
- Optional Redis SignalR backplane for scale-out.

## Architecture

This repository intentionally avoids a heavy multi-project Clean Architecture setup. The codebase uses pragmatic boundaries inside a single API project:

```text
src/ShoppingListBackend.Api/
  Data/          EF Core DbContext and migrations
  DTOs/          HTTP and realtime contracts
  Endpoints/     Minimal API route groups
  Exceptions/    Application exception types
  Extensions/    Dependency injection and endpoint helpers
  Hubs/          SignalR hub
  Mappers/       AutoMapper profiles
  Middleware/    API key authentication and error handling
  Models/        EF Core entities
  Repositories/  Persistence abstractions and EF implementations
  Services/      Business logic and use cases
  Validators/    FluentValidation validators

tests/ShoppingListBackend.Tests/
  IntegrationTests/
  UnitTests/
  Helpers/
```

The result is a backend that is small enough to understand quickly, but not so flat that business rules, database access, and transport concerns become tangled.

## Tech Stack

- .NET 8 Minimal APIs
- Entity Framework Core
- PostgreSQL via Npgsql
- SignalR
- Redis SignalR backplane support
- FluentValidation
- AutoMapper
- BCrypt.Net
- xUnit, FluentAssertions, Moq, WebApplicationFactory

## Client Integration

The companion Android app is currently local-first and stores data in Room with local `long` identifiers. This backend exposes server-owned `Guid` identifiers and authenticated network contracts, so connecting the app should be done through a dedicated remote data source or sync layer rather than by replacing the Room repository one-to-one.

A mobile client should:

- Register the device with `POST /api/devices/register`.
- Store the returned API key securely on the device.
- Send authenticated HTTP requests with the `X-API-Key` header.
- Store backend IDs alongside local Room IDs, or migrate remote-backed screens to server IDs.
- Use the HTTP endpoints for durable changes.
- Connect to `/hub/shoppingLists` for realtime updates while a list is open.
- Treat local preferences, such as favourite ordering and display settings, as client-owned unless they are deliberately promoted into the backend contract.

The backend is ready to power an online collaborative client. Full offline conflict resolution is a client-side sync concern and is not implemented as a dedicated sync protocol in this API.

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

Authenticated HTTP requests use:

```http
X-API-Key: returned-once
```

SignalR accepts the same API key. Native clients should send it as a header. Browser clients may pass `apiKey` as a query string during WebSocket negotiation when headers are not available.

## HTTP API

Health:

- `GET /health`

Devices:

- `POST /api/devices/register`
- `GET /api/devices/me`
- `PUT /api/devices/me/username`
- `PUT /api/devices/me/colour`
- `POST /api/devices/me/friends`
- `GET /api/devices/me/friends`
- `DELETE /api/devices/me/friends/{friendId}`
- `DELETE /api/devices/me`

Shopping lists:

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

Validation failures return standard validation problem responses. Unhandled application errors are shaped by the central error middleware as `application/problem+json`.

## Realtime API

Hub URL:

```text
/hub/shoppingLists
```

Common hub methods:

- `JoinList(listId)`
- `LeaveList(listId)`
- `CreateList(title)`
- `UpdateListTitle(listId, newTitle)`
- `DeleteList(listId)`
- `CopyList(listId)`
- `AddEditor(listId, editorDeviceId)`
- `RemoveEditor(listId, editorDeviceId)`
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

List changes are broadcast to joined clients as `ShoppingListEvent` payloads. Editing presence changes are broadcast as `CurrentlyEditingChanged`.

## Running Locally

### Docker Compose

Create a local environment file from the example, adjust the password, then start the stack:

```bash
cp .env.example .env
docker compose up --build
```

The API is exposed at:

```text
http://localhost:8080
```

Docker Compose starts PostgreSQL, Redis, and the API. In `Development`, the API applies EF Core migrations automatically on startup.

### Local .NET

Start PostgreSQL, then provide a connection string:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ShoppingListApp;Username=ShoppingListApp;Password=change-me"
dotnet run --project src/ShoppingListBackend.Api
```

Optional local settings:

```powershell
$env:ConnectionStrings__SignalRRedis="localhost:6379"
$env:Cors__AllowedOrigins__0="http://localhost:5173"
```

## Configuration

Primary settings:

- `ConnectionStrings:DefaultConnection` - PostgreSQL connection string.
- `ConnectionStrings:SignalRRedis` - optional Redis backplane connection string.
- `Cors:AllowedOrigins` - exact browser origins allowed to call the API with credentials.

Production should always configure explicit CORS origins. Development can fall back to loopback origins for local clients.

## Database

EF Core migrations live in:

```text
src/ShoppingListBackend.Api/Data/Migrations
```

Apply migrations manually for production deployments:

```bash
dotnet ef database update --project src/ShoppingListBackend.Api --startup-project src/ShoppingListBackend.Api
```

Development startup applies migrations automatically for relational providers. Tests use EF Core InMemory and create the schema directly.

## Testing

Run the complete suite:

```bash
dotnet test
```

Check formatting:

```bash
dotnet format --verify-no-changes
```

The test suite covers validators, services, repositories, mapping, middleware, HTTP endpoints, EF Core mappings, SignalR behavior, collaboration workflows, CORS, authorization boundaries, and database-backed editing presence.

## Production Notes

- Run EF Core migrations as part of deployment.
- Use HTTPS at the public edge.
- Keep API keys out of logs, analytics, crash reports, and client-visible telemetry.
- Store environment files and production secrets outside source control.
- Configure exact CORS origins before exposing the API to browser clients.
- Configure Redis when running more than one API instance.
- Add API key rotation or recovery flows if long-lived production devices need them.

