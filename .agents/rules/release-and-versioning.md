# Rule: Versioning and Thunderstore Packaging

## Mandatory Rules
1. **Triple Version Synchronization**:
   Whenever a version bump occurs, the version string must be updated in all 3 files simultaneously:
   - `ExpandedPlayerInventory.csproj`: `<Version>X.Y.Z</Version>`
   - `Plugin.cs`: `public const string ModVersion = "X.Y.Z";`
   - `manifest.json`: `"version_number": "X.Y.Z"`

2. **Automated Packaging Validation**:
   - Use `powershell -ExecutionPolicy Bypass -File .\package-thunderstore.ps1`.
   - The script builds Release mode, checks for `icon.png` (256x256), `manifest.json`, `README.md`, and outputs `dist/ExpandedPlayerInventory-vX.Y.Z.zip`.
   - Never release without running this packaging script.
