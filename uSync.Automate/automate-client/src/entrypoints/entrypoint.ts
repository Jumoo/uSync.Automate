import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";

// The uSync dashboard itself is entirely server-rendered - this entrypoint exists only so the
// localization bundle below loads. Nothing else to register here in phase 1.
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
};
