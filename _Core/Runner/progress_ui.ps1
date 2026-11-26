# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (://github.com)
function Send-NbPipe($msg) {
  try {
    $p = New-Object System.IO.Pipes.NamedPipeClientStream('.','NeuroBridge_UI_Pipe','Out')
    $p.Connect(150); $sw = New-Object System.IO.StreamWriter($p); $sw.AutoFlush = $true
    $sw.WriteLine($msg); $sw.Dispose(); $p.Dispose()
  } catch {}
}
function Get-ActiveSlots { return @() }
function Get-ActiveProgressSlots { return @() }
function Test-BgActive { return $false }
function Reserve-UiSlot { return @{ Slot=0; Lock=$null } }
function Free-UiSlot($lk) {}
function New-ProgressWindow($title,$total,[switch]$Background,[switch]$Marquee) {
  $m = if ($Marquee) { '1' } else { '0' }
  Send-NbPipe "INIT|0|$title|$total|$m"
  $global:nbAutoBar = @{ Slot=0; Tot=$total; Tit=$title }; return $global:nbAutoBar
}
function Update-ProgressWindow($w,$cur,$item) {
  if ($w) { Send-NbPipe "PROG|0|$cur|$item" }
}
function Close-ProgressWindow($w) {
  if ($w) { Send-NbPipe "CLOSE|0" }; $global:nbAutoBar = $null
}


