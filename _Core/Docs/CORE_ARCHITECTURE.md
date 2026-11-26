# [CLASS: CORE_MODE]
# [CORE_ARCHITECTURE] NEUROBRIDGE CORE & MULTI-PROJECT PIPELINE ARCHITECTURE
## 1. PORTABLE ENVIRONMENT (C:\NEUROBRIDGE)
- Root: `README.md`, `start_new_chat.txt`, `NeuroBridge_report.txt`, `NeuroBridge_error.txt`, `NeuroBridge_Manager.lnk`, `Start_NeuroBridge.cmd`.
- `_Core\Docs\`: `CORE_RULES.md`, `CORE_SCRIPT_GUIDE.md`, `CORE_STYLE_UI.md`, `CORE_ARCHITECTURE.md`, `CORE_TERMINOLOGY.md`, `CORE_ERRORS.md`, `CORE_CHANGELOG.md`, `CORE_BACKLOG.md`.
- `_Core\Runner\`: `projects.json`, `watcher_go.ps1`, `hotkey_go.ps1`, `go.ps1`, `auto_footer.ps1`, `snapshot_knowledge.ps1`, `log_chat.ps1`, `progress_ui.ps1`, `check_error_class.ps1`, `audit_docs.ps1`, `_sounds\`, `_launcher_golden\`.
- `_Core\TrayApp\`: `NeuroBridgeTray.csproj`, `Program.cs`, binary `App\NeuroBridgeTray.exe`.
- `Projects\<id>\`: isolated projects with `<id>_report.txt`, `02_Docs\` (`DOC_*.md`, `PROJECT_ERRORS.md`, `DIALOG_LOG.md`, `_dialog_history\`).
## 2. NEW DIALOG PROTOCOL (3 STEPS)
- Step 1 (`start_new_chat.txt`): NEVER start tasks; confirm Core rules, name last active project, ask whether to continue or switch.
- Step 2 (project confirmed): read ALL `<Path>\02_Docs\DOC_*.md` completely + tail of `DIALOG_LOG.md` via read-only script.
- Step 3 (full project docs received): confirm exact breakpoint and proceed.
## 3. PIPELINE, CONCURRENCY & AUTO-ERROR REPORTING
- Manager (`projects.json -> active`) belongs strictly to `NeuroBridgeTray.exe`; scripts set `$out` directly.
- Chain: `watcher_go.ps1` (AST check `Test-Ok`) / `hotkey_go.ps1` (Alt+E) -> `go.ps1` (`Global\NeuroBridge_Go_Lock_<id>`, FIFO `queue_<chanId>`, `step_<id>.ps1`) -> `auto_footer.ps1` -> `snapshot_knowledge.ps1`.
- Auto-error report: on runtime crash (`go.ps1` catch) or AST syntax break (`watcher_go.ps1`), system writes `NeuroBridge_error.txt` + Core docs, copies it to clipboard, and lights up the red button in Manager (`error_report_ready.flag`).
- Auto-doc classification: every `.md` declares `# [CLASS: ALWAYS|CORE_MODE|ON_ERROR|ON_AUDIT|ON_MENTION]` on line 1; `auto_footer.ps1` matches `ON_MENTION` via normalized filenames (stripping `_` and `-`).
- Return stack: `_Core\Logs\active_bookmark.json` (`PAUSED BOOKMARK:`) & `_Core\Logs\error_resume.json` (`ERROR RESUME:`).

- Backups layout (_Backups\): Core_Backups\Regular\, Core_Backups\Golden\, Projects\<id>\ (active snapshots), _Trash\ (legacy/doubtful archives). Active mod golden releases live inside Projects\VS_Mods\03_Tools\_releases\golden\.

