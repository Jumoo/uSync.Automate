const localizations: UmbExtensionManifest = {
  type: "localization",
  alias: "uSync.Automate.localization.en",
  name: "uSync Automate English Localization",
  weight: 100,
  meta: {
    culture: "en",
  },
  js: () => import("./en.js"),
};

export const manifests: Array<UmbExtensionManifest> = [localizations];
