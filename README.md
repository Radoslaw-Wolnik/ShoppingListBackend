# Shopping List Backend

**A collaborative shopping list backend with offline sync, real‑time updates, and per‑device accounts.**

This service powers a shopping list app where multiple devices can edit the same list simultaneously, even when offline, with seamless synchronization when connectivity is restored.

---

## Features

- **Per‑device accounts** – each device automatically registers and receives a unique API key; no password hassles.
- **Shopping lists** – lists contain categories, each with nested items (description, completed state, position).
- **Collaboration** – share lists with other devices via invite codes (permanent or single‑use). Permission levels: owner (full control) and editor (can edit but not delete/change ownership).
- **Private lists** – lists can be marked as private and never shared.
- **Real‑time collaboration** – changes propagate instantly via **SignalR** to all connected clients using partial updates (only changed data is sent).
- **Offline‑first sync** – clients cache data locally with `LastUpdatedAt` timestamps. The server supports delta sync: clients fetch only what changed since their last sync.
- **Partial updates** – both HTTP API and SignalR use granular operations (e.g., “add item”, “toggle item”) instead of sending whole lists, minimising bandwidth and simplifying conflict resolution.
- **Docker ready** – includes a `Dockerfile` and `docker-compose.yml` for easy deployment.


## Architecture: Pragmatic Clean Architecture

We avoid the overhead of full Clean Architecture (multiple projects, MediatR, etc.) but still maintain clear separation of concerns for testability and maintainability. The project is a single .NET 8 web application organised into logical folders:

```
ShoppingListBackend.Api/
├── Models/               # EF Core entities (database models)
├── DTOs/                 # Request/response data transfer objects
├── Repositories/         # Data access layer (interfaces + EF Core implementations)
├── Services/             # Business logic (use cases)
├── Hubs/                 # SignalR hubs for real‑time communication
├── Middleware/           # Custom middleware (e.g., API key auth)
├── Validators/           # FluentValidation validators
├── Mappers/              # Mapping profiles (DTO ↔ Model)
├── Data/                 # DbContext and migrations
├── Extensions/           # DI extension methods
├── Enpoints/             # Minimal Api endpoints - routes
└── Program.cs            # Application entry point (Minimal APIs)
```


## Technology Stack

- **.NET 8** (Minimal APIs + SignalR)
- **PostgreSQL** – primary database (EF Core)
- **SignalR** – real‑time communication
- **AutoMapper** – object mapping
- **FluentValidation** – request validation
- **BCrypt** – API key hashing
- **Docker** – containerisation


##  Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (optional, for PostgreSQL)
- [PostgreSQL](https://www.postgresql.org/) (or use Docker)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/shopping-list-backend.git
   cd shopping-list-backend
   ```

2. **Configure environment variables**
   Copy `.env.example` to `.env` and fill in your values:
   ```bash
   cp .env.example .env
   ```
   Required variables:
   - `POSTGRES_CONNECTION` – connection string to PostgreSQL
   - `ASPNETCORE_ENVIRONMENT` – `Development` or `Production`

3. **Run PostgreSQL (Docker)**
   ```bash
   docker run --name shopping-db -e POSTGRES_PASSWORD=secret -e POSTGRES_DB=shopping -p 5432:5432 -d postgres
   ```
   Or use the provided `docker-compose.yml` to run both the database and the app.

4. **Run database migrations**
   ```bash
   dotnet ef database update --project ShoppingListBackend.Api
   ```

5. **Run the application**
   ```bash
   cd ShoppingListBackend.Api
   dotnet run
   ```
   The API will be available at `http://localhost:5000`.

### Docker Compose (full stack)

```bash
docker-compose up -d
```


##  Authentication

Each device is a “user”. On first launch, the app generates a unique device ID (GUID) and registers it with the backend:

```
POST /api/register
{
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

The server returns an **API key** (a long random string). The device stores it securely and includes it in every request as the `X-API-Key` header. The key is hashed with BCrypt before storage.

All endpoints (except `/api/register`) require this key. SignalR connections also authenticate using the same key (passed as a query string).


## Real‑time Collaboration (SignalR)

Clients connect to the hub at `/hub/shoppingLists`. After authenticating (the key is validated automatically), they can join list groups and perform operations:

| Hub method             | Description                                    |
|------------------------|------------------------------------------------|
| `JoinList(listId)`     | Subscribe to real‑time updates for a list      |
| `AddItem(listId, description, categoryId?)` | Add a new item to a category      |
| `ToggleItem(listId, itemId)` | Toggle completion state                  |
| `DeleteItem(listId, itemId)` | Remove an item                           |
| `RenameList(listId, newName)` | Change list name                       |
| `ReorderItem(listId, itemId, newOrder)` | Reorder items                      |

When any change occurs, the server broadcasts a **partial update** (e.g., `ItemAdded`, `ItemToggled`) to all other clients in the list group, containing only the affected data. This keeps bandwidth low and ensures UI updates are instant.


## Data Model Overview

The main entities are:

- **Device** – represents a client device. Contains `Id` (public device ID), `ApiKeyHash`, `ApiKeyPrefix` (for fast lookup), `CreatedAt`.
- **ShoppingList** – has `Id`, `Name`, `OwnerDeviceId`, `CreatedAt`, `LastUpdatedAt`, `IsPrivate`.
- **Category** – optional grouping within a list. Has `Id`, `Name`, `ShoppingListId`, `Order`.
- **ShoppingListItem** – belongs to a category (or directly to a list if no categories). Has `Id`, `Description`, `IsChecked`, `Order`, `CategoryId` (nullable), `ShoppingListId`.
- **Share** – links a device to a list with a permission level (`Owner` or `Editor`). Used for collaboration.
- **FriendCode** – a code generated by a device to allow others to add them as friends. Contains `Code`, `DeviceId`, `ExpiresAt`, `IsSingleUse`.

For the full schema, see the `Models/` folder.


##  Sync Strategy (Offline‑First)

To support offline editing and efficient syncing:

- Every entity has a `LastUpdatedAt` UTC timestamp.
- Clients store the latest `LastUpdatedAt` they have received for each list.
- The client requests changes since that timestamp using `GET /api/lists/sync?since={timestamp}`.
- The server returns only entities that changed after that time (a delta).
- When the client makes a change while offline, it stores the change locally with a pending flag.
- On reconnection, the client sends the pending changes to the server (as partial operations, not the whole list). The server applies them and updates timestamps.

All writes (HTTP and SignalR) are idempotent and include the new `LastUpdatedAt` in the response.


## Permissions & Friend System

- **List owners** can:
  - Delete the list
  - Change list name
  - Share the list with other devices (as editor or co‑owner)
  - Remove editors/co‑owners
  - Add/remove items (they can also act as editors)

- **Editors** can:
  - Add, modify, reorder, and delete items
  - Toggle completion
  - They cannot delete the list, change its name, or manage shares.

- **Friend codes**:
  - A device can generate a 6 digit code (e.g., `ABC123`) that another device can redeem to add the first device as a friend.
  - Codes can be permanent or single‑use.
  - Once two devices are friends, they can share lists with each other directly (by adding the friend’s device ID to the list’s shares).


## Testing

- **Unit tests** – for services and repositories (using xUnit + Moq).
- **Integration tests** – for API endpoints and SignalR hubs (using `WebApplicationFactory`).

Run all tests:
```bash
dotnet test
```
