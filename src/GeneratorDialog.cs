using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CourseOfFire
{
    // Range setup on the left (what's available and the rules), generated stage preview
    // on the right. Re-roll as often as you like, then save the one you want.
    class GeneratorDialog : Form
    {
        public event Action<Stage> StageSaved;
        public event EventHandler RangeChanged;

        readonly Library library;
        RangeSetup range;
        Stage current;
        bool loading;

        readonly ComboBox discipline;
        readonly TableLayoutPanel settings;
        readonly TableLayoutPanel targetList;
        readonly FlowLayoutPanel propTiles;
        readonly TextBox positions;
        readonly NumericUpDown minTargets, maxTargets, minHits, maxHits, minTime, maxTime, minProps, maxProps, positionCount;
        readonly CheckBox requireMovement, supportSide, switchSecondary, nearToFar;
        readonly PictureBox preview;
        readonly Panel previewHost;
        readonly Label status;
        readonly Button save;

        public GeneratorDialog(Library library, string startDiscipline)
        {
            this.library = library;
            Text = "Generate a random stage";
            ShowIcon = false;
            Font = Ui.F(9.5f);
            BackColor = Ui.Bg;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            ClientSize = new Size(Math.Min(Ui.S(1320), area.Width - 40), Math.Min(Ui.S(860), area.Height - 40));
            MinimumSize = new Size(Ui.S(1000), Ui.S(600));

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel2,
                BackColor = Ui.Border,
                SplitterWidth = 1
            };
            Controls.Add(split);

            // Left: range setup
            var left = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Ui.Bg };
            split.Panel1.Controls.Add(left);
            settings = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Padding = new Padding(Ui.S(22), Ui.S(18), Ui.S(22), Ui.S(24)),
                BackColor = Ui.Bg
            };
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            left.Controls.Add(settings);

            var top = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, Ui.S(14)) };
            var t = Ui.Label("Your range", 15f, true);
            t.Margin = new Padding(0, 0, Ui.S(18), 0);
            discipline = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Ui.S(180), Font = Ui.F(10f), Margin = new Padding(0, Ui.S(4), Ui.S(12), 0) };
            discipline.Items.AddRange(Disciplines.All);
            var reset = Ui.Button("Reset to defaults");
            reset.Margin = new Padding(0, Ui.S(1), 0, 0);
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Reset the " + range.Discipline + " range to the built-in targets, props, positions and rules?",
                        "Reset range", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                library.ResetRange(range.Discipline);
                LoadRange(range.Discipline);
                OnRangeChanged();
            };
            top.Controls.AddRange(new Control[] { t, discipline, reset });
            settings.Controls.Add(top);

            var intro = Ui.Label("The generator only picks from what you list here. Changes are saved for next time.", 9.5f, false, Ui.Muted);
            intro.Margin = new Padding(0, 0, 0, Ui.S(16));
            settings.Controls.Add(intro);

            // Rules
            settings.Controls.Add(SectionTitle("Rules"));
            var rules = new TableLayoutPanel { AutoSize = true, ColumnCount = 6, Margin = new Padding(0, 0, 0, Ui.S(8)) };
            minTargets = Number(1, 30); maxTargets = Number(1, 30);
            minHits = Number(1, 20); maxHits = Number(1, 20);
            minTime = Number(1, 3600); maxTime = Number(1, 3600);
            minProps = Number(0, 10); maxProps = Number(0, 10);
            positionCount = Number(1, 20);
            AddRule(rules, 0, "Targets", minTargets, maxTargets);
            AddRule(rules, 1, "Hits per target", minHits, maxHits);
            AddRule(rules, 2, "Par time (seconds)", minTime, maxTime);
            AddRule(rules, 3, "Props used", minProps, maxProps);
            AddRule(rules, 4, "Positions used", positionCount, null);
            settings.Controls.Add(rules);

            var checks = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 0, Ui.S(18)) };
            requireMovement = Check("Require movement between positions when possible");
            supportSide = Check("Force at least one support-side string");
            switchSecondary = Check("Include a switch to the secondary weapon");
            nearToFar = Check("Engage targets near to far");
            checks.Controls.AddRange(new Control[] { requireMovement, supportSide, switchSecondary, nearToFar });
            settings.Controls.Add(checks);

            // Targets
            var addTarget = Ui.Button("+  Add target", true);
            addTarget.Click += (s, e) => AddTarget();
            settings.Controls.Add(SectionTitle("Targets on your range", addTarget));
            targetList = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(18)) };
            targetList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            settings.Controls.Add(targetList);

            // Props
            settings.Controls.Add(SectionTitle("Props available", null, "Click to turn on or off."));
            propTiles = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(12)) };
            foreach (var p in Catalog.Props.Where(p => p.Id != "custom_prop"))
            {
                string id = p.Id;
                var tile = new Tile(false, p.Id, p.Name, null) { Toggle = true };
                tile.Activated += (s, e) =>
                {
                    if (loading) return;
                    range.PropIds.Remove(id);
                    if (((Tile)s).Checked) range.PropIds.Add(id);
                    OnRangeChanged();
                };
                propTiles.Controls.Add(tile);
            }
            settings.Controls.Add(propTiles);

            // Positions
            settings.Controls.Add(SectionTitle("Positions and stage options", null, "One per line."));
            positions = new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Height = Ui.S(120),
                Font = Ui.F(10f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            positions.TextChanged += (s, e) =>
            {
                if (loading) return;
                range.Positions = positions.Lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                OnRangeChanged();
            };
            settings.Controls.Add(positions);

            // Right: preview and actions
            split.Panel2.BackColor = Color.FromArgb(226, 232, 240);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(Ui.S(16), Ui.S(12), Ui.S(16), Ui.S(12)), BackColor = Color.White };
            var generate = Ui.Button("Generate", true);
            generate.Text = "↻  Generate";
            save = Ui.Button("Save to library");
            save.Enabled = false;
            var close = Ui.Button("Close");
            close.Click += (s, e) => Close();
            generate.Click += (s, e) => Generate();
            save.Click += (s, e) => SaveCurrent();
            status = Ui.Label("", 9.5f, false, Ui.Muted);
            status.Margin = new Padding(Ui.S(6), Ui.S(8), 0, 0);
            actions.Controls.AddRange(new Control[] { generate, save, close, status });
            previewHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(226, 232, 240), Padding = new Padding(Ui.S(18)) };
            preview = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(Ui.S(18), Ui.S(18)) };
            previewHost.Controls.Add(preview);
            split.Panel2.Controls.Add(previewHost);
            split.Panel2.Controls.Add(actions);
            previewHost.Resize += (s, e) => RenderPreview();

            Load += (s, e) =>
            {
                split.SplitterDistance = Math.Max(Ui.S(560), split.Width - Ui.S(560));
                discipline.SelectedItem = Disciplines.All.Contains(startDiscipline) ? startDiscipline : Disciplines.Rifle;
                ShowEmptyPreview();
            };
            discipline.SelectedIndexChanged += (s, e) => LoadRange((string)discipline.SelectedItem);

            // Rule fields
            foreach (var n in new[] { minTargets, maxTargets, minHits, maxHits, minTime, maxTime, minProps, maxProps, positionCount })
                n.ValueChanged += (s, e) => ReadRules();
            foreach (var c in new[] { requireMovement, supportSide, switchSecondary, nearToFar })
                c.CheckedChanged += (s, e) => ReadRules();

            KeyPreview = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            if (keyData == (Keys.Control | Keys.G) || keyData == Keys.F5) { Generate(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        static NumericUpDown Number(int min, int max)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Width = Ui.S(70), Font = Ui.F(9.5f), TextAlign = HorizontalAlignment.Right, Margin = new Padding(0, 0, 0, Ui.S(6)) };
        }

        static CheckBox Check(string text)
        {
            return new CheckBox { Text = text, AutoSize = true, Font = Ui.F(9.5f), Margin = new Padding(0, 0, 0, Ui.S(4)) };
        }

        static void AddRule(TableLayoutPanel table, int row, string label, NumericUpDown min, NumericUpDown max)
        {
            var l = Ui.Label(label, 9.5f);
            l.Margin = new Padding(0, Ui.S(4), Ui.S(14), 0);
            table.Controls.Add(l, 0, row);
            var from = Ui.Label(max == null ? "" : "from", 9f, false, Ui.Muted);
            from.Margin = new Padding(0, Ui.S(5), Ui.S(6), 0);
            table.Controls.Add(from, 1, row);
            table.Controls.Add(min, 2, row);
            if (max != null)
            {
                var to = Ui.Label("to", 9f, false, Ui.Muted);
                to.Margin = new Padding(Ui.S(8), Ui.S(5), Ui.S(6), 0);
                table.Controls.Add(to, 3, row);
                table.Controls.Add(max, 4, row);
            }
        }

        static Control SectionTitle(string title, Button action = null, string hint = null)
        {
            var row = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(8)) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var text = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            var t = Ui.Label(title, 12f, true);
            t.Margin = new Padding(0, 0, Ui.S(10), 0);
            text.Controls.Add(t);
            if (hint != null)
            {
                var h = Ui.Label(hint, 9f, false, Ui.Muted);
                h.Margin = new Padding(0, Ui.S(6), 0, 0);
                text.Controls.Add(h);
            }
            row.Controls.Add(text, 0, 0);
            if (action != null)
            {
                action.Margin = new Padding(0);
                row.Controls.Add(action, 1, 0);
            }
            return row;
        }

        void LoadRange(string disc)
        {
            loading = true;
            range = library.Range(disc);
            settings.SuspendLayout();
            minTargets.Value = Clamp(minTargets, range.MinTargets); maxTargets.Value = Clamp(maxTargets, range.MaxTargets);
            minHits.Value = Clamp(minHits, range.MinHits); maxHits.Value = Clamp(maxHits, range.MaxHits);
            minTime.Value = Clamp(minTime, range.MinTime); maxTime.Value = Clamp(maxTime, range.MaxTime);
            minProps.Value = Clamp(minProps, range.MinProps); maxProps.Value = Clamp(maxProps, range.MaxProps);
            positionCount.Value = Clamp(positionCount, range.PositionCount);
            requireMovement.Checked = range.RequireMovement;
            supportSide.Checked = range.SupportSide;
            switchSecondary.Checked = range.SwitchSecondary;
            switchSecondary.Visible = disc == Disciplines.Pistol;
            nearToFar.Checked = range.NearToFar;
            foreach (Tile tile in propTiles.Controls) tile.Checked = range.PropIds.Contains(tile.Shape);
            positions.Text = string.Join(Environment.NewLine, range.Positions);
            RebuildTargets();
            settings.ResumeLayout(true);
            loading = false;
        }

        static decimal Clamp(NumericUpDown n, int value)
        {
            return Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        void ReadRules()
        {
            if (loading) return;
            range.MinTargets = (int)minTargets.Value; range.MaxTargets = (int)maxTargets.Value;
            range.MinHits = (int)minHits.Value; range.MaxHits = (int)maxHits.Value;
            range.MinTime = (int)minTime.Value; range.MaxTime = (int)maxTime.Value;
            range.MinProps = (int)minProps.Value; range.MaxProps = (int)maxProps.Value;
            range.PositionCount = (int)positionCount.Value;
            range.RequireMovement = requireMovement.Checked;
            range.SupportSide = supportSide.Checked;
            range.SwitchSecondary = switchSecondary.Checked;
            range.NearToFar = nearToFar.Checked;
            OnRangeChanged();
        }

        void RebuildTargets()
        {
            targetList.SuspendLayout();
            foreach (Control c in targetList.Controls.Cast<Control>().ToList()) c.Dispose();
            targetList.Controls.Clear();
            foreach (var target in range.Targets)
            {
                var t = target;
                var row = new TargetRow(t, false) { Dock = DockStyle.Fill };
                row.SetNumber(0);
                row.Changed += (s, e) => OnRangeChanged();
                row.RemoveClicked += (s, e) => { range.Targets.Remove(t); RebuildTargets(); OnRangeChanged(); };
                row.MoveClicked += dir =>
                {
                    int from = range.Targets.IndexOf(t), to = from + dir;
                    if (to < 0 || to >= range.Targets.Count) return;
                    range.Targets.RemoveAt(from);
                    range.Targets.Insert(to, t);
                    RebuildTargets();
                    OnRangeChanged();
                };
                row.TypeClicked += (s, e) =>
                {
                    using (var picker = new PickerDialog(true))
                    {
                        if (picker.ShowDialog(this) != DialogResult.OK) return;
                        t.TypeId = picker.SelectedId;
                        t.Size = Catalog.Target(t.TypeId).Sizes[0];
                        row.LoadValues();
                        row.SetNumber(0);
                        OnRangeChanged();
                    }
                };
                targetList.Controls.Add(row);
            }
            targetList.ResumeLayout(true);
        }

        void AddTarget()
        {
            using (var picker = new PickerDialog(true))
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                var last = range.Targets.LastOrDefault();
                var t = StageTarget.Of(picker.SelectedId, last != null ? last.Distance : 0);
                if (last != null) t.Unit = last.Unit;
                range.Targets.Add(t);
                RebuildTargets();
                OnRangeChanged();
            }
        }

        void OnRangeChanged()
        {
            if (RangeChanged != null) RangeChanged(this, EventArgs.Empty);
        }

        void Generate()
        {
            string error;
            var stage = Generator.Generate(range, out error);
            if (stage == null)
            {
                status.Text = error;
                status.ForeColor = Ui.Danger;
                return;
            }
            current = stage;
            save.Enabled = true;
            status.Text = "";
            RenderPreview();
        }

        void SaveCurrent()
        {
            if (current == null) return;
            if (StageSaved != null) StageSaved(current);
            status.Text = "✔  Saved \"" + current.Name + "\" to your library.";
            status.ForeColor = Color.FromArgb(21, 128, 61);
            current = null;
            save.Enabled = false;
        }

        void ShowEmptyPreview()
        {
            if (preview.Image != null) preview.Image.Dispose();
            preview.Image = null;
            status.Text = "Press Generate to build a stage from your range.";
            status.ForeColor = Ui.Muted;
        }

        void RenderPreview()
        {
            if (current == null) return;
            int width = previewHost.ClientSize.Width - Ui.S(36) - SystemInformation.VerticalScrollBarWidth;
            if (width < 200) return;
            float dpi;
            using (var g = CreateGraphics()) dpi = g.DpiX;
            var old = preview.Image;
            preview.Image = StageSheet.RenderBitmap(current, width, dpi);
            if (old != null) old.Dispose();
        }
    }
}
