# NeuroBridge Open Protocol Specification v1.1 (NB-SPEC-v1)
## 1. Overview & System Requirements
NeuroBridge is an open, model-agnostic execution and persistent memory protocol linking any Web or Local LLM with Windows OS without API keys or cloud lock-in.
- Platform: Windows 10 (1607+) / Windows 11 x64, .NET 10.0 Desktop Runtime (`net10.0-windows`), Windows PowerShell 5.1.
## 2. Workspace Layout
- `_Core\Docs\`: Core rules (`CORE_RULES.md`), script canon (`CORE_SCRIPT_GUIDE.md`), architecture (`CORE_ARCHITECTURE.md`), UI standard (`CORE_STYLE_UI.md`), terminology (`CORE_TERMINOLOGY.md`), error base (`CORE_ERRORS.md`).
- `_Core\Runner\`: Execution pipeline (`projects.json`, `watcher_go.ps1`, `hotkey_go.ps1`, `go.ps1`, `auto_footer.ps1`, `snapshot_knowledge.ps1`, `new_project.ps1`, `progress_ui.ps1`, `log_chat.ps1`).
- `_Core\TrayApp\`: System tray manager (`Program.cs`, `NeuroBridgeTray.csproj`, `App\NeuroBridgeTray.exe`) with live Memory Map and error indicator.
- `Projects\<id>\`: Isolated project workspaces (`01_Work\`, `02_Docs\`, `03_Tools\`, `<id>_report.txt`).
## 3. Execution & Memory Contract
- Block tags: `# TASK_SINGLE` (single task), `# BLOCK_N` (intermediate step), `# FINAL_STEP` (final step with footer injection), `# BG_TASK` (background STA worker).
- Document classification: line-1 `# [CLASS: ALWAYS|CORE_MODE|ON_ERROR|ON_AUDIT|ON_MENTION]` governs deterministic footer assembly (`last_manifest.json`).
- Error & Abort recovery: runtime exceptions, AST syntax breaks, or user abort emit `NeuroBridge_error.txt` with Core docs into clipboard and flag the Manager UI.
## 4. License & Attribution
Copyright (C) 2026 psytuner — licensed under GNU AGPL-3.0-or-later.


