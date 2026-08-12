# uSync.Automate.Actions

Umbraco.Automate actions and triggers for uSync:

- **Actions** - run a uSync export, import or report from an Automate workflow (`uSync.export`,
  `uSync.import`, `uSync.report`), or target a single item (`uSync.exportItem`, `uSync.importItem`).
- **Triggers** - start a workflow when a uSync import/export/report begins or completes, or
  (opt-in, see below) when an individual item is imported or exported.

Does not reference [uSync.Automate](https://www.nuget.org/packages/uSync.Automate) - this package
runs uSync operations from a workflow; that one syncs Automate's own settings. Install either or
both depending on what you need.

## Loop protection

A uSync import can fire thousands of Umbraco content-saved notifications, which can in turn
trigger Automate content automations. If those automations call back into uSync (an export, say),
the two systems can cascade into each other. This package guards against that in its own actions
and triggers, but **Automate's built-in content triggers have no equivalent guard when their
settings are empty** - see the project's risk log. If you run uSync imports on a site that also
uses Automate content triggers, set `OnAutomationOriginatedEvent` to `Skip Entirely` on those
triggers.

## License

MPL-2.0
