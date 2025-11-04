# Sommerhus Project

## Admin authentication

Both the API and MVC front end rely on a shared `AdminAuth` configuration section. Configure the credentials in `appsettings.json`, environment variables, or user secrets:

```
"AdminAuth": {
  "Username": "admin",
  "Password": "sommerhus123"
}
```

- **Sommerhus.Api** secures `/api/admin/*` endpoints with HTTP Basic authentication. Update `AdminAuth` to control the valid username and password and ensure any external clients send the corresponding `Authorization: Basic ...` header.
- **Sommerhus.Mvc** uses the same credentials for the admin login form and forwards them as Basic auth when calling the API. The MVC site issues its own cookie after a successful login.

For non-development environments, move the credentials into [ASP.NET Core secret storage](https://learn.microsoft.com/aspnet/core/security/app-secrets) or a key vault provider instead of committing them to source control.
