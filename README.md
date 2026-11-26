**Language:** English | [Русский](README.ru.md)

# NeuroBridge Open Protocol (v1.1.0)

**Turn any Web or Local LLM into a stateful Windows partner with persistent memory and real hands — zero API keys, zero cloud lock-in.**

## Why NeuroBridge Exists (Solving Real Web AI Limits)
Browser AI chats (ChatGPT, Claude, Gemini, DeepSeek, Qwen) are powerful, but out of the box they hit hard walls during real engineering:
- **Session Amnesia & No Disk Access**: Every new chat starts blank and cannot see your files unless you manually copy-paste context over and over.
- **Context Fatigue in Long Threads**: Once a conversation grows huge, models slow down, lose agreements, and start making avoidable mistakes.
- **Browser Code-Block Splits & Escaping Issues**: Long scripts pasted in web UIs get truncated or mangled by markdown parsers.
- **Language Drift on Errors**: Because models are heavily English-trained, complex errors can cause internal reasoning to drift into English.

**NeuroBridge solves all four at the protocol level:**
1. **Deterministic Disk Memory (`auto_footer.ps1`)**: Attaches only the exact Core laws and project docs (`# [CLASS: ...]`) needed for the active workspace into `<id>_report.txt`.
2. **Co-Designed Project Rules & Gates**: You and the AI define your project's own laws (`02_Docs\DOC_PROJECT_RULES.md`) and testing gates so the model enforces quality automatically.
3. **Instant Fresh-Chat Handoff (`start_new_chat.txt`)**: Whenever a thread gets heavy, open a new chat, attach `start_new_chat.txt`, and continue without losing a single rule or milestone.
4. **Safe Local Execution & Auto-Diagnosis**: PowerShell AST syntax gate (`watcher_go.ps1`), 60fps progress bar with instant **Abort**, automatic `NeuroBridge_error.txt` capture, and Tray `RU / EN` language anchoring.

## System Requirements & Quick Start
- **OS & Runtime**: Windows 10 (1607+) / Windows 11 (x64), **.NET 10.0 Desktop Runtime** (`net10.0-windows`), Windows PowerShell 5.1.
1. Unpack to `C:\NeuroBridge` and run **`Start_NeuroBridge.cmd`** to launch **`NeuroBridgeTray.exe`** in the system tray (starts muted with **`Demo Project`** active).
2. Open a fresh LLM chat and attach **`start_new_chat.txt`** from the root folder.
3. Click **Copy** on any PowerShell block from the AI — `watcher_go.ps1` runs it automatically (or press **Alt+E**) and places the fresh report into your clipboard (`Ctrl+V`).

## Documentation & License
- **Guides**: [User Guide (EN)](USER_GUIDE.md) | [Руководство пользователя (RU)](USER_GUIDE.ru.md) | [Technical Spec](SPEC_v1.md)
- **License**: **GNU AGPL-3.0-or-later** — see [LICENSE](LICENSE) and [NOTICE](NOTICE).
