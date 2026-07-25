# Artisan Marketplace

A microservices demo: Identity, Product, and Order services behind an Ocelot API
Gateway, with a React front-end. Adapted for **macOS** — the original Windows
README assumed SQL Server LocalDB; this build uses **SQLite** instead (each
service creates its own `*.db` file on first run, no server to install or
start).

## 1. Prerequisites

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download) — **not yet installed
  on this machine.** Install with:
  ```bash
  brew install --cask dotnet-sdk
  ```
  or download the installer from the link above. Verify with `dotnet --version`.
- Node.js 18+ and npm (already installed here).
- No SQL Server / LocalDB / Visual C++ redistributable needed — SQLite is
  file-based.

## 2. Folder structure

```
Project/
├─ ArtisanMarketplace.IdentityService/   ← users, register/login, issues JWTs
├─ ArtisanMarketplace.ProductService/    ← categories + products (seeded)
├─ ArtisanMarketplace.OrderService/      ← checkout (stub, no real Stripe call)
├─ ArtisanMarketplace.ApiGateway/        ← Ocelot gateway, routes /api/* out
├─ artisan-marketplace-client/           ← React (Vite) front-end
└─ ArtisanMarketplace.sln
```

## 3. Configuration

Each service's `appsettings.json` already points at a local SQLite file
(`Data Source=<servicename>.db`), created automatically on first run via
`Database.EnsureCreated()` — no `dotnet ef database update` step needed.

Identity and Order services share a dev JWT signing key in
`appsettings.json` (`Jwt:Key`). It's fine for local dev; override it for
anything beyond your own machine:
```bash
export Jwt__Key="a-much-longer-random-secret"
```
Stripe isn't wired up yet (matches the original README — checkout is a stub
that just records the order).

## 4. Run each .NET service

Open one terminal per project:

```bash
cd ArtisanMarketplace.IdentityService && dotnet restore && dotnet run
cd ArtisanMarketplace.ProductService  && dotnet restore && dotnet run
cd ArtisanMarketplace.OrderService    && dotnet restore && dotnet run
cd ArtisanMarketplace.ApiGateway      && dotnet restore && dotnet run
```

Default ports (`Properties/launchSettings.json`, all plain HTTP — no
self-signed cert to trust):

| Service          | URL                          |
|------------------|-------------------------------|
| IdentityService  | http://localhost:5090        |
| ProductService   | http://localhost:5284        |
| OrderService     | http://localhost:5231        |
| ApiGateway       | http://localhost:5189        |

Swagger UI is available at `/swagger` on each backend service in Development.

## 5. Front-end (React) setup

```bash
cd artisan-marketplace-client
npm install
npm start
```

Opens `http://localhost:3000`; the Vite dev server proxies `/api/*` requests
to the gateway at `http://localhost:5189`.

## 6. Testing the ecosystem

1. Start all four .NET services, then the React client.
2. Open `http://localhost:3000` — home page appears.
3. Register a new user.
4. Browse "Handcrafted Products" — data comes from the Product service
   (pre-seeded with 4 categories, 8 products).
5. Add to cart → `/checkout` → place order (no real Stripe call — it just
   records the order in the Order service's SQLite DB).
6. View `/orders` to see your order history.

## 7. Troubleshooting

- **Port already in use** → change `applicationUrl` in that project's
  `Properties/launchSettings.json`.
- **"Could not load products. Is the API Gateway running?"** in the UI →
  make sure all four .NET services are running before the React app calls
  the API.
- **Reset a service's data** → stop it and delete its `*.db` file; it's
  recreated (with seed data, for Product) on next `dotnet run`.
- **CORS errors** → each service allows any origin in dev
  (`AllowAll` policy); this shouldn't come up unless you changed the client
  port.

That's it — once the .NET SDK is installed, `dotnet run` × 4 + `npm start`
brings up the whole Artisan Marketplace locally.
