# Rule: PowerShell Script Execution

## Context
When running complex or multi-line PowerShell inspection commands, avoid passing inline blocks which can suffer from escaping, quoting, or argument parsing errors.

## Mandatory Rule
### Execution Rule
Do NOT execute long PowerShell inspection scripts directly via inline `powershell -Command @"..."`.
Instead, always follow these 2 steps:
1. Write the script to a fixed file: `temp_inspect.ps1`
2. Execute it using this exact command: `powershell -ExecutionPolicy Bypass -File temp_inspect.ps1`
