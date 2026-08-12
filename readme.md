# uSync.Automate

uSync support for [Umbraco.Automate](https://jumoo.co.uk/) — sync Automate workspaces, groups and
automations between environments as XML files, and drive uSync from Automate workflows.

This repo contains two packages:

- **uSync.Automate** — uSync handlers and serializers for Automate's `Workspace`, `WorkspaceGroup`
  and `Automation` settings, so they export to disk and travel between servers the same way
  content types, data types and other uSync-managed settings do.
- **uSync.Automate.Actions** — Umbraco.Automate actions and triggers that run uSync import/export/report
  operations from a workflow, and fire workflows from uSync events.

`Connection`s are deliberately **not** synced — see the design notes below. Automations and
workspaces reference connections by alias and report when one is missing on the target server.

## Status

Early development — not yet released.

## Building

```
dotnet restore
dotnet build
```

See `dist/build-package.ps1` for packaging.

## License

[MPL-2.0](LICENSE)
