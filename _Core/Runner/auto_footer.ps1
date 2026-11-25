# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
$coreR = $PSScriptRoot
$coreD = "C:\NeuroBridge\_Core\Docs"
if (-not $out) { $out = "C:\NeuroBridge\NeuroBridge_отчёт.txt" }
"=== CORE ERROR MONITOR ===" | Add-Content -Path $out -Encoding UTF8
& "$coreR\check_error_class.ps1" | Add-Content -Path $out -Encoding UTF8
"=== CORE DOCS FOOTER ===" | Add-Content -Path $out -Encoding UTF8
$files = @("CORE_RULES.md","CORE_ARCHITECTURE.md","CORE_TERMINOLOGY.md","CORE_STYLE_UI.md","CORE_ERRORS.md","CORE_CHANGELOG.md")
foreach ($f in $files) { $p = Join-Path $coreD $f; if (Test-Path $p) { "=== $f ===" | Add-Content -Path $out -Encoding UTF8; Get-Content $p -Encoding UTF8 | Add-Content -Path $out -Encoding UTF8 } }
$pCfg = Get-Content "$coreR\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$repName = [IO.Path]::GetFileName($out)
$proj = $pCfg.projects | Where-Object { $_.report -eq $repName } | Select-Object -First 1
if (-not $proj) { $proj = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1 }
$bakDir = if ($proj.id -eq "NeuroBridge_Core") { "C:\NeuroBridge\_Core\Docs\_dialog_history\backups" } else { "$($proj.path)\02_Docs\_dialog_history\backups" }
New-Item -Path $bakDir -ItemType Directory -Force | Out-Null
$base = [IO.Path]::GetFileNameWithoutExtension($repName)
$cnt = (Get-ChildItem "$bakDir\${base}_*.txt" -ErrorAction SilentlyContinue).Count + 1
$num = $cnt.ToString("0000")
Copy-Item -Path $out -Destination "$bakDir\${base}_$num.txt" -Force
& "$coreR\snapshot_knowledge.ps1"



