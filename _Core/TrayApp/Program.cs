// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 psytuner (github.com/psytuner/NeuroBridge)
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
      Application.SetHighDpiMode(HighDpiMode.SystemAware);
      Application.EnableVisualStyles();
      using var m = new Mutex(true, "Global\\NeuroBridge_Tray_Lock", out bool ok);
      if (!ok) return;
      Application.Run(new HubForm(silent));
    }
  }
  public class ProjBtn { public string text { get; set; } public string type { get; set; } public string target { get; set; } public string bg { get; set; } public string bd { get; set; } }
  public class ProjItem { public string id { get; set; } public string name { get; set; } public string path { get; set; } public string report { get; set; } public List<ProjBtn> buttons { get; set; } }
  public class ProjCfg { public string active { get; set; } public string lang { get; set; } public List<ProjItem> projects { get; set; } }
  public class UiSlotBox { public Form F; public ProgressBar B; public Label L1, L2; public DateTime T0; public int Tot; public string Tit; }
  public class HubForm : Form {
    readonly string root, coreR;
    readonly NotifyIcon tray; readonly Panel pMain, pSync, pNew, pExp, pProjBtns; readonly ComboBox cbProj;
    readonly CheckBox cbW, cbA, cbS; readonly TextBox tbSyncInfo, lblNew, tbNewName, tbNewErr, tbExpInfo, lExp1, lExp2;
    readonly Label title; readonly Button btnCopyRep, btnSound, btnLang, btnProj, btnExp, btnDedup, btnExpBack, btnExpRun, btnNewBack, btnNewRun, btnSyncBack, btnSyncRun, btnSyncMods, btnSyncVsd;
    readonly CheckedListBox clbProjs, clbBaks; readonly List<ProjItem> expProjMap = new();
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
      var hdr = new Panel { Location = new Point(2, 2), Size = new Size(420, 36), BackColor = ColorTranslator.FromHtml("#111318") };
      title = new Label { Text = "  NeuroBridge", Location = new Point(6, 0), Size = new Size(376, 36), Font = new Font("Segoe UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White };
      var btnX = new Button { Text = "X", Location = new Point(384, 2), Size = new Size(34, 32), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#111318"), ForeColor = ColorTranslator.FromHtml("#94A3B8"), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
      btnX.FlatAppearance.BorderSize = 0; btnX.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#7A2626"); btnX.Click += (_, __) => Hide();
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
      cbW = MkChk("", 46); cbA = MkChk("", 76); cbS = MkChk("", 106);
      cbW.Click += (_, __) => ToggleFlag("watcher.disabled", "watcher_go.ps1"); cbA.Click += (_, __) => ToggleFlag("alt_e.disabled", "hotkey_go.ps1"); cbS.Click += (_, __) => ToggleStartup();
      btnCopyRep = MkBtn("", 14, 46, 392, CopyActiveReport, "#163A59", "#38BDF8");
      pProjBtns = new Panel { Location = new Point(14, 84), Size = new Size(392, 168), BackColor = Color.Transparent };
      btnProj = MkBtn("", 14, 260, 100, () => Process.Start("explorer.exe", ActiveProj.path), "#222733", "#3B4252");
      btnExp = MkBtn("", 118, 260, 96, OpenExport, "#163A59", "#38BDF8");
      btnDedup = MkBtn("", 218, 260, 106, ResetHashes, "#4A2C11", "#D97706");
      btnSound = new Button { Text = "🔊", Location = new Point(328, 260), Size = new Size(36, 32), Padding = new Padding(0), TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#144229"), ForeColor = Color.White, Font = new Font("Segoe UI Emoji", 10f) };
      btnSound.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#22C55E"); btnSound.Click += (_, __) => ToggleSound();
      btnLang = new Button { Text = "RU", Location = new Point(370, 260), Size = new Size(36, 32), Padding = new Padding(0), TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml("#163A59"), ForeColor = ColorTranslator.FromHtml("#38BDF8"), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
      btnLang.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#38BDF8"); btnLang.Click += (_, __) => ToggleLang();
      foreach (Control c in new List<Control> { cbProj, btnPlus, cbW, cbA, cbS, btnCopyRep, pProjBtns, btnProj, btnExp, btnDedup, btnSound, btnLang }) pMain.Controls.Add(c);
      pSync = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      tbSyncInfo = MkInfoBox(14, 50, 392, 74, "#D8E2EC", false);
      btnSyncBack = MkBtn("", 14, 10, 120, () => { pSync.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252");
      btnSyncRun = MkBtn("", 14, 132, 392, DoSyncMods, "#144229", "#22C55E");
      btnSyncMods = MkBtn("", 14, 170, 93, () => Process.Start("explorer.exe", @"\\DESKTOP-489MN5T\Users\аяц\AppData\Roaming\VintagestoryData\Mods"), "#222733", "#3B4252");
      btnSyncVsd = MkBtn("", 113, 170, 93, () => Process.Start("explorer.exe", @"\\DESKTOP-489MN5T\Users\аяц\AppData\Roaming\VintagestoryData"), "#222733", "#3B4252");
      foreach (Control c in new List<Control> { btnSyncBack, tbSyncInfo, btnSyncRun, btnSyncMods, btnSyncVsd }) pSync.Controls.Add(c);
      pNew = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      lblNew = MkInfoBox(14, 54, 392, 24, "#D8E2EC", true);
      tbNewName = new TextBox { Location = new Point(14, 86), Size = new Size(392, 28), BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f) };
      tbNewErr = MkInfoBox(14, 162, 392, 54, "#EF4444", false);
      btnNewBack = MkBtn("", 14, 10, 120, () => { pNew.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252"); btnNewRun = MkBtn("", 14, 124, 392, CreateProjConfirm, "#144229", "#22C55E"); foreach (Control c in new List<Control> { btnNewBack, lblNew, tbNewName, btnNewRun, tbNewErr }) pNew.Controls.Add(c);
      pExp = new Panel { Location = new Point(2, 38), Size = new Size(420, 300), BackColor = Color.Transparent, Visible = false };
      lExp1 = MkInfoBox(14, 46, 392, 20, "#38BDF8", true);
      clbProjs = new CheckedListBox { Location = new Point(14, 68), Size = new Size(392, 144), CheckOnClick = true, BackColor = ColorTranslator.FromHtml("#222733"), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
      
      
      tbExpInfo = MkInfoBox(14, 258, 392, 36, "#94A3B8", false); btnExpRun = MkBtn("", 14, 222, 392, DoExportZip, "#144229", "#22C55E");
      btnExpBack = MkBtn("", 14, 8, 120, () => { pExp.Visible = false; pMain.Visible = true; }, "#222733", "#3B4252"); foreach (Control c in new List<Control> { btnExpBack, lExp1, clbProjs, btnExpRun, tbExpInfo }) pExp.Controls.Add(c);
      Controls.Add(hdr); Controls.Add(pMain); Controls.Add(pSync); Controls.Add(pNew); Controls.Add(pExp);
      var mn = new ContextMenuStrip(); mn.Items.Add("Open / Hide", null, (_, __) => ToggleWin()); mn.Items.Add("Exit", null, (_, __) => { tray.Visible = false; Application.Exit(); });
      tray = new NotifyIcon { Text = "NeuroBridge", ContextMenuStrip = mn, Visible = true }; tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleWin(); };
      LoadProjects(); tm.Tick += (_, __) => Sync(); tm.Start(); Sync(); if (silent) Opacity = 0;
      var pt = new Thread(PipeLoop) { IsBackground = true }; pt.Start(); if (silent) Shown += (_, __) => { Hide(); Opacity = 1; };
    }
    void PipeLoop() {
      while (true) {
        try {
          using var srv = new NamedPipeServerStream("NeuroBridge_UI_Pipe", PipeDirection.In, 4);
          srv.WaitForConnection();
          using var sr = new StreamReader(srv, Encoding.UTF8);
          string line = sr.ReadLine();
          if (!string.IsNullOrEmpty(line) && IsHandleCreated) BeginInvoke(new Action(() => HandlePipeMsg(line)));
        } catch { Thread.Sleep(50); }
      }
    }
    void PlayWav(string wav) { try { if (!File.Exists(Path.Combine(coreR, "sound.disabled")) && !string.IsNullOrEmpty(wav) && File.Exists(wav)) new SoundPlayer(wav).Play(); } catch { } }
    void CloseSlotBox(int slot) { if (uiSlots.TryGetValue(slot, out var b)) { if (b.F != null && !b.F.IsDisposed) { b.F.Close(); b.F.Dispose(); } uiSlots.Remove(slot); } }
    UiSlotBox EnsureSlotBox(int slot, string titleText, int tot, bool marquee) {
      if (!uiSlots.TryGetValue(slot, out var b) || b.F == null || b.F.IsDisposed) {
        var f = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true, ShowInTaskbar = false, Size = new Size(360, 82), BackColor = ColorTranslator.FromHtml("#1E222A") };
        var wa = Screen.PrimaryScreen.WorkingArea; f.Location = new Point(wa.Right - 376, wa.Bottom - (98 + slot * 92));
        var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
        p.Paint += (_, e) => { using var pen = new Pen(ColorTranslator.FromHtml("#38BDF8"), 2); e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 2, p.Height - 2); };
        var l1 = new Label { Dock = DockStyle.Top, Height = 20, ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
        var pb = new ProgressBar { Dock = DockStyle.Top, Height = 16, MarqueeAnimationSpeed = 28 };
        var l2 = new Label { Dock = DockStyle.Fill, ForeColor = ColorTranslator.FromHtml("#D8E2EC"), Font = new Font("Segoe UI", 9f) };
        p.Controls.Add(l2); p.Controls.Add(pb); p.Controls.Add(l1); f.Controls.Add(p); f.Show();
        b = new UiSlotBox { F = f, B = pb, L1 = l1, L2 = l2 }; uiSlots[slot] = b;
      }
      b.T0 = DateTime.Now; b.Tot = Math.Max(1, tot); b.Tit = titleText;
      b.B.Style = marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
      b.B.Maximum = b.Tot; b.B.Value = 0;
      b.L1.Text = marquee ? titleText : titleText + " (0 / " + b.Tot + ")";
      b.L2.Text = "Выполняется...";
      return b;
    }
    void ShowUiToast(int slot, string tit, string sub, string bg, string bd, string wav, int hold) {
      CloseSlotBox(slot);
      var f = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true, ShowInTaskbar = false, Size = new Size(360, 64), BackColor = ColorTranslator.FromHtml(bg) };
      var wa = Screen.PrimaryScreen.WorkingArea; f.Location = new Point(wa.Right - 376, wa.Bottom - (80 + slot * 92));
      var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8) };
      p.Paint += (_, e) => { using var pen = new Pen(ColorTranslator.FromHtml(bd), 2); e.Graphics.DrawRectangle(pen, 1, 1, p.Width - 2, p.Height - 2); };
      var l1 = new Label { Text = tit, Dock = DockStyle.Top, Height = 22, ForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
      var l2 = new Label { Text = sub, Dock = DockStyle.Fill, ForeColor = ColorTranslator.FromHtml("#D8E2EC"), Font = new Font("Segoe UI", 9.5f) };
      p.Controls.Add(l2); p.Controls.Add(l1); f.Controls.Add(p); f.Show();
      PlayWav(wav);
      var t = new System.Windows.Forms.Timer { Interval = Math.Max(300, hold) };
      t.Tick += (_, __) => { t.Stop(); t.Dispose(); if (!f.IsDisposed) { f.Close(); f.Dispose(); } };
      t.Start();
    }
    void HandlePipeMsg(string line) {
      var parts = line.Split('|'); if (parts.Length < 2) return;
      int.TryParse(parts.ElementAt(1), out int slot);
      string cmd = parts.ElementAt(0);
      if (cmd == "START" && parts.Length >= 3) EnsureSlotBox(slot, parts.ElementAt(2), 100, true);
      else if (cmd == "INIT" && parts.Length >= 4) { int.TryParse(parts.ElementAt(3), out int tot); EnsureSlotBox(slot, parts.ElementAt(2), tot, false); }
      else if (cmd == "PROG" && parts.Length >= 4 && uiSlots.TryGetValue(slot, out var b)) {
        int.TryParse(parts.ElementAt(2), out int cur);
        b.B.Style = ProgressBarStyle.Continuous; int c = Math.Min(cur, b.Tot); int pct = (c * 100) / b.Tot;
        b.B.Value = c; b.L1.Text = b.Tit + ": " + c + " из " + b.Tot + " (" + pct + "%)";
        double el = (DateTime.Now - b.T0).TotalSeconds; int eta = c > 0 ? (int)((el / c) * (b.Tot - c)) : 0;
        string ts = eta >= 60 ? "~" + (eta / 60) + " мин " + (eta % 60) + " сек" : "~" + eta + " сек";
        b.L2.Text = "Осталось: " + ts + " | " + parts.ElementAt(3);
      }
      else if (cmd == "CLOSE") CloseSlotBox(slot);
      else if (cmd == "TOAST" && parts.Length >= 8) { int.TryParse(parts.ElementAt(7), out int hold); ShowUiToast(slot, parts.ElementAt(2), parts.ElementAt(3), parts.ElementAt(4), parts.ElementAt(5), parts.ElementAt(6), hold); }
    }

    void RelocatePaths() { try { var c = LoadCfgObj(); bool ch = false; foreach (var p in c.projects) { string exp = p.id == "NeuroBridge_Core" ? root : Path.Combine(root, "Projects", p.id); if (!string.Equals(p.path, exp, StringComparison.OrdinalIgnoreCase)) { p.path = exp; ch = true; } } if (ch) SaveCfgObj(c); } catch { } }
    string CfgFile => Path.Combine(coreR, "projects.json");
    ProjCfg LoadCfgObj() => JsonSerializer.Deserialize<ProjCfg>(File.ReadAllText(CfgFile));
    void SaveCfgObj(ProjCfg c) => File.WriteAllText(CfgFile, JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
    ProjItem ActiveProj { get { var c = LoadCfgObj(); return c.projects.First(x => x.id == c.active); } }
    string Tr(string k) => L.TryGetValue(k, out var v) ? v : k;
    void CopyFileToClip(string f) { if (File.Exists(f)) { var sc = new System.Collections.Specialized.StringCollection(); sc.Add(f); Clipboard.SetFileDropList(sc); } }
    void CopyActiveReport() { var ap = ActiveProj; string rep = string.IsNullOrEmpty(ap.report) ? Tr("core_report") : ap.report; CopyFileToClip(Path.Combine(ap.path, rep)); }
    void ToggleSound() { string sf = Path.Combine(coreR, "sound.disabled"); if (File.Exists(sf)) File.Delete(sf); else File.WriteAllText(sf, "1"); Sync(); }
    void ApplyUiLang(string lng) {
      string lf = Path.Combine(root, "_Core", "Lang", lng + ".json");
      if (File.Exists(lf)) L = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(lf));
      curLang = lng; btnLang.Text = lng.ToUpper(); title.Text = Tr("title");
      cbW.Text = Tr("chk_watch"); cbA.Text = Tr("chk_alte"); cbS.Text = Tr("chk_start");
      btnProj.Text = Tr("btn_proj"); btnExp.Text = Tr("btn_exp"); btnDedup.Text = Tr("btn_dedup");
      tip.SetToolTip(btnDedup, lng == "ru" ? "Сбросить защиту от повтора" : "Reset duplicate hash");
      tip.SetToolTip(btnSound, lng == "ru" ? "Включить / отключить звуки конвейера" : "Toggle pipeline sounds");
      lExp1.Text = Tr("exp_p1"); btnExpRun.Text = Tr("exp_run"); tbExpInfo.Text = Tr("exp_hint"); btnSyncBack.Text = Tr("sync_back"); btnNewBack.Text = Tr("sync_back"); btnExpBack.Text = Tr("sync_back"); lblNew.Text = Tr("new_title"); btnNewRun.Text = Tr("new_create"); btnSyncRun.Text = Tr("sync_run"); btnSyncMods.Text = Tr("pc2_mods"); btnSyncVsd.Text = Tr("pc2_vsd");
    }
    void ToggleLang() { string next = curLang == "ru" ? "en" : "ru"; string q = ((char)34).ToString(); Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File " + q + Path.Combine(coreR, "apply_lang.ps1") + q + " " + next) { UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000); Sync(); }
    void OpenExport() { clbProjs.Items.Clear(); expProjMap.Clear(); foreach (var p in LoadCfgObj().projects.Where(x => x.id != "NeuroBridge_Core")) { expProjMap.Add(p); clbProjs.Items.Add(p.name + " (" + p.id + ")", false); }  tbExpInfo.Text = Tr("exp_hint"); pMain.Visible = false; pExp.Visible = true; }
    int GetFreeSlot() { for (int s = 0; s < 8; s++) if (!uiSlots.ContainsKey(s)) return s; return 0; }
    bool SkipDir(string n) => n == "Projects" || n == "obj" || n == "bin" || n.StartsWith("queue_");
    bool SkipFile(string fn) => fn.EndsWith(".hash") || fn.EndsWith(".lock") || fn.StartsWith("step_") || fn.StartsWith("bg_step_") || fn.StartsWith("NeuroBridge_Export_");
    void CollectFiles(string src, string rel, List<(string full, string entry)> list) {
      if (!Directory.Exists(src) || SkipDir(Path.GetFileName(src))) return;
      foreach (var f in Directory.GetFiles(src)) { string fn = Path.GetFileName(f); if (!SkipFile(fn)) list.Add((f, rel + "/" + fn)); }
      foreach (var d in Directory.GetDirectories(src)) CollectFiles(d, rel + "/" + Path.GetFileName(d), list);
    }
    void DoExportZip() {
      btnExpRun.Enabled = false; int sl = GetFreeSlot();
      var sel = new List<ProjItem>();
      for (int i = 0; i < clbProjs.Items.Count; i++) if (clbProjs.GetItemChecked(i)) sel.Add(expProjMap.ElementAt(i));
      string tit = curLang == "ru" ? "Экспорт NeuroBridge" : "NeuroBridge Export";
      EnsureSlotBox(sl, tit, 100, true);
      Task.Run(() => {
        try {
          var files = new List<(string full, string entry)>();
          CollectFiles(root, "NeuroBridge", files);
          var corePr = LoadCfgObj().projects.First(x => x.id == "NeuroBridge_Core");
          var newCfg = new ProjCfg { active = "NeuroBridge_Core", lang = curLang, projects = new List<ProjItem> { corePr } };
          foreach (var pr in sel) { newCfg.projects.Add(pr); CollectFiles(pr.path, "NeuroBridge/Projects/" + pr.id, files); }
          int tot = files.Count + 1, cur = 0; long lastUi = 0;
          BeginInvoke(new Action(() => EnsureSlotBox(sl, tit, tot, false)));
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
    void RenderProjButtons(ProjItem p) {
      pProjBtns.Controls.Clear(); var btns = p.buttons ?? new List<ProjBtn>();
      if (btns.Count <= 2) {
        for (int i = 0; i < btns.Count; i++) { var btn = btns.ElementAt(i); pProjBtns.Controls.Add(MkBtn(btn.text, 0, i * 36, 392, () => ExecProjBtn(btn), btn.bg ?? "#222733", btn.bd ?? "#3B4252")); }
      } else {
        var b0 = btns.ElementAt(0); pProjBtns.Controls.Add(MkBtn(b0.text, 0, 0, 392, () => ExecProjBtn(b0), b0.bg ?? "#222733", b0.bd ?? "#3B4252"));
        var xs = new List<int> { 0, 99, 198 }; var ws = new List<int> { 93, 93, 194 };
        for (int i = 1; i < Math.Min(btns.Count, 4); i++) { var btn = btns.ElementAt(i); int c = i - 1; pProjBtns.Controls.Add(MkBtn(btn.text, xs.ElementAt(c), 36, ws.ElementAt(c), () => ExecProjBtn(btn), btn.bg ?? "#222733", btn.bd ?? "#3B4252")); }
      }
    }
    void ExecProjBtn(ProjBtn b) {
      if (b.type == "run_lnk") RunProjLnk(b.target);
      else if (b.type == "open_sync") { pMain.Visible = false; pSync.Visible = true; UpdateSyncInfo(); }
      else if (b.type == "open_dir" && Directory.Exists(b.target)) Process.Start("explorer.exe", b.target);
      else if (b.type == "copy_handoff") CopyFileToClip(Path.Combine(root, Tr("start_file")));
      else if (b.type == "run_ps1" && File.Exists(b.target)) {
        var psi = new ProcessStartInfo("powershell.exe", "-sta -NoProfile -ExecutionPolicy Bypass -File \"" + b.target + "\"") { CreateNoWindow = true, UseShellExecute = false };
        Process.Start(psi);
      }
    }
    void CleanMissingProjects() { var c = LoadCfgObj(); int n = c.projects.Count; c.projects = c.projects.Where(x => x.id == "NeuroBridge_Core" || Directory.Exists(x.path)).ToList(); if (c.projects.Count != n) { if (!c.projects.Any(x => x.id == c.active)) c.active = "NeuroBridge_Core"; SaveCfgObj(c); LoadProjects(); } } void LoadProjects() { var c = LoadCfgObj(); cbProj.Items.Clear(); foreach (var p in c.projects) cbProj.Items.Add(p.name); int i = c.projects.FindIndex(x => x.id == c.active); if (i >= 0) cbProj.SelectedIndex = i; }
    void SwitchProj() { if (cbProj.SelectedIndex < 0) return; var c = LoadCfgObj(); c.active = c.projects.ElementAt(cbProj.SelectedIndex).id; SaveCfgObj(c); Sync(); }
    void OpenNewProj() { tbNewName.Text = ""; tbNewErr.Text = ""; pMain.Visible = false; pNew.Visible = true; tbNewName.Focus(); }
    void CreateProjConfirm() {
      string raw = (tbNewName.Text ?? "").Trim();
      if (raw.Length < 2) { tbNewErr.Text = Tr("new_err_short"); return; }
      if (Regex.IsMatch(raw, @"[^a-zA-Z0-9 _-]")) { tbNewErr.Text = Tr("new_err_latin"); return; }
      string slug = Regex.Replace(raw, @"[ -]+", "_").Trim('_');
      if (slug.Length < 2) slug = "Project_" + DateTime.Now.ToString("HHmmss");
      var c = LoadCfgObj(); if (c.projects.Any(x => x.id.Equals(slug, StringComparison.OrdinalIgnoreCase))) { tbNewErr.Text = Tr("new_err_dup"); return; }
      string d = Path.Combine(root, "Projects", slug), docD = Path.Combine(d, "02_Docs");
      Directory.CreateDirectory(Path.Combine(docD, "_dialog_history", "backups"));
      File.WriteAllText(Path.Combine(docD, "_dialog_history", "task_counter.txt"), "0", Encoding.UTF8);
      string suf = (c.lang == "en") ? "_report.txt" : "_отчёт.txt"; string rep = slug + suf;
      File.WriteAllText(Path.Combine(docD, "DOC_PROJECT_RULES.md"), "# [DOC_PROJECT_RULES] ПРАВИЛА ПРОЕКТА " + raw + "\r\n- Базовые законы и канон скриптов: C:\\NeuroBridge\\_Core\\Docs\\CORE_RULES.md, CORE_ARCHITECTURE.md, CORE_STYLE_UI.md, CORE_TERMINOLOGY.md.\r\n- Целевой отчёт проекта: $out = \"C:\\NeuroBridge\\Projects\\" + slug + "\\" + rep + "\".\r\n", Encoding.UTF8);
      File.WriteAllText(Path.Combine(docD, "DOC_PROJECT_RUNTIME.md"), "# [DOC_PROJECT_RUNTIME] КАРТА И ТОЧКА ОСТАНОВКИ: " + raw + "\r\n## ТЕКУЩАЯ ТОЧКА ОСТАНОВКИ (HANDOFF)\r\n- Проект создан в Диспетчере NeuroBridge. Готов к первичной постановке задач.\r\n", Encoding.UTF8);
      File.WriteAllText(Path.Combine(docD, "DOC_PROJECT_ARCHIVE.md"), "# [DOC_PROJECT_ARCHIVE] СПЕЦИФИКАЦИИ И РЕШЕНИЯ: " + raw + "\r\n", Encoding.UTF8);
      File.WriteAllText(Path.Combine(docD, "PROJECT_ERRORS.md"), "# [PROJECT_ERRORS] ЖУРНАЛ ОШИБОК ПРОЕКТА: " + raw + "\r\n", Encoding.UTF8);
      File.WriteAllText(Path.Combine(docD, "DIALOG_LOG.md"), "# [DIALOG_LOG] ЖУРНАЛ ДИАЛОГОВ ПРОЕКТА: " + raw + "\r\n", Encoding.UTF8);
      c.projects.Add(new ProjItem { id = slug, name = raw, path = d, report = rep, buttons = new List<ProjBtn>() });
      c.active = slug; SaveCfgObj(c); LoadProjects(); pNew.Visible = false; pMain.Visible = true;
      string q = ((char)34).ToString(); string initPs = "$out = " + q + Path.Combine(d, rep) + q + "; " + q + "=== СТАРТОВЫЙ ОТЧЁТ ПРОЕКТА: " + raw + " ===" + q + " | Out-File $out -Encoding UTF8; & " + q + Path.Combine(coreR, "auto_footer.ps1") + q; string enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(initPs));
      Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand " + enc) { UseShellExecute = false, CreateNoWindow = true });
    }
    void ToggleFlag(string f, string ps) { string p = Path.Combine(coreR, f); if (File.Exists(p)) { File.Delete(p); RunPs(ps); } else File.WriteAllText(p, "1"); Sync(); }
    TextBox MkInfoBox(int x, int y, int w, int h, string hex, bool b) => new TextBox { Location = new Point(x, y), Size = new Size(w, h), Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = ColorTranslator.FromHtml("#181B22"), ForeColor = ColorTranslator.FromHtml(hex), Font = new Font("Segoe UI", 9.5f, b ? FontStyle.Bold : FontStyle.Regular), TabStop = false };
    string SyncStampFile => Path.Combine(ActiveProj.path, "03_Tools", "last_sync_mods.txt");
    string ResolveIp(string h) { try { var ip = Dns.GetHostAddresses(h).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork); return ip != null ? ip.ToString() : "offline"; } catch { return "offline"; } }
    void UpdateSyncInfo() { string st = File.Exists(SyncStampFile) ? File.ReadAllText(SyncStampFile).Trim() : "-"; tbSyncInfo.Text = "DESKTOP-489MN5T | IP: " + ResolveIp("DESKTOP-489MN5T") + "\r\n" + Tr("sync_last") + ": " + st; }
    void DoSyncMods() { RunProjLnk("Обновить моды на DESKTOP-489MN5T.lnk"); File.WriteAllText(SyncStampFile, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")); UpdateSyncInfo(); }
    void ToggleWin() { if (Visible) Hide(); else { Show(); WindowState = FormWindowState.Normal; Activate(); } }
    CheckBox MkChk(string t, int y) => new CheckBox { Text = t, Location = new Point(16, y), Size = new Size(388, 24), ForeColor = ColorTranslator.FromHtml("#D8E2EC") };
    Button MkBtn(string t, int x, int y, int w, Action a, string bg, string bd) { var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(w, 32), FlatStyle = FlatStyle.Flat, BackColor = ColorTranslator.FromHtml(bg), ForeColor = Color.White }; b.FlatAppearance.BorderColor = ColorTranslator.FromHtml(bd); b.Click += (_, __) => a(); return b; }
    string StLnk => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "NeuroBridgeTray.lnk");
    void RunPs(string f) { string q = ((char)34).ToString(); Process.Start(new ProcessStartInfo("powershell.exe", "-sta -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File " + q + Path.Combine(coreR, f) + q) { UseShellExecute = false, CreateNoWindow = true }); }
    void RunProjLnk(string n) { var p = Path.Combine(ActiveProj.path, n); if (File.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); }
    void ResetHashes() { foreach (var f in new List<string> { "last_step.hash", "last_watcher.hash" }) { var p = Path.Combine(coreR, f); if (File.Exists(p)) File.Delete(p); } }
    void ToggleStartup() { if (File.Exists(StLnk)) File.Delete(StLnk); else { var p = Path.Combine(root, Tr("lnk_name")); if (File.Exists(p)) File.Copy(p, StLnk, true); } Sync(); }
    bool IsPidAlive(string pf) { try { if (!File.Exists(pf)) return false; if (!int.TryParse(File.ReadAllText(pf).Trim(), out int pid)) return false; var p = Process.GetProcessById(pid); return !p.HasExited && p.ProcessName.Equals("powershell", StringComparison.OrdinalIgnoreCase); } catch { return false; } }
    int healTick = 0;
    void EnsureRunners(bool w, bool a) { if (++healTick % 3 != 1) return; if (w && !IsPidAlive(Path.Combine(coreR, "watcher.pid"))) RunPs("watcher_go.ps1"); if (a && !IsPidAlive(Path.Combine(coreR, "hotkey.pid"))) RunPs("hotkey_go.ps1"); }
    void Sync() { var cfg = LoadCfgObj(); string lng = string.IsNullOrEmpty(cfg.lang) ? "en" : cfg.lang; if (curLang != lng) ApplyUiLang(lng); bool w = !File.Exists(Path.Combine(coreR, "watcher.disabled")), a = !File.Exists(Path.Combine(coreR, "alt_e.disabled")), snd = !File.Exists(Path.Combine(coreR, "sound.disabled")); cbW.Checked = w; cbA.Checked = a; cbS.Checked = File.Exists(StLnk); btnSound.Text = snd ? "🔊" : "🔇"; btnSound.BackColor = ColorTranslator.FromHtml(snd ? "#144229" : "#4A2C11"); btnSound.FlatAppearance.BorderColor = ColorTranslator.FromHtml(snd ? "#22C55E" : "#D97706"); var ap = cfg.projects.First(x => x.id == cfg.active); bool isCore = ap.id == "NeuroBridge_Core"; cbW.Visible = isCore; cbA.Visible = isCore; cbS.Visible = isCore; btnExp.Visible = isCore; btnSound.Visible = isCore; btnLang.Visible = isCore; btnProj.SetBounds(14, 260, isCore ? 100 : 236, 32); btnDedup.SetBounds(isCore ? 218 : 256, 260, isCore ? 106 : 150, 32); string rep = string.IsNullOrEmpty(ap.report) ? Tr("core_report") : ap.report; string pfx = lng == "ru" ? "Скопировать" : "Copy"; btnCopyRep.Text = pfx + " " + rep; btnCopyRep.Location = new Point(14, isCore ? 140 : 46); pProjBtns.Location = new Point(14, isCore ? 176 : 82); pProjBtns.Size = new Size(392, isCore ? 76 : 170); if (lastRenderedProj != ap.id) { lastRenderedProj = ap.id; RenderProjButtons(ap); } string icoName = w ? "nb_auto.ico" : (a ? "nb_alte.ico" : "nb_pause.ico"); string icoPath = Path.Combine(root, "_Core", "TrayApp", icoName); if (File.Exists(icoPath)) tray.Icon = new Icon(icoPath); EnsureRunners(w, a); }
  }
}
















