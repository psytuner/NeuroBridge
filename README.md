# NeuroBridge Open Protocol (NB-SPEC-v1)
**Open, model-agnostic execution and persistent memory protocol linking any Web or Local LLM with Windows OS — zero API keys, zero cloud lock-in.**
**Открытый модель-независимый протокол исполнения и долговременной памяти, связывающий любую веб- или локальную LLM с ОС Windows без API-ключей и облачной привязки.**
## Key Features / Ключевые возможности
- **Zero-API Execution Pipeline**: Clipboard AST-validated PowerShell runner (`watcher_go.ps1` + `Alt+E` fallback) with per-project mutex isolation and FIFO block queues.
- **Persistent Context & Self-Learning**: Automatic injection of core rules, project docs, and error knowledge base (`auto_footer.ps1` + `snapshot_knowledge.ps1`).
- **Multi-Project Isolation**: Dedicated workspaces (`Projects/<id>/`), isolated reports, and system tray manager (`NeuroBridgeTray.exe`).
## Specification & License / Спецификация и лицензия
- Protocol Specification: `SPEC_v1.md`
- Author & Copyright: **psytuner** (`https://github.com/psytuner/NeuroBridge`)
- License: **GNU Affero General Public License v3.0 (AGPL-3.0-or-later)** — see `LICENSE` and `NOTICE`.



## Quick Start / Быстрый старт
1. Extract to C:\NeuroBridge\ / Распакуйте архив в C:\NeuroBridge\
2. Run Start_NeuroBridge.cmd / Запустите Start_NeuroBridge.cmd
3. Enable Autostart / Автозапуск in the tray manager / Включите Автозапуск в Диспетчере.
