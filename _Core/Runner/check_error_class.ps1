# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param([string]$ErrorClass)
$root = (Split-Path (Split-Path $PSScriptRoot))
$pCfg = Get-Content "$root\_Core\Runner\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$act = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1
$targets = @("$root\_Core\Docs\CORE_ERRORS.md")
if ($act -and $act.path -and (Test-Path "$($act.path)\02_Docs\PROJECT_ERRORS.md")) { $targets += "$($act.path)\02_Docs\PROJECT_ERRORS.md" }
$openAll = @()
foreach ($f in $targets) {
    $lines = Get-Content $f -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($ErrorClass)) {
        $open = $lines | Where-Object { $_ -match '^## \[В РАБОТЕ\]' }
        if ($open) { $openAll += $open }
    } else {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match [regex]::Escape($ErrorClass)) {
                $end = [Math]::Min($lines.Count - 1, $i + 2)
                $lines[$i..$end] -join "`r`n"
            }
        }
    }
}
if ([string]::IsNullOrWhiteSpace($ErrorClass)) {
    if ($openAll.Count -gt 0) { "OPEN_TICKETS:"; $openAll } else { "OPEN_TICKETS: 0 (ALL CLEAR)" }
}



