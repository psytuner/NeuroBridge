# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
param($Root = "C:\NeuroBridge")
"=== READ-ONLY АУДИТ ДОКУМЕНТАЦИИ (0 ПРАВОК НА ДИСКЕ) ==="
$docs = Get-ChildItem "$Root\_Core\Docs", "$Root\Projects" -Recurse -Filter "*.md"
$docs = $docs | Where-Object { $_.FullName -notmatch '_dialog_history|_doc_backups|_archive_and_tools|CHANGELOG|DIALOG_LOG|AUDIT_LOG|L2_ARCHIVE|PROJECT_ERRORS|BACKLOG' }
$bad = New-Object System.Collections.ArrayList
foreach ($d in $docs) {
    $rel = $d.FullName.Replace("$Root\", "")
    $n = 0
    foreach ($rawLn in (Get-Content $d.FullName -Encoding UTF8)) {
        $n++
        $ln = "$rawLn".Trim()
        if ($ln -match '_prereleases|deploy_prereleases|active в projects\.json|-pre\d|-preN') {
            $null = $bad.Add("[УСТАРЕЛО] $rel строка $n")
        }
    }
}
"Проверено актуальных документов: $($docs.Count) | Найдено хвостов: $($bad.Count)"
if ($bad.Count -gt 0) { $bad } else { "ALL CLEAR: противоречий не найдено." }



