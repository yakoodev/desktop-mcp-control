# Contributing

Thanks for helping improve Desktop MCP Control.

## Local setup

```powershell
dotnet restore DesktopMcp.slnx
dotnet build DesktopMcp.slnx
dotnet test DesktopMcp.slnx
```

## Pull requests

- Keep changes focused and easy to review.
- Add or update tests for runtime, settings, and authorization behavior when possible.
- For UI changes, include a screenshot or short screen recording in the pull request.
- Do not commit local settings, secrets, build output, or files from `artifacts/`.

## Coding style

The repository uses `.editorconfig` for baseline formatting. Prefer clear, small methods and explicit names over clever shortcuts.
