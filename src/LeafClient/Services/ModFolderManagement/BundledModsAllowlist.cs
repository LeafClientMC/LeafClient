using System;
using System.Collections.Generic;

namespace LeafClient.Services.ModFolderManagement
{
    public static class BundledModsAllowlist
    {
        public static readonly HashSet<string> ManagedModIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "sodium",
            "lithium",
            "ferritecore",
            "immediatelyfast",
            "entityculling",
            "modernfix",
            "ebe",
            "dynamicfps",
            "dynamic_fps",
            "c2me",
            "cloth-config",
            "cloth-config2",
            "moreculling",
            "krypton",
            "sodium-extra",
            "sodiumextra",
            "cullleaves",
            "debugify",
            "nofade",
            "betterbeds",
            "reeses-sodium-options",
            "starlight",
            "indium",
            "ksyxis",
            "puzzle",
            "vmp",
            "forcecloseloadingscreen",
            "no_double_sneak",
            "nodoublesneak",
            "serverpingfixer",
            "viafabricplus",
            "chestsearchbar",
            "fabric-api",
            "fabricloader",
            "java",
            "minecraft",
            "phosphor",
            "iris",
            "dynamiclights",
            "betterfps",
            "leafclient",
            "owo"
        };

        public static bool IsLauncherManaged(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return false;
            return ManagedModIds.Contains(modId);
        }

        public static readonly HashSet<string> FabricApiSubModules = new(StringComparer.OrdinalIgnoreCase)
        {
            "fabric-api-base",
            "fabric-api-lookup-api-v1",
            "fabric-biome-api-v1",
            "fabric-block-api-v1",
            "fabric-block-view-api-v2",
            "fabric-blockrenderlayer-v1",
            "fabric-client-tags-api-v1",
            "fabric-command-api-v1",
            "fabric-command-api-v2",
            "fabric-commands-v0",
            "fabric-content-registries-v0",
            "fabric-convention-tags-v1",
            "fabric-convention-tags-v2",
            "fabric-crash-report-info-v1",
            "fabric-data-attachment-api-v1",
            "fabric-data-generation-api-v1",
            "fabric-dimensions-v1",
            "fabric-entity-events-v1",
            "fabric-events-interaction-v0",
            "fabric-game-rule-api-v1",
            "fabric-gametest-api-v1",
            "fabric-item-api-v1",
            "fabric-item-group-api-v1",
            "fabric-key-binding-api-v1",
            "fabric-keybindings-v0",
            "fabric-lifecycle-events-v1",
            "fabric-loot-api-v2",
            "fabric-loot-api-v3",
            "fabric-message-api-v1",
            "fabric-model-loading-api-v1",
            "fabric-models-v0",
            "fabric-networking-api-v1",
            "fabric-object-builder-api-v1",
            "fabric-particles-v1",
            "fabric-recipe-api-v1",
            "fabric-registry-sync-v0",
            "fabric-renderer-api-v1",
            "fabric-renderer-indigo",
            "fabric-renderer-registries-v1",
            "fabric-rendering-data-attachment-v1",
            "fabric-rendering-fluids-v1",
            "fabric-rendering-v1",
            "fabric-resource-conditions-api-v1",
            "fabric-resource-loader-v0",
            "fabric-screen-api-v1",
            "fabric-screen-handler-api-v1",
            "fabric-sound-api-v1",
            "fabric-tag-api-v1",
            "fabric-transfer-api-v1",
            "fabric-transitive-access-wideners-v1",
        };
    }
}
