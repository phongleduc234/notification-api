# Mail API Service

## Overview

**Mail API Service** is a .NET 8 web application that provides email sending capabilities through a RESTful API with user management and rate limiting features.

## Features

- User registration with automatic API key generation  
- Email sending with authentication via API key  
- Daily email sending limits per user  
- Automatic daily counter reset  
- Health check endpoints  
- Swagger API documentation  

## Technical Stack

- .NET 8.0  
- ASP.NET Core Web API  
- Entity Framework Core 8.0  
- PostgreSQL database  
- Docker support  

## Database Schema

The application uses a PostgreSQL database with the following schema:

### `EmailUsers` Table

| Column          | Type         | Description                      |
|-----------------|--------------|----------------------------------|
| Id              | text         | Primary key                      |
| Username        | varchar(50)  | Unique username                  |
| Email           | varchar(100) | User's email address             |
| ApiKey          | varchar(64)  | Unique API key for authentication |
| CreatedAt       | timestamp    | Account creation date            |
| LastResetDate   | timestamp    | Last daily counter reset date    |
| IsActive        | boolean      | User account status              |
| DailyEmailLimit | integer      | Maximum emails per day           |
| EmailsSentToday | integer      | Counter for daily emails         |

## API Endpoints

### User Management

- `POST /api/EmailUsers` - Create a new user  
- `GET /api/EmailUsers/{id}` - Get user by ID  
- `GET /api/EmailUsers/apikey/{apiKey}` - Get user by API key  

### Email Sending

- `POST /api/Email/send` - Send an email (requires API key in header)  

## Setup

### Prerequisites

- .NET 8.0 SDK  
- PostgreSQL database  
- Docker (optional)  

### Configuration

Update the connection string in `appsettings.json`:

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mailapi;Username=postgres;Password=yourpassword"
  }
}
```

### Running the Application
#### Using .NET CLI:
```
dotnet restore
dotnet run
```

#### Using Docker:
```
docker build -t mailapi .
docker run -p 8080:80 mailapi
```

### API Usage Examples
#### Creating a new user:
```
  curl -X POST "https://localhost:5001/api/EmailUsers" \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"user@example.com"}'
```
  
#### Sending an email:
```
  curl -X POST "https://localhost:5001/api/Email/send" \
  -H "Content-Type: application/json" \
  -H "apiKey: YOUR_API_KEY" \
  -d '{
    "to":"recipient@example.com",
    "subject":"Test Email",
    "body":"This is a test email sent via the Mail API",
    "isHtml":false
  }'
```
  
### API Access Control
- Each user gets a unique API key upon registration
- API key must be included in the request header for authentication
- Email sending is limited by the daily quota set for each user
- Counters automatically reset at midnight UTC

### Health Monitoring
A health check endpoint is available at `/health` for monitoring the service status.

### Documentation
Swagger UI is available at `/swagger` when the application is running.