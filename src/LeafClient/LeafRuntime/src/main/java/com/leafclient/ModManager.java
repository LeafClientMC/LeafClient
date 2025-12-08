package com.leafclient;
import com.leafclient.modules.*;
import java.io.File;
import java.util.*;
/**


Central registry & lifecycle handler for all Leaf mods.
*/
public class ModManager {
/* ------------------------------------------------------------

FIELDS
------------------------------------------------------------ */
private final List<ILeafMod> mods = new ArrayList<>();
private final File configDir;

/* ------------------------------------------------------------


CONSTRUCTOR


------------------------------------------------------------ */
public ModManager(File mcDir) {
this.configDir = new File(mcDir, "leafclient");
if (!configDir.exists()) configDir.mkdirs();
/* Register every mod instance here */
register(new FullBrightMod());
register(new FPSMod());
register(new CPSMod());
register(new PingMod());
register(new CoordinatesMod());
register(new PerformanceMod());
register(new ZoomMod());
register(new ToggleSprintMod());
register(new FreelookMod());
register(new MinimapMod());
//register(new ReplayMod());
register(new WaypointsMod());
register(new ArmorHUDMod());
register(new KeystrokesMod());
register(new ItemCounterMod());
register(new ChatMacrosMod());
register(new CrosshairMod());
register(new ServerInfoMod());
register(new LeafLogoMod());
/* Sync enabled flags with settings file */
updateModStatesFromFile();
}


/* ------------------------------------------------------------

REGISTRATION
------------------------------------------------------------ */

public void register(ILeafMod mod) {
mods.add(mod);
}
/* ------------------------------------------------------------

ACCESSORS
------------------------------------------------------------ */

/** Immutable list of all loaded mods. */
public List<ILeafMod> getMods() {
return Collections.unmodifiableList(mods);
}
/** Fetch the first mod that is an instance of {@code clazz}. */
@SuppressWarnings("unchecked")
public <T extends ILeafMod> T getMod(Class<T> clazz) {
for (ILeafMod m : mods) if (clazz.isInstance(m)) return (T) m;
return null;
}
/** Fetch a mod by its display name (case-insensitive). */
public ILeafMod getMod(String name) {
for (ILeafMod m : mods)
if (m.getName().equalsIgnoreCase(name)) return m;
return null;
}
public File getConfigDir() { return configDir; }
/* ------------------------------------------------------------

SETTINGS SYNC
------------------------------------------------------------ */

/** Load enabled/disabled flags from the on-disk settings file. */
@SuppressWarnings("unchecked")
public void updateModStatesFromFile() {
    Map<String, Object> root = ModSettingsFileManager.readModSettings();
    for (ILeafMod mod : mods) {
        String key = mod.getName().toUpperCase();
        if (root.containsKey(key) && root.get(key) instanceof Map) {
            Object enabled = ((Map<?, ?>) root.get(key)).get("enabled");
            if (enabled instanceof Boolean flag) {
                mod.setEnabled(flag);
            }
        }
        
        // Reload ItemCounter settings specifically
        if (mod instanceof ItemCounterMod) {
            ((ItemCounterMod) mod).loadSettings();
        }
    }
}
}

