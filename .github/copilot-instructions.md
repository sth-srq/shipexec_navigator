# Copilot Instructions

## General Guidelines
- **HARD LIMIT: Maximum 7 repair/fix passes.** If code does not compile or tests do not pass after 6 fix attempts, comment out only the minimal lines causing the break with `// NOT WORKING - <explanation of what it was supposed to do>` and move on. Do not loop beyond 7 attempts.
- Keep instructions concise and actionable.
- Use imperative mood for all instructions.
- Group general instructions before project-specific ones.

## Code Style
- Use 1TBS (One True Brace Style) for the brace format of generated CBR JavaScript code.
- Follow language-appropriate formatting and naming conventions for generated code.

## Project-Specific Rules
- **CRITICAL: Each generated class MUST be in its own separate file.** Never put multiple classes in a single file.
- When creating custom classes in the ShipExec SBR project, always include the PSI.Sox namespaces.
- Use proper PSI.Sox types (e.g., Weight is an object, not a number; enums must use PSI.Sox enum types).
- Always check types before assigning variables.
- **SBR/CBR Project Generation Process**:
  1. Copy the entire template folder (`CodeStandards\TemplateCodeShipExec20BusinessRules\`) to the new project directory EXACTLY as-is — including the .csproj and all References DLLs. Do NOT modify anything during the copy.
  2. Make code changes in the copied files as needed.
  3. If new .cs files are added, update the copied .csproj to include `<Compile Include="NewFile.cs" />` entries for them.
  - Steps 2-3 also apply during repair passes. Never regenerate the .csproj from scratch. Never remove existing references.
- **Output file rules**: When generating SBR/CBR project output, do NOT include any .md files except readme.md.