# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param([string]$src, [string]$msg)
$lf = 'C:\NeuroBridge\_Core\Runner\runner_actions.log'
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'
Add-Content -Path $lf -Value "[$ts] [$src] $msg" -Encoding UTF8 -ErrorAction SilentlyContinue
if ((Get-Item $lf -EA SilentlyContinue).Length -gt 65536) {
    $tail = Get-Content $lf -Tail 200 -Encoding UTF8
    Set-Content -Path $lf -Value $tail -Encoding UTF8
}


