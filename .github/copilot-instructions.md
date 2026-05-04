# Copilot Instructions

## General Guidelines
- Keep instructions concise and actionable.
- Use imperative mood for all instructions.
- Group general instructions before project-specific ones.

## Code Style
- Use 1TBS (One True Brace Style) for the brace format of generated CBR JavaScript code.
- Follow language-appropriate formatting and naming conventions for generated code.

## Project-Specific Rules
- When creating custom classes in the ShipExec SBR project, always include the PSI.Sox namespaces.
- Use proper PSI.Sox types (e.g., Weight is an object, not a number; enums must use PSI.Sox enum types).
- Always check types before assigning variables.