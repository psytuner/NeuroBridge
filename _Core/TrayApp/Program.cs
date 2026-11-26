// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 psytuner (://github.com)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace NeuroBridgeTray {
  static class Program {
    [STAThread]
    static void Main(string[] args) { bool silent = args.Contains("--silent");
      Application.SetHighDpiMode(HighDpiMode.SystemAware); Application.EnableVisualStyles(); Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); Application.ThreadException += (_, ex) => { try { File.AppendAllText(@"C:\NeuroBridge\_Core\Logs\tray_crash.log", DateTime.Now + " UI: " + ex.Exception + "\r\n"); } catch { } }; AppDomain.CurrentDomain.UnhandledException += (_, ex) => { try { File.AppendAllText(@"C:\NeuroBridge\_Core\Logs\tray_crash.log", DateTime.Now + " DOM: " + ex.ExceptionObject + "\r\n"); } catch { } };
      using var m = new Mutex(true, "Global\\NeuroBridge_Tray_Lock", out bool ok);
      if (!ok) return; Application.Run(new HubForm(silent));
    }
  }
  public class ProjBtn { public string text { get; set; } public string text_en { get; set; } public string type { get; set; } public string target { get; set; } public string bg { get; set; } public string bd { get; set; } }
  public class ProjItem { public string id { get; set; } public string name { get; set; } public string name_en { get; set; } public string path { get; set; } public string report { get; set; } public List<ProjBtn> buttons { get; set; } }
  public class ProjCfg { public string active { get; set; } public string lang { get; set; } public List<ProjItem> projects { get; set; } }
  public class UiSlotBox { public Form F; public Panel B; public int Cur, MarqPos, AnimW; public Label L1, L2; public Button Ab; public System.Windows.Forms.Timer Tm; public bool IsMarq; public DateTime T0; public int Tot; public string Tit; }
  public class HubForm : Form {
    readonly string root, coreR;
    readonly NotifyIcon tray; readonly Panel pMain, pSync, pNew, pExp, pSet, pProjBtns; readonly ComboBox cbProj;
    readonly CheckBox cbW, cbA, cbS; readonly TextBox tbSyncInfo, lblNew, tbNewName, tbNewErr, tbExpInfo, lExp1, lblSetTitle;
    readonly Label title; readonly Button btnX, btnCopyRep, btnErrRep, btnMap, btnSound, btnLang, btnProj, btnExp, btnDedup, btnSet, btnSetBack, btnExpBack, btnExpRun, btnNewBack, btnNewRun, btnSyncBack, btnSyncRun, btnSyncMods, btnSyncVsd;
    readonly CheckedListBox clbProjs; readonly List<ProjItem> expProjMap = new();
    readonly Dictionary<int, UiSlotBox> uiSlots = new();
    readonly ToolTip tip = new() { AutoPopDelay = 8000, InitialDelay = 250, ReshowDelay = 100 };
    readonly System.Windows.Forms.Timer tm = new() { Interval = 1000 };
    Point dragPt; bool dragging; string lastRenderedProj = "", curLang = "";
    Dictionary<string, string> L = new();
    public HubForm(bool silent = false) {
      root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")); coreR = Path.Combine(root, "_Core", "Runner"); RelocatePaths();
      FormBorderStyle = FormBorderStyle.None; Size = new Size(424, 340); StartPosition = FormStartPosition.Manual;
      var wa = Screen.PrimaryScreen.WorkingArea; Location = new Point(wa.Right - 440, wa.Bottom - 356);
      BackColor = ColorTranslator.FromHtml("#181B22"); ForeColor = Color.White; Font = new Font("Segoe UI", 9.5f); ShowInTaskbar = false;
      Paint += (_, e) => { using var p = new Pen(ColorTranslator.FromHtml("#38BDF8"), 2); e.Graphics.DrawRectangle(p, 1, 1, Width - 2, Height - 2); };
      var hdr = new Panel { Location = new Point(2, 2), Size = new Size(420, 36), BackColor = ColorTranslator.FromHtml("#0F2942") };
      title = new Label { Text = "  NeuroBridge", Location = new Point(6, 0), Size = new Size(376, 36), Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White };
      btnX = new Button { Text = "—", Location = new Point(384, 2), Size = new Size(34, 32), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#111318"), ForeColor = ColorTranslator.FromHtml("#94A3B8"), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
      btnX.FlatAppearance.BorderSize = 0; btnX.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#1E3A5F"); btnX.Click += (_, __) => Hide();
      MouseEventHandler md = (_, e) => { if (e.Button == MouseButtons.Left) { dragging = true; dragPt = e.Location; } };
      MouseEventHandler mm = (_, e) => { if (dragging) Location = new Point(Location.X + e.X - dragPt.X, Location.Y + e.Y - dragPt.Y); };
      MouseEventHandler mu = (_, __) => dragging = false;
      hdr.MouseDown += md; hdr.MouseMove += mm; hdr.MouseUp += mu; title.MouseDown += md; title.MouseMove += mm; title.MouseUp += mu;
      hdr.Controls.Add(title); hdr.Controls.Add(btnX);
      pMain = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent };
      cbProj = new ComboBox { Location = new Point(14, 10), Size = new Size(350, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
      cbProj.SelectedIndexChanged += (_, __) => SwitchProj(); cbProj.DropDown += (_, __) => CleanMissingProjects();
      var btnPlus = new Button { Text = "+", Location = new Point(370, 10), Size = new Size(36, 26), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#163A59"), ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
      btnPlus.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#38BDF8"); btnPlus.Click += (_, __) => OpenNewProj();
      btnCopyRep = MkBtn("", 14, 46, 392, CopyActiveReport, "#163A59", "#38BDF8");
      btnErrRep = MkBtn("", 14, 222, 392, HandleErrBtn, "#1E293B", "#38BDF8");
      pProjBtns = new Panel { Location = new Point(14, 84), Size = new Size(392, 132), BackColor = Color.Transparent };
      btnProj = MkBtn("", 14, 260, 180, () => Process.Start("explorer.exe", ActiveProj.path), "#222733", "#3B4252");
      btnMap = new Button { Text = "🗺", Location = new Point(198, 260), Size = new Size(40, 32), Padding = new Padding(0), TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#163A59"), ForeColor = Color.White, Font = new Font("Segoe UI Emoji", 10.5f) };
      btnMap.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#38BDF8"); btnMap.Click += (_, __) => OpenMemoryMap();
      btnExp = MkBtn("", 14, 232, 392, OpenExport, "#163A59", "#38BDF8");
      btnDedup = MkBtn("", 242, 260, 120, ResetHashes, "#4A2C11", "#D97706");
      btnSet = new Button { Text = "⚙", Location = new Point(366, 260), Size = new Size(40, 32), Padding = new Padding(0), TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, Font = new Font("Segoe UI Emoji", 10.5f) };
      btnSet.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#38BDF8"); btnSet.Click += (_, __) => { pMain.Visible = false; pSet.Visible = true; };
      foreach (Control c in new List<Control> { cbProj, btnPlus, btnCopyRep, pProjBtns, btnErrRep, btnProj, btnMap, btnDedup, btnSet }) pMain.Controls.Add(c);
      pSet = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      btnSetBack = MkBtn("", 14, 10, 120, () => { pSet.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252");
      lblSetTitle = MkInfoBox(14, 50, 392, 24, "#38BDF8", true);
      cbW = MkChk("", 82); cbA = MkChk("", 114); cbS = MkChk("", 146);
      cbW.Click += (_, __) => ToggleFlag("watcher.disabled", "watcher_go.ps1"); cbA.Click += (_, __) => ToggleFlag("alt_e.disabled", "hotkey_go.ps1"); cbS.Click += (_, __) => ToggleStartup();
      btnSound = new Button { Text = "🔊", Location = new Point(14, 188), Size = new Size(190, 34), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#144229"), ForeColor = Color.White, Font = new Font("Segoe UI Emoji", 12f) };
      btnSound.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#22C55E"); btnSound.Click += (_, __) => ToggleSound();
      btnLang = new Button { Text = "RU", Location = new Point(216, 188), Size = new Size(190, 34), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#163A59"), ForeColor = ColorTranslator.FromHtml("#38BDF8"), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
      btnLang.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#38BDF8"); btnLang.Click += (_, __) => ToggleLang();
      foreach (Control c in new List<Control> { btnSetBack, lblSetTitle, cbW, cbA, cbS, btnSound, btnLang, btnExp }) pSet.Controls.Add(c);
      pSync = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      tbSyncInfo = MkInfoBox(14, 50, 392, 74, "#D8E2EC", false); btnSyncBack = MkBtn("", 14, 10, 120, () => { pSync.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252");
      btnSyncRun = MkBtn("", 14, 132, 392, DoSyncMods, "#144229", "#22C55E");
      btnSyncMods = MkBtn("", 14, 170, 93, () => Process.Start("explorer.exe", @"\\DESKTOP-489MN5T\Users\Заяц\AppData\Roaming\VintagestoryData\Mods"), "#222733", "#3B4252");
      btnSyncVsd = MkBtn("", 113, 170, 93, () => Process.Start("explorer.exe", @"\\DESKTOP-489MN5T\Users\Заяц\AppData\Roaming\VintagestoryData"), "#222733", "#3B4252");
      foreach (Control c in new List<Control> { btnSyncBack, tbSyncInfo, btnSyncRun, btnSyncMods, btnSyncVsd }) pSync.Controls.Add(c);
      pNew = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      lblNew = MkInfoBox(14, 54, 392, 24, "#D8E2EC", true); tbNewName = new TextBox { Location = new Point(14, 86), Size = new Size(392, 28), BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f) };
      tbNewErr = MkInfoBox(14, 162, 392, 54, "#EF4444", false); btnNewBack = MkBtn("", 14, 10, 120, () => { pNew.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252"); btnNewRun = MkBtn("", 14, 124, 392, CreateProjConfirm, "#144229", "#22C55E");
      foreach (Control c in new List<Control> { btnNewBack, lblNew, tbNewName, btnNewRun, tbNewErr }) pNew.Controls.Add(c);
      pExp = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      lExp1 = MkInfoBox(14, 46, 392, 20, "#38BDF8", true); clbProjs = new CheckedListBox { Location = new Point(14, 68), Size = new Size(392, 144), CheckOnClick = true, BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
      tbExpInfo = MkInfoBox(14, 258, 392, 36, "#94A3B8", false); btnExpRun = MkBtn("", 14, 222, 392, DoExportZip, "#144229", "#22C55E");
      btnExpBack = MkBtn("", 14, 8, 120, () => { pExp.Visible = false; pSet.Visible = true; }, "#222733", "#3B4252");
      foreach (Control c in new List<Control> { btnExpBack, lExp1, clbProjs, btnExpRun, tbExpInfo }) pExp.Controls.Add(c);
      Controls.Add(hdr); Controls.Add(pMain); Controls.Add(pSet); Controls.Add(pSync); Controls.Add(pNew); Controls.Add(pExp);
      var mn = new ContextMenuStrip(); mn.Items.Add("Open / Hide", null, (_, __) => ToggleWin()); mn.Items.Add("Exit", null, (_, __) => { StopRunners(); tray.Visible = false; Application.Exit(); });
      tray = new NotifyIcon { Text = "NeuroBridge", ContextMenuStrip = mn, Visible = true }; tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleWin(); };
      LoadProjects(); tm.Tick += (_, __) => Sync(); tm.Start(); Sync(); if (silent) Opacity = 0;
      var pt = new Thread(PipeLoop) { IsBackground = true }; pt.Start(); if (silent) Shown += (_, __) => { Hide(); Opacity = 1; };
    }
    void PipeLoop() {
      while (true) {
        try {
          using var srv = new NamedPipeServerStream("NeuroBridge_UI_Pipe", PipeDirection.In, 4);
          srv.WaitForConnection(); using var sr = new StreamReader(srv, Encoding.UTF8);
          string line = sr.ReadLine();
          if (!string.IsNullOrEmpty(line) && IsHandleCreated && !IsDisposed) BeginInvoke(new Action(() => { if (!IsDisposed) HandlePipeMsg(line); }));
        } catch { Thread.Sleep(50); }
      }
    }
    void PlayWav(string wav) { try { if (!File.Exists(Path.Combine(coreR, "sound.disabled")) && !string.IsNullOrEmpty(wav) && File.Exists(wav)) new SoundPlayer(wav).Play(); } catch { } }
    void CloseSlotBox(int slot) { try { if (uiSlots.TryGetValue(slot, out var b)) { if (b.Tm != null) { b.Tm.Stop(); b.Tm.Dispose(); } if (b.F != null && !b.F.IsDisposed) { b.F.Close(); b.F.Dispose(); } uiSlots.Remove(slot); } } catch { } }
    void AbortSlotRun(int slot) { CloseSlotBox(slot); var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(coreR, "abort_step.ps1") + "\"") { CreateNoWindow = true, UseShellExecute = false }; var p = Process.Start(psi); if (p != null) p.WaitForExit(4000); CopyFileToClip(Path.Combine(root, "NeuroBridge_error.txt")); ShowUiToast(slot, curLang == "ru" ? "Прервано" : "Aborted", "NeuroBridge_error.txt", "#451A1A", "#EF4444", Path.Combine(coreR, "_sounds", "error_glass.wav"), 3500); }
    UiSlotBox EnsureSlotBox(int slot, string titleText, int tot, bool marquee) {
      if (!uiSlots.TryGetValue(slot, out var b) || b.F == null || b.F.IsDisposed) {
        var f = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true, ShowInTaskbar = false, Size = new Size(360, 68), BackColor = ColorTranslator.FromHtml("#163A59") };
        var wa = Screen.PrimaryScreen.WorkingArea; f.Location = new Point(wa.Right - 376, wa.Bottom - (98 + slot * 92));
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
        p.Paint += (_, e) => { using var pen = new Pen(ColorTranslator.FromHtml("#38BDF8"), 2); e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 2, p.Height - 2); };
        var l1 = new Label { Location = new Point(12, 7), Size = new Size(256, 18), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        var ab = new Button { Location = new Point(272, 5), Size = new Size(78, 22), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#7F1D1D"), ForeColor = Color.White, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
        ab.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#EF4444"); ab.Click += (_, __) => AbortSlotRun(slot);
        var pb = new Panel { Location = new Point(12, 28), Size = new Size(336, 10), BackColor = ColorTranslator.FromHtml("#0F2942") };
        var l2 = new Label { Location = new Point(12, 44), Size = new Size(336, 18), ForeColor = ColorTranslator.FromHtml("#D8E2EC"), Font = new Font("Segoe UI", 9f) };
        var tm = new System.Windows.Forms.Timer { Interval = 16 }; typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(pb, true, null);
        p.Controls.Add(l1); p.Controls.Add(ab); p.Controls.Add(pb); p.Controls.Add(l2); ab.BringToFront(); f.Controls.Add(p); f.Show();
        b = new UiSlotBox { F = f, B = pb, L1 = l1, L2 = l2, Ab = ab, Tm = tm }; uiSlots[slot] = b;
        pb.Paint += (_, e) => { bool done = !b.IsMarq && b.Cur >= b.Tot; using var br = new SolidBrush(ColorTranslator.FromHtml(done ? "#22C55E" : "#38BDF8")); e.Graphics.FillRectangle(br, 0, 0, Math.Max(4, b.AnimW), pb.Height); }; tm.Tick += (_, __) => { int tgt = b.IsMarq ? (pb.Width * Math.Min(92, 15 + b.MarqPos)) / 100 : (int)((pb.Width * (long)Math.Min(b.Cur, b.Tot)) / Math.Max(1, b.Tot)); if (b.IsMarq && b.MarqPos < 77) b.MarqPos += 2; if (b.AnimW < tgt) { b.AnimW += Math.Max(1, (tgt - b.AnimW + 3) / 4); pb.Invalidate(); } else if (b.AnimW > tgt) { b.AnimW = tgt; pb.Invalidate(); } if (b.IsMarq) { double s = (DateTime.Now - b.T0).TotalSeconds; b.L2.Text = (curLang == "ru" ? "Выполняется... " : "Running... ") + s.ToString("0.00") + (curLang == "ru" ? " с" : "s"); } };
      }
      b.T0 = DateTime.Now; b.Tot = Math.Max(1, tot); b.Tit = titleText; b.IsMarq = marquee;
      b.Ab.Text = curLang == "ru" ? "Прервать" : "Abort"; b.Cur = 0; b.MarqPos = 0; b.AnimW = 4; b.B.Invalidate();
       b.L1.Text = marquee ? titleText : titleText + " (0 / " + b.Tot + ")";
      b.L2.Text = curLang == "ru" ? "Выполняется... 0.00 с" : "Running... 0.00s"; b.Tm.Start(); return b;
    }
    void ShowUiToast(int slot, string tit, string sub, string bg, string bd, string wav, int hold) {
      CloseSlotBox(slot);
      var f = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true, ShowInTaskbar = false, Size = new Size(360, 68), BackColor = ColorTranslator.FromHtml(bg) };
      var wa = Screen.PrimaryScreen.WorkingArea; f.Location = new Point(wa.Right - 376, wa.Bottom - (80 + slot * 92));
      var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
      p.Paint += (_, e) => { using var pen = new Pen(ColorTranslator.FromHtml(bd), 2); e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 2, p.Height - 2); };
      var l1 = new Label { Text = tit, Dock = DockStyle.Top, Height = 22, ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
      var l2 = new Label { Text = sub, Dock = DockStyle.Fill, ForeColor = ColorTranslator.FromHtml("#D8E2EC"), Font = new Font("Segoe UI", 9.5f) };
      p.Controls.Add(l2); p.Controls.Add(l1); f.Controls.Add(p); f.Show(); PlayWav(wav);
      uiSlots[slot] = new UiSlotBox { F = f }; if (hold >= 0) { var t = new System.Windows.Forms.Timer { Interval = Math.Max(300, hold) };
      t.Tick += (_, __) => { t.Stop(); t.Dispose(); CloseSlotBox(slot); }; t.Start(); }
    }
    void HandlePipeMsg(string line) {
      var parts = line.Split('|'); if (parts.Length < 2) return;
      int.TryParse(parts.ElementAt(1), out int slot); string cmd = parts.ElementAt(0);
      if (cmd == "START" && parts.Length >= 3) EnsureSlotBox(slot, parts.ElementAt(2), 100, true);
      else if (cmd == "INIT" && parts.Length >= 4) { int.TryParse(parts.ElementAt(3), out int tot); EnsureSlotBox(slot, parts.ElementAt(2), tot, false); }
      else if (cmd == "PROG" && parts.Length >= 4 && uiSlots.TryGetValue(slot, out var b)) {
        int.TryParse(parts.ElementAt(2), out int cur); b.IsMarq = false;
        int c = Math.Min(cur, b.Tot); int pct = (c * 100) / b.Tot; b.Cur = c; b.B.Invalidate();
        b.L1.Text = b.Tit + ": " + c + (curLang == "ru" ? " из " : " of ") + b.Tot + " (" + pct + "%)";
        double el = (DateTime.Now - b.T0).TotalSeconds; int eta = c > 0 ? (int)((el / c) * (b.Tot - c)) : 0;
        string ts = curLang == "ru" ? (eta >= 60 ? "~" + (eta / 60) + " мин " + (eta % 60) + " сек" : "~" + eta + " сек") : (eta >= 60 ? "~" + (eta / 60) + "m " + (eta % 60) + "s" : "~" + eta + "s");
        b.L2.Text = (curLang == "ru" ? "Осталось: " : "Remaining: ") + ts + " | " + parts.ElementAt(3);
      } else if (cmd == "CLOSE" && uiSlots.TryGetValue(slot, out var bc)) { bc.IsMarq = false; bc.Cur = bc.Tot; bc.AnimW = bc.B.Width; bc.B.Invalidate(); var tc = new System.Windows.Forms.Timer { Interval = 160 }; tc.Tick += (_, __) => { tc.Stop(); tc.Dispose(); CloseSlotBox(slot); }; tc.Start(); } else if (cmd == "CLOSE") CloseSlotBox(slot);
      else if (cmd == "TOAST" && parts.Length >= 8) { int.TryParse(parts.ElementAt(7), out int hold); ShowUiToast(slot, parts.ElementAt(2), parts.ElementAt(3), parts.ElementAt(4), parts.ElementAt(5), parts.ElementAt(6), hold); }
    }
    void RelocatePaths() { try { var c = LoadCfgObj(); bool ch = false; foreach (var p in c.projects) { string exp = p.id == "NeuroBridge_Core" ? root : Path.Combine(root, "Projects", p.id); if (!string.Equals(p.path, exp, StringComparison.OrdinalIgnoreCase)) { p.path = exp; ch = true; } } if (ch) SaveCfgObj(c); } catch { } }
    string CfgFile => Path.Combine(coreR, "projects.json");
    ProjCfg LoadCfgObj() => JsonSerializer.Deserialize<ProjCfg>(File.ReadAllText(CfgFile));
    void SaveCfgObj(ProjCfg c) => File.WriteAllText(CfgFile, JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
    ProjItem ActiveProj { get { var c = LoadCfgObj(); return c.projects.First(x => x.id == c.active); } }
    string Tr(string k) => L.TryGetValue(k, out var v) ? v : k;
    void CopyFileToClip(string f) { if (File.Exists(f)) { var sc = new System.Collections.Specialized.StringCollection(); sc.Add(f); Clipboard.SetFileDropList(sc); ShowUiToast(GetFreeSlot(), curLang == "ru" ? "Скопировано в буфер" : "Copied to clipboard", Path.GetFileName(f), "#163A59", "#38BDF8", "", 1400); } }
    void HandleErrBtn() { string fl = Path.Combine(root, "_Core", "Logs", "error_report_ready.flag"); if (File.Exists(fl)) { string p = File.ReadAllText(fl, Encoding.UTF8).Trim(); if (File.Exists(p)) { CopyFileToClip(p); return; } File.Delete(fl); } string fp = Path.Combine(root, "NeuroBridge_error.txt"); if (File.Exists(fp)) Process.Start("explorer.exe", "/select,\"" + fp + "\""); else Process.Start("explorer.exe", root); }
    void CopyActiveReport() { var ap = ActiveProj; string rep = string.IsNullOrEmpty(ap.report) ? Tr("core_report") : ap.report; CopyFileToClip(Path.Combine(ap.path, rep)); }
    void ToggleSound() { string sf = Path.Combine(coreR, "sound.disabled"); if (File.Exists(sf)) File.Delete(sf); else File.WriteAllText(sf, "1"); Sync(); }
    void ApplyUiLang(string lng) {
      string lf = Path.Combine(root, "_Core", "Lang", lng + ".json");
      if (File.Exists(lf)) L = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(lf));
      curLang = lng; btnLang.Text = lng == "ru" ? "Язык: RU" : "Lang: EN"; LoadProjects(); lastRenderedProj = ""; title.Text = Tr("title");
      lblSetTitle.Text = lng == "ru" ? "Настройки конвейера и интерфейса" : "Pipeline & UI Settings";
      cbW.Text = Tr("chk_watch"); cbA.Text = Tr("chk_alte"); cbS.Text = Tr("chk_start");
      btnProj.Text = Tr("btn_proj"); btnExp.Text = Tr("btn_exp"); btnDedup.Text = Tr("btn_dedup");
      tip.SetToolTip(btnDedup, lng == "ru" ? "Сбросить защиту от повтора" : "Reset duplicate hash");
      tip.SetToolTip(btnMap, lng == "ru" ? "Карта связей документации" : "Memory & Docs Map");
      tip.SetToolTip(btnX, lng == "ru" ? "Свернуть в трей" : "Minimize to tray"); tip.SetToolTip(btnSet, lng == "ru" ? "Настройки" : "Settings"); tip.SetToolTip(btnProj, lng == "ru" ? "Открыть папку проекта" : "Open project folder"); tip.SetToolTip(btnExp, lng == "ru" ? "Экспорт чистого архива на Рабочий стол" : "Export clean archive to Desktop"); tip.SetToolTip(btnExpRun, lng == "ru" ? "Сохранить чистый ZIP на Рабочий стол" : "Save clean ZIP to Desktop"); tip.SetToolTip(btnSound, lng == "ru" ? "Включить / выключить звуки" : "Toggle sound notifications"); tip.SetToolTip(btnLang, lng == "ru" ? "Переключить язык интерфейса и чата (RU / EN)" : "Switch UI & chat language (RU / EN)");
      lExp1.Text = Tr("exp_p1"); btnExpRun.Text = Tr("exp_run"); tbExpInfo.Text = Tr("exp_hint"); btnSyncBack.Text = Tr("sync_back"); btnNewBack.Text = Tr("sync_back"); btnExpBack.Text = Tr("sync_back"); btnSetBack.Text = Tr("sync_back"); lblNew.Text = Tr("new_title"); btnNewRun.Text = Tr("new_create"); btnSyncRun.Text = Tr("sync_run"); btnSyncMods.Text = Tr("pc2_mods"); btnSyncVsd.Text = Tr("pc2_vsd");
    }
    void ToggleLang() { string next = curLang == "ru" ? "en" : "ru"; string q = ((char)34).ToString(); Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File " + q + Path.Combine(coreR, "apply_lang.ps1") + q + " " + next) { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000); Sync(); }
    void OpenExport() { clbProjs.Items.Clear(); expProjMap.Clear(); foreach (var p in LoadCfgObj().projects.Where(x => x.id != "NeuroBridge_Core")) { expProjMap.Add(p); clbProjs.Items.Add(p.name + " (" + p.id + ")", false); } tbExpInfo.Text = Tr("exp_hint"); pSet.Visible = false; pExp.Visible = true; }
    int GetFreeSlot() { for (int s = 0; s < 8; s++) if (!uiSlots.ContainsKey(s)) return s; return 0; }
    bool SkipDir(string n) => n == "Projects" || n == "obj" || n == "bin" || n.StartsWith("queue_");
    bool SkipFile(string fn) => fn.EndsWith(".hash") || fn.EndsWith(".lock") || fn.StartsWith("step_") || fn.StartsWith("bg_step_") || fn.StartsWith("NeuroBridge_Export_");
    void CollectFiles(string src, string rel, List<(string full, string entry)> list) {
      if (!Directory.Exists(src) || SkipDir(Path.GetFileName(src))) return;
      foreach (var f in Directory.GetFiles(src)) { string fn = Path.GetFileName(f); if (!SkipFile(fn)) list.Add((f, rel + "/" + fn)); }
      foreach (var d in Directory.GetDirectories(src)) CollectFiles(d, rel + "/" + Path.GetFileName(d), list);
    }
    void DoExportZip() {
      btnExpRun.Enabled = false; int sl = GetFreeSlot(); var sel = new List<ProjItem>();
      for (int i = 0; i < clbProjs.Items.Count; i++) if (clbProjs.GetItemChecked(i)) sel.Add(expProjMap.ElementAt(i));
      string tit = curLang == "ru" ? "Экспорт NeuroBridge" : "NeuroBridge Export"; EnsureSlotBox(sl, tit, 100, true);
      Task.Run(() => {
        try {
          var files = new List<(string full, string entry)>(); CollectFiles(root, "NeuroBridge", files);
          var corePr = LoadCfgObj().projects.First(x => x.id == "NeuroBridge_Core");
          var newCfg = new ProjCfg { active = "NeuroBridge_Core", lang = curLang, projects = new List<ProjItem> { corePr } };
          foreach (var pr in sel) { newCfg.projects.Add(pr); CollectFiles(pr.path, "NeuroBridge/Projects/" + pr.id, files); }
          int tot = files.Count + 1, cur = 0; long lastUi = 0; BeginInvoke(new Action(() => EnsureSlotBox(sl, tit, tot, false)));
          string desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
          string zipOut = Path.Combine(desk, "NeuroBridge_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");
          using (var za = ZipFile.Open(zipOut, ZipArchiveMode.Create)) {
            za.CreateEntry("NeuroBridge/Projects/"); za.CreateEntry("NeuroBridge/_Backups/");
            foreach (var it in files) {
              if (!it.entry.Equals("NeuroBridge/_Core/Runner/projects.json", StringComparison.OrdinalIgnoreCase)) za.CreateEntryFromFile(it.full, it.entry, CompressionLevel.Optimal);
              cur++; long now = Environment.TickCount64;
              if (now - lastUi >= 40 || cur == tot) { lastUi = now; int c = cur; string fn = Path.GetFileName(it.full); BeginInvoke(new Action(() => HandlePipeMsg("PROG|" + sl + "|" + c + "|" + fn))); }
            }
            var pe = za.CreateEntry("NeuroBridge/_Core/Runner/projects.json");
            using (var sw = new StreamWriter(pe.Open(), Encoding.UTF8)) sw.Write(JsonSerializer.Serialize(newCfg, new JsonSerializerOptions { WriteIndented = true }));
            BeginInvoke(new Action(() => HandlePipeMsg("PROG|" + sl + "|" + tot + "|projects.json")));
          }
          string wav = Path.Combine(coreR, "_sounds", "task_ok.wav"), okT = curLang == "ru" ? "Экспорт завершён" : "Export complete";
          BeginInvoke(new Action(() => { CloseSlotBox(sl); ShowUiToast(sl, okT, Path.GetFileName(zipOut), "#144229", "#22C55E", wav, 4000); tbExpInfo.Text = "OK: " + Path.GetFileName(zipOut); btnExpRun.Enabled = true; }));
        } catch (Exception ex) { BeginInvoke(new Action(() => { CloseSlotBox(sl); ShowUiToast(sl, "Export Error", ex.Message, "#451A1A", "#EF4444", "", 5000); tbExpInfo.Text = ex.Message; btnExpRun.Enabled = true; })); }
      });
    }
    string BtnLabel(ProjBtn b) => (curLang == "en" && !string.IsNullOrEmpty(b.text_en)) ? b.text_en : b.text;
    void RenderProjButtons(ProjItem p) {
      pProjBtns.Controls.Clear(); var btns = p.buttons ?? new List<ProjBtn>();
      if (btns.Count <= 2) {
        for (int i = 0; i < btns.Count; i++) { var btn = btns.ElementAt(i); pProjBtns.Controls.Add(MkBtn(BtnLabel(btn), 0, i * 36, 392, () => ExecProjBtn(btn), btn.bg ?? "#222733", btn.bd ?? "#3B4252")); }
      } else {
        var b0 = btns.ElementAt(0); pProjBtns.Controls.Add(MkBtn(BtnLabel(b0), 0, 0, 392, () => ExecProjBtn(b0), b0.bg ?? "#222733", b0.bd ?? "#3B4252"));
        var xs = new List<int> { 0, 99, 198 }; var ws = new List<int> { 93, 93, 194 };
        for (int i = 1; i < Math.Min(btns.Count, 4); i++) { var btn = btns.ElementAt(i); int c = i - 1; pProjBtns.Controls.Add(MkBtn(BtnLabel(btn), xs.ElementAt(c), 36, ws.ElementAt(c), () => ExecProjBtn(btn), btn.bg ?? "#222733", btn.bd ?? "#3B4252")); }
      }
    }
    void ExecProjBtn(ProjBtn b) {
      if (b.type == "run_lnk") RunProjLnk(b.target);
      else if (b.type == "open_sync") { pMain.Visible = false; pSync.Visible = true; UpdateSyncInfo(); }
      else if (b.type == "open_dir" && Directory.Exists(b.target)) Process.Start("explorer.exe", b.target);
      else if (b.type == "copy_handoff") CopyFileToClip(Path.Combine(root, Tr("start_file")));
      else if (b.type == "run_ps1" && File.Exists(b.target)) Process.Start(new ProcessStartInfo("powershell.exe", "-sta -NoProfile -ExecutionPolicy Bypass -File \"" + b.target + "\"") { CreateNoWindow = true, UseShellExecute = false });
    }
    void CleanMissingProjects() {
      var c = LoadCfgObj(); bool chg = false; int n = c.projects.Count;
      c.projects = c.projects.Where(x => x.id == "NeuroBridge_Core" || Directory.Exists(x.path)).ToList(); if (c.projects.Count != n) chg = true;
      string pRoot = Path.Combine(root, "Projects");
      if (Directory.Exists(pRoot)) { foreach (var dir in Directory.GetDirectories(pRoot)) { string id = Path.GetFileName(dir); if (id.StartsWith("_")) continue; if (!c.projects.Any(x => x.id.Equals(id, StringComparison.OrdinalIgnoreCase))) { c.projects.Add(new ProjItem { id = id, name = id, path = dir, report = id + "_report.txt", buttons = new List<ProjBtn>() }); chg = true; } } }
      if (chg) { if (!c.projects.Any(x => x.id == c.active)) c.active = "NeuroBridge_Core"; SaveCfgObj(c); } LoadProjects();
    }
    void LoadProjects() { var c = LoadCfgObj(); cbProj.Items.Clear(); foreach (var p in c.projects) cbProj.Items.Add((curLang == "en" && !string.IsNullOrEmpty(p.name_en)) ? p.name_en : p.name); int i = c.projects.FindIndex(x => x.id == c.active); if (i >= 0) cbProj.SelectedIndex = i; }
    void SwitchProj() { if (cbProj.SelectedIndex < 0) return; var c = LoadCfgObj(); c.active = c.projects.ElementAt(cbProj.SelectedIndex).id; SaveCfgObj(c); Sync(); }
    void OpenNewProj() { tbNewName.Text = ""; tbNewErr.Text = ""; pMain.Visible = false; pNew.Visible = true; tbNewName.Focus(); }
    void CreateProjConfirm() {
      string raw = (tbNewName.Text ?? "").Trim(); if (raw.Length < 2) { tbNewErr.Text = Tr("new_err_short"); return; }
      if (Regex.IsMatch(raw, @"[^a-zA-Z0-9 _-]")) { tbNewErr.Text = Tr("new_err_latin"); return; }
      string slug = Regex.Replace(raw, @"[ -]+", "_").Trim('_'); if (slug.Length < 2) slug = "Project_" + DateTime.Now.ToString("HHmmss");
      var c = LoadCfgObj(); if (c.projects.Any(x => x.id.Equals(slug, StringComparison.OrdinalIgnoreCase))) { tbNewErr.Text = Tr("new_err_dup"); return; }
      string q = ((char)34).ToString(); string args = "-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File " + q + Path.Combine(coreR, "new_project.ps1") + q + " -Name " + q + raw + q + " -Slug " + q + slug + q;
      var p = Process.Start(new ProcessStartInfo("powershell.exe", args) { UseShellExecute = false, CreateNoWindow = true }); if (p != null) p.WaitForExit(5000);
      LoadProjects(); pNew.Visible = false; pMain.Visible = true; Sync();
    }
    void ToggleFlag(string f, string ps) { string p = Path.Combine(coreR, f); if (File.Exists(p)) { File.Delete(p); RunPs(ps); } else File.WriteAllText(p, "1"); Sync(); }
    TextBox MkInfoBox(int x, int y, int w, int h, string hex, bool b) => new TextBox { Location = new Point(x, y), Size = new Size(w, h), Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = ColorTranslator.FromHtml("#181B22"), ForeColor = ColorTranslator.FromHtml(hex), Font = new Font("Segoe UI", 9.5f, b ? FontStyle.Bold : FontStyle.Regular), TabStop = false };
    string SyncStampFile => Path.Combine(ActiveProj.path, "03_Tools", "last_sync_mods.txt");
    string ResolveIp(string h) { try { var ip = Dns.GetHostAddresses(h).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork); return ip != null ? ip.ToString() : "offline"; } catch { return "offline"; } }
    void UpdateSyncInfo() { string st = File.Exists(SyncStampFile) ? File.ReadAllText(SyncStampFile).Trim() : "-"; tbSyncInfo.Text = "DESKTOP-489MN5T | IP: " + ResolveIp("DESKTOP-489MN5T") + "\r\n" + Tr("sync_last") + ": " + st; }
    void DoSyncMods() { RunProjLnk("Update_Mods_PC2.lnk"); File.WriteAllText(SyncStampFile, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")); UpdateSyncInfo(); }
    void ToggleWin() { if (Visible) Hide(); else { Show(); WindowState = FormWindowState.Normal; Activate(); } }
    CheckBox MkChk(string t, int y) => new CheckBox { Text = t, Location = new Point(16, y), Size = new Size(388, 24), ForeColor = ColorTranslator.FromHtml("#D8E2EC") };
    Button MkBtn(string t, int x, int y, int w, Action a, string bg, string bd) { var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(w, 32), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml(bg), ForeColor = Color.White }; b.FlatAppearance.BorderColor = ColorTranslator.FromHtml(bd); b.Click += (_, __) => a(); return b; }
    string StLnk => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "NeuroBridgeTray.lnk");
    void RunPs(string f) { string q = ((char)34).ToString(); Process.Start(new ProcessStartInfo("powershell.exe", "-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File " + q + Path.Combine(coreR, f) + q) { UseShellExecute = false, CreateNoWindow = true }); }
    void RunProjLnk(string n) { var p = Path.Combine(ActiveProj.path, n); if (File.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); }
    void ResetHashes() { foreach (var f in new List<string> { "last_step.hash", "last_watcher.hash" }) { var p = Path.Combine(coreR, f); if (File.Exists(p)) File.Delete(p); } }
    void ToggleStartup() { if (File.Exists(StLnk)) File.Delete(StLnk); else { var p = Path.Combine(root, Tr("lnk_name")); if (File.Exists(p)) File.Copy(p, StLnk, true); } Sync(); }
    void KillPidFile(string pf) { try { if (File.Exists(pf)) { if (int.TryParse(File.ReadAllText(pf).Trim(), out int pid)) { var p = Process.GetProcessById(pid); if (!p.HasExited) p.Kill(); } File.Delete(pf); } } catch { } } void StopRunners() { KillPidFile(Path.Combine(coreR, "watcher.pid")); KillPidFile(Path.Combine(coreR, "hotkey.pid")); } bool IsPidAlive(string pf) { try { if (!File.Exists(pf)) return false; if (!int.TryParse(File.ReadAllText(pf).Trim(), out int pid)) return false; var p = Process.GetProcessById(pid); return !p.HasExited && p.ProcessName.Equals("powershell", StringComparison.OrdinalIgnoreCase); } catch { return false; } }
    int healTick = 0; void EnsureRunners(bool w, bool a) { if (++healTick % 3 != 1) return; if (w && !IsPidAlive(Path.Combine(coreR, "watcher.pid"))) RunPs("watcher_go.ps1"); if (a && !IsPidAlive(Path.Combine(coreR, "hotkey.pid"))) RunPs("hotkey_go.ps1"); }
    void Sync() { var cfg = LoadCfgObj(); string lng = string.IsNullOrEmpty(cfg.lang) ? "en" : cfg.lang; if (curLang != lng) ApplyUiLang(lng); bool w = !File.Exists(Path.Combine(coreR, "watcher.disabled")), a = !File.Exists(Path.Combine(coreR, "alt_e.disabled")), snd = !File.Exists(Path.Combine(coreR, "sound.disabled")); cbW.Checked = w; cbA.Checked = a; cbS.Checked = File.Exists(StLnk); btnSound.Text = snd ? "🔊" : "🔇"; btnSound.BackColor = ColorTranslator.FromHtml(snd ? "#144229" : "#4A2C11"); btnSound.FlatAppearance.BorderColor = ColorTranslator.FromHtml(snd ? "#22C55E" : "#D97706"); var ap = cfg.projects.First(x => x.id == cfg.active); string rep = string.IsNullOrEmpty(ap.report) ? Tr("core_report") : ap.report; btnCopyRep.Text = (lng == "ru" ? "Скопировать" : "Copy") + " " + rep; if (lastRenderedProj != ap.id) { lastRenderedProj = ap.id; RenderProjButtons(ap); } string fl = Path.Combine(root, "_Core", "Logs", "error_report_ready.flag"); bool hasE = File.Exists(fl); btnErrRep.Text = lng == "ru" ? (hasE ? "Скопировать отчёт об ошибке" : "Открыть папку с отчётом об ошибке") : (hasE ? "Copy error report" : "Open error report folder"); btnErrRep.BackColor = ColorTranslator.FromHtml(hasE ? "#7F1D1D" : "#144229"); btnErrRep.FlatAppearance.BorderColor = ColorTranslator.FromHtml(hasE ? "#EF4444" : "#22C55E"); string icoName = w ? "nb_auto.ico" : (a ? "nb_alte.ico" : "nb_pause.ico"); string icoPath = Path.Combine(root, "_Core", "TrayApp", icoName); if (File.Exists(icoPath)) tray.Icon = new Icon(icoPath); EnsureRunners(w, a); }
    void OpenMemoryMap() { new MemoryMapForm(root, curLang).Show(); }
  }
  public class MemoryMapForm : Form {
    readonly string root, lng; Point dragPt; bool dragging;
    public MemoryMapForm(string r, string l) {
      root = r; lng = l; FormBorderStyle = FormBorderStyle.None; Size = new Size(780, 480); StartPosition = FormStartPosition.CenterScreen;
      BackColor = ColorTranslator.FromHtml("#181B22"); ForeColor = Color.White; Font = new Font("Segoe UI", 9f); DoubleBuffered = true;
      var hdr = new Panel { Location = new Point(2, 2), Size = new Size(776, 34), BackColor = ColorTranslator.FromHtml("#0F2942") };
      var tit = new Label { Text = lng == "ru" ? "  NeuroBridge — Карта памяти и маршрутов документации" : "  NeuroBridge — Memory & Docs Route Map", Location = new Point(6, 0), Size = new Size(730, 34), Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
      var bx = new Button { Text = "X", Location = new Point(740, 2), Size = new Size(34, 30), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#111318"), ForeColor = ColorTranslator.FromHtml("#94A3B8"), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
      bx.FlatAppearance.BorderSize = 0; bx.Click += (_, __) => Close(); hdr.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { dragging = true; dragPt = e.Location; } };
      hdr.MouseMove += (_, e) => { if (dragging) Location = new Point(Location.X + e.X - dragPt.X, Location.Y + e.Y - dragPt.Y); }; hdr.MouseUp += (_, __) => dragging = false; hdr.Controls.Add(tit); hdr.Controls.Add(bx); Controls.Add(hdr);
      var t = new System.Windows.Forms.Timer { Interval = 1000 }; t.Tick += (_, __) => Invalidate(); t.Start(); FormClosed += (_, __) => t.Dispose();
    }
    string GetJsonVal(string raw, string key) { var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*\"([^\"]+)\""); return m.Success ? m.Groups.Cast<Group>().Skip(1).First().Value : ""; }
    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      using (var bp = new Pen(ColorTranslator.FromHtml("#38BDF8"), 2)) g.DrawRectangle(bp, 1, 1, Width - 2, Height - 2);
      string mf = Path.Combine(root, "_Core", "Logs", "last_manifest.json"); string raw = File.Exists(mf) ? File.ReadAllText(mf, Encoding.UTF8) : "";
      string rep = GetJsonVal(raw, "report"); string ts = GetJsonVal(raw, "ts"); string pid = GetJsonVal(raw, "proj");
      DrawBox(g, 18, 48, 220, 414, lng == "ru" ? "Законы Ядра (_Core\\Docs)" : "_Core\\Docs (Core Laws)", "#38BDF8"); DrawBox(g, 280, 140, 220, 210, lng == "ru" ? "Конвейер (auto_footer.ps1)" : "Pipeline (auto_footer.ps1)", "#22C55E"); DrawBox(g, 542, 48, 220, 414, lng == "ru" ? "Документы проекта" : "Projects\\02_Docs (Project)", "#D97706");
      using (var ap = new Pen(ColorTranslator.FromHtml("#22C55E"), 2)) { g.DrawLine(ap, 238, 245, 280, 245); g.DrawLine(ap, 542, 245, 500, 245); }
      int y = 78; foreach (var f in Directory.GetFiles(Path.Combine(root, "_Core", "Docs"), "*.md").OrderBy(x => x)) { string fn = Path.GetFileName(f); DrawNode(g, 26, y, 204, 24, fn, raw.Contains(fn)); y += 28; }
      g.DrawString((lng == "ru" ? "Отчёт: " : "Report: ") + (rep == "" ? "NeuroBridge_report.txt" : rep), new Font("Segoe UI", 9f, FontStyle.Bold), Brushes.White, 290, 172); g.DrawString((lng == "ru" ? "Синхр.: " : "Last sync: ") + (ts == "" ? "-" : ts), new Font("Segoe UI", 8.5f), Brushes.LightGray, 290, 196);
      g.DrawString(lng == "ru" ? "Зелёный = передано в отчёте" : "Green = SENT in last report", new Font("Segoe UI", 8.5f), Brushes.LightGreen, 290, 226); g.DrawString(lng == "ru" ? "Серый = не требуется" : "Gray = IDLE (not needed)", new Font("Segoe UI", 8.5f), Brushes.Gray, 290, 246);
      string pd = Path.Combine(root, "Projects", pid == "" ? "VS_Mods" : pid, "02_Docs"); y = 78;
      if (Directory.Exists(pd)) { foreach (var f in Directory.GetFiles(pd, "*.md").OrderBy(x => x)) { string fn = Path.GetFileName(f); if (fn == "DIALOG_LOG.md") continue; DrawNode(g, 550, y, 204, 24, fn, raw.Contains(fn)); y += 28; if (y > 430) break; } }
    }
    void DrawBox(Graphics g, int x, int y, int w, int h, string t, string hex) { using var br = new SolidBrush(ColorTranslator.FromHtml("#111318")); using var pn = new Pen(ColorTranslator.FromHtml(hex), 1); g.FillRectangle(br, x, y, w, h); g.DrawRectangle(pn, x, y, w, h); g.DrawString(t, new Font("Segoe UI", 9f, FontStyle.Bold), new SolidBrush(ColorTranslator.FromHtml(hex)), x + 8, y + 6); }
    void DrawNode(Graphics g, int x, int y, int w, int h, string t, bool on) { using var br = new SolidBrush(ColorTranslator.FromHtml(on ? "#144229" : "#222733")); using var pn = new Pen(ColorTranslator.FromHtml(on ? "#22C55E" : "#3B4252"), 1); g.FillRectangle(br, x, y, w, h); g.DrawRectangle(pn, x, y, w, h); g.DrawString((on ? "● " : "○ ") + t, new Font("Segoe UI", 8.5f, on ? FontStyle.Bold : FontStyle.Regular), on ? Brushes.White : Brushes.LightGray, x + 6, y + 4); }
  }
}

























