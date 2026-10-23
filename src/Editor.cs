using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CourseOfFire
{
    // Edits one stage. Every change is written straight into the Stage and reported
    // through Changed so the library can be saved.
    class StageEditor : Panel
    {
        public event EventHandler Changed;

        Stage stage;
        bool loading;
        readonly TableLayoutPanel body;
        readonly TextBox name, start, positions, procedure, notes;
        readonly ComboBox discipline;
        readonly NumericUpDown parTime;
        readonly Label summary;
        readonly TableLayoutPanel targetList;
        readonly FlowLayoutPanel propTiles;

        public StageEditor()
        {
            AutoScroll = true;
            BackColor = Ui.Bg;

            body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(Ui.S(26), Ui.S(20), Ui.S(26), Ui.S(30)),
                BackColor = Ui.Bg
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            name = new TextBox { Font = Ui.F(17f, true), BorderStyle = BorderStyle.None, BackColor = Ui.Bg, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(4)) };
            Ui.Tips.SetToolTip(name, "Stage name");
            summary = Ui.Label("", 9.5f, false, Ui.Muted);
            summary.Margin = new Padding(0, 0, 0, Ui.S(16));
            Add(name);
            Add(summary);

            // Basics
            var basics = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(18)) };
            discipline = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Ui.S(170), Font = Ui.F(9.5f) };
            discipline.Items.AddRange(Disciplines.All);
            parTime = new NumericUpDown { Minimum = 1, Maximum = 3600, Width = Ui.S(80), Font = Ui.F(9.5f), TextAlign = HorizontalAlignment.Right };
            start = new TextBox { Width = Ui.S(420), Font = Ui.F(9.5f) };
            basics.Controls.Add(Field("Discipline", discipline));
            basics.Controls.Add(Field("Par time (seconds)", parTime));
            basics.Controls.Add(Field("Start position", start));
            Add(basics);

            // Targets
            var addTarget = Ui.Button("+  Add target", true);
            addTarget.Click += (s, e) => AddTarget();
            Add(Header("Targets", "Click a target's picture to change its type.", addTarget));
            targetList = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(18)) };
            targetList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Add(targetList);

            // Props
            Add(Header("Props and obstacles", null, null));
            propTiles = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, Ui.S(12)) };
            Add(propTiles);

            positions = Multiline(Ui.S(104));
            Add(Header("Positions", "One per line.", null));
            Add(positions);

            var write = Ui.Button("Write it for me");
            write.Click += (s, e) =>
            {
                if (stage == null) return;
                procedure.Text = Generator.WriteProcedure(stage);
            };
            procedure = Multiline(Ui.S(84));
            Add(Header("Procedure", "What the shooter does after the start signal.", write));
            Add(procedure);

            notes = Multiline(Ui.S(84));
            Add(Header("Notes", "Rules, penalties, safety reminders.", null));
            Add(notes);

            Controls.Add(body);

            name.TextChanged += (s, e) => Edit(() => stage.Name = name.Text.Trim().Length > 0 ? name.Text.Trim() : "Untitled stage");
            discipline.SelectedIndexChanged += (s, e) => Edit(() => stage.Discipline = (string)discipline.SelectedItem);
            parTime.ValueChanged += (s, e) => Edit(() => stage.ParTime = (int)parTime.Value);
            start.TextChanged += (s, e) => Edit(() => stage.Start = start.Text);
            positions.TextChanged += (s, e) => Edit(() => stage.Positions = positions.Lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList());
            procedure.TextChanged += (s, e) => Edit(() => stage.Procedure = procedure.Text);
            notes.TextChanged += (s, e) => Edit(() => stage.Notes = notes.Text);
        }

        public Stage Stage
        {
            get { return stage; }
        }

        public void ShowStage(Stage s)
        {
            loading = true;
            stage = s;
            SuspendLayout();
            name.Text = s.Name;
            discipline.SelectedItem = s.Discipline;
            parTime.Value = Math.Max(1, Math.Min(3600, s.ParTime));
            start.Text = s.Start;
            positions.Text = string.Join(Environment.NewLine, s.Positions);
            procedure.Text = s.Procedure;
            notes.Text = s.Notes;
            RebuildTargets();
            RebuildProps();
            UpdateSummary();
            ResumeLayout(true);
            AutoScrollPosition = new Point(0, 0);
            loading = false;
        }

        void Add(Control c)
        {
            body.Controls.Add(c);
        }

        static Control Field(string label, Control input)
        {
            var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, Ui.S(18), 0) };
            panel.Controls.Add(Ui.Label(label, 9f, true, Ui.Muted));
            input.Margin = new Padding(0);
            panel.Controls.Add(input);
            return panel;
        }

        static Control Header(string title, string hint, Button action)
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

        static TextBox Multiline(int height)
        {
            return new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Height = height,
                Font = Ui.F(10f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, Ui.S(18))
            };
        }

        void Edit(Action apply)
        {
            if (loading || stage == null) return;
            apply();
            Notify();
        }

        void Notify()
        {
            stage.Touch();
            UpdateSummary();
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        void UpdateSummary()
        {
            summary.Text = TextExport.Summary(stage) + "   ·   Saved automatically";
        }

        void RebuildTargets()
        {
            targetList.SuspendLayout();
            foreach (Control c in targetList.Controls.Cast<Control>().ToList()) c.Dispose();
            targetList.Controls.Clear();
            targetList.RowStyles.Clear();
            for (int i = 0; i < stage.Targets.Count; i++)
            {
                var row = new TargetRow(stage.Targets[i], true) { Dock = DockStyle.Fill };
                row.SetNumber(i + 1);
                var target = stage.Targets[i];
                row.Changed += (s, e) => Notify();
                row.RemoveClicked += (s, e) =>
                {
                    stage.Targets.Remove(target);
                    RebuildTargets();
                    Notify();
                };
                row.MoveClicked += dir =>
                {
                    int from = stage.Targets.IndexOf(target), to = from + dir;
                    if (to < 0 || to >= stage.Targets.Count) return;
                    stage.Targets.RemoveAt(from);
                    stage.Targets.Insert(to, target);
                    RebuildTargets();
                    Notify();
                };
                row.TypeClicked += (s, e) =>
                {
                    string id = Pick(true);
                    if (id == null) return;
                    target.TypeId = id;
                    target.Size = Catalog.Target(id).Sizes[0];
                    row.LoadValues();
                    row.SetNumber(stage.Targets.IndexOf(target) + 1);
                    Notify();
                };
                targetList.Controls.Add(row);
            }
            if (stage.Targets.Count == 0)
            {
                var empty = Ui.Label("No targets yet. Press Add target to choose from the catalog.", 9.5f, false, Ui.Muted);
                empty.Margin = new Padding(0, Ui.S(4), 0, Ui.S(8));
                targetList.Controls.Add(empty);
            }
            targetList.ResumeLayout(true);
        }

        void RebuildProps()
        {
            propTiles.SuspendLayout();
            foreach (Control c in propTiles.Controls.Cast<Control>().ToList()) c.Dispose();
            propTiles.Controls.Clear();
            foreach (var prop in stage.Props)
            {
                var p = prop;
                var tile = new Tile(false, p.TypeId, p.DisplayName, null) { ShowRemove = true, Cursor = Cursors.Default };
                Ui.Tips.SetToolTip(tile, p.TypeId == "custom_prop" ? "Click to rename" : p.DisplayName);
                tile.RemoveClicked += (s, e) =>
                {
                    stage.Props.Remove(p);
                    RebuildProps();
                    Notify();
                };
                if (p.TypeId == "custom_prop")
                {
                    tile.Cursor = Cursors.Hand;
                    tile.Activated += (s, e) =>
                    {
                        string newName = Prompt.Ask(FindForm(), "Rename prop", "Prop name", p.CustomName);
                        if (string.IsNullOrEmpty(newName)) return;
                        p.CustomName = newName;
                        RebuildProps();
                        Notify();
                    };
                }
                propTiles.Controls.Add(tile);
            }
            var add = new Tile(false, null, "Add prop or obstacle", null) { Dashed = true };
            add.Activated += (s, e) => AddProp();
            propTiles.Controls.Add(add);
            propTiles.ResumeLayout(true);
        }

        void AddTarget()
        {
            if (stage == null) return;
            string id = Pick(true);
            if (id == null) return;
            var last = stage.Targets.LastOrDefault();
            var t = StageTarget.Of(id, last != null ? last.Distance : 0);
            t.Unit = last != null ? last.Unit : "yd";
            t.Hits = last != null ? last.Hits : (stage.Discipline == Disciplines.Pistol ? 2 : 1);
            stage.Targets.Add(t);
            RebuildTargets();
            Notify();
            ScrollControlIntoView(targetList.Controls[targetList.Controls.Count - 1]);
        }

        void AddProp()
        {
            if (stage == null) return;
            string id = Pick(false);
            if (id == null) return;
            var prop = StageProp.Of(id);
            if (id == "custom_prop")
            {
                string custom = Prompt.Ask(FindForm(), "Other prop", "What is it?", "");
                if (string.IsNullOrEmpty(custom)) return;
                prop.CustomName = custom;
            }
            stage.Props.Add(prop);
            RebuildProps();
            Notify();
        }

        string Pick(bool targets)
        {
            using (var picker = new PickerDialog(targets))
                return picker.ShowDialog(FindForm()) == DialogResult.OK ? picker.SelectedId : null;
        }
    }
}
