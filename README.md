# Birdsoft Infrastructure

This repository contains shared infrastructure components used by Birdsoft services.

## Coverage

Windows (PowerShell):

- `pwsh -NoProfile -ExecutionPolicy Bypass -File .\build\coverage.ps1`

Outputs:

- HTML report: `artifacts/coverage/index.html`
- Summary text: `artifacts/coverage/Summary.txt`
- Raw Cobertura XML(s): `artifacts/test-results/**/coverage.cobertura.xml`

Notes:

- Runs tests in Release (`-c Release`).
- Excludes `*.Tests.*` assemblies and `Program.cs` by default.
- Also excludes file patterns that are typically data-only (`*Dto.cs`, `*Options.cs`, `*Request.cs`, `*Response.cs`) so coverage focuses on behavior; adjust `build/coverage.runsettings` if you want strict line coverage.
