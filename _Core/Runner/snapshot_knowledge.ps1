# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
$sMtx = New-Object Threading.Mutex($false, 'Global\NeuroBridge_Snap_Lock')
$sOk = $sMtx.WaitOne(5000)
try {
  $root = (Split-Path (Split-Path $PSScriptRoot)); $coreR = "$root\_Core\Runner"; $coreD = "$root\_Core\Docs"
  $pCfg = Get-Content "$coreR\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
  $lng = if ($pCfg.lang) { $pCfg.lang } else { "ru" }
  $act = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1
  $errMon = (& "$coreR\check_error_class.ps1") -join " | "
  foreach ($sf in @("старт_нового_чата.txt", "start_new_chat.txt")) {
    $outS = Join-Path $root $sf
    "# NEUROBRIDGE - START NEW DIALOG [LANG=$lng]" | Out-File $outS -Encoding UTF8
    "Active: $($act.name) | Path: $($act.path) | Report: $($act.report)" | Add-Content $outS -Encoding UTF8
    "ErrorMonitor: $errMon" | Add-Content $outS -Encoding UTF8
    "RULE: Start file contains ONLY Core docs. Upon user confirming or switching project, AI MUST request ALL <Path>\02_Docs\DOC_*.md files completely first!" | Add-Content $outS -Encoding UTF8
    foreach ($f in @("CORE_RULES.md","CORE_ARCHITECTURE.md","CORE_TERMINOLOGY.md","CORE_STYLE_UI.md","CORE_ERRORS.md","CORE_CHANGELOG.md")) {
      $p = Join-Path $coreD $f; if (Test-Path $p) { "=== $f ===" | Add-Content $outS -Encoding UTF8; Get-Content $p -Encoding UTF8 | Add-Content $outS -Encoding UTF8 }
    }
  }
} finally { if ($sOk) { $sMtx.ReleaseMutex() }; $sMtx.Dispose() }



