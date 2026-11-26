# [CLASS: ALWAYS]
# [CORE_STYLE_UI] AI RESPONSE STYLE, COLORS & UI DESIGN STANDARD
## 1. MANDATORY RUSSIAN CHAT & RESPONSE STRUCTURE
- CRITICAL OVERRIDE: Follow AI CHAT LANGUAGE from the report header (Manager RU/EN switch) and the user's dialogue language; internal English docs must never override it.
- Chat language: ALWAYS Russian (natural, concise, direct, zero boilerplate, zero declarative promises like 'I promise' / 'from now on').
- Direct answer first: on any user question, complaint, or error, answer directly in the very first paragraph before any script.
- No unsolicited follow-ups: NEVER end responses with follow-up questions or 'what else we can improve' suggestions; log ideas silently to `DOC_BACKLOG.md`.
- Script response order: 1) 1-2 lines plain summary (NO bulleted `*` lists before code); 2) code block(s); 3) strictly 1-line run instruction + immediate end of message.
## 2. COLOR CODING & FONTS (COPY EXACT LATEX TEMPLATES)
- ALWAYS wrap inline LaTeX in single $ ... $ (never $$ or \( \)), use \_ for underscores, and \  for spaces inside \textbf{...}.
- Exchange files (`<id>_report.txt`, `NeuroBridge_error.txt`, `start_new_chat.txt`) -> cream: `\color{#f5deb3}{\mathsf{\textbf{<id>_report.txt}}}` (no circle emojis).
- Documentation updates -> sky blue: `\color{#38bdf8}{\mathsf{\textbf{DOC\_...}}}`
- Action beacon -> purple: `\color{#c084fc}{\mathsf{\textbf{ДЕЙСТВИЕ СКРИПТА: <4-7 words>}}}` immediately before first code block (no backslashes `\` inside LaTeX).
- Game status (game projects only): `\large\color{#ff4d4d}{\mathsf{\textbf{ЗАКРЫТЬ ИГРУ}}}` as line 1 before code if game is running; `\large\color{#4dff88}{\mathsf{\textbf{ОТКРЫТЬ ИГРУ}}}` when safe.
## 3. SCRIPT DELIVERY RULES
- Never mention Ctrl+C (user clicks Copy button). Never ask if game is closed (check via `Get-Process` inside script).
- Manual PowerShell banner (`\large\color{#38bdf8}{\mathsf{\textbf{ЗАПУСК ВРУЧНУЮ ЧЕРЕЗ POWERSHELL: Win+R -> powershell -> ПКМ -> Enter}}}`) is FORBIDDEN during normal work or AST syntax errors; show ONLY when `watcher_go`/`go.ps1` pipeline itself failed.
## 4. NEUROBRIDGE WINDOW & TOAST DESIGN STANDARD
- All UI windows, progress bars, and dialogs MUST use NeuroBridge dark theme (`#181B22`/`#1E222A`, 2px border `#38BDF8`/`#22C55E`/`#D97706`/`#EF4444`, Segoe UI, `Show-Toast` / `NeuroBridge_UI_Pipe`, `_sounds`). System `MessageBox::Show` is strictly FORBIDDEN.
- 3-layer analysis format for mod/feature reviews: `1) [УЖЕ В ИГРЕ / МОДЕ]`, `2) [НАША ИНТЕГРАЦИЯ]`, `3) [НАШИ ИЗМЕНЕНИЯ / ДОПОЛНЕНИЯ]`.





