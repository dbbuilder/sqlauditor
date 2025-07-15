# SendGrid Email Configuration

## Environment Variables

The application supports the following environment variables for email configuration:

```bash
# Required for email functionality
SENDGRID_API_KEY=your-sendgrid-api-key-here

# Optional overrides
EMAIL_ENABLED=true
EMAIL_FROM=noreply@yourdomain.com
```

## Setting Environment Variables

### For Development (Local)

#### Windows PowerShell:
```powershell
$env:SENDGRID_API_KEY="your-api-key"
$env:EMAIL_ENABLED="true"
```

#### Windows Command Prompt:
```cmd
set SENDGRID_API_KEY=your-api-key
set EMAIL_ENABLED=true
```

#### Linux/Mac:
```bash
export SENDGRID_API_KEY="your-api-key"  
export EMAIL_ENABLED="true"
```

### For Azure App Service

1. Go to your App Service in Azure Portal
2. Navigate to Configuration > Application settings
3. Add new application settings:
   - Name: `SENDGRID_API_KEY`
   - Value: Your SendGrid API key
   - Name: `EMAIL_ENABLED`
   - Value: `true`

### Using .env file (Development)

Create a `.env` file in the API project root:
```
SENDGRID_API_KEY=your-api-key
EMAIL_ENABLED=true
EMAIL_FROM=noreply@yourdomain.com
```

## Configuration Priority

The application checks configuration in this order:
1. Environment variables (highest priority)
2. appsettings.{Environment}.json
3. appsettings.json (lowest priority)

## SendGrid Setup

1. Create a SendGrid account at https://sendgrid.com
2. Generate an API key with "Mail Send" permissions
3. Verify your sender email address or domain
4. Set the API key using one of the methods above

## Security Notes

- **NEVER** commit API keys to source control
- Use environment variables or Azure Key Vault for production
- Regenerate API keys if they are ever exposed
- Use different API keys for different environments

## Testing Email

To test if email is working:
1. Ensure `EMAIL_ENABLED` is set to `true`
2. Set a valid `SENDGRID_API_KEY`
3. Run an analysis to completion
4. Check the logs for email sending status

## Troubleshooting

If emails are not sending:
1. Check application logs for "Email service initialized with SendGrid"
2. Verify the API key is correct
3. Ensure sender email is verified in SendGrid
4. Check SendGrid dashboard for blocked emails or errors