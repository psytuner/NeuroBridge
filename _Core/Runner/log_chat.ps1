# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param([string]$u,[string]$a,[string]$p)
$coreR = $PSScriptRoot
$pCfg = Get-Content "$coreR\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$repName = if ($global:out) { [IO.Path]::GetFileName($global:out) } elseif ($out) { [IO.Path]::GetFileName($out) } else { "" }
$proj = $null
if (-not [string]::IsNullOrWhiteSpace($p)) { $proj = $pCfg.projects | Where-Object { $_.id -eq $p } | Select-Object -First 1 }
if (-not $proj -and -not [string]::IsNullOrWhiteSpace($repName)) { $proj = $pCfg.projects | Where-Object { $_.report -eq $repName } | Select-Object -First 1 }
if (-not $proj) { $proj = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1 }
$docDir = if ($proj.id -eq "NeuroBridge_Core") { "C:\NeuroBridge\_Core\Docs" } else { "$($proj.path)\02_Docs" }
$histDir = "$docDir\_dialog_history"
New-Item -Path $histDir -ItemType Directory -Force | Out-Null
$dl = "$docDir\DIALOG_LOG.md"
$dayFile = "$histDir\HISTORY_$((Get-Date).ToString('yyyy-MM-dd')).md"
$ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss UTC+07:00")
$entry = "`r`n### [$ts] | Проект: $($proj.id)`r`n- **USER:** $u`r`n- **AI:** $a`r`n"
Add-Content -Path $dl -Value $entry -Encoding UTF8
Add-Content -Path $dayFile -Value $entry -Encoding UTF8



