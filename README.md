# Rating System

A gym management system built with ASP.NET Core that allows managing gyms, services, and appointments.

## Features

- **Gym Management**: Create, read, update, and delete gym locations
- **Service Management**: Manage services offered at each gym
- **User Management**: Admin and customer roles with authentication
- **Appointment System**: Book and manage appointments for services
- **Redis Caching**: Improved performance with Redis cache integration

## Technology Stack

- **Backend API**: ASP.NET Core 9.0 Web API
- **Frontend Web**: ASP.NET Core 9.0 MVC
- **Database**: PostgreSQL
- **Cache**: Redis
- **Authentication**: JWT Bearer tokens
- **ORM**: Entity Framework Core
- **Containerization**: Docker & Docker Compose

## Project Structure

```
RatingSystem/
├── Project.API/          # Backend REST API
│   ├── Controllers/      # API endpoints
│   ├── Models/          # Domain models
│   ├── DTO/             # Data transfer objects
│   ├── Data/            # DbContext
│   ├── Services/        # Cache services
│   └── Validators/      # Input validation
├── Project.web/         # Frontend MVC application
│   ├── Controllers/     # MVC controllers
│   ├── Views/          # Razor views
│   ├── Models/         # View models
│   └── wwwroot/        # Static files
└── docker-compose.yml   # Docker orchestration
```

## Getting Started

### Prerequisites

- Docker Desktop
- .NET 9.0 SDK (for local development)

### Running with Docker

1. Clone the repository:
```bash
git clone https://github.com/YusaKarapinar/RatingSystem.git
cd RatingSystem
```

2. Start the application:
```bash
docker-compose up --build -d
```

3. Access the applications:
   - Web App: http://localhost:5002
   - API: http://localhost:8080

### Services

The Docker Compose setup includes:
- **project_web_server**: Frontend MVC application (port 5002)
- **project_api_server**: Backend API (port 8080)
- **project_postgres_db**: PostgreSQL database (port 5432)
- **project_redis**: Redis cache (port 6379)

### Default Users

Check `KULLANICILAR.txt` for default user credentials.

## API Endpoints

### Gyms
- `GET /api/Gyms` - List all gyms
- `GET /api/Gyms/{id}` - Get gym details
- `POST /api/Gyms` - Create new gym
- `PUT /api/Gyms/{id}` - Update gym
- `DELETE /api/Gyms/{id}` - Delete gym

### Services
- `GET /api/Services` - List all services
- `GET /api/Services/{id}` - Get service details
- `POST /api/Services` - Create new service
- `PUT /api/Services/{id}` - Update service
- `DELETE /api/Services/{id}` - Delete service

### Users
- `POST /api/Users/register` - Register new user
- `POST /api/Users/login` - User login
- `GET /api/Users` - List users (Admin only)

### Appointments
- `GET /api/Appointments` - List appointments
- `POST /api/Appointments` - Create appointment
- `PUT /api/Appointments/{id}` - Update appointment
- `DELETE /api/Appointments/{id}` - Delete appointment

## Development

### Database Migrations

```bash
cd Project.API
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running Locally

1. Update connection strings in `appsettings.Development.json`
2. Run API:
```bash
cd Project.API
dotnet run
```

3. Run Web:
```bash
cd Project.web
dotnet run
```

## License

This project is for educational purposes.

## Author

Yusa Karapinar
