# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
function Get-ActiveSlots {
  $sys = $PSScriptRoot; $alive = @()
  Get-ChildItem "$sys\slot_*.lock" -EA 0 | ForEach-Object {
    $procId = (Get-Content $_.FullName -Raw -EA 0).Trim()
    if ($procId -and (Get-Process -Id $procId -EA 0)) { $alive += $_.FullName } else { Remove-Item $_.FullName -Force -EA 0 }
  }
  return $alive
}
function Get-ActiveProgressSlots { return (Get-ActiveSlots) }
function Test-BgActive { return ((Get-ActiveSlots).Count -gt 0) }
function Reserve-UiSlot {
  if ($global:nbUiSlot -and $global:nbUiSlot.Lock -and (Test-Path $global:nbUiSlot.Lock)) { return $global:nbUiSlot }
  $slMtx = New-Object Threading.Mutex($false, 'Global\NeuroBridge_Slot_Lock'); $slOk = $slMtx.WaitOne(2000)
  try {
    $sys = $PSScriptRoot; $used = @()
    Get-ChildItem "$sys\slot_*.lock" -EA 0 | ForEach-Object {
      $procId = (Get-Content $_.FullName -Raw -EA 0).Trim()
      if ($procId -and (Get-Process -Id $procId -EA 0)) {
        $numStr = ($_.Name -replace '^slot_(\d+)_.*$', '$1')
        if ($numStr -match '^\d+$') { $used += [int]$numStr }
      } else { Remove-Item $_.FullName -Force -EA 0 }
    }
    $s = 0; while ($used -contains $s) { $s++ }
    $lk = "$sys\slot_${s}_$PID.lock"; "$PID" | Out-File $lk -Encoding UTF8
    $global:nbUiSlot = @{ Slot=$s; Lock=$lk }
    return $global:nbUiSlot
  } finally { if ($slOk) { $slMtx.ReleaseMutex() }; $slMtx.Dispose() }
}
function Free-UiSlot($lk) { if ($lk -and (Test-Path $lk)) { Remove-Item $lk -Force -EA 0 }; $global:nbUiSlot = $null }
function New-ProgressWindow($title,$total,[switch]$Background,[switch]$Marquee) {
  if ($global:nbAutoBar -and $global:nbAutoBar.F -and -not $global:nbAutoBar.F.IsDisposed) {
    $w = $global:nbAutoBar
    $w.Tot = [Math]::Max(1,$total); $w.Tit = $title; $w.T0 = [DateTime]::Now
    $w.B.Style = if ($Marquee) { 'Marquee' } else { 'Continuous' }
    $w.B.Maximum = $w.Tot; $w.B.Value = 0
    $w.L1.Text = if ($Marquee) { $title } else { "$title (0 / $total)" }
    $w.L2.Text = 'Выполняется...'
    [Windows.Forms.Application]::DoEvents()
    return $w
  }
  $sl = Reserve-UiSlot
  $f = New-Object Windows.Forms.Form; $f.FormBorderStyle = 'None'; $f.StartPosition = 'Manual'; $f.TopMost = $true; $f.ShowInTaskbar = $false
  $f.Size = New-Object Drawing.Size(360,82); $f.BackColor = [Drawing.ColorTranslator]::FromHtml('#1E222A')
  $wa = [Windows.Forms.Screen]::PrimaryScreen.WorkingArea; $y = $wa.Bottom - (98 + ($sl.Slot * 92))
  $f.Location = New-Object Drawing.Point(($wa.Right - 376),$y)
  $p = New-Object Windows.Forms.Panel; $p.Dock = 'Fill'; $p.Padding = New-Object Windows.Forms.Padding(12,8,12,8)
  $p.Add_Paint({ param($s,$e) $pen = New-Object Drawing.Pen([Drawing.ColorTranslator]::FromHtml('#38BDF8'),2); $e.Graphics.DrawRectangle($pen,1,1,$s.Width-2,$s.Height-2) })
  $l1 = New-Object Windows.Forms.Label; $l1.Dock = 'Top'; $l1.Height = 20; $l1.ForeColor = [Drawing.Color]::White; $l1.Text = if ($Marquee) { $title } else { "$title (0 / $total)" }; $l1.Font = New-Object Drawing.Font('Segoe UI',10,[Drawing.FontStyle]::Bold)
  $pb = New-Object Windows.Forms.ProgressBar; $pb.Dock = 'Top'; $pb.Height = 16; $pb.Maximum = [Math]::Max(1,$total); $pb.Style = if ($Marquee) { 'Marquee' } else { 'Continuous' }; $pb.MarqueeAnimationSpeed = 28
  $l2 = New-Object Windows.Forms.Label; $l2.Dock = 'Fill'; $l2.ForeColor = [Drawing.ColorTranslator]::FromHtml('#D8E2EC'); $l2.Font = New-Object Drawing.Font('Segoe UI',9,[Drawing.FontStyle]::Regular); $l2.Text = 'Выполняется...'
  $p.Controls.AddRange(@($l2,$pb,$l1)); $f.Controls.Add($p); $f.Show(); [Windows.Forms.Application]::DoEvents()
  $obj = @{ F=$f; B=$pb; L1=$l1; L2=$l2; T0=[DateTime]::Now; Tot=[Math]::Max(1,$total); Tit=$title }
  $global:nbAutoBar = $obj
  return $obj
}
function Update-ProgressWindow($w,$cur,$item) {
  if (-not $w -or $w.F.IsDisposed) { return }
  $w.B.Style = 'Continuous'
  $tot = [Math]::Max(1,$w.Tot); $c = [Math]::Min($cur,$tot); $pct = [Math]::Floor(($c * 100) / $tot)
  $w.B.Value = $c; $w.L1.Text = "$($w.Tit): $c из $tot ($pct%)"
  $el = ([DateTime]::Now - $w.T0).TotalSeconds; $eta = if ($c -gt 0) { [int](($el / $c) * ($tot - $c)) } else { 0 }
  $ts = if ($eta -ge 60) { "~$([int]($eta/60)) мин $($eta%60) сек" } else { "~$eta сек" }
  $w.L2.Text = "сталось: $ts | $item"; [Windows.Forms.Application]::DoEvents()
}
function Close-ProgressWindow($w) {
  if ($w -and -not $w.F.IsDisposed) { $w.F.Close(); $w.F.Dispose() }
  $global:nbAutoBar = $null
}




