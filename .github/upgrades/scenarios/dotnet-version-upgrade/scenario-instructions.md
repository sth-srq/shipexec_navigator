# Scenario Instructions: .NET Version Upgrade

## Scenario Parameters
- **Target Framework**: net10.0 (.NET 10.0 LTS)
- **Solution**: ShipExecNavigator.slnx

## Preferences

### Flow Mode
**Automatic** — Run end-to-end, only pause when blocked or needing user input.

### Source Control
- **Source branch**: `Test`
- **Working branch**: `upgrade-to-NET10`

## User Preferences

### Technical Preferences
- Upgrade ALL projects to .NET 10

### Execution Style
- Automatic flow mode (user requested "Make every project .NET 10")

## Strategy
**Selected**: All-at-Once
**Rationale**: 10 projects total; only 3 legacy ClassicClassLibrary projects need migration work; remaining 7 already target net10.0.

### Execution Constraints
- All projects upgraded in a single atomic operation — no tier gating
- Convert legacy projects to SDK-style before TFM change
- Process dependency order within legacy projects: Model → BusinessLogic
- Full solution build validates after all changes complete
- Remove Microsoft.AspNet.Identity.Core (no compatible .NET 10 version exists)

## Preferences

### Flow Mode
**Automatic** — Run end-to-end, only pause when blocked or needing user input.

### Commit Strategy
**Single Commit at End** — All-at-Once strategy; one atomic upgrade, one commit.

### Source Control
- **Source branch**: `Test`
- **Working branch**: `upgrade-to-NET10`

## User Preferences

### Technical Preferences
- Upgrade ALL projects to .NET 10

### Execution Style
- Automatic flow mode (user requested "Make every project .NET 10")

## Key Decisions Log
- 2025-07-15: User requested all projects be upgraded to .NET 10 (LTS). Working branch: upgrade-to-NET10.
- 2025-07-15: Strategy selected: All-at-Once. Commit strategy: Single Commit at End.
