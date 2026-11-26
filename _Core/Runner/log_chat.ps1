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
$dl = "$docDir\DIALOG_LOG.md"
$ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$items = if (Test-Path $dl) { (Get-Content $dl -Raw -Encoding UTF8) -split "(?m)^### \[" | Where-Object { $_.Trim() -ne "" } } else { @() }
$keep = @($items | Select-Object -Last 29) | ForEach-Object { "### [" + $_.Trim() }
$keep += "### [$ts | $($proj.id)]`r`n- USER: $u`r`n- AI: $a"
($keep -join "`r`n`r`n") | Out-File $dl -Encoding UTF8

