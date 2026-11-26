# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param([string]$targetLang)
$nb = (Split-Path (Split-Path $PSScriptRoot)); $r = "$nb\_Core\Runner"; $pj = "$r\projects.json"
$cfg = Get-Content $pj -Raw -Encoding UTF8 | ConvertFrom-Json
if ($targetLang -ne 'en' -and $targetLang -ne 'ru') { $targetLang = if ($cfg.lang) { $cfg.lang } else { 'ru' } }
$cfg | Add-Member -NotePropertyName 'lang' -NotePropertyValue $targetLang -Force
$cfg | ConvertTo-Json -Depth 6 | Out-File $pj -Encoding UTF8
& "$r\snapshot_knowledge.ps1"

