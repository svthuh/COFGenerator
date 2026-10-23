using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace CourseOfFire
{
    // Lays out a printable stage sheet. Everything is measured in points so the same
    // drawing works on screen, in images, and on paper.
    static class StageSheet
    {
        static readonly Color Ink = Color.FromArgb(15, 23, 42);
        static readonly Color Muted = Color.FromArgb(100, 116, 139);
        static readonly Color Accent = Color.FromArgb(37, 99, 235);
        static readonly Color Rule = Color.FromArgb(226, 232, 240);
        static readonly Color Pill = Color.FromArgb(239, 246, 255);
        static readonly Color PillLine = Color.FromArgb(191, 219, 254);

        // Draws the sheet inside bounds and returns how far down it went.
        public static float Draw(Graphics g, Stage s, RectangleF b)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float x = b.X, w = b.Width, y = b.Y;

            using (var kicker = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var title = new Font("Segoe UI Semibold", 19f))
            using (var small = new Font("Segoe UI", 8.5f))
            using (var body = new Font("Segoe UI", 9.5f))
            using (var bold = new Font("Segoe UI Semibold", 9.5f))
            using (var section = new Font("Segoe UI Semibold", 11f))
            using (var ink = new SolidBrush(Ink))
            using (var muted = new SolidBrush(Muted))
            using (var accent = new SolidBrush(Accent))
            using (var rule = new Pen(Rule, 0.75f))
            {
                y += Text(g, "COURSE OF FIRE", kicker, accent, x, y, w) + 1;
                y += Text(g, s.Name, title, ink, x, y, w) + 2;
                y += Text(g, s.Discipline + "   ·   Last edited " + s.ModifiedTime.ToString("MMMM d, yyyy"), small, muted, x, y, w) + 8;

                // Summary pills
                float px = x;
                var pills = new List<string>
                {
                    s.Targets.Count + (s.Targets.Count == 1 ? " target" : " targets"),
                    s.RoundCount + " rounds",
                    s.ParTime + " s par time",
                    s.Positions.Count + (s.Positions.Count == 1 ? " position" : " positions")
                };
                float pillHeight = 0;
                foreach (var pill in pills)
                {
                    var size = g.MeasureString(pill, bold);
                    var r = new RectangleF(px, y, size.Width + 12, size.Height + 4);
                    if (r.Right > x + w) { px = x; y += r.Height + 4; r.X = px; r.Y = y; }
                    using (var path = RoundRect(r, r.Height / 2))
                    using (var fill = new SolidBrush(Pill))
                    using (var line = new Pen(PillLine, 0.75f))
                    {
                        g.FillPath(fill, path);
                        g.DrawPath(line, path);
                    }
                    g.DrawString(pill, bold, ink, r.X + 6, r.Y + 2);
                    px = r.Right + 5;
                    pillHeight = r.Height;
                }
                y += pillHeight + 14;

                // Targets
                y = Section(g, "Targets", section, ink, rule, x, y, w);
                if (s.Targets.Count == 0)
                {
                    y += Text(g, "No targets yet.", body, muted, x, y, w) + 6;
                }
                else
                {
                    float iconW = 34, numW = 24, sizeW = 82, distW = 64, hitsW = 34;
                    float nameW = w - iconW - numW - sizeW - distW - hitsW - 12;
                    float cx = x + iconW + 6, cNum = cx, cName = cNum + numW, cSize = cName + nameW + 6, cDist = cSize + sizeW, cHits = cDist + distW;
                    g.DrawString("#", small, muted, cNum, y);
                    g.DrawString("Target", small, muted, cName, y);
                    g.DrawString("Size", small, muted, cSize, y);
                    g.DrawString("Distance", small, muted, cDist, y);
                    g.DrawString("Hits", small, muted, cHits, y);
                    y += small.GetHeight(g) + 3;
                    g.DrawLine(rule, x, y, x + w, y);
                    for (int i = 0; i < s.Targets.Count; i++)
                    {
                        var t = s.Targets[i];
                        var type = Catalog.Target(t.TypeId);
                        float nameH = Measure(g, type.Name, bold, nameW);
                        float notesH = string.IsNullOrEmpty(t.Notes) ? 0 : Measure(g, t.Notes, small, nameW);
                        float rowH = Math.Max(iconW + 4, nameH + notesH + 8);
                        Icons.Draw(g, true, type.Shape, new RectangleF(x, y + (rowH - iconW) / 2, iconW, iconW));
                        float ty = y + (rowH - nameH - notesH) / 2;
                        g.DrawString("T" + (i + 1), bold, accent, cNum, ty);
                        Text(g, type.Name, bold, ink, cName, ty, nameW);
                        if (notesH > 0) Text(g, t.Notes, small, muted, cName, ty + nameH, nameW);
                        Text(g, t.Size, body, ink, cSize, ty, sizeW - 4);
                        g.DrawString(t.DistanceText, body, ink, cDist, ty);
                        g.DrawString(t.Hits.ToString(), body, ink, cHits, ty);
                        y += rowH;
                        g.DrawLine(rule, x, y, x + w, y);
                    }
                    y += 14;
                }

                // Props
                if (s.Props.Count > 0)
                {
                    y = Section(g, "Props and obstacles", section, ink, rule, x, y, w);
                    float tileW = 74, iconSize = 46, tx = x, rowBottom = y;
                    foreach (var p in s.Props)
                    {
                        if (tx + tileW > x + w + 0.5f) { tx = x; y = rowBottom + 6; }
                        Icons.Draw(g, false, p.TypeId, new RectangleF(tx + (tileW - iconSize) / 2, y, iconSize, iconSize));
                        float labelH = Text(g, p.DisplayName, small, ink, tx, y + iconSize + 2, tileW - 4, true);
                        rowBottom = Math.Max(rowBottom, y + iconSize + 2 + labelH);
                        tx += tileW + 4;
                    }
                    y = rowBottom + 14;
                }

                // Positions
                if (s.Positions.Count > 0)
                {
                    y = Section(g, "Positions", section, ink, rule, x, y, w);
                    for (int i = 0; i < s.Positions.Count; i++)
                    {
                        g.DrawString((i + 1) + ".", bold, accent, x, y);
                        y += Text(g, s.Positions[i], body, ink, x + 18, y, w - 18) + 2;
                    }
                    y += 12;
                }

                foreach (var block in new[] { new[] { "Start", s.Start }, new[] { "Procedure", s.Procedure }, new[] { "Notes", s.Notes } })
                {
                    if (string.IsNullOrWhiteSpace(block[1])) continue;
                    y = Section(g, block[0], section, ink, rule, x, y, w);
                    foreach (var line in block[1].Replace("\r", "").Split('\n').Where(l => l.Trim().Length > 0))
                    {
                        if (block[0] == "Notes")
                        {
                            g.DrawString("•", bold, accent, x + 2, y);
                            y += Text(g, line.Trim(), body, ink, x + 14, y, w - 14) + 2;
                        }
                        else y += Text(g, line.Trim(), body, ink, x, y, w) + 3;
                    }
                    y += 12;
                }

                g.DrawLine(rule, x, y, x + w, y);
                y += 5;
                y += Text(g, "Review every stage for safety, target visibility, movement paths, and fairness before use.", small, muted, x, y, w);
            }
            return y;
        }

        static float Section(Graphics g, string title, Font font, Brush brush, Pen rule, float x, float y, float w)
        {
            g.DrawString(title, font, brush, x, y);
            return y + font.GetHeight(g) + 5;
        }

        static float Text(Graphics g, string text, Font font, Brush brush, float x, float y, float w, bool center = false)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            using (var format = new StringFormat { Alignment = center ? StringAlignment.Center : StringAlignment.Near })
            {
                var size = g.MeasureString(text, font, new SizeF(w, 10000), format);
                g.DrawString(text, font, brush, new RectangleF(x, y, w, size.Height + 1), format);
                return size.Height;
            }
        }

        static float Measure(Graphics g, string text, Font font, float w)
        {
            return string.IsNullOrEmpty(text) ? 0 : g.MeasureString(text, font, new SizeF(w, 10000)).Height;
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Height in points the sheet needs at the given width.
        public static float MeasureHeight(Stage s, float widthPt)
        {
            using (var bmp = new Bitmap(1, 1))
            {
                bmp.SetResolution(144, 144);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.PageUnit = GraphicsUnit.Point;
                    return Draw(g, s, new RectangleF(0, 0, widthPt, 100000));
                }
            }
        }

        public static Bitmap RenderBitmap(Stage s, int widthPx, float dpi)
        {
            const float margin = 28;
            float widthPt = widthPx * 72f / dpi;
            float heightPt = MeasureHeight(s, widthPt - margin * 2) + margin * 2;
            var bmp = new Bitmap(widthPx, (int)Math.Ceiling(heightPt * dpi / 72f));
            bmp.SetResolution(dpi, dpi);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.PageUnit = GraphicsUnit.Point;
                Draw(g, s, new RectangleF(margin, margin, widthPt - margin * 2, 100000));
            }
            return bmp;
        }
    }

    static class Printing
    {
        public const string PdfPrinter = "Microsoft Print to PDF";

        // One stage per page; a stage that doesn't fit is shrunk to the page.
        public static PrintDocument CreateDocument(IList<Stage> stages)
        {
            var doc = new PrintDocument
            {
                DocumentName = stages.Count == 1 ? stages[0].Name : "Courses of fire",
                OriginAtMargins = true
            };
            int index = 0;
            doc.BeginPrint += (s, e) => index = 0;
            doc.QueryPageSettings += (s, e) => e.PageSettings.Margins = new Margins(50, 50, 50, 50);
            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.PageUnit = GraphicsUnit.Point;
                float width = e.MarginBounds.Width * 0.72f;
                float height = e.MarginBounds.Height * 0.72f;
                float needed = StageSheet.MeasureHeight(stages[index], width);
                if (needed > height)
                {
                    float scale = height / needed;
                    g.ScaleTransform(scale, scale);
                    width /= scale;
                }
                StageSheet.Draw(g, stages[index], new RectangleF(0, 0, width, 100000));
                index++;
                e.HasMorePages = index < stages.Count;
            };
            return doc;
        }

        public static bool HasPdfPrinter()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>().Contains(PdfPrinter);
        }

        public static void SavePdf(IList<Stage> stages, string path)
        {
            var doc = CreateDocument(stages);
            doc.PrinterSettings.PrinterName = PdfPrinter;
            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = path;
            doc.PrintController = new StandardPrintController();
            if (File.Exists(path)) File.Delete(path);
            doc.Print();
        }

        public static void SavePng(Stage stage, string path)
        {
            using (var bmp = StageSheet.RenderBitmap(stage, 1400, 175))
                bmp.Save(path, ImageFormat.Png);
        }
    }
}
