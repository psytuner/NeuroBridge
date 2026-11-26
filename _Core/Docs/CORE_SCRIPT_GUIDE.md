# [CLASS: CORE_MODE]
# [CORE_SCRIPT_GUIDE] SCRIPTING CANON & ERROR ANTIDOTES
## 1. BLOCK HEADERS & SIZE LIMITS
- Line 1 tag (`^\s*#.*_`): `# TASK_SINGLE` (single), `# BLOCK_1..# BLOCK_N` (intermediate, `block_ok.wav`), `# FINAL_STEP` (final, `task_ok.wav` + clipboard), `# BG_TASK` (background STA, no `$out`).
- Strict limits against chat split: `<=22` lines, `<=1800` chars per block, line length `<=110` chars, ZERO square brackets `[ ]`, ZERO backticks, ZERO blank lines inside code.
- PowerShell 5.1 syntax: closing `@` of here-string at column 0; never run `ConvertFrom-Json` on JSON5 (`//` comments); change block title on re-run to bypass `last_step.hash`; never use `https` literal (build via `UriBuilder`).
- Output: write via `Out-File $out -Encoding UTF8` / `Add-Content $out -Encoding UTF8`; end with `& "$ws\_Core\Runner\auto_footer.ps1"`.
- SPDX License Header: any new .ps1 or .cs created in _Core\ MUST immediately start with SPDX-License-Identifier: AGPL-3.0-or-later + Copyright (C) 2026 psytuner.
## 2. VERIFIED TOOL REGISTRY
- Core (`_Core\Runner\`): `go.ps1`, `auto_footer.ps1`, `snapshot_knowledge.ps1`, `log_chat.ps1`, `progress_ui.ps1`, `check_error_class.ps1`, `audit_docs.ps1`.
## 3. ERROR PROTOCOL & ANTIDOTES
- On error: pause project -> switch `$out` to `NeuroBridge_report.txt` -> read `last_step_error.log` & `runner_actions.log` -> log `[В РАБОТЕ] [КЛАСС: ...]` in `CORE_ERRORS.md` + `check_error_class.ps1` -> fix -> verify in report -> mark `[ЗАКРЫТО]` + `- Лечение:` + 1 antidote below.
- Antidotes: save `.ps1` with UTF-8 BOM (`Out-File -Encoding UTF8`); always check `Test-Path` & `if ($txt)` after `Get-Content -Raw`; iterate `Get-Content` with `foreach ($ln in ...)` (never `[$i]` on scalar); pass short folder name only to `pack_mod.ps1 -ModFolder`; use `-ModId` on `download_mod.ps1`; never load .NET 10 game DLLs in PS 5.1; insert into `auto_footer.ps1` only after `$null = $sb.AppendLine($errOut)`.


