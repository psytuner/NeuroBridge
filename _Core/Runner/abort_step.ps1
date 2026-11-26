# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (://github.com)
$w = 'C:\NeuroBridge'; $ef = "$w\NeuroBridge_error.txt"
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" | Where-Object { $_.CommandLine -match 'go\.ps1|step_' -and $_.ProcessId -ne $PID } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -EA 0 }
'=== STEP ABORTED BY USER ===' | Out-File $ef -Encoding UTF8
$out = $ef; $global:out = $ef
& "$w\_Core\Runner\auto_footer.ps1"
$ef | Out-File "$w\_Core\Logs\error_report_ready.flag" -Encoding UTF8
