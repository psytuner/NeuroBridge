# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
$coreR = $PSScriptRoot
$coreD = "C:\NeuroBridge\_Core\Docs"
if (-not $out) { $out = "C:\NeuroBridge\NeuroBridge_report.txt" }
$pCfg = Get-Content "$coreR\projects.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$repName = Split-Path $out -Leaf
$proj = $pCfg.projects | Where-Object { $_.report -eq $repName } | Select-Object -First 1
if (-not $proj) { $proj = $pCfg.projects | Where-Object { $_.id -eq $pCfg.active } | Select-Object -First 1 }
$isCore = ($proj.id -eq "NeuroBridge_Core")
$stepPath = Join-Path $coreR "step_$($proj.id).ps1"
$stepTxt = if (Test-Path $stepPath) { Get-Content $stepPath -Raw -Encoding UTF8 } else { "" }
$bodyRaw = if (Test-Path $out) { Get-Content $out -Raw -Encoding UTF8 } else { "" }
$scanTxt = "$stepTxt`n$bodyRaw"; $scanNorm = $scanTxt.ToLower().Replace("_","").Replace("-",""); $mapItems = New-Object System.Collections.Generic.List[object]
$errOut = (& "$coreR\check_error_class.ps1") -join "`r`n"
$errLog = Join-Path $coreR "last_step_error.log"
$hasFreshErr = (Test-Path $errLog) -and ((Get-Item $errLog).Length -gt 0) -and (((Get-Date) - (Get-Item $errLog).LastWriteTime).TotalMinutes -lt 5)
$needErr = ($errOut -notmatch 'OPEN_TICKETS:\s*0') -or $hasFreshErr -or ($scanTxt -match 'Error|Exception|GATE FAIL|CORE_ERRORS|PROJECT_ERRORS')
$needAudit = $isCore -or ($scanTxt -match 'DOC_[A-Z0-9_]+\.md|CORE_[A-Z0-9_]+\.md|Аудит|CORE_CHANGELOG|DOC_AUDIT_LOG')
$man = New-Object System.Collections.Generic.List[string]
$sb = New-Object System.Text.StringBuilder
$null = $sb.AppendLine("=== CORE ERROR MONITOR ===")
$null = $sb.AppendLine($errOut); $bmF = "C:\NeuroBridge\_Core\Logs\active_bookmark.json"; if (Test-Path $bmF) { $bm = Get-Content $bmF -Raw -Encoding UTF8 | ConvertFrom-Json; if ($out -like "*\$($bm.paused_project)\*") { Remove-Item $bmF -Force } else { $null = $sb.AppendLine("PAUSED BOOKMARK: $($bm.paused_project) | $($bm.stage) - $($bm.summary)") } }; $erF = "C:\NeuroBridge\_Core\Logs\error_resume.json"; if (Test-Path $erF) { $er = Get-Content $erF -Raw -Encoding UTF8 | ConvertFrom-Json; $null = $sb.AppendLine("ERROR RESUME: $($er.project) | $($er.task) (остались: $($er.remaining_blocks))") }
if (-not $isCore) {
    $rdyDir = Join-Path $proj.path "03_Tools\_releases\ready"
    if (Test-Path $rdyDir) {
        $rdyFiles = (Get-ChildItem $rdyDir -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name) -join ", "
        if ($rdyFiles) { $null = $sb.AppendLine("READY IN _RELEASES\READY: $rdyFiles") }
    }
    $srcCache = Join-Path $proj.path "03_Tools\_archive_and_tools\_vs_src"
    if (Test-Path $srcCache) {
        $cDirs = (Get-ChildItem $srcCache -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name) -join ", "
        if ($cDirs) { $null = $sb.AppendLine("DECOMPILED CACHE (_VS_SRC): $cDirs") }
        $idxF = Join-Path $srcCache "_INDEX.md"
        if (Test-Path $idxF) { $man.Add("_vs_src\_INDEX.md [ALWAYS]"); $null = $sb.AppendLine("=== _vs_src\_INDEX.md ==="); $null = $sb.AppendLine((Get-Content $idxF -Raw -Encoding UTF8)) }
    }
}
$null = $sb.AppendLine("=== CORE DOCS FOOTER ===")
foreach ($f in @("CORE_RULES.md","CORE_STYLE_UI.md","CORE_TERMINOLOGY.md")) {
    $p = Join-Path $coreD $f
    if (Test-Path $p) {
        $kb = [Math]::Round((Get-Item $p).Length / 1024.0, 1)
        $man.Add("$f [ALWAYS] (${kb}KB)")
        $null = $sb.AppendLine("=== $f ===")
        $null = $sb.AppendLine((Get-Content $p -Raw -Encoding UTF8))
    }
}
if ($isCore) {
    foreach ($f in @("CORE_SCRIPT_GUIDE.md","CORE_ARCHITECTURE.md")) {
        $p = Join-Path $coreD $f
        if (Test-Path $p) {
            $kb = [Math]::Round((Get-Item $p).Length / 1024.0, 1)
            $man.Add("$f [CORE_MODE] (${kb}KB)")
            $null = $sb.AppendLine("=== $f ===")
            $null = $sb.AppendLine((Get-Content $p -Raw -Encoding UTF8))
        }
    }
}
if ($needErr) {
    $p = Join-Path $coreD "CORE_ERRORS.md"
    if (Test-Path $p) {
        $kb = [Math]::Round((Get-Item $p).Length / 1024.0, 1)
        $man.Add("CORE_ERRORS.md [ON_ERROR] (${kb}KB)")
        $null = $sb.AppendLine("=== CORE_ERRORS.md ===")
        $null = $sb.AppendLine((Get-Content $p -Raw -Encoding UTF8))
    }
}
$clPath = Join-Path $coreD "CORE_CHANGELOG.md"
if (Test-Path $clPath) {
    if ($needAudit) {
    } else {
        $tailCl = (Get-Content $clPath -Tail 15 -Encoding UTF8) -join "`r`n"
        $man.Add("CORE_CHANGELOG.md [TAIL_15] (1.5KB)")
        $null = $sb.AppendLine("=== CORE_CHANGELOG.md (TAIL 15) ===")
        $null = $sb.AppendLine($tailCl)
    }
}
$pDocs = Join-Path $proj.path "02_Docs"
if (-not $isCore -and (Test-Path $pDocs)) {
    $null = $sb.AppendLine("=== PROJECT DOCS FOOTER ===")
    Get-ChildItem $pDocs -Recurse -Filter "*.md" | Where-Object { $_.FullName -notmatch '\\_dialog_history\\' } | ForEach-Object {
        $rel = $_.FullName.Replace("$pDocs\", "")
        $inc = $false; $cls = ""
        $h1 = (Get-Content $_.FullName -TotalCount 1 -Encoding UTF8) -join ""; $tagC = if ($h1 -match "#\s*\[CLASS:\s*([A-Z_]+)\]") { $Matches.1 } else { "" }; $bNorm = $_.BaseName.ToLower().Replace("_","").Replace("-",""); if ($tagC -eq "ALWAYS") { $inc = $true; $cls = "ALWAYS" } elseif ($tagC -eq "ON_ERROR" -and $needErr) { $inc = $true; $cls = "ON_ERROR" } elseif ($tagC -eq "ON_AUDIT" -and $needAudit) { $inc = $true; $cls = "ON_AUDIT" } elseif ($tagC -eq "ON_MENTION" -and $bNorm.Length -ge 4 -and $scanNorm.Contains($bNorm)) { $inc = $true; $cls = "ON_MENTION" } elseif ($_.DirectoryName -eq $pDocs) {
            if ($_.Name -match 'RULES|FEATURES') { $inc = $true; $cls = "ALWAYS" }
            elseif ($_.Name -match 'PROJECT_ERRORS' -and $needErr) { $inc = $true; $cls = "ON_ERROR" }
            elseif ($_.Name -match 'AUDIT_LOG' -and $needAudit) { $inc = $true; $cls = "ON_AUDIT" }
            elseif ($scanTxt -match [Regex]::Escape($_.BaseName)) { $inc = $true; $cls = "ON_MENTION" }
        } else { if ($bNorm.Length -ge 4 -and $scanNorm.Contains($bNorm)) { $inc = $true; $cls = "ON_MENTION" } }; $mapItems.Add(@{ group="Project"; name=$rel; cls=$(if($cls){$cls}else{"ON_MENTION"}); active=$inc })
        if ($inc) {
            $kb = [Math]::Round($_.Length / 1024.0, 1)
            $man.Add("$rel [$cls] (${kb}KB)")
            $null = $sb.AppendLine("=== $rel ===")
            $null = $sb.AppendLine((Get-Content $_.FullName -Raw -Encoding UTF8))
        }
    }
}
$totKb = [Math]::Round(([Text.Encoding]::UTF8.GetByteCount($bodyRaw) + [Text.Encoding]::UTF8.GetByteCount($sb.ToString())) / 1024.0, 1)
$uiLng = if ($pCfg.lang) { $pCfg.lang.ToUpper() } else { "RU" }; $mLine = "=== AI CHAT LANGUAGE: $uiLng (FROM MANAGER SWITCH + DIALOGUE) | ATTACHED DOCS MANIFEST (TOTAL: ${totKb}KB): " + ($man -join " | ") + " ==="
$lngBanner = if ($uiLng -eq "RU") { "=== ЯЗЫК РАССУЖДЕНИЙ И ОТВЕТА ИИ: СТРОГО РУССКИЙ (НЕ ПЕРЕХОДИ НА АНГЛИЙСКИЙ) ===
=== ИНСТРУКЦИЯ ДЛЯ ИИ: ВЕДИ ВСЕ ВНУТРЕННИЕ РАССУЖДЕНИЯ (REASON) И ОТВЕТ СТРОГО НА РУССКОМ ЯЗЫКЕ ===" } else { "=== AI REASONING & RESPONSE LANGUAGE: STRICTLY ENGLISH ===
=== AI DIRECTIVE: WRITE ALL INTERNAL REASONING AND RESPONSES STRICTLY IN ENGLISH ===" }
$finalOut = "$lngBanner`r`n$mLine`r`n$bodyRaw`r`n$($sb.ToString())`r`n$lngBanner"
$finalOut | Out-File $out -Encoding UTF8
$bakDir = if ($isCore) { "C:\NeuroBridge\_Core\Docs\_dialog_history\backups" } else { "$($proj.path)\02_Docs\_dialog_history\backups" }
New-Item -Path $bakDir -ItemType Directory -Force | Out-Null
$base = [IO.Path]::GetFileNameWithoutExtension($repName)
$cnt = (Get-ChildItem "$bakDir\${base}_*.txt" -ErrorAction SilentlyContinue).Count + 1
Copy-Item -Path $out -Destination "$bakDir\${base}_$($cnt.ToString('0000')).txt" -Force
@{ ts=(Get-Date -Format "HH:mm:ss"); proj=$proj.id; report=$repName; needErr=$needErr; needAudit=$needAudit; attached=$man; nodes=$mapItems } | ConvertTo-Json -Depth 4 | Out-File "C:\NeuroBridge\_Core\Logs\last_manifest.json" -Encoding UTF8; & "$coreR\snapshot_knowledge.ps1"













