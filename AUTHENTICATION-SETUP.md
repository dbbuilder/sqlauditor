# SQL Analyzer Authentication Setup

## Overview

SQL Analyzer now includes basic username/password authentication using JWT tokens. This provides initial protection for the application and can be expanded later.

## Default Credentials

- **Username**: `admin`
- **Password**: `SqlAnalyzer2024!`

## How It Works

### Backend (API)
1. **JWT Authentication**: Uses ASP.NET Core JWT Bearer authentication
2. **Login Endpoint**: `/api/v1/auth/login` accepts username/password
3. **Token Generation**: Returns JWT token valid for 24 hours
4. **Protected Endpoints**: All API endpoints require valid JWT token (except login)
5. **SignalR Support**: JWT tokens work with SignalR connections

### Frontend (Vue)
1. **Login Page**: Redirects unauthenticated users to `/login`
2. **Auth Store**: Pinia store manages authentication state
3. **Token Storage**: JWT stored in localStorage
4. **Auto-logout**: Tokens expire after 24 hours
5. **API Integration**: Axios automatically includes JWT in headers

## Configuration

### API Configuration (appsettings.json)
```json
{
  "Authentication": {
    "JwtSecret": "",  // Auto-generated if not set
    "JwtExpirationHours": 24,
    "DefaultUsername": "admin",
    "DefaultPassword": "SqlAnalyzer2024!"
  }
}
```

### Environment Variables
For production, set these environment variables:
- `Authentication__JwtSecret`: Your secure JWT signing key
- `Authentication__DefaultUsername`: Admin username
- `Authentication__DefaultPassword`: Admin password

## Security Features

1. **Password Protection**: All endpoints require authentication
2. **Token Expiration**: Tokens expire after 24 hours
3. **Secure Headers**: CORS configured for specific origins
4. **Auto-logout**: Invalid tokens redirect to login
5. **SignalR Auth**: Real-time connections require valid token

## Usage

### Initial Login
1. Navigate to the application
2. You'll be redirected to `/login`
3. Enter default credentials
4. Click "Sign In"

### API Authentication
Include JWT token in requests:
```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  https://sqlanalyzer-api.azurewebsites.net/api/v1/analysis/types
```

### Logout
Click the "Logout" button in the navbar to clear authentication.

## Future Enhancements

This basic authentication can be expanded to include:
1. User registration
2. Role-based access control (RBAC)
3. Password reset functionality
4. Multi-factor authentication (MFA)
5. OAuth2/OpenID Connect integration
6. User management interface
7. Audit logging

## Deployment Notes

### Azure App Service
Add these application settings:
- `Authentication__JwtSecret`: Generate a secure 64+ character key
- `Authentication__DefaultPassword`: Change from default

### Local Development
The default credentials work out of the box for development.

## Troubleshooting

### "Unauthorized" Errors
1. Check token hasn't expired (24 hours)
2. Verify credentials are correct
3. Clear browser cache and localStorage
4. Check API logs for authentication errors

### CORS Issues with Auth
Ensure allowed origins include your frontend URL in:
- API CORS configuration
- Azure Portal CORS settings (should be empty)