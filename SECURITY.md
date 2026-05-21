# Security Policy

Desktop MCP Control can control the local desktop through MCP tools, so treat endpoint exposure carefully.

## Supported versions

The project is pre-1.0. Security fixes target the latest `main` branch until versioned releases are introduced.

## Reporting a vulnerability

Please report security issues privately if possible. If private reporting is not configured for the GitHub repository yet, open a minimal public issue without exploit details and ask for a private contact channel.

## Operational guidance

- Keep the endpoint bound to `127.0.0.1` unless remote access is explicitly required.
- Enable bearer token authorization before binding to external interfaces.
- Use firewall rules to restrict access when listening outside localhost.
- Rotate the token if it may have been exposed.
