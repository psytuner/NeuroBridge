# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
. "C:\NeuroBridge\_Core\Runner\progress_ui.ps1"
function Show-Toast($title,$sub,$bg,$bd,$wav,$hold,$slot){
  $f=New-Object Windows.Forms.Form;$f.FormBorderStyle='None';$f.StartPosition='Manual';$f.TopMost=$true;$f.ShowInTaskbar=$false
  $f.Size=New-Object Drawing.Size(360,64);$f.BackColor=[Drawing.ColorTranslator]::FromHtml($bg);$f.Opacity=0
  $wa=[Windows.Forms.Screen]::PrimaryScreen.WorkingArea;$off=80+($slot*92)
  $f.Location=New-Object Drawing.Point(($wa.Right-376),($wa.Bottom-$off))
  $p=New-Object Windows.Forms.Panel;$p.Dock='Fill';$p.Padding=New-Object Windows.Forms.Padding(12,8,12,8)
  $p.Add_Paint({param($s,$e)$pen=New-Object Drawing.Pen([Drawing.ColorTranslator]::FromHtml($bd),2);$e.Graphics.DrawRectangle($pen,1,1,$s.Width-2,$s.Height-2)})
  $l1=New-Object Windows.Forms.Label;$l1.Text=$title;$l1.Dock='Top';$l1.Height=22;$l1.ForeColor=[Drawing.Color]::White;$l1.Font=New-Object Drawing.Font('Segoe UI',10.5,[Drawing.FontStyle]::Bold)
  $l2=New-Object Windows.Forms.Label;$l2.Text=$sub;$l2.Dock='Fill';$l2.ForeColor=[Drawing.ColorTranslator]::FromHtml('#D8E2EC');$l2.Font=New-Object Drawing.Font('Segoe UI',9.5,[Drawing.FontStyle]::Regular)
  $p.Controls.Add($l2);$p.Controls.Add($l1);$f.Controls.Add($p);$f.Show()
  $mute=Test-Path "C:\NeuroBridge\_Core\Runner\sound.disabled"
  if(-not $mute -and $wav -and (Test-Path $wav)){(New-Object Media.SoundPlayer $wav).Play()}
  for($i=1;$i -le 8;$i++){$f.Opacity=$i*0.12;[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 14}
  if($hold -gt 0){if ($hold -gt 0) { if ($hold -gt 0) { Start-Sleep -Milliseconds $hold } };for($i=7;$i -ge 0;$i--){$f.Opacity=$i*0.12;[Windows.Forms.Application]::DoEvents();Start-Sleep -Milliseconds 14};$f.Close();$f.Dispose()}else{return $f}
}
$nb=(Split-Path (Split-Path $PSScriptRoot));$sys="$nb\_Core\Runner";$pCfg=(Get-Content "$sys\projects.json" -Raw -Encoding UTF8)|ConvertFrom-Json
$lng=if($pCfg.lang){$pCfg.lang}else{"ru"};$d=(Get-Content "$nb\_Core\Lang\$lng.json" -Raw -Encoding UTF8)|ConvertFrom-Json
$act=$pCfg.projects|Where-Object{$_.id -eq $pCfg.active}|Select-Object -First 1;$pTitle=$act.name;$defaultOut=Join-Path $act.path $act.report;$chanId=$act.id
$cb="";for($r=0;$r -lt 5;$r++){try{$cb=[Windows.Forms.Clipboard]::GetText();if($cb){break}}catch{};Start-Sleep -Milliseconds 60}
if([string]::IsNullOrWhiteSpace($cb) -or $cb -notmatch '^\s*#.*_'){Show-Toast $pTitle $d.toast_empty '#4A2C11' '#D97706' "$env:WINDIR\Media\Windows Ding.wav" 1200 0;exit}
foreach($pr in $pCfg.projects){if($pr.report -and $cb.Contains($pr.report)){$chanId=$pr.id;$pTitle=$pr.name;$defaultOut=Join-Path $pr.path $pr.report;break}}
if($cb -match '^\s*#\s*\u0424\u041E\u041D_'){
  $bgF="$sys\bg_step_$(Get-Date -Format 'HHmmss')_$PID.ps1";$cb|Out-File $bgF -Encoding UTF8
  Start-Process powershell "-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$bgF`"" -WindowStyle Hidden
  Start-Sleep -Milliseconds 300;Show-Toast $pTitle $d.toast_run '#1E293B' '#38BDF8' "$sys\_sounds\block_ok.wav" 1100 0;exit
}
$sha=[BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($cb.Trim())))
$hf="$sys\last_step.hash"
if((Test-Path $hf) -and ((Get-Content $hf -Raw -EA 0).Trim() -eq $sha)){Show-Toast $pTitle $d.toast_dup '#4A2C11' '#D97706' "$env:WINDIR\Media\Windows Ding.wav" 1350 0;exit}
$sha|Out-File $hf -Encoding UTF8
$qDir="$sys\queue_$chanId";New-Item -Path $qDir -ItemType Directory -Force|Out-Null
$qFile=Join-Path $qDir ("$(Get-Date -Format 'yyyyMMdd_HHmmss_fff')_$PID.ps1")
$cb|Out-File $qFile -Encoding UTF8
$created=$false;$mtx=New-Object Threading.Mutex($true,"Global\NeuroBridge_Go_Lock_$chanId",[ref]$created)
if(-not $created){
  $qCnt=(Get-ChildItem "$qDir\*.ps1" -EA 0).Count
  $qMsg=if($lng -eq "ru"){" очереди канала ($qCnt)"}else{"Queued in channel ($qCnt)"}
  Show-Toast $pTitle $qMsg '#163A59' '#38BDF8' $null 900 0;exit
}
$uiSl=Reserve-UiSlot;$tmp="$sys\step_$chanId.ps1"
try{
  while($true){
    $nextItem=Get-ChildItem "$qDir\*.ps1" -EA 0|Sort-Object Name|Select-Object -First 1
    if(-not $nextItem){break}
    $curCode=Get-Content $nextItem.FullName -Raw -Encoding UTF8
    Remove-Item $nextItem.FullName -Force -EA 0
    $firstLine=($curCode -split "`r?`n").Trim()
    $sw=[Diagnostics.Stopwatch]::StartNew();$autoW=New-ProgressWindow -title $pTitle -total 100 -Marquee
    $out=$null;$curCode|Out-File $tmp -Encoding UTF8;. $tmp
    if([string]::IsNullOrWhiteSpace($out)){$out=$defaultOut}
    $repName=[IO.Path]::GetFileName($out);$byRep=$pCfg.projects|Where-Object{$_.report -eq $repName}|Select-Object -First 1;if($byRep){$pTitle=$byRep.name}
    $rem=[Math]::Max(0,650-$sw.ElapsedMilliseconds);if($rem -gt 0){Start-Sleep -Milliseconds $rem}
    Close-ProgressWindow $autoW
    if($firstLine -match '^#.*_[0-9]+$'){Show-Toast $pTitle $d.toast_blk '#163A59' '#38BDF8' "$sys\_sounds\block_ok.wav" -1 $uiSl.Slot}
    elseif(Test-Path $out){ Remove-Item "$nb\_Core\Logs\error_report_ready.flag" -Force -EA 0;
      $cMtx=New-Object Threading.Mutex($false,'Global\NeuroBridge_Clip_Lock');$cOk=$cMtx.WaitOne(2000)
      try{[Windows.Forms.Clipboard]::Clear();$sc=New-Object Collections.Specialized.StringCollection;$sc.Add($out)|Out-Null;[Windows.Forms.Clipboard]::SetFileDropList($sc)}finally{if($cOk){$cMtx.ReleaseMutex()};$cMtx.Dispose()}
      Show-Toast $pTitle $d.toast_ok '#144229' '#22C55E' "$sys\_sounds\task_ok.wav" 1350 $uiSl.Slot
    }
  }
}catch{
  Close-ProgressWindow $global:nbAutoBar
  if([string]::IsNullOrWhiteSpace($out)){$out=$defaultOut}
  $errTxt=$_.Exception.Message + "`r`n" + $_.ScriptStackTrace;$errTxt|Out-File "$sys\last_step_error.log" -Encoding UTF8
  & "$sys\runner_log.ps1" 'GO_ERR' $_.Exception.Message
  $out="$nb\NeuroBridge_error.txt"; $global:out=$out; $hdr = if ($raw -match "#\s*TEST_ERROR_SIMULATION") { "=== SIMULATED TEST ERROR (PIPELINE OK) ===" } else { "=== SCRIPT ERROR ===" }; "$hdr`r`n$errTxt"|Out-File $out -Encoding UTF8; & "$sys\auto_footer.ps1"; $out|Out-File "$nb\_Core\Logs\error_report_ready.flag" -Encoding UTF8
  if(Test-Path $out){[Windows.Forms.Clipboard]::Clear();$sc=New-Object Collections.Specialized.StringCollection;$sc.Add($out)|Out-Null;[Windows.Forms.Clipboard]::SetFileDropList($sc)}
  Show-Toast $pTitle $d.toast_err '#5C1919' '#EF4444' "$sys\_sounds\error_glass.wav" 1600 $uiSl.Slot
}finally{Free-UiSlot $uiSl.Lock;if($mtx){$mtx.ReleaseMutex();$mtx.Dispose()}}












