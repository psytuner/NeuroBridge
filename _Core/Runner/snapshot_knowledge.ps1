# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (://github.com)
$sMtx = New-Object Threading.Mutex($false, 'Global\NeuroBridge_Snap_Lock')
$sOk = $sMtx.WaitOne(5000)
try {
  $root = (Split-Path (Split-Path $PSScriptRoot)); $coreR = "$root\_Core\Runner"; $coreD = "$root\_Core\Docs"
  $pCfg = Get-Content "$coreR\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
  $lng = if ($pCfg.lang) { $pCfg.lang } else { "ru" }
  $act = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1
  $errMon = (& "$coreR\check_error_class.ps1") -join " | "
  $boot = @(
    "=== SYSTEM BOOTSTRAP (READ FIRST) ===",
    "1. ROLE & BRIDGE: User connected NeuroBridge local Windows automation & persistent memory runtime. You have no direct disk access; your PowerShell blocks execute locally via watcher_go.ps1 / go.ps1.",
    "2. EXECUTION LOOP: Output PowerShell blocks starting with '# TASK_SINGLE' (or '# BLOCK_1'..'# FINAL_STEP') and set `$out to the target report path. The runner executes it and copies `$out to user clipboard automatically.",
    "3. FIRST RESPONSE NOW: Do NOT output any script yet. Greet the user in [LANG=$lng], confirm NeuroBridge Core rules are loaded, state active project ($($act.name)), and ask whether to continue it or switch/create a project."
  )
  foreach ($sf in @("start_new_chat.txt")) {
    $outS = Join-Path $root $sf
    "# NEUROBRIDGE - START NEW DIALOG [LANG=$lng]" | Out-File $outS -Encoding UTF8
    $boot | Add-Content $outS -Encoding UTF8
    "Active: $($act.name) | Path: $($act.path) | Report: $($act.report)" | Add-Content $outS -Encoding UTF8
    "ErrorMonitor: $errMon" | Add-Content $outS -Encoding UTF8
    "RULE: Start file contains ONLY Core docs. Upon user confirming or switching project, AI MUST request ALL <Path>\02_Docs\DOC_*.md files completely first!" | Add-Content $outS -Encoding UTF8
    foreach ($f in @("CORE_RULES.md","CORE_SCRIPT_GUIDE.md","CORE_ARCHITECTURE.md","CORE_TERMINOLOGY.md","CORE_STYLE_UI.md","CORE_ERRORS.md","CORE_CHANGELOG.md")) {
      $p = Join-Path $coreD $f; if (Test-Path $p) { "=== $f ===" | Add-Content $outS -Encoding UTF8; Get-Content $p -Encoding UTF8 | Add-Content $outS -Encoding UTF8 }
    }
  }
} finally { if ($sOk) { $sMtx.ReleaseMutex() }; $sMtx.Dispose() }



