# [CLASS: ALWAYS]
# [CORE_TERMINOLOGY] NEUROBRIDGE SYSTEM TERMINOLOGY
- **NeuroBridge** (`C:\NeuroBridge\`): portable external memory & local Windows automation bridge between LLM and OS.
- **Core** (`_Core\`): shared engine (`_Core\Docs\`, `_Core\Runner\`, `_Core\TrayApp\`) governing all projects.
- **Project Manager** (`NeuroBridgeTray.exe`): system tray GUI for switching projects, creating projects, syncing, and pipeline control.
- **Pipeline / Runner** (`go.ps1` + `auto_footer.ps1` + `snapshot_knowledge.ps1`): script executor with UI toasts, sounds, and auto-appended memory.
- **Auto-Watcher** (`watcher_go.ps1`): clipboard monitor with PowerShell AST syntax check and automatic execution.
- **Manual Hotkey** (`hotkey_go.ps1`, Alt+E): fallback manual script trigger from clipboard.
- **Anti-Duplicate** (`last_step.hash` / `last_watcher.hash`): SHA256 guard against re-running the same block; reset via Manager button.
- **Project Report** (`<id>_report.txt`, e.g., `VS_report.txt`): isolated output file per project placed into clipboard automatically.
- **Error Report** (`C:\NeuroBridge\NeuroBridge_error.txt`): isolated crash/AST error report auto-copied on failure.
- **Start Snapshot** (`C:\NeuroBridge\start_new_chat.txt`): auto-updated Core bootstrap file for new chat sessions.
- **Golden Snapshot** (`_launcher_golden\`): verified backup snapshots of working Runner, Manager, and Core docs.
