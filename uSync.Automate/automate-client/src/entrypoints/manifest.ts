export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "backofficeEntryPoint",
    name: "uSync.Automate Entrypoint",
    alias: "uSync.Automate.Entrypoint",
    js: () => import("./entrypoint.js"),
  },
];
