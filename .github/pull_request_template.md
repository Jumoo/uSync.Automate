## What does this change?

<!-- A sentence or two on the change and why it is needed. -->

## Notes for the reviewer

<!-- Anything non-obvious: behaviour changes, things you decided against, areas you want a
     second opinion on. Delete if there is nothing to say. -->

## Checklist

- [ ] `dotnet build uSync.Automate.slnx -c Release` is clean
- [ ] `dotnet test uSync.Automate.Tests/uSync.Automate.Tests.csproj -c Release` passes
- [ ] `dotnet test uSync.Automate.Tests.Complete/uSync.Automate.Tests.Complete.csproj -c Release` passes
- [ ] If an action/trigger alias, settings field, or output shape changed, any readme examples
      for that package still match
