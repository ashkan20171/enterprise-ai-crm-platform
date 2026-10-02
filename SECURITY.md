# Security Notes

The project demonstrates defense-in-depth patterns including ASP.NET Core Identity, role authorization, server-side data-scope enforcement, anti-forgery protection on state-changing MVC forms, login auditing, rate limiting, security headers, audit events and governance visibility.

## Production checklist
- Move all secrets to a managed secret store/environment configuration.
- Replace development/demo credentials.
- Enforce HTTPS/HSTS at the deployment edge.
- Review reverse-proxy forwarded-header configuration.
- Configure least-privilege SQL credentials.
- Add automated dependency, SAST and integration-security checks.
- Define retention, backup and restore procedures.
- Review logs for PII and secret leakage.
