# uSync.Automate

uSync handlers and serializers for [Umbraco.Automate](https://jumoo.co.uk/) workspaces, groups
and automations — export them to disk as XML and move them between environments the same way
uSync moves content types, data types and other settings.

`Connection`s are not synced (each environment owns its own connections); automations and
workspaces reference connections by alias and report when one is missing on the target server.

See [uSync.Automate.Actions](https://www.nuget.org/packages/uSync.Automate.Actions) for
Umbraco.Automate actions/triggers that run uSync operations from a workflow.

## License

MPL-2.0
