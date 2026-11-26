# [CLASS: ALWAYS]
# [CORE_RULES] SUPREME LAWS & INVARIANTS OF NEUROBRIDGE
## 0. SUPREME PRIORITY, LANGUAGE & PROJECT ISOLATION
- MANDATORY CHAT LANGUAGE: Follow AI CHAT LANGUAGE in report header (synced with Manager RU/EN switch) and user's dialogue language (never switch to English because internal docs are in English).
- Core priority & project switch: `_Core\Docs\*` govern every step in every project. Core project is `NeuroBridge_Core` (`NeuroBridge_report.txt`). On switching to any project `<id>`, AI MUST first read ALL `<Path>\02_Docs\DOC_*.md` completely and output strictly to `<id>_report.txt`.
- Direct answer first: on any user question, complaint, or error, answer directly in paragraph 1 before any script; zero declarative promises ('I promise', 'from now on').
- Mandatory doc read on questions: NEVER guess about project rules, docs, or AI behavior — issue a read-only script reading `CORE_*.md` / `DOC_*.md` first.
- Mandatory project changelog: EVERY project MUST keep `02_Docs\DOC_PROJECT_CHANGELOG.md` (`# [CLASS: TAIL_15]`), created automatically by `new_project.ps1` and updated after every feature, fix, or release step.
- Isolated logs & backups: `DIALOG_LOG.md`, `_dialog_history\HISTORY_YYYY-MM-DD.md`, and `_dialog_history\backups\` save strictly into the active `$out` project folder.
- Audit & live spec sync: before editing `.md`, check duplicates/stale links; any Core feature/structure change MUST immediately update `CORE_ARCHITECTURE.md`, `CORE_CHANGELOG.md`, and `Projects\NB_Protocol\01_Spec\USER_GUIDE.md` + `SPEC_v1.md`.
- Sovereignty & 2-phase loop: Core laws override project rules. Phase 1 (Brainstorming): new tasks, bugs, or rule proposals run iterative discussion + 'fixation comma' draft scripts until explicit user approval. Phase 2 (Execution): execute project pipeline from `02_Docs\DOC_*RULES.md`.
- Return stack: when leaving an active project `<id>` for Core or another project, update `HANDOFF` in `DOC_*RUNTIME.md` and write `_Core\Logs\active_bookmark.json` (`PAUSED BOOKMARK:` shown in `CORE ERROR MONITOR` until return).
## 1. PIPELINE & SCRIPT EXECUTION LAWS
- Mandatory script per turn: every AI response with a task runs one script (`# TASK_SINGLE` or series `# BLOCK_1..# FINAL_STEP`) and ONE `auto_footer.ps1` call; pure text/brainstorming turns MUST still emit a lightweight 'fixation comma' script so no turn lacks a fresh report.
- No extra main threads: NEVER spawn nested `Start-Process` over `step.ps1` or duplicate main threads.
- Zero desync: NEVER interpret results or issue next step before receiving user's `<id>_report.txt`.
- Screenshot split recovery: on code split screenshot at block N, resume strictly from block N without repeating 1..N-1.
- Pipeline failure protocol: if `watcher_go`/`go.ps1` failed (stale report header), stop mentioning Alt+E, switch to manual PowerShell (`Win+R -> powershell -> RMB -> Enter`), diagnose `runner_actions.log` / `last_step_error.log` / `*.lock` before any backup restore, and backup `_Core\Runner\*.ps1` to `_launcher_golden\` before upgrades.
## 2. CODE PRECISION, SAFETY & GAME INTEGRITY
- C# safety: NEVER truncate `.cs` by line numbers (`Select-Object -First N`) or blind regex `.Replace` without reading signatures; rewrite whole methods/files with balanced `{ }` before `dotnet build`.
- Forbidden actions: NEVER add delete-project/data buttons in `NeuroBridgeTray.exe`; NEVER delete user files outside `_tmp_*`; NEVER run or propose exports without explicit user request; NEVER ask user to attach secondary logs (only `<id>_report.txt` or `start_new_chat.txt`).
- Game & mod rules: check game process via `Get-Process` (never ask user); status stays `READY FOR TEST` until user confirms in-game test; batch related fixes before testing; fix entire feature class via reference comparison; polite web scraping (`>=1.2s` delay, `curl.exe -L --fail --silent`).
- Error lock (`CORE_ERRORS.md` + `PROJECT_ERRORS.md`): ignore `=== SIMULATED TEST ERROR` headers (`# TEST_ERROR_SIMULATION`); on real errors pause task -> log `[В РАБОТЕ] [КЛАСС: ...]` + `error_resume.json` + `check_error_class.ps1` -> fix -> confirm in report -> close `[ЗАКРЫТО]` + `- Лечение:` + finish remaining blocks.









