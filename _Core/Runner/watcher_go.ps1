# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
$coreR = $PSScriptRoot
$pidFile = "$coreR\watcher.pid"
"$PID" | Out-File $pidFile -Encoding ASCII
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$flg = "$coreR\watcher.disabled"; $hf = "$coreR\last_step.hash"; $whf = "$coreR\last_watcher.hash"
& "$coreR\runner_log.ps1" 'WATCHER' "Started PID=$PID"
function Test-Ok([string]$t) {
  if ([string]::IsNullOrWhiteSpace($t)) { return $false }
  $first = ($t.TrimStart() -split "`r?`n") | Select-Object -First 1
  if ($first -notmatch '^#\s*\S+_\S+') { return $false }
  if ($t -notmatch 'auto_footer\.ps1' -and $first -notmatch '1|2|3|4|5|6|7|8|9') { return $false }
  $tok = $null; $err = $null
  [void][Management.Automation.Language.Parser]::ParseInput($t, [ref]$tok, [ref]$err)
  if ($err -ne $null -and $err.Count -gt 0) { & "$coreR\runner_log.ps1" 'WATCHER' "Skip: AST err $($err.Item(0).Message)"; return $false }
  return $true
}
$f = New-Object Windows.Forms.Form
$f.ShowInTaskbar = $false
$f.FormBorderStyle = 'SizableToolWindow'
$f.StartPosition = 'Manual'
$f.Location = New-Object Drawing.Point(-32000,-32000)
$f.Size = New-Object Drawing.Size(0,0)
$tm = New-Object Windows.Forms.Timer; $tm.Interval = 350
$tm.Add_Tick({
  if (Test-Path $flg) { return }
  try {
    if ([Windows.Forms.Clipboard]::ContainsFileDropList() -or -not [Windows.Forms.Clipboard]::ContainsText()) { return }
    $cb = [Windows.Forms.Clipboard]::GetText()
    if (-not (Test-Ok $cb)) { return }
    $sha = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($cb.Trim())))
    if (((Test-Path $hf) -and ([IO.File]::ReadAllText($hf).Trim() -eq $sha)) -or ((Test-Path $whf) -and ([IO.File]::ReadAllText($whf).Trim() -eq $sha))) { return }
    [IO.File]::WriteAllText($whf, $sha)
    & "$coreR\runner_log.ps1" 'WATCHER' "Trigger go.ps1 sha=$($sha.Substring(0,8))"
    Start-Process powershell '-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "C:\NeuroBridge\_Core\Runner\go.ps1"' -WindowStyle Hidden
  } catch { & "$coreR\runner_log.ps1" 'WATCHER' "Err: $($_.Exception.Message)" }
})
$tm.Start(); [Windows.Forms.Application]::Run($f)




