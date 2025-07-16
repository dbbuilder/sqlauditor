# Test Email Endpoint

To test if the email endpoint is now available, you can use these commands:

## 1. First, get a fresh token:

```bash
curl -X POST https://sqlanalyzer-api-win.azurewebsites.net/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"AnalyzeThis!!\"}"
```

## 2. Then test the email endpoint:

Replace `YOUR_TOKEN` with the token from step 1:

```bash
curl -X POST https://sqlanalyzer-api-win.azurewebsites.net/api/v1/email/test \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d "{\"email\":\"your.email@example.com\"}"
```

## 3. Check email status:

```bash
curl -H "Authorization: Bearer YOUR_TOKEN" \
  https://sqlanalyzer-api-win.azurewebsites.net/api/v1/email/status
```

## Expected Results:

- If the endpoint returns 404: The EmailController hasn't been deployed yet
- If it returns 200: The email test was sent successfully
- If it returns 401: The token is invalid or expired