# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param([string]$targetLang)
$nb = (Split-Path (Split-Path $PSScriptRoot)); $r = "$nb\_Core\Runner"; $pj = "$r\projects.json"
$cfg = Get-Content $pj -Raw -Encoding UTF8 | ConvertFrom-Json
if ($targetLang -ne 'en' -and $targetLang -ne 'ru') { $targetLang = if ($cfg.lang) { $cfg.lang } else { 'en' } }
$cfg | Add-Member -NotePropertyName 'lang' -NotePropertyValue $targetLang -Force
foreach ($p in $cfg.projects) {
    $oldRep = Join-Path $p.path $p.report
    $newRepName = if ($targetLang -eq 'en') { $p.report.Replace('_отчёт.txt', '_report.txt') } else { $p.report.Replace('_report.txt', '_отчёт.txt') }
    $p.report = $newRepName
    $newRep = Join-Path $p.path $newRepName
    if (($oldRep -ne $newRep) -and (Test-Path $oldRep)) { Move-Item $oldRep $newRep -Force }
}
$cfg | ConvertTo-Json -Depth 6 | Out-File $pj -Encoding UTF8
$oldLnk = if ($targetLang -eq 'en') { "$nb\NeuroBridge_Диспетчер.lnk" } else { "$nb\NeuroBridge_Manager.lnk" }
$newLnk = if ($targetLang -eq 'en') { "$nb\NeuroBridge_Manager.lnk" } else { "$nb\NeuroBridge_Диспетчер.lnk" }
if (Test-Path $oldLnk) { Remove-Item $oldLnk -Force }
$ws = New-Object -ComObject WScript.Shell
$l = $ws.CreateShortcut($newLnk); $l.TargetPath = "$nb\_Core\TrayApp\App\NeuroBridgeTray.exe"; $l.Save()
$oldStart = if ($targetLang -eq 'en') { "$nb\старт_нового_чата.txt" } else { "$nb\start_new_chat.txt" }

& "$r\snapshot_knowledge.ps1"




