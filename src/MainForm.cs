using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Course of Fire")]
[assembly: System.Reflection.AssemblyProduct("Course of Fire")]
[assembly: System.Reflection.AssemblyCopyright("© Michael Leaf")]
[assembly: System.Reflection.AssemblyVersion("5.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("5.0.0.0")]

namespace CourseOfFire
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Ui.Scale = g.DpiX / 96f;
            Application.Run(new MainForm());
        }
    }

    class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        readonly Library library;
        readonly ListBox list;
        readonly TextBox search;
        readonly Label countLabel, emptyLabel;
        readonly StageEditor editor;
        readonly Panel editorHost;
        readonly Button printButton, exportButton, duplicateButton, deleteButton;
        readonly Timer saveTimer;
        bool dirty;
        string lastDiscipline = Disciplines.Rifle;

        public MainForm()
        {
            library = Store.Load();

            Text = "Course of Fire";
            Font = Ui.F(9.5f);
            BackColor = Ui.Bg;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            var area = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(Ui.S(1440), area.Width - 40), Math.Min(Ui.S(920), area.Height - 40));
            MinimumSize = new Size(Ui.S(1100), Ui.S(640));
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = Ui.S(64), BackColor = Ui.Navy };
            var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(Ui.S(18), 0, Ui.S(12), 0) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            header.Controls.Add(bar);

            var leftGroup = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            var brand = new BrandMark { Size = new Size(Ui.S(30), Ui.S(30)), Margin = new Padding(0, Ui.S(2), Ui.S(10), 0) };
            var title = new Label { Text = "Course of Fire", AutoSize = true, Font = Ui.F(14f, true), ForeColor = Color.White, Margin = new Padding(0, Ui.S(4), Ui.S(28), 0) };
            var newButton = Ui.HeaderButton("New stage  ▾");
            var generateButton = Ui.HeaderButton("↻  Generate stage", true);
            leftGroup.Controls.AddRange(new Control[] { brand, title, newButton, generateButton });
            bar.Controls.Add(leftGroup, 0, 0);

            var rightGroup = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right, Margin = new Padding(0) };
            printButton = Ui.HeaderButton("Print  ▾");
            exportButton = Ui.HeaderButton("Export  ▾");
            duplicateButton = Ui.HeaderButton("Duplicate");
            deleteButton = Ui.HeaderButton("Delete");
            var moreButton = Ui.HeaderButton("More  ▾");
            moreButton.Margin = new Padding(0);
            rightGroup.Controls.AddRange(new Control[] { printButton, exportButton, duplicateButton, deleteButton, moreButton });
            bar.Controls.Add(rightGroup, 2, 0);

            // Library
            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, BackColor = Ui.Border, SplitterWidth = 1 };
            var side = split.Panel1;
            side.BackColor = Color.White;
            var searchHost = new Panel { Dock = DockStyle.Top, Height = Ui.S(58), Padding = new Padding(Ui.S(14), Ui.S(14), Ui.S(14), Ui.S(8)), BackColor = Color.White };
            search = new TextBox { Dock = DockStyle.Fill, Font = Ui.F(10.5f) };
            search.HandleCreated += (s, e) => SendMessage(search.Handle, 0x1501, (IntPtr)1, "Search stages, targets, props");
            searchHost.Controls.Add(search);
            countLabel = new Label { Dock = DockStyle.Bottom, Height = Ui.S(34), ForeColor = Ui.Muted, Font = Ui.F(9f), Padding = new Padding(Ui.S(14), 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White };
            list = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = Ui.S(80),
                IntegralHeight = false,
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.MultiExtended,
                BackColor = Color.White
            };
            list.DrawItem += DrawListItem;
            list.SelectedIndexChanged += (s, e) => ShowSelected();
            side.Controls.Add(list);
            side.Controls.Add(searchHost);
            side.Controls.Add(countLabel);

            // Editor
            editorHost = split.Panel2;
            editorHost.BackColor = Ui.Bg;
            editor = new StageEditor { Dock = DockStyle.Fill, Visible = false };
            editor.Changed += (s, e) => { MarkDirty(); list.Invalidate(); };
            emptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Ui.Muted,
                Font = Ui.F(11f),
                Text = "Pick a stage on the left, or start one with New stage or Generate stage."
            };
            editorHost.Controls.Add(editor);
            editorHost.Controls.Add(emptyLabel);

            Controls.Add(split);
            Controls.Add(header);

            saveTimer = new Timer { Interval = 800 };
            saveTimer.Tick += (s, e) => SaveNow();

            // Menus
            newButton.Click += (s, e) => Ui.ShowMenu(newButton, NewMenu());
            generateButton.Click += (s, e) => OpenGenerator();
            printButton.Click += (s, e) =>
            {
                var m = Ui.Menu();
                m.Items.Add("Print…", null, (o, a) => Print(false));
                m.Items.Add("Print preview…", null, (o, a) => Print(true));
                Ui.ShowMenu(printButton, m);
            };
            exportButton.Click += (s, e) => Ui.ShowMenu(exportButton, ExportMenu());
            duplicateButton.Click += (s, e) => Duplicate();
            deleteButton.Click += (s, e) => DeleteSelected();
            moreButton.Click += (s, e) => Ui.ShowMenu(moreButton, MoreMenu());
            search.TextChanged += (s, e) => RefreshList(SelectedStages().Select(st => st.Id).ToArray());

            Load += (s, e) =>
            {
                split.SplitterDistance = Ui.S(340);
                RefreshList();
                if (list.Items.Count > 0) list.SelectedIndex = 0;
                if (!File.Exists(Store.LibraryPath)) SaveNow();
                if (Store.LoadWarning != null)
                    MessageBox.Show(this, Store.LoadWarning, "Course of Fire", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            FormClosing += (s, e) => { if (dirty) SaveNow(); };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.N: NewStage(lastDiscipline, null); return true;
                case Keys.Control | Keys.G: OpenGenerator(); return true;
                case Keys.Control | Keys.P: Print(false); return true;
                case Keys.Control | Keys.D: Duplicate(); return true;
                case Keys.Control | Keys.F: search.Focus(); search.SelectAll(); return true;
                case Keys.Delete:
                    if (list.Focused) { DeleteSelected(); return true; }
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---- Library list ----

        List<Stage> SelectedStages()
        {
            return list.SelectedIndices.Cast<int>().OrderBy(i => i).Select(i => (Stage)list.Items[i]).ToList();
        }

        void RefreshList(params string[] selectIds)
        {
            string q = search.Text.Trim().ToLowerInvariant();
            var stages = library.Stages
                .Where(s => q.Length == 0 || SearchText(s).Contains(q))
                .OrderByDescending(s => s.ModifiedTime)
                .ToList();
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var s in stages) list.Items.Add(s);
            for (int i = 0; i < stages.Count; i++)
                if (selectIds.Contains(stages[i].Id)) list.SetSelected(i, true);
            list.EndUpdate();
            countLabel.Text = library.Stages.Count + (library.Stages.Count == 1 ? " stage" : " stages") +
                              (q.Length > 0 ? "  ·  " + stages.Count + " shown" : "") + "  ·  saved on this PC";
            ShowSelected();
        }

        static string SearchText(Stage s)
        {
            var sb = new StringBuilder(s.Name).Append(' ').Append(s.Discipline).Append(' ').Append(s.Notes).Append(' ').Append(s.Procedure);
            foreach (var t in s.Targets) sb.Append(' ').Append(Catalog.Target(t.TypeId).Name).Append(' ').Append(t.Notes);
            foreach (var p in s.Props) sb.Append(' ').Append(p.DisplayName);
            return sb.ToString().ToLowerInvariant();
        }

        void ShowSelected()
        {
            var selected = SelectedStages();
            bool any = selected.Count > 0;
            printButton.Enabled = exportButton.Enabled = duplicateButton.Enabled = deleteButton.Enabled = any;
            if (!any)
            {
                editor.Visible = false;
                emptyLabel.Visible = true;
                return;
            }
            var stage = list.SelectedIndex >= 0 ? (Stage)list.Items[list.SelectedIndex] : selected[0];
            if (editor.Stage != stage) editor.ShowStage(stage);
            editor.Visible = true;
            emptyLabel.Visible = false;
            lastDiscipline = stage.Discipline;
        }

        void DrawListItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var s = (Stage)list.Items[e.Index];
            var g = e.Graphics;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            var b = e.Bounds;
            using (var bg = new SolidBrush(selected ? Ui.AccentSoft : Color.White)) g.FillRectangle(bg, b);
            if (selected) using (var bar = new SolidBrush(Ui.Accent)) g.FillRectangle(bar, b.X, b.Y, Ui.S(4), b.Height);
            using (var line = new Pen(Color.FromArgb(241, 245, 249))) g.DrawLine(line, b.X, b.Bottom - 1, b.Right, b.Bottom - 1);

            int x = b.X + Ui.S(16), w = b.Width - Ui.S(28);
            var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, s.Name, Ui.F(10.5f, true), new Rectangle(x, b.Y + Ui.S(10), w, Ui.S(22)), Ui.Text, flags);
            TextRenderer.DrawText(g, s.Discipline + "  ·  " + s.Targets.Count + " targets  ·  " + s.RoundCount + " rds  ·  " + s.ParTime + " s",
                Ui.F(9f), new Rectangle(x, b.Y + Ui.S(33), w, Ui.S(18)), Ui.Muted, flags);
            TextRenderer.DrawText(g, Friendly(s.ModifiedTime), Ui.F(8.5f), new Rectangle(x, b.Y + Ui.S(53), w, Ui.S(18)), Ui.Muted, flags);

            // A few target drawings along the bottom right
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int size = Ui.S(22), ix = b.Right - Ui.S(12) - size;
            foreach (var t in s.Targets.Take(5).Reverse())
            {
                Icons.Draw(g, true, Catalog.Target(t.TypeId).Shape, new RectangleF(ix, b.Y + Ui.S(50), size, size));
                ix -= size + Ui.S(2);
            }
        }

        static string Friendly(DateTime when)
        {
            var span = DateTime.Now - when;
            if (span.TotalMinutes < 1) return "Edited just now";
            if (span.TotalHours < 1) return "Edited " + (int)span.TotalMinutes + " min ago";
            if (when.Date == DateTime.Today) return "Edited today " + when.ToString("h:mm tt");
            if (when.Date == DateTime.Today.AddDays(-1)) return "Edited yesterday";
            return "Edited " + when.ToString("MMM d, yyyy");
        }

        // ---- Saving ----

        void MarkDirty()
        {
            dirty = true;
            saveTimer.Stop();
            saveTimer.Start();
        }

        void SaveNow()
        {
            saveTimer.Stop();
            try
            {
                Store.Save(library);
                dirty = false;
            }
            catch (Exception ex)
            {
                countLabel.Text = "Couldn't save: " + ex.Message;
            }
        }

        // ---- Actions ----

        ContextMenuStrip NewMenu()
        {
            var m = Ui.Menu();
            foreach (var d in Disciplines.All)
            {
                string disc = d;
                m.Items.Add("Blank " + d.ToLowerInvariant() + " stage", null, (o, a) => NewStage(disc, null));
            }
            m.Items.Add(new ToolStripSeparator());
            var ideas = new ToolStripMenuItem("From a PRS stage idea");
            foreach (var idea in Defaults.Ideas)
            {
                var i = idea;
                ideas.DropDownItems.Add(i.Name, null, (o, a) => NewStage(Disciplines.Rifle, i));
            }
            m.Items.Add(ideas);
            return m;
        }

        void NewStage(string discipline, StageIdea idea)
        {
            var stage = new Stage
            {
                Name = idea != null ? idea.Name : "New " + discipline.ToLowerInvariant() + " stage",
                Discipline = discipline,
                ParTime = discipline == Disciplines.Pistol ? 30 : 120,
                Start = Disciplines.DefaultStart(discipline),
                Procedure = idea != null ? idea.Text : ""
            };
            if (idea != null) stage.Props = idea.Props.Select(p => StageProp.Of(p)).ToList();
            AddStages(new[] { stage });
        }

        void AddStages(IEnumerable<Stage> stages)
        {
            var added = stages.ToList();
            library.Stages.AddRange(added);
            MarkDirty();
            search.Text = "";
            RefreshList(added.Select(s => s.Id).ToArray());
            int first = list.Items.Cast<Stage>().ToList().FindIndex(s => s.Id == added[0].Id);
            if (first >= 0) list.TopIndex = Math.Max(0, first - 1);
        }

        void OpenGenerator()
        {
            using (var dialog = new GeneratorDialog(library, lastDiscipline))
            {
                dialog.RangeChanged += (s, e) => MarkDirty();
                dialog.StageSaved += stage => AddStages(new[] { stage });
                dialog.ShowDialog(this);
            }
        }

        void Duplicate()
        {
            var selected = SelectedStages();
            if (selected.Count == 0) return;
            AddStages(selected.Select(s =>
            {
                var copy = s.Copy();
                copy.Name = s.Name + " (copy)";
                return copy;
            }).ToList());
        }

        void DeleteSelected()
        {
            var selected = SelectedStages();
            if (selected.Count == 0) return;
            string question = selected.Count == 1
                ? "Delete \"" + selected[0].Name + "\"?"
                : "Delete these " + selected.Count + " stages?";
            if (MessageBox.Show(this, question + "\n\nThis can't be undone.", "Delete", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            foreach (var s in selected) library.Stages.Remove(s);
            MarkDirty();
            RefreshList();
            if (list.Items.Count > 0) list.SelectedIndex = 0;
        }

        void Print(bool preview)
        {
            var stages = SelectedStages();
            if (stages.Count == 0) return;
            using (var doc = Printing.CreateDocument(stages))
            {
                if (preview)
                {
                    using (var dialog = new PrintPreviewDialog { Document = doc, Width = Ui.S(1000), Height = Ui.S(900), ShowIcon = false })
                    {
                        dialog.PrintPreviewControl.Zoom = 1.0;
                        dialog.ShowDialog(this);
                    }
                }
                else
                {
                    using (var dialog = new PrintDialog { Document = doc, UseEXDialog = true, AllowSomePages = false })
                        if (dialog.ShowDialog(this) == DialogResult.OK) doc.Print();
                }
            }
        }

        ContextMenuStrip ExportMenu()
        {
            var m = Ui.Menu();
            m.Items.Add("PDF…", null, (o, a) => ExportPdf());
            m.Items.Add("Picture (PNG)…", null, (o, a) => ExportPng());
            m.Items.Add("Copy as text", null, (o, a) =>
            {
                Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, SelectedStages().Select(st => TextExport.ToText(st))));
                countLabel.Text = "Copied to the clipboard.";
            });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Spreadsheet (CSV)…", null, (o, a) => ExportCsv(SelectedStages()));
            m.Items.Add("Course of Fire file to share (.cof)…", null, (o, a) => ExportCof(SelectedStages()));
            return m;
        }

        ContextMenuStrip MoreMenu()
        {
            var m = Ui.Menu();
            m.Items.Add("Import a Course of Fire file…", null, (o, a) => Import());
            m.Items.Add("Back up all stages…", null, (o, a) => ExportCof(library.Stages));
            m.Items.Add("Export all stages to a spreadsheet…", null, (o, a) => ExportCsv(library.Stages));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Open data folder", null, (o, a) =>
            {
                Directory.CreateDirectory(Store.Folder);
                Process.Start("explorer.exe", "\"" + Store.Folder + "\"");
            });
            m.Items.Add("About Course of Fire", null, (o, a) => MessageBox.Show(this,
                "Course of Fire 5.0\n© Michael Leaf\n\nBuild random courses of fire for precision rifle, NRL22 and pistol / carbine.\n\n" +
                "Your stages are saved automatically in:\n" + Store.LibraryPath,
                "About Course of Fire", MessageBoxButtons.OK, MessageBoxIcon.Information));
            return m;
        }

        string AskSavePath(string title, string filter, string fileName)
        {
            using (var dialog = new SaveFileDialog { Title = title, Filter = filter, FileName = SafeFileName(fileName), OverwritePrompt = true })
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }

        static string SafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '-');
            return name.Trim();
        }

        string DefaultName(List<Stage> stages)
        {
            return stages.Count == 1 ? stages[0].Name : "Courses of fire " + DateTime.Now.ToString("yyyy-MM-dd");
        }

        void ExportPdf()
        {
            var stages = SelectedStages();
            if (stages.Count == 0) return;
            if (!Printing.HasPdfPrinter())
            {
                MessageBox.Show(this, "The \"Microsoft Print to PDF\" printer isn't installed on this PC. Use Print and choose any PDF printer instead.",
                    "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string path = AskSavePath("Export PDF", "PDF document (*.pdf)|*.pdf", DefaultName(stages) + ".pdf");
            if (path == null) return;
            Run(() => Printing.SavePdf(stages, path), "Saved " + Path.GetFileName(path), path);
        }

        void ExportPng()
        {
            var stages = SelectedStages();
            if (stages.Count == 0) return;
            if (stages.Count == 1)
            {
                string path = AskSavePath("Export picture", "PNG picture (*.png)|*.png", stages[0].Name + ".png");
                if (path == null) return;
                Run(() => Printing.SavePng(stages[0], path), "Saved " + Path.GetFileName(path), path);
                return;
            }
            using (var dialog = new FolderBrowserDialog { Description = "Choose a folder for the " + stages.Count + " pictures" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Run(() =>
                {
                    foreach (var s in stages)
                    {
                        string file = Path.Combine(dialog.SelectedPath, SafeFileName(s.Name) + ".png");
                        for (int n = 2; File.Exists(file); n++)
                            file = Path.Combine(dialog.SelectedPath, SafeFileName(s.Name) + " (" + n + ").png");
                        Printing.SavePng(s, file);
                    }
                }, "Saved " + stages.Count + " pictures", dialog.SelectedPath);
            }
        }

        void ExportCsv(List<Stage> stages)
        {
            if (stages.Count == 0) return;
            string path = AskSavePath("Export spreadsheet", "CSV spreadsheet (*.csv)|*.csv", DefaultName(stages) + ".csv");
            if (path == null) return;
            Run(() => File.WriteAllText(path, TextExport.ToCsv(stages), new UTF8Encoding(true)), "Saved " + Path.GetFileName(path), path);
        }

        void ExportCof(List<Stage> stages)
        {
            if (stages.Count == 0) return;
            string name = stages.Count == library.Stages.Count && stages.Count > 1
                ? "Course of Fire backup " + DateTime.Now.ToString("yyyy-MM-dd")
                : DefaultName(stages);
            string path = AskSavePath("Save Course of Fire file", "Course of Fire file (*.cof)|*.cof", name + ".cof");
            if (path == null) return;
            Run(() => Store.ExportCourseFile(path, stages), "Saved " + Path.GetFileName(path), path);
        }

        void Import()
        {
            using (var dialog = new OpenFileDialog { Title = "Import", Filter = "Course of Fire files (*.cof;*.json)|*.cof;*.json|All files (*.*)|*.*", Multiselect = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var imported = new List<Stage>();
                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        imported.AddRange(Store.ImportCourseFile(file));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Couldn't read " + Path.GetFileName(file) + ".\n\n" + ex.Message, "Import",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                if (imported.Count == 0) return;
                var ids = new HashSet<string>(library.Stages.Select(s => s.Id));
                foreach (var s in imported)
                    if (!ids.Add(s.Id)) { s.Id = Stage.NewId(); ids.Add(s.Id); }
                AddStages(imported);
                countLabel.Text = "Imported " + imported.Count + (imported.Count == 1 ? " stage." : " stages.");
            }
        }

        // Runs an export, then offers to show the result.
        void Run(Action export, string done, string path)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                export();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show(this, "That didn't work.\n\n" + ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Cursor = Cursors.Default;
            if (MessageBox.Show(this, done + ".\n\nOpen it now?", "Export", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                try { Process.Start(path); } catch { }
            }
        }
    }

    // The scope reticle in the header.
    class BrandMark : Control
    {
        public BrandMark()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Ui.Navy);
            float w = Width, c = w / 2;
            using (var pen = new Pen(Color.FromArgb(121, 184, 255), w * 0.08f))
            using (var dot = new SolidBrush(Color.FromArgb(121, 184, 255)))
            {
                g.DrawEllipse(pen, w * 0.18f, w * 0.18f, w * 0.64f, w * 0.64f);
                float gap = w * 0.09f, reach = w * 0.46f;
                g.DrawLine(pen, c, c - reach, c, c - gap);
                g.DrawLine(pen, c, c + gap, c, c + reach);
                g.DrawLine(pen, c - reach, c, c - gap, c);
                g.DrawLine(pen, c + gap, c, c + reach, c);
                g.FillEllipse(dot, c - w * 0.05f, c - w * 0.05f, w * 0.1f, w * 0.1f);
            }
        }
    }
}
