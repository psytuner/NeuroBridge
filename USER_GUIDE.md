# NeuroBridge User Guide (v1.1.0): Turn AI Into a Partner With Memory & Hands

## 1. Why NeuroBridge & 2-Minute Quickstart
Standard AI chats suffer from amnesia: once a thread grows long, the model forgets agreements, mixes up paths, and forces you to copy-paste files manually. **NeuroBridge** gives the AI persistent disk memory and a safe local PowerShell execution bridge on Windows.

1. Unpack the release into `C:\NeuroBridge` and launch **`Start_NeuroBridge.cmd`** to start the Tray Project Manager.
2. Open a fresh AI chat, attach **`start_new_chat.txt`** from the root folder, and send it.
3. Sounds start muted by default and **`Demo Project`** is pre-selected so you can test right away, or click **`+`** in the Manager to spin up your own workspace.

## 2. The Core Secret: Co-Design Project Rules & Gates With AI
Jumping straight into coding without project laws leads to drift. NeuroBridge shines when you **co-design the workflow inside your project together with the AI**:

- **Step 1. Define Project Laws (`02_Docs\DOC_PROJECT_RULES.md`)**: Agree with the AI on your workflow, protected paths, and mandatory **testing gates** before any release (e.g., *"Never package a release until all 5 pipeline tests pass"* or *"Always read target signatures before editing code"*).
- **Step 2. Keep a Live Pulse (`02_Docs\DOC_PROJECT_RUNTIME.md`)**: Have the AI check off completed milestones and update the `HANDOFF` breakpoint after every step so any future session resumes effortlessly.
- **Step 3. Separate Core From Projects**: When upgrading the pipeline or Tray Manager itself, switch to **`NeuroBridge_Core`** (`NeuroBridge_report.txt`). Keep domain work isolated inside `Projects\<YourProject>\`.
- **Step 4. Refresh Chats Freely**: Whenever a chat grows heavy, open a new tab and attach **`start_new_chat.txt`** — your entire ruleset and state come along automatically.

## 3. Control Center: Tray Project Manager (`NeuroBridgeTray.exe`)
- **Project Switcher & `+` Button**: Switch workspaces or scaffold a clean project (`01_Work`, `02_Docs`, `03_Tools`) in one click.
- **Copy Report**: Copies the latest `<id>_report.txt` with auto-attached Core and Project docs (`auto_footer.ps1`).
- **Dual Error Button**: Stays green to open the error folder when clear; turns red on any crash, AST error, or user abort to copy **`NeuroBridge_error.txt`** in one click.
- **Unified Bottom Bar**: Active project folder (`Project`), visual **Memory Map (`🗺`)** highlighting active docs in real time, **Reset Dup** (clears SHA256 repeat guard), and **Settings (`⚙`)** housing Auto-Watcher, `Alt+E`, Windows Startup, Sound, `RU/EN` language switch, and clean Desktop Export.
- **60fps Progress Bar & Abort**: Every script step shows live progress and can be halted immediately via the red **Abort** button.

## 4. Web AI Client Quirks & Limits (And How NeuroBridge Handles Them)
Different web AI interfaces (Google AI Mode, ChatGPT, Claude, etc.) have distinct UI and context-window limits worth keeping in mind:
- **Code Block Length Splits**: Browser chat UIs can split oversized code blocks or alter special characters during rendering. NeuroBridge solves this by splitting large tasks into short sequential steps (`# BLOCK_1` -> `# BLOCK_2` -> `# FINAL_STEP`) with a persistent wait toast guiding you to the next block.
- **Context Fatigue in Long Threads**: As a single chat grows very long, reasoning slows down and slip-ups become more likely. Whenever a thread feels heavy, open a fresh chat and attach **`start_new_chat.txt`** to continue seamlessly with full memory intact.
- **Language Drift on Errors**: Because underlying models are heavily English-trained, complex errors can trigger English drift. The **`RU / EN`** switch in Tray Settings (`⚙`) injects an explicit language anchor at both the top and bottom of every report.
- **No Blind Disk Access**: A browser LLM only knows your local state through the attached `<id>_report.txt`. Always attach the fresh report after each run so the AI stays grounded in reality.
