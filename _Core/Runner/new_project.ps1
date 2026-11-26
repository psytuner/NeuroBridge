# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (://github.com)
param([Parameter(Mandatory=$true)][string]$Name, [string]$Slug = "", [switch]$NoActivate)
$nb = Split-Path (Split-Path $PSScriptRoot)
$rDir = "$nb\_Core\Runner"
$pj = "$rDir\projects.json"
$raw = $Name.Trim()
if ([string]::IsNullOrWhiteSpace($Slug)) { $Slug = ($raw -replace '[^a-zA-Z0-9]+', '_').Trim('_') }
if ($Slug.Length -lt 2) { $Slug = "Project_" + (Get-Date -Format 'HHmmss') }
$pDir = "$nb\Projects\$Slug"
$docD = "$pDir\02_Docs"
$rep = "${Slug}_report.txt"
New-Item -ItemType Directory -Path "$pDir\01_Work", $docD, "$pDir\03_Tools" -Force | Out-Null
"# [CLASS: ALWAYS]`r`n# [DOC_PROJECT_RULES] PROJECT RULES: $raw`r`n- Core laws: C:\NeuroBridge\_Core\Docs\CORE_RULES.md`r`n- Target report: `$out = `"C:\NeuroBridge\Projects\$Slug\$rep`"`r`n" | Out-File "$docD\DOC_PROJECT_RULES.md" -Encoding UTF8
"# [CLASS: ALWAYS]`r`n# [DOC_PROJECT_RUNTIME] LIVE STATUS & HANDOFF: $raw`r`n## CURRENT HANDOFF`r`n- Project initialized via new_project.ps1. Ready for initial task definition.`r`n" | Out-File "$docD\DOC_PROJECT_RUNTIME.md" -Encoding UTF8
"# [CLASS: ALWAYS]`r`n# [DOC_FEATURES] FEATURE REGISTRY: $raw`r`n" | Out-File "$docD\DOC_FEATURES.md" -Encoding UTF8
"# [CLASS: ON_ERROR]`r`n# [PROJECT_ERRORS] ERROR LOG: $raw`r`n" | Out-File "$docD\PROJECT_ERRORS.md" -Encoding UTF8
"# [CLASS: ON_AUDIT]`r`n# [DOC_AUDIT_LOG] AUDIT LOG: $raw`r`n" | Out-File "$docD\DOC_AUDIT_LOG.md" -Encoding UTF8
"# [CLASS: TAIL_15]`r`n# [DOC_PROJECT_CHANGELOG] DEVELOPMENT LOG: $raw`r`n- [$(Get-Date -Format 'yyyy-MM-dd')] Project initialized via new_project.ps1.`r`n" | Out-File "$docD\DOC_PROJECT_CHANGELOG.md" -Encoding UTF8
$cfg = Get-Content $pj -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not ($cfg.projects | Where-Object { $_.id -ieq $Slug })) {
  $cfg.projects += [PSCustomObject]@{ id = $Slug; name = $raw; name_en = $raw; path = $pDir; report = $rep; buttons = @() }
}
if (-not $NoActivate) { $cfg.active = $Slug }
$cfg | ConvertTo-Json -Depth 6 | Out-File $pj -Encoding UTF8
$prevOut = $global:out; $global:out = "$pDir\$rep"
"=== INITIAL PROJECT REPORT: $raw ($Slug) ===" | Out-File $global:out -Encoding UTF8
& "$rDir\auto_footer.ps1"; $global:out = $prevOut


