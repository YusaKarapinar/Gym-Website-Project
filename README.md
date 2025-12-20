# Gym Website Project

Full-stack gym management platform built with ASP.NET Core Web API and MVC frontend.

## Tech Stack
- Backend: ASP.NET Core Web API, Entity Framework Core, Identity, JWT auth
- Frontend: ASP.NET Core MVC (Razor)
- Data: PostgreSQL, Redis (cache)
- AI: Gemini API for fitness recommendations

## Projects
- Project.API — REST API (authentication, gyms, services, appointments, AI)
- Project.web — MVC frontend consuming the API
- Project.API.Tests, Project.web.Tests — test projects

## Default Roles & Credentials
- Admin: `ogrencinumarasi@sakarya.edu.tr` / `sau`
- Trainer (seeded): `ahmet@fittrack.com` / `Trainer123`
- Member (seeded): `mehmet@test.com` / `Member123`

## Validation
- Server-side: DataAnnotations on DTOs (Required, Range, Email, MinLength)
- Client-side: MVC validation via the same annotations and `_ValidationScriptsPartial`

## Run with Docker
```bash
docker compose up -d --build
```
- API: http://localhost:8080
- Web: http://localhost:8081 (if exposed in compose)

## Local Development
Requirements: .NET 9 SDK, PostgreSQL, Redis.

1) Restore
```bash
dotnet restore
```
2) Migrate database (API project)
```bash
cd Project.API
dotnet ef database update
```
3) Run API
```bash
dotnet run --project Project.API/Project.API.csproj
```
4) Run Web
```bash
dotnet run --project Project.web/Project.web.csproj
```

## Environment
- Configure connection strings in `Project.API/appsettings.json`
- `.env` can override settings for local runs (see `.env.example`)

## Key Endpoints (API)
- `POST /api/Users/login`, `POST /api/Users/register`
- `GET/POST/PUT/DELETE /api/Gyms`
- `GET/POST/PUT/DELETE /api/Services`
- `POST /api/Appointments/create`, `PUT /api/Appointments/{id}`, `DELETE /api/Appointments/{id}`, `POST /api/Appointments/get`, `GET /api/Appointments/{id}`
- `POST /api/AI/fitness-recommendation`

## Tests
```bash
dotnet test
```

## Notes
- Identity password policy set to min length 3 to allow seeded admin password.
- Seed data runs on startup: roles, admin, sample trainers/members, gyms, services, appointment.
