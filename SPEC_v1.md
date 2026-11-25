# NeuroBridge Open Protocol Specification v1.0 (NB-SPEC-v1)
## 1. Overview
NeuroBridge is an open, model-agnostic execution and persistent memory protocol linking any Web or Local LLM with Windows OS without mandatory API keys or cloud vendor lock-in.
## 2. Workspace Layout
- `C:\NeuroBridge\_Core\Docs\`: Core rules (`CORE_RULES.md`), architecture (`CORE_ARCHITECTURE.md`), error knowledge base (`CORE_ERRORS.md`).
- `C:\NeuroBridge\_Core\Runner\`: Execution pipeline (`projects.json`, `watcher_go.ps1`, `hotkey_go.ps1`, `go.ps1`, `auto_footer.ps1`, `snapshot_knowledge.ps1`).
- `C:\NeuroBridge\Projects\<id>\`: Isolated project workspaces with dedicated reports (`<id>_report.txt`).
## 3. Execution Contract
- Block tags: `# ЗАДАЧА_ОДИН_БЛОК` (single task), `# БЛОК_N` (intermediate step), `# ФИНАЛ_ШАГ` (final step with footer injection), `# ФОН_ЗАДАЧА` (background STA worker).
- Safety & Determinism: PowerShell AST syntax validation prior to execution, SHA256 deduplication (`last_step.hash`), mutex single-instance lock, and automatic memory/error injection into output reports.
## 4. License & Attribution
Reference implementation and protocol specification are copyright (C) 2026 psytuner (psytuner) and licensed under GNU AGPLv3.

