# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
$coreR = $PSScriptRoot
$pidFile = "$coreR\hotkey.pid"
if (Test-Path $pidFile) { $old = (Get-Content $pidFile -Raw).Trim(); if ($old -and $old -ne "$PID") { Stop-Process -Id $old -Force -EA SilentlyContinue } }
"$PID" | Out-File $pidFile -Encoding ASCII
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class HK { [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int i, uint m, uint k); [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int i); }'
& "$coreR\runner_log.ps1" 'HOTKEY' "Started PID=$PID"
$f = New-Object Windows.Forms.Form
$f.ShowInTaskbar = $false
$f.FormBorderStyle = 'SizableToolWindow'
$f.StartPosition = 'Manual'
$f.Location = New-Object Drawing.Point(-32000,-32000)
$f.Size = New-Object Drawing.Size(0,0)
$null = [HK]::RegisterHotKey($f.Handle, 1, 1, 69)
$f.Add_FormClosing({ [HK]::UnregisterHotKey($f.Handle, 1) })
$tm = New-Object Windows.Forms.Timer; $tm.Interval = 120
$tm.Add_Tick({ [Windows.Forms.Application]::DoEvents() })
Add-Type -TypeDefinition 'using System; using System.Windows.Forms; public class MsgFilter : IMessageFilter { public Action OnHit; public bool PreFilterMessage(ref Message m) { if (m.Msg == 786 && m.WParam.ToInt32() == 1) { OnHit(); return true; } return false; } }' -ReferencedAssemblies System.Windows.Forms
$mf = New-Object MsgFilter; $mf.OnHit = { if (-not (Test-Path "$coreR\alt_e.disabled")) { & "$coreR\runner_log.ps1" 'HOTKEY' 'Alt+E -> go.ps1'; Start-Process powershell '-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "C:\NeuroBridge\_Core\Runner\go.ps1"' -WindowStyle Hidden } }
[Windows.Forms.Application]::AddMessageFilter($mf); $tm.Start(); [Windows.Forms.Application]::Run($f)




