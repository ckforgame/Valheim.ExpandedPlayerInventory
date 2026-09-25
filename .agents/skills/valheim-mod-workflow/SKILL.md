---
name: valheim-mod-workflow
description: >-
  Workflows, runbooks, and procedures for building, debugging, extending, and packaging ExpandedPlayerInventory.
  Activate when compiling the mod, adding configuration settings, modifying Harmony patches, or preparing releases.
---

# Valheim Mod Development Workflow

This skill guides AI agents through the complete lifecycle of developing, verifying, and packaging the **ExpandedPlayerInventory** mod.

---

## 1. Building and Verification

### Standard Build
Run in the project root:
```powershell
dotnet build -c Release
```
- Verify `0 Warning(s)`, `0 Error(s)`.
- Output binary is generated at `bin/Release/ExpandedPlayerInventory.dll`.

### Custom Valheim / BepInEx Paths
If building in an environment where Valheim is installed on a non-standard drive:
```powershell
dotnet build -c Release -p:ValheimPath="<PathToValheim>"
```

---

## 2. Testing Locally

1. Copy the built DLL:
   ```powershell
   Copy-Item "bin\Release\ExpandedPlayerInventory.dll" -Destination "<ValheimPath>\BepInEx\plugins\" -Force
   ```
2. Enable console logging in `<ValheimPath>\BepInEx\config\BepInEx.cfg`:
   ```ini
   [Logging.Console]
   Enabled = true
   ```
3. Look for the startup log:
   ```text
   ExpandedPlayerInventory 1.2.0 loaded successfully! Configured rows: 20...
   ```

---

## 3. Extending Configuration

1. In [Plugin.cs](../../../Plugin.cs):
   - Declare:
     ```csharp
     public static ConfigEntry<T> NewConfig { get; private set; } = null!;
     ```
   - Bind in `Awake()`:
     ```csharp
     NewConfig = Config.Bind("General", "newConfigKey", defaultValue, new ConfigDescription("Description"));
     ```
2. Use in patch logic via `ExpandedPlayerInventoryPlugin.NewConfig.Value`.
3. Document the new setting in [README.md](../../../README.md).

---

## 4. Packaging and Releasing

When ready to package for Thunderstore:
1. Ensure version is updated across:
   - [ExpandedPlayerInventory.csproj](../../../ExpandedPlayerInventory.csproj) (`<Version>`)
   - [Plugin.cs](../../../Plugin.cs) (`ModVersion`)
   - [manifest.json](../../../manifest.json) (`version_number`)
2. Run the packaging script from the project root:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\package-thunderstore.ps1
   ```
3. Inspect `dist/` to confirm the `.zip` archive was generated cleanly.
