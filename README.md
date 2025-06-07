# CareAI

An intelligent, empathetic AI assistant designed to handle social service interactions, built with .NET 8.0 and Azure OpenAI.

## Features

- **Conversation Management**: Maintains context-aware, multi-turn conversations
- **Service Integration**: Handles service recommendations and processing
- **AI-Powered**: Uses Azure OpenAI's GPT models for natural language understanding
- **Secure**: API key authentication and proper error handling
- **Persistent Storage**: Entity Framework Core with SQL Server

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB works for development)
- Azure OpenAI Service (for production)

## Setup

1. Clone the repository
2. Update `appsettings.json` with your connection strings and API keys:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CareAIDb;Trusted_Connection=True;MultipleActiveResultSets=true;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
     },
     "OpenAI": {
       "Enabled": false,
       "ApiKey": "YOUR_OPENAI_API_KEY",
       "Endpoint": "YOUR_OPENAI_ENDPOINT",
       "ModelName": "gpt-4",
       "DeploymentName": "gpt-4"
     },
     "Security": {
       "ApiKey": "YOUR_SECURE_API_KEY"
     }
   }
   ```

3. Apply database migrations:
   ```bash
   cd src/CareAI.API
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run --project src/CareAI.API
   ```

## API Documentation

Once the application is running, you can access the Swagger UI at `https://localhost:5001/swagger` or `http://localhost:5000/swagger`

## Project Structure

- **CareAI.API**: Web API project with controllers and startup configuration
- **CareAI.Core**: Domain models, enums, and interfaces
- **CareAI.Infrastructure**: Data access and persistence
- **CareAI.Services**: Business logic and service implementations

## API Endpoints

- `POST /api/conversation/start`: Start a new conversation
  ```json
  {
    "userId": "user-123"
  }
  ```

- `POST /api/conversation/{conversationId}/message`: Send a message
  ```json
  {
    "userId": "user-123",
    "message": "Hello, I need help with housing"
  }
  ```

## Development

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Deployment

The application can be deployed to Azure App Service or any other .NET Core compatible hosting environment. Make sure to configure the appropriate connection strings and API keys in the production environment.
