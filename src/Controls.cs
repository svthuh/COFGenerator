using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CourseOfFire
{
    static class Ui
    {
        public static float Scale = 1f;

        public static readonly Color Navy = Color.FromArgb(15, 23, 42);
        public static readonly Color NavyHover = Color.FromArgb(30, 41, 59);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
        public static readonly Color AccentSoft = Color.FromArgb(239, 246, 255);
        public static readonly Color Bg = Color.FromArgb(241, 245, 249);
        public static readonly Color Border = Color.FromArgb(218, 225, 234);
        public static readonly Color Text = Color.FromArgb(15, 23, 42);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);

        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();

        public static int S(float v)
        {
            return (int)Math.Round(v * Scale);
        }

        public static Font F(float size, bool bold = false)
        {
            string key = size + (bold ? "b" : "");
            Font font;
            if (!fonts.TryGetValue(key, out font))
            {
                font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size);
                fonts[key] = font;
            }
            return font;
        }

        public static Button Button(string text, bool primary = false)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = F(9.5f, primary),
                Padding = new Padding(S(8), 0, S(8), 0),
                MinimumSize = new Size(0, S(34)),
                Margin = new Padding(0, 0, S(8), 0),
                UseVisualStyleBackColor = false,
                BackColor = primary ? Accent : Color.White,
                ForeColor = primary ? Color.White : Text
            };
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(248, 250, 252);
            b.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(30, 64, 175) : Bg;
            return b;
        }

        public static Button HeaderButton(string text, bool primary = false)
        {
            var b = Button(text, primary);
            if (!primary)
            {
                b.BackColor = Navy;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                b.FlatAppearance.MouseOverBackColor = NavyHover;
                b.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 65, 85);
            }
            return b;
        }

        public static Button SmallButton(string text, string tip)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(S(30), S(28)),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(S(2), 0, 0, 0),
                BackColor = Color.White,
                ForeColor = Muted,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = Bg;
            Tips.SetToolTip(b, tip);
            return b;
        }

        public static readonly ToolTip Tips = new ToolTip();

        public static Label Label(string text, float size = 9.5f, bool bold = false, Color? color = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = F(size, bold),
                ForeColor = color ?? Text,
                Margin = new Padding(0, 0, 0, S(4))
            };
        }

        public static void ShowMenu(Control anchor, ContextMenuStrip menu)
        {
            menu.Show(anchor, new Point(0, anchor.Height + S(2)));
        }

        public static ContextMenuStrip Menu()
        {
            return new ContextMenuStrip { Font = F(9.5f), ShowImageMargin = false };
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            return StageSheet.RoundRect(r, radius);
        }
    }

    // Shows one target or prop drawing.
    class IconBox : Control
    {
        public bool IsTarget;
        public string Shape;

        public IconBox(bool isTarget, string shape)
        {
            IsTarget = isTarget;
            Shape = shape;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Icons.Draw(e.Graphics, IsTarget, Shape, new RectangleF(1, 1, Width - 2, Height - 2));
        }
    }

    // A card with a drawing and a name. Used in pickers, the stage's prop list, and
    // (as a toggle) the generator's list of available props.
    class Tile : Control
    {
        public readonly bool IsTarget;
        public readonly string Shape;
        public string Title, Subtitle;
        public bool Toggle, ShowRemove;
        public bool Dashed;
        bool isChecked, hover;

        public event EventHandler Activated, RemoveClicked;

        public Tile(bool isTarget, string shape, string title, string subtitle)
        {
            IsTarget = isTarget;
            Shape = shape;
            Title = title;
            Subtitle = subtitle;
            Size = new Size(Ui.S(124), Ui.S(subtitle == null ? 118 : 138));
            Margin = new Padding(0, 0, Ui.S(10), Ui.S(10));
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public bool Checked
        {
            get { return isChecked; }
            set { isChecked = value; Invalidate(); }
        }

        Rectangle RemoveRect
        {
            get { return new Rectangle(Width - Ui.S(28), Ui.S(4), Ui.S(24), Ui.S(24)); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (ShowRemove && RemoveRect.Contains(e.Location))
            {
                if (RemoveClicked != null) RemoveClicked(this, EventArgs.Empty);
                return;
            }
            if (Toggle) Checked = !Checked;
            if (Activated != null) Activated(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            bool dim = Toggle && !isChecked;
            var card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Ui.RoundRect(card, Ui.S(10)))
            {
                var fill = dim ? Color.FromArgb(248, 250, 252) : (isChecked && Toggle ? Ui.AccentSoft : Color.White);
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                var border = hover || (isChecked && Toggle) ? Ui.Accent : Ui.Border;
                using (var p = new Pen(border, hover || (isChecked && Toggle) ? 1.6f : 1f))
                {
                    if (Dashed) p.DashStyle = DashStyle.Dash;
                    g.DrawPath(p, path);
                }
            }

            int pad = Ui.S(10);
            int iconSize = Width - pad * 4;
            var iconRect = new RectangleF((Width - iconSize) / 2f, pad, iconSize, iconSize * 0.82f);
            if (Shape != null)
            {
                Icons.Draw(g, IsTarget, Shape, iconRect);
                if (dim)
                    using (var veil = new SolidBrush(Color.FromArgb(150, 248, 250, 252))) g.FillRectangle(veil, iconRect);
            }
            else
            {
                TextRenderer.DrawText(g, "+", Ui.F(26), Rectangle.Round(iconRect), Ui.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            int textTop = (int)iconRect.Bottom + Ui.S(4);
            var titleRect = new Rectangle(Ui.S(6), textTop, Width - Ui.S(12), Ui.S(36));
            TextRenderer.DrawText(g, Title, Ui.F(9f, true), titleRect, dim ? Ui.Muted : (Shape == null ? Ui.Accent : Ui.Text), flags);
            if (Subtitle != null)
            {
                int titleH = TextRenderer.MeasureText(g, Title, Ui.F(9f, true), titleRect.Size, flags).Height;
                TextRenderer.DrawText(g, Subtitle, Ui.F(8f), new Rectangle(titleRect.X, textTop + titleH, titleRect.Width, Ui.S(30)),
                    Ui.Muted, flags);
            }

            if (Toggle)
            {
                var box = new Rectangle(Width - Ui.S(26), Ui.S(6), Ui.S(18), Ui.S(18));
                using (var b = new SolidBrush(isChecked ? Ui.Accent : Color.White)) g.FillEllipse(b, box);
                using (var p = new Pen(isChecked ? Ui.Accent : Ui.Border, 1.4f)) g.DrawEllipse(p, box);
                if (isChecked)
                    using (var p = new Pen(Color.White, Ui.S(2)))
                        g.DrawLines(p, new[] { new Point(box.X + Ui.S(5), box.Y + Ui.S(9)), new Point(box.X + Ui.S(8), box.Y + Ui.S(12)),
                                               new Point(box.X + Ui.S(13), box.Y + Ui.S(6)) });
            }
            if (ShowRemove && hover)
                TextRenderer.DrawText(g, "✕", Ui.F(10f), RemoveRect, Ui.Danger,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // One target: drawing, name, size, distance, hits and notes.
    class TargetRow : TableLayoutPanel
    {
        public readonly StageTarget Target;
        readonly IconBox icon;
        readonly Label title;
        readonly ComboBox size;
        readonly NumericUpDown distance, hits;
        readonly ComboBox unit;
        readonly TextBox notes;
        bool loading;

        public event EventHandler Changed, RemoveClicked, TypeClicked;
        public event Action<int> MoveClicked;

        public TargetRow(StageTarget target, bool showHits)
        {
            Target = target;
            ColumnCount = 3;
            RowCount = 2;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.White;
            Padding = new Padding(Ui.S(8), Ui.S(6), Ui.S(8), Ui.S(6));
            Margin = new Padding(0, 0, 0, Ui.S(6));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.S(70)));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));

            icon = new IconBox(true, Catalog.Target(target.TypeId).Shape)
            {
                Size = new Size(Ui.S(60), Ui.S(60)),
                Cursor = Cursors.Hand,
                Margin = new Padding(0),
                BackColor = Color.White
            };
            Ui.Tips.SetToolTip(icon, "Click to change the target type");
            icon.Click += (s, e) => { if (TypeClicked != null) TypeClicked(this, EventArgs.Empty); };
            Controls.Add(icon, 0, 0);
            SetRowSpan(icon, 2);

            title = Ui.Label("", 10f, true);
            title.Margin = new Padding(0, Ui.S(2), 0, Ui.S(4));
            Controls.Add(title, 1, 0);

            // The notes box takes whatever width is left over.
            var fields = new TableLayoutPanel { AutoSize = true, RowCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };
            size = new ComboBox { Width = Ui.S(140), Font = Ui.F(9.5f) };
            distance = new NumericUpDown { Width = Ui.S(72), Maximum = 3000, Font = Ui.F(9.5f), TextAlign = HorizontalAlignment.Right };
            unit = new ComboBox { Width = Ui.S(52), DropDownStyle = ComboBoxStyle.DropDownList, Font = Ui.F(9.5f) };
            unit.Items.AddRange(new object[] { "yd", "m" });
            hits = new NumericUpDown { Width = Ui.S(56), Minimum = 1, Maximum = 50, Font = Ui.F(9.5f), TextAlign = HorizontalAlignment.Right };
            notes = new TextBox { Font = Ui.F(9.5f), MinimumSize = new Size(Ui.S(80), 0), Anchor = AnchorStyles.Left | AnchorStyles.Right };
            AddField(fields, "Size", size);
            AddField(fields, "Distance", distance);
            unit.Margin = new Padding(Ui.S(2), 0, Ui.S(14), 0);
            AddCell(fields, unit);
            if (showHits) AddField(fields, "Hits", hits);
            AddField(fields, "Notes", notes);
            notes.Margin = new Padding(0);
            for (int i = 0; i < fields.ColumnCount; i++)
                fields.ColumnStyles.Add(i == fields.ColumnCount - 1 ? new ColumnStyle(SizeType.Percent, 100) : new ColumnStyle(SizeType.AutoSize));
            Controls.Add(fields, 1, 1);

            var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(Ui.S(6), 0, 0, 0) };
            var up = Ui.SmallButton("▲", "Move up");
            var down = Ui.SmallButton("▼", "Move down");
            var remove = Ui.SmallButton("✕", "Remove target");
            remove.ForeColor = Ui.Danger;
            up.Click += (s, e) => { if (MoveClicked != null) MoveClicked(-1); };
            down.Click += (s, e) => { if (MoveClicked != null) MoveClicked(1); };
            remove.Click += (s, e) => { if (RemoveClicked != null) RemoveClicked(this, EventArgs.Empty); };
            buttons.Controls.AddRange(new Control[] { up, down, remove });
            Controls.Add(buttons, 2, 0);
            SetRowSpan(buttons, 2);

            LoadValues();
            size.TextChanged += (s, e) => Edit(() => Target.Size = size.Text.Trim());
            distance.ValueChanged += (s, e) => Edit(() => Target.Distance = (int)distance.Value);
            unit.SelectedIndexChanged += (s, e) => Edit(() => Target.Unit = (string)unit.SelectedItem);
            hits.ValueChanged += (s, e) => Edit(() => Target.Hits = (int)hits.Value);
            notes.TextChanged += (s, e) => Edit(() => Target.Notes = notes.Text);
        }

        static void AddField(TableLayoutPanel panel, string label, Control control)
        {
            var l = Ui.Label(label, 8.5f, false, Ui.Muted);
            l.Margin = new Padding(0, Ui.S(6), Ui.S(4), 0);
            control.Margin = new Padding(0, 0, Ui.S(14), 0);
            AddCell(panel, l);
            AddCell(panel, control);
        }

        static void AddCell(TableLayoutPanel panel, Control control)
        {
            panel.ColumnCount = panel.Controls.Count + 1;
            panel.Controls.Add(control, panel.Controls.Count, 0);
        }

        void Edit(Action apply)
        {
            if (loading) return;
            apply();
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        public void SetNumber(int number)
        {
            title.Text = (number > 0 ? "T" + number + "   " : "") + Catalog.Target(Target.TypeId).Name;
        }

        // Reloads after the target type changes.
        public void LoadValues()
        {
            loading = true;
            var type = Catalog.Target(Target.TypeId);
            icon.Shape = type.Shape;
            icon.Invalidate();
            size.Items.Clear();
            size.Items.AddRange(type.Sizes);
            size.Text = Target.Size;
            distance.Value = Math.Max(0, Math.Min(3000, Target.Distance));
            unit.SelectedItem = Target.Unit == "m" ? "m" : "yd";
            hits.Value = Math.Max(1, Math.Min(50, Target.Hits));
            notes.Text = Target.Notes;
            loading = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Ui.Border)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    // Grid of every target or prop to choose from.
    class PickerDialog : Form
    {
        public string SelectedId;
        readonly FlowLayoutPanel grid;
        readonly bool targets;
        string category = "All";
        readonly TextBox search;

        public PickerDialog(bool targets)
        {
            this.targets = targets;
            Text = targets ? "Add a target" : "Add a prop or obstacle";
            ShowIcon = false;
            Font = Ui.F(9.5f);
            BackColor = Ui.Bg;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            ClientSize = new Size(Ui.S(860), Ui.S(640));
            KeyPreview = true;

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(Ui.S(14), Ui.S(12), Ui.S(14), Ui.S(4)), BackColor = Ui.Bg };
            search = new TextBox { Width = Ui.S(220), Font = Ui.F(10f), Margin = new Padding(0, Ui.S(4), Ui.S(16), 0) };
            Ui.Tips.SetToolTip(search, "Search");
            top.Controls.Add(Ui.Label("Search", 9.5f, false, Ui.Muted));
            top.Controls[0].Margin = new Padding(0, Ui.S(7), Ui.S(6), 0);
            top.Controls.Add(search);
            if (targets)
            {
                foreach (var cat in new[] { "All" }.Concat(Catalog.TargetCategories))
                {
                    var b = Ui.Button(cat);
                    string c = cat;
                    b.Click += (s, e) => { category = c; Fill(); HighlightCategory(top); };
                    b.Tag = cat;
                    top.Controls.Add(b);
                }
                HighlightCategory(top);
            }
            search.TextChanged += (s, e) => Fill();

            grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(Ui.S(14), Ui.S(8), Ui.S(4), Ui.S(8)), BackColor = Ui.Bg };
            Controls.Add(grid);
            Controls.Add(top);
            Fill();
            Shown += (s, e) => search.Focus();
        }

        void HighlightCategory(Control bar)
        {
            foreach (Control c in bar.Controls)
            {
                var b = c as Button;
                if (b == null || b.Tag == null) continue;
                bool on = (string)b.Tag == category;
                b.BackColor = on ? Ui.Accent : Color.White;
                b.ForeColor = on ? Color.White : Ui.Text;
                b.FlatAppearance.MouseOverBackColor = on ? Ui.AccentHover : Color.FromArgb(248, 250, 252);
            }
        }

        void Fill()
        {
            string q = search.Text.Trim().ToLowerInvariant();
            grid.SuspendLayout();
            grid.Controls.Clear();
            if (targets)
            {
                foreach (var t in Catalog.Targets)
                {
                    if (category != "All" && t.Category != category && t.Id != "custom_target") continue;
                    if (q.Length > 0 && !(t.Name + " " + t.Hint + " " + t.Category).ToLowerInvariant().Contains(q)) continue;
                    AddTile(new Tile(true, t.Shape, t.Name, t.Hint), t.Id);
                }
            }
            else
            {
                foreach (var p in Catalog.Props)
                {
                    if (q.Length > 0 && !(p.Name + " " + p.Hint).ToLowerInvariant().Contains(q)) continue;
                    AddTile(new Tile(false, p.Id, p.Name, p.Hint), p.Id);
                }
            }
            grid.ResumeLayout();
        }

        void AddTile(Tile tile, string id)
        {
            tile.Activated += (s, e) =>
            {
                SelectedId = id;
                DialogResult = DialogResult.OK;
            };
            grid.Controls.Add(tile);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    static class Prompt
    {
        public static string Ask(IWin32Window owner, string title, string label, string value)
        {
            using (var form = new Form
            {
                Text = title,
                Font = Ui.F(9.5f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White
            })
            {
                var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Padding = new Padding(Ui.S(18)) };
                var box = new TextBox { Text = value, Width = Ui.S(320), Font = Ui.F(10f), Margin = new Padding(0, 0, 0, Ui.S(14)) };
                var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0) };
                var ok = Ui.Button("OK", true);
                var cancel = Ui.Button("Cancel");
                ok.DialogResult = DialogResult.OK;
                cancel.DialogResult = DialogResult.Cancel;
                buttons.Controls.Add(ok);
                buttons.Controls.Add(cancel);
                layout.Controls.Add(Ui.Label(label));
                layout.Controls.Add(box);
                layout.Controls.Add(buttons);
                form.Controls.Add(layout);
                form.AcceptButton = ok;
                form.CancelButton = cancel;
                return form.ShowDialog(owner) == DialogResult.OK ? box.Text.Trim() : null;
            }
        }
    }
}
