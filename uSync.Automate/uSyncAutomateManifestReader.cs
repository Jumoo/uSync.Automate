using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace uSync.Automate;

/// <summary>
/// Server-driven backoffice manifest for uSync.Automate - a single bundle that ships the
/// localization overrides in <c>automate-client/src/lang/en.ts</c> so item type names render
/// correctly in the uSync dashboard's group/results views. Mirrors uSync.Forms'
/// <c>uSyncFormsManifestReader</c>.
/// </summary>
internal class uSyncAutomateManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        PackageManifest manifest = new()
        {
            Id = "uSync.Automate",
            Name = "uSync.Automate",
            AllowTelemetry = true,
            Version = "1.0.0",
            Extensions =
            [
                new JsonObject
                {
                    ["name"] = "uSync.Automate bundle",
                    ["alias"] = "uSync.Automate.Bundle",
                    ["type"] = "bundle",
                    ["js"] = "/App_Plugins/uSync.Automate/bundle.js",
                },
            ],
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
