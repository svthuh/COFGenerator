using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CourseOfFire
{
    // Vector drawings of each target and prop, so they look right at any size and in print.
    // Every drawing works on a 100 x 100 grid that is scaled into the requested rectangle.
    static class Icons
    {
        static readonly Color SteelLight = Color.FromArgb(232, 237, 243);
        static readonly Color SteelDark = Color.FromArgb(150, 163, 180);
        static readonly Color SteelLine = Color.FromArgb(51, 65, 85);
        static readonly Color Iron = Color.FromArgb(71, 85, 105);
        static readonly Color IronDark = Color.FromArgb(30, 41, 59);
        static readonly Color Wood = Color.FromArgb(214, 164, 108);
        static readonly Color WoodDark = Color.FromArgb(166, 114, 64);
        static readonly Color WoodLine = Color.FromArgb(112, 70, 33);
        static readonly Color Cardboard = Color.FromArgb(214, 184, 140);
        static readonly Color CardboardLine = Color.FromArgb(124, 92, 56);
        static readonly Color Ink = Color.FromArgb(30, 41, 59);
        static readonly Color Opening = Color.FromArgb(241, 245, 249);

        public static void Draw(Graphics g, bool isTarget, string shape, RectangleF r)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var c = new Canvas(g, r);
            if (isTarget) DrawTarget(c, shape); else DrawProp(c, shape);
            g.SmoothingMode = oldMode;
        }

        static void DrawTarget(Canvas c, string shape)
        {
            switch (shape)
            {
                case "gong":
                    Hanger(c, 30, 70, 40, 60, 26);
                    c.Steel(c.EllipsePath(16, 24, 68, 68));
                    c.Shine(26, 32, 24, 14);
                    break;
                case "square":
                    Hanger(c, 28, 72, 34, 66, 22);
                    c.Steel(c.RectPath(18, 22, 64, 64));
                    c.Shine(24, 28, 22, 12);
                    break;
                case "rect":
                    Hanger(c, 34, 66, 40, 60, 16);
                    c.Steel(c.RectPath(30, 16, 40, 78));
                    c.Shine(34, 21, 14, 10);
                    break;
                case "ipsc":
                    c.Line(Iron, 2.2f, 50, 1, 50, 9);
                    c.Steel(Silhouette(c, 4));
                    c.Shine(26, 38, 18, 12);
                    break;
                case "diamond":
                    c.Line(Iron, 2f, 50, 2, 50, 12);
                    c.Steel(c.PolyPath(50, 10, 88, 50, 50, 90, 12, 50));
                    c.Shine(36, 32, 16, 10);
                    break;
                case "triangle":
                    Hanger(c, 22, 78, 28, 72, 22);
                    c.Steel(c.PolyPath(12, 22, 88, 22, 50, 90));
                    c.Shine(28, 27, 22, 8);
                    break;
                case "kyl":
                    c.Shadow(50, 95, 92, 5);
                    c.Line(IronDark, 3.4f, 12, 18, 6, 94);
                    c.Line(IronDark, 3.4f, 88, 18, 94, 94);
                    c.Line(IronDark, 4f, 8, 18, 92, 18);
                    float x = 12.5f;
                    foreach (float s in new[] { 20f, 16f, 12f, 9f, 6f })
                    {
                        float cx = x + s / 2;
                        c.Line(Iron, 1.4f, cx, 18, cx, 28);
                        c.Steel(c.RectPath(x, 28, s, s), 1.4f);
                        x += s + 3;
                    }
                    break;
                case "coyote":
                    c.Shadow(52, 94, 84, 5);
                    c.SteelUnion(1.9f,
                        c.RectPath(12, 86, 80, 6),
                        c.PolyPath(26, 50, 6, 62, 9, 67, 30, 56),
                        c.RectPath(27, 52, 6, 34), c.RectPath(38, 54, 6, 32),
                        c.RectPath(57, 54, 6, 32), c.RectPath(67, 52, 6, 34),
                        c.EllipsePath(20, 34, 56, 27),
                        c.PolyPath(64, 40, 70, 22, 75, 32, 80, 23, 82, 35, 95, 44, 93, 49, 80, 51, 68, 55));
                    c.Ellipse(Ink, Color.Empty, 0, 80, 37, 3, 3);
                    break;
                case "prairiedog":
                    c.Shadow(50, 95, 60, 5);
                    // Standing upright: small head, tiny ears, pear-shaped body, paws at the chest
                    c.SteelUnion(1.9f,
                        c.RectPath(28, 88, 44, 6),
                        c.PolyPath(40, 30, 60, 30, 66, 44, 72, 66, 72, 82, 66, 90, 34, 90, 28, 82, 28, 66, 34, 44),
                        c.EllipsePath(38, 10, 24, 24),
                        c.EllipsePath(39, 11, 6, 5), c.EllipsePath(55, 11, 6, 5),
                        c.EllipsePath(44, 26, 12, 9));
                    c.Line(SteelLine, 1.6f, 40, 46, 44, 54, 48, 54);
                    c.Line(SteelLine, 1.6f, 60, 46, 56, 54, 52, 54);
                    c.Ellipse(Ink, Color.Empty, 0, 43, 18, 3, 3);
                    c.Ellipse(Ink, Color.Empty, 0, 54, 18, 3, 3);
                    c.Ellipse(Ink, Color.Empty, 0, 48.5f, 26, 3, 2);
                    break;
                case "uspsa":
                case "noshoot":
                    bool noShoot = shape == "noshoot";
                    Laths(c);
                    var body = Silhouette(c, 0, 0.92f);
                    c.Fill(body, noShoot ? Color.White : Cardboard, noShoot ? Ink : CardboardLine, 1.8f);
                    var zone = noShoot ? Color.FromArgb(160, 170, 185) : CardboardLine;
                    c.Dashed(zone, 1.1f, c.PolyPath(28, 30, 72, 30, 72, 66, 64, 75, 36, 75, 28, 66));
                    c.Dashed(zone, 1.1f, c.RectPath(41, 33, 18, 28));
                    c.Dashed(zone, 1.1f, c.RectPath(45, 6, 10, 8));
                    break;
                case "idpa":
                    Laths(c);
                    c.FillUnion(Cardboard, CardboardLine, 1.8f,
                        c.PolyPath(39, 3, 61, 3, 63, 5, 63, 20, 61, 22, 39, 22, 37, 20, 37, 5),
                        c.PolyPath(25, 22, 75, 22, 80, 28, 80, 78, 72, 86, 28, 86, 20, 78, 20, 28));
                    c.Dashed(CardboardLine, 1.1f, c.EllipsePath(45, 8, 10, 10));
                    c.Dashed(CardboardLine, 1.1f, c.EllipsePath(39, 30, 22, 22));
                    c.Dashed(CardboardLine, 1.1f, c.PolyPath(30, 27, 70, 27, 72, 30, 72, 70, 66, 78, 34, 78, 28, 70, 28, 30));
                    break;
                case "bullseye":
                    Laths(c);
                    c.Rect(Color.FromArgb(252, 251, 246), Color.FromArgb(150, 150, 140), 1.4f, 14, 4, 72, 80);
                    c.Ellipse(Color.Empty, Ink, 1.2f, 20, 14, 60, 60);
                    c.Ellipse(Color.Empty, Ink, 1.2f, 27, 21, 46, 46);
                    c.Ellipse(Ink, Ink, 1.2f, 34, 28, 32, 32);
                    c.Ellipse(Color.Empty, Color.White, 1.1f, 40, 34, 20, 20);
                    c.Ellipse(Color.Empty, Color.White, 1.1f, 46, 40, 8, 8);
                    break;
                case "popper":
                    Popper(c);
                    break;
                case "minipopper":
                    Popper(c.Sub(16, 26, 68, 74));
                    break;
                case "platerack":
                    c.Shadow(50, 95, 90, 5);
                    c.Line(IronDark, 3.4f, 14, 62, 8, 94);
                    c.Line(IronDark, 3.4f, 86, 62, 92, 94);
                    for (int i = 0; i < 6; i++)
                    {
                        float px = 14.5f + i * 14.2f;
                        c.Line(Iron, 2f, px, 50, px, 58);
                        c.Steel(c.EllipsePath(px - 6.5f, 40, 13, 13), 1.4f);
                    }
                    c.Rect(Iron, IronDark, 1.4f, 5, 57, 90, 7);
                    break;
                case "duelingtree":
                    c.Shadow(50, 95, 50, 5);
                    c.Line(IronDark, 4f, 30, 94, 70, 94);
                    c.Rect(Iron, IronDark, 1.4f, 47, 8, 6, 86);
                    float[] ys = { 22, 38, 54, 70 };
                    for (int i = 0; i < ys.Length; i++)
                    {
                        bool left = i % 2 == 0;
                        c.Line(Iron, 2.4f, 50, ys[i], left ? 30 : 70, ys[i]);
                        c.Steel(c.RectPath(left ? 14 : 70, ys[i] - 8, 16, 16), 1.4f);
                    }
                    break;
                case "texasstar":
                    c.Shadow(50, 96, 56, 5);
                    c.Line(IronDark, 3.6f, 50, 44, 26, 96);
                    c.Line(IronDark, 3.6f, 50, 44, 74, 96);
                    for (int i = 0; i < 5; i++)
                    {
                        double a = (-90 + i * 72) * Math.PI / 180;
                        float px = 50 + (float)Math.Cos(a) * 32, py = 44 + (float)Math.Sin(a) * 32;
                        c.Line(Iron, 2.6f, 50, 44, px, py);
                        c.Steel(c.EllipsePath(px - 8, py - 8, 16, 16), 1.4f);
                    }
                    c.Ellipse(IronDark, IronDark, 1, 45, 39, 10, 10);
                    break;
                case "spinner":
                    // Stake with a pivot; a small and a large paddle on one arm, mid-spin
                    c.Shadow(50, 95, 50, 5);
                    c.Line(IronDark, 3.4f, 50, 48, 50, 95);
                    c.Line(IronDark, 3f, 38, 95, 62, 95);
                    using (var motion = c.Pen(Color.FromArgb(148, 163, 184), 1.6f))
                    {
                        motion.DashStyle = DashStyle.Dash;
                        c.G.DrawArc(motion, c.R(14, 12, 72, 72), 200, 50);
                        c.G.DrawArc(motion, c.R(14, 12, 72, 72), 20, 50);
                    }
                    c.Line(Iron, 3f, 32, 26, 70, 72);
                    c.Steel(c.EllipsePath(22, 14, 20, 20), 1.5f);
                    c.Steel(c.EllipsePath(58, 58, 28, 28), 1.5f);
                    c.Ellipse(IronDark, IronDark, 1, 46, 44, 8, 8);
                    break;
                case "swinger":
                    c.Shadow(50, 95, 76, 5);
                    c.Line(IronDark, 3.4f, 18, 8, 18, 94);
                    c.Line(IronDark, 3.4f, 82, 8, 82, 94);
                    c.Line(IronDark, 3.4f, 18, 8, 82, 8);
                    c.Line(Iron, 2.4f, 50, 8, 62, 58);
                    c.Steel(c.EllipsePath(50, 54, 26, 26), 1.6f);
                    c.Ellipse(IronDark, IronDark, 1, 47, 5, 6, 6);
                    break;
                default:
                    c.Shadow(50, 95, 40, 5);
                    c.Line(IronDark, 4f, 34, 94, 66, 94);
                    c.Rect(Iron, IronDark, 1.2f, 47, 56, 6, 38);
                    c.Steel(c.RectPath(22, 6, 56, 52));
                    c.Text("?", Ink, 34, 22, 6, 56, 52);
                    break;
            }
        }

        static void DrawProp(Canvas c, string shape)
        {
            switch (shape)
            {
                case "barricade":
                    // PRS rulebook barricade: 66 in wide, steps at 29 in and 49 in, an 18 in wide top
                    // 6 in higher, and shooting ledges 12 in deep. Drawn with its thickness showing.
                    c.Shadow(50, 91, 96, 6);
                    float[] steps = { 8, 88, 8, 51.1f, 23.3f, 51.1f, 23.3f, 25.6f, 38.5f, 25.6f, 38.5f, 18,
                                      61.5f, 18, 61.5f, 25.6f, 76.7f, 25.6f, 76.7f, 51.1f, 92, 51.1f, 92, 88 };
                    var back = (float[])steps.Clone();
                    for (int i = 0; i < back.Length; i += 2) { back[i] += 3; back[i + 1] -= 3; }
                    c.Poly(WoodDark, WoodLine, 1.4f, back);
                    c.Poly(Wood, WoodLine, 1.8f, steps);
                    foreach (float[] ledge in new[] { new[] { 8f, 51.1f, 23.3f }, new[] { 23.3f, 25.6f, 38.5f }, new[] { 38.5f, 18f, 61.5f },
                                                      new[] { 61.5f, 25.6f, 76.7f }, new[] { 76.7f, 51.1f, 92f } })
                        c.Poly(Color.FromArgb(236, 204, 160), WoodLine, 1.2f,
                            ledge[0], ledge[1], ledge[2], ledge[1], ledge[2] + 3, ledge[1] - 3, ledge[0] + 3, ledge[1] - 3);
                    c.Line(WoodDark, 1f, 38.5f, 26.5f, 38.5f, 87);
                    c.Line(WoodDark, 1f, 61.5f, 26.5f, 61.5f, 87);
                    break;
                case "vtac":
                    // Viking Tactics barricade, to the plan's proportions: stepped right edge,
                    // square and angled ports, and three notches cut out of the bottom.
                    c.Shadow(50, 95, 84, 6);
                    c.Beam(WoodDark, 3.4f, 26, 36, 11, 93);
                    c.Rect(WoodDark, WoodLine, 1.2f, 13, 89, 22, 5);
                    c.Rect(WoodDark, WoodLine, 1.2f, 65, 89, 22, 5);
                    c.Poly(Color.FromArgb(226, 196, 150), WoodLine, 1.6f,
                        20, 2, 35.2f, 2, 35.2f, 16.8f, 42.7f, 16.8f, 42.7f, 24.1f, 50.1f, 24.1f, 50.1f, 38.8f,
                        57.1f, 38.8f, 57.1f, 46.6f, 64.9f, 46.6f, 64.9f, 61, 79.4f, 61, 79.4f, 90,
                        69.8f, 90, 62.5f, 83.3f, 55.9f, 90, 51.3f, 90, 51.3f, 84.3f, 44.5f, 84.3f, 44.5f, 90,
                        38.8f, 90, 38.8f, 79.7f, 27.8f, 79.7f, 27.8f, 90, 20, 90);
                    c.Rect(Opening, WoodLine, 1.2f, 34.8f, 33.9f, 7.6f, 7.5f);
                    c.Rect(Opening, WoodLine, 1.2f, 27.4f, 44.2f, 8f, 7.6f);
                    c.Rect(Opening, WoodLine, 1.2f, 27.4f, 59.7f, 15f, 2.8f);
                    c.Rect(Opening, WoodLine, 1.2f, 44.5f, 66.6f, 13.2f, 2.8f);
                    c.Fill(c.Slot(28.4f, 19.7f, 10.1f, 2.6f, 35), Opening, WoodLine, 1.2f);
                    c.Fill(c.Slot(45.9f, 49.4f, 8.9f, 2.6f, -55), Opening, WoodLine, 1.2f);
                    break;
                case "tanktrap":
                    c.Shadow(50, 95, 84, 6);
                    c.Beam(Iron, 9, 40, 97, 60, 5);
                    c.Beam(SteelDark, 9, 10, 92, 86, 14);
                    c.Beam(SteelDark, 9, 90, 92, 14, 14);
                    break;
                case "rooftop":
                    c.Shadow(54, 94, 88, 7);
                    c.Poly(Wood, WoodLine, 1.6f, 6, 86, 34, 38, 62, 86);
                    c.Line(WoodDark, 1.4f, 34, 44, 34, 86);
                    c.Poly(Color.FromArgb(116, 92, 80), Color.FromArgb(70, 50, 40), 1.8f, 34, 38, 70, 24, 98, 72, 62, 86);
                    for (float t = 0.2f; t < 0.95f; t += 0.2f)
                        c.Line(Color.FromArgb(80, 60, 50), 1.1f, Lerp(34, 62, t), Lerp(38, 86, t), Lerp(70, 98, t), Lerp(24, 72, t));
                    break;
                case "ladder":
                    c.Shadow(58, 95, 46, 5);
                    c.Beam(Color.FromArgb(203, 213, 225), 4.5f, 32, 96, 50, 4);
                    c.Beam(Color.FromArgb(203, 213, 225), 4.5f, 58, 96, 76, 4);
                    for (float t = 0.1f; t < 0.95f; t += 0.13f)
                        c.Beam(Color.FromArgb(203, 213, 225), 3f, Lerp(32, 50, t), Lerp(96, 4, t), Lerp(58, 76, t), Lerp(96, 4, t));
                    break;
                case "tripod":
                    c.Shadow(50, 95, 76, 6);
                    c.Beam(Color.FromArgb(55, 65, 81), 3.6f, 50, 18, 14, 94);
                    c.Beam(Color.FromArgb(55, 65, 81), 3.6f, 50, 18, 86, 94);
                    c.Beam(Color.FromArgb(75, 85, 99), 4f, 50, 18, 56, 92);
                    c.Rect(IronDark, IronDark, 1, 43, 10, 14, 10);
                    c.Poly(Color.FromArgb(75, 85, 99), IronDark, 1.4f, 34, 2, 44, 10, 56, 10, 66, 2, 60, 2, 54, 7, 46, 7, 40, 2);
                    break;
                case "barrel":
                    c.Shadow(50, 95, 60, 6);
                    Barrel(c, 26, 6, 48, 88);
                    break;
                case "barrelstack":
                    c.Shadow(50, 95, 92, 6);
                    Barrel(c, 8, 48, 40, 47);
                    Barrel(c, 52, 48, 40, 47);
                    Barrel(c, 30, 3, 40, 47);
                    break;
                case "tire":
                    c.Shadow(50, 95, 84, 6);
                    var tire = new GraphicsPath(FillMode.Alternate);
                    tire.AddEllipse(c.R(6, 4, 88, 88));
                    tire.AddEllipse(c.R(30, 28, 40, 40));
                    c.Fill(tire, Color.FromArgb(38, 42, 48), Color.FromArgb(17, 20, 24), 1.6f);
                    c.Ellipse(Color.Empty, Color.FromArgb(70, 76, 84), 1.4f, 20, 18, 60, 60);
                    for (int i = 0; i < 24; i++)
                    {
                        double a = i * 15 * Math.PI / 180;
                        float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
                        c.Line(Color.FromArgb(80, 86, 94), 2.2f, 50 + cos * 37, 48 + sin * 37, 50 + cos * 42, 48 + sin * 42);
                    }
                    break;
                case "sawhorse":
                    c.Shadow(50, 95, 92, 6);
                    c.Beam(WoodDark, 5, 24, 36, 12, 94);
                    c.Beam(WoodDark, 5, 76, 36, 88, 94);
                    c.Beam(Wood, 5, 28, 36, 36, 94);
                    c.Beam(Wood, 5, 72, 36, 64, 94);
                    c.Rect(Wood, WoodLine, 1.6f, 6, 26, 88, 11);
                    c.Line(WoodDark, 1f, 10, 31.5f, 90, 31.5f);
                    break;
                case "spool":
                    c.Shadow(50, 94, 88, 8);
                    c.Ellipse(WoodDark, WoodLine, 1.4f, 8, 72, 84, 20);
                    c.Ellipse(Wood, WoodLine, 1.4f, 8, 67, 84, 20);
                    c.Rect(WoodDark, WoodLine, 1.4f, 34, 38, 32, 40);
                    c.Line(WoodLine, 0.9f, 42, 40, 42, 76);
                    c.Line(WoodLine, 0.9f, 50, 40, 50, 78);
                    c.Line(WoodLine, 0.9f, 58, 40, 58, 76);
                    c.Ellipse(WoodDark, WoodLine, 1.4f, 8, 30, 84, 20);
                    c.Ellipse(Color.FromArgb(226, 186, 136), WoodLine, 1.4f, 8, 24, 84, 20);
                    c.Line(WoodDark, 0.9f, 22, 28, 22, 42);
                    c.Line(WoodDark, 0.9f, 36, 25, 36, 44);
                    c.Line(WoodDark, 0.9f, 64, 25, 64, 44);
                    c.Line(WoodDark, 0.9f, 78, 28, 78, 42);
                    c.Ellipse(WoodLine, WoodLine, 1, 44, 31, 12, 6);
                    break;
                case "vehicle":
                    c.Shadow(50, 88, 96, 7);
                    c.Poly(Color.FromArgb(90, 104, 124), Color.FromArgb(40, 50, 66), 1.8f,
                        3, 74, 3, 58, 20, 52, 32, 34, 68, 34, 82, 52, 96, 56, 97, 74);
                    c.Poly(Color.FromArgb(191, 219, 254), Color.FromArgb(40, 50, 66), 1.2f, 35, 38, 48, 38, 48, 52, 24, 52);
                    c.Poly(Color.FromArgb(191, 219, 254), Color.FromArgb(40, 50, 66), 1.2f, 52, 38, 66, 38, 77, 52, 52, 52);
                    c.Line(Color.FromArgb(40, 50, 66), 1.1f, 50, 54, 50, 72);
                    c.Line(Color.FromArgb(40, 50, 66), 1.6f, 54, 58, 60, 58);
                    c.Rect(Color.FromArgb(253, 224, 71), Color.Empty, 0, 91, 58, 5, 4);
                    foreach (float wx in new[] { 14f, 64f })
                    {
                        c.Ellipse(Color.FromArgb(30, 32, 36), Color.FromArgb(15, 16, 18), 1.2f, wx, 62, 22, 22);
                        c.Ellipse(Color.FromArgb(180, 188, 200), Color.FromArgb(90, 96, 104), 1, wx + 6, 68, 10, 10);
                    }
                    break;
                case "wallport":
                case "lowport":
                    c.Shadow(50, 95, 92, 6);
                    c.Rect(WoodDark, WoodLine, 1.4f, 12, 88, 16, 7);
                    c.Rect(WoodDark, WoodLine, 1.4f, 72, 88, 16, 7);
                    c.Rect(Wood, WoodLine, 1.8f, 6, 4, 88, 86);
                    c.Line(WoodDark, 1f, 35, 6, 35, 88);
                    c.Line(WoodDark, 1f, 65, 6, 65, 88);
                    if (shape == "wallport")
                    {
                        c.Rect(Opening, WoodLine, 2.2f, 28, 24, 44, 28);
                        c.Rect(WoodDark, WoodLine, 1.4f, 25, 52, 50, 5);
                    }
                    else
                    {
                        c.Rect(Opening, WoodLine, 2.2f, 26, 62, 48, 18);
                        c.Rect(WoodDark, WoodLine, 1.4f, 23, 80, 54, 4);
                    }
                    break;
                case "table":
                    c.Shadow(50, 95, 92, 6);
                    c.Rect(WoodDark, WoodLine, 1.2f, 22, 42, 5, 46);
                    c.Rect(WoodDark, WoodLine, 1.2f, 73, 42, 5, 46);
                    c.Poly(Color.FromArgb(226, 186, 136), WoodLine, 1.6f, 12, 30, 88, 30, 96, 40, 4, 40);
                    c.Rect(Wood, WoodLine, 1.6f, 4, 40, 92, 7);
                    c.Rect(Wood, WoodLine, 1.4f, 10, 47, 6, 47);
                    c.Rect(Wood, WoodLine, 1.4f, 84, 47, 6, 47);
                    break;
                case "shootingbox":
                    var orange = Color.FromArgb(234, 88, 12);
                    c.Beam(orange, 6, 22, 52, 78, 52);
                    c.Beam(orange, 6, 22, 52, 5, 90);
                    c.Beam(orange, 6, 78, 52, 95, 90);
                    c.Beam(orange, 7, 5, 90, 95, 90);
                    break;
                case "gate":
                    var pipe = Color.FromArgb(160, 172, 188);
                    c.Shadow(50, 95, 96, 5);
                    c.Rect(Iron, IronDark, 1.2f, 4, 12, 7, 84);
                    c.Rect(Iron, IronDark, 1.2f, 89, 12, 7, 84);
                    c.Beam(pipe, 3, 14, 80, 86, 22);
                    foreach (float gy in new[] { 22f, 37f, 51f, 66f, 80f })
                        c.Beam(pipe, gy == 22f || gy == 80f ? 4.2f : 3f, 11, gy, 89, gy);
                    c.Beam(pipe, 4.2f, 14, 22, 14, 80);
                    c.Beam(pipe, 4.2f, 86, 22, 86, 80);
                    break;
                case "rock":
                    c.Shadow(50, 93, 92, 8);
                    var rock = c.PolyPath(6, 90, 12, 60, 28, 40, 52, 28, 74, 34, 90, 54, 96, 90);
                    using (var brush = new LinearGradientBrush(c.R(6, 28, 90, 62), Color.FromArgb(176, 180, 186), Color.FromArgb(108, 114, 122), 70f))
                        c.G.FillPath(brush, rock);
                    c.Poly(Color.FromArgb(70, 255, 255, 255), Color.Empty, 0, 28, 40, 52, 28, 74, 34, 66, 56, 40, 60);
                    c.Line(Color.FromArgb(92, 98, 106), 1.2f, 28, 40, 40, 60, 12, 60);
                    c.Line(Color.FromArgb(92, 98, 106), 1.2f, 40, 60, 66, 56, 74, 34);
                    c.Line(Color.FromArgb(92, 98, 106), 1.2f, 66, 56, 90, 54);
                    c.Line(Color.FromArgb(92, 98, 106), 1.2f, 40, 60, 46, 90);
                    c.Outline(rock, Color.FromArgb(70, 76, 84), 1.8f);
                    break;
                case "log":
                    c.Shadow(50, 84, 92, 8);
                    c.Ellipse(Color.FromArgb(110, 76, 48), Color.FromArgb(70, 46, 26), 1.6f, 4, 40, 14, 38);
                    c.Rect(Color.FromArgb(120, 84, 54), Color.Empty, 0, 11, 40, 69, 38);
                    c.Line(Color.FromArgb(70, 46, 26), 1.6f, 11, 40, 80, 40);
                    c.Line(Color.FromArgb(70, 46, 26), 1.6f, 11, 78, 80, 78);
                    foreach (float ly in new[] { 48f, 56f, 64f, 71f })
                        c.Line(Color.FromArgb(88, 60, 36), 1.1f, 14, ly, 74, ly + 1.5f);
                    c.Ellipse(Color.FromArgb(226, 186, 136), Color.FromArgb(70, 46, 26), 1.6f, 70, 40, 22, 38);
                    c.Ellipse(Color.Empty, WoodDark, 1, 75, 48, 12, 22);
                    c.Ellipse(Color.Empty, WoodDark, 1, 78.5f, 54, 5, 10);
                    break;
                case "pronemat":
                    var olive = Color.FromArgb(84, 96, 54);
                    c.Shadow(52, 86, 92, 10);
                    c.Poly(Color.FromArgb(52, 60, 32), Color.FromArgb(40, 46, 24), 1.2f, 24, 88, 96, 72, 96, 77, 24, 93);
                    c.Poly(olive, Color.FromArgb(40, 46, 24), 1.6f, 6, 62, 74, 52, 96, 72, 24, 88);
                    c.Dashed(Color.FromArgb(170, 180, 130), 0.9f, c.PolyPath(12, 63, 72, 55, 89, 71, 26, 84));
                    break;
                case "bag":
                    var tan = Color.FromArgb(184, 148, 94);
                    c.Shadow(50, 92, 84, 8);
                    using (var strap = c.Pen(Color.FromArgb(110, 86, 52), 4.5f))
                        c.G.DrawArc(strap, c.R(36, 24, 28, 30), 180, 180);
                    var bag = c.RoundRectPath(8, 38, 84, 50, 16);
                    using (var brush = new LinearGradientBrush(c.R(8, 38, 84, 50), Color.FromArgb(206, 172, 116), Color.FromArgb(150, 118, 72), 90f))
                        c.G.FillPath(brush, bag);
                    c.Outline(bag, Color.FromArgb(110, 86, 52), 1.8f);
                    c.Dashed(Color.FromArgb(110, 86, 52), 1f, c.RoundRectPath(13, 43, 74, 40, 12));
                    c.Line(Color.FromArgb(130, 102, 62), 1.2f, 36, 40, 36, 86);
                    c.Line(Color.FromArgb(130, 102, 62), 1.2f, 64, 40, 64, 86);
                    break;
                default:
                    c.Shadow(52, 92, 86, 7);
                    c.Poly(Color.FromArgb(226, 186, 136), WoodLine, 1.6f, 12, 34, 32, 18, 88, 18, 68, 34);
                    c.Poly(WoodDark, WoodLine, 1.6f, 68, 34, 88, 18, 88, 72, 68, 88);
                    c.Rect(Wood, WoodLine, 1.6f, 12, 34, 56, 54);
                    c.Line(WoodDark, 1.1f, 12, 52, 68, 52);
                    c.Line(WoodDark, 1.1f, 12, 70, 68, 70);
                    c.Line(WoodLine, 1.6f, 14, 36, 66, 86);
                    break;
            }
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        // A crossbar with two chains, for hanging steel.
        static void Hanger(Canvas c, float barLeft, float barRight, float chainLeft, float chainRight, float plateTop)
        {
            c.Line(IronDark, 2.6f, barLeft, 5, barRight, 5);
            c.Chain(chainLeft - 3, 5, chainLeft, plateTop + 2);
            c.Chain(chainRight + 3, 5, chainRight, plateTop + 2);
        }

        // IPSC-style silhouette: head, angled shoulders, clipped bottom corners.
        static GraphicsPath Silhouette(Canvas c, float top, float scaleY = 1f)
        {
            float[] pts = { 42, 8, 58, 8, 60, 10, 60, 24, 78, 32, 80, 34, 80, 80, 70, 94, 30, 94, 20, 80, 20, 34, 22, 32, 40, 24, 40, 10 };
            for (int i = 1; i < pts.Length; i += 2) pts[i] = top + (pts[i] - 8) * scaleY + 4;
            return c.PolyPath(pts);
        }

        static void Laths(Canvas c)
        {
            c.Beam(Wood, 3.4f, 34, 80, 34, 99);
            c.Beam(Wood, 3.4f, 66, 80, 66, 99);
        }

        static void Popper(Canvas c)
        {
            c.Shadow(50, 95, 56, 5);
            c.Rect(Iron, IronDark, 1.4f, 26, 86, 48, 8);
            c.Rect(IronDark, IronDark, 1, 43, 79, 14, 8);
            var p = new GraphicsPath();
            p.AddArc(c.R(30, 4, 40, 40), 180, 180);
            p.AddLine(c.P(70, 24), c.P(59, 80));
            p.AddLine(c.P(59, 80), c.P(41, 80));
            p.CloseFigure();
            c.Steel(p);
            c.Shine(37, 11, 14, 10);
        }

        static void Barrel(Canvas c, float x, float y, float w, float h)
        {
            var blue = Color.FromArgb(37, 99, 235);
            var dark = Color.FromArgb(23, 51, 130);
            float lid = h * 0.16f;
            var body = new GraphicsPath();
            body.AddLine(c.P(x, y + lid / 2), c.P(x, y + h - lid / 2));
            body.AddArc(c.R(x, y + h - lid, w, lid), 180, -180);
            body.AddLine(c.P(x + w, y + h - lid / 2), c.P(x + w, y + lid / 2));
            body.CloseFigure();
            using (var brush = new LinearGradientBrush(c.R(x, y, w, h), dark, dark, 0f))
            {
                var blend = new ColorBlend
                {
                    Colors = new[] { dark, blue, Color.FromArgb(96, 150, 250), blue, dark },
                    Positions = new[] { 0f, 0.3f, 0.45f, 0.7f, 1f }
                };
                brush.InterpolationColors = blend;
                c.G.FillPath(brush, body);
            }
            c.Outline(body, Color.FromArgb(15, 32, 80), 1.4f);
            using (var rib = c.Pen(Color.FromArgb(15, 32, 80), 1.2f))
            {
                c.G.DrawArc(rib, c.R(x, y + h * 0.33f, w, lid), 0, 180);
                c.G.DrawArc(rib, c.R(x, y + h * 0.6f, w, lid), 0, 180);
            }
            c.Ellipse(Color.FromArgb(59, 130, 246), Color.FromArgb(15, 32, 80), 1.4f, x, y, w, lid);
            c.Ellipse(Color.FromArgb(30, 64, 175), Color.Empty, 0, x + w * 0.62f, y + lid * 0.3f, w * 0.14f, lid * 0.35f);
        }

        // Scales the 100 x 100 drawing grid into a rectangle and holds the drawing helpers.
        class Canvas
        {
            public readonly Graphics G;
            readonly float x0, y0, s;

            public Canvas(Graphics g, RectangleF r)
            {
                G = g;
                s = Math.Min(r.Width, r.Height) / 100f;
                x0 = r.X + (r.Width - 100 * s) / 2;
                y0 = r.Y + (r.Height - 100 * s) / 2;
            }

            // A smaller grid inside this one (x, y, size in this grid's units).
            public Canvas Sub(float x, float y, float w, float h)
            {
                return new Canvas(G, R(x, y, w, h));
            }

            public float X(float v) { return x0 + v * s; }
            public float Y(float v) { return y0 + v * s; }
            public float L(float v) { return v * s; }
            public PointF P(float x, float y) { return new PointF(X(x), Y(y)); }
            public RectangleF R(float x, float y, float w, float h) { return new RectangleF(X(x), Y(y), L(w), L(h)); }

            public Pen Pen(Color color, float width)
            {
                var pen = new Pen(color, L(width));
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = pen.EndCap = LineCap.Round;
                return pen;
            }

            public GraphicsPath PolyPath(params float[] xy)
            {
                var pts = new PointF[xy.Length / 2];
                for (int i = 0; i < pts.Length; i++) pts[i] = P(xy[i * 2], xy[i * 2 + 1]);
                var path = new GraphicsPath();
                path.AddPolygon(pts);
                return path;
            }

            public GraphicsPath RectPath(float x, float y, float w, float h)
            {
                var path = new GraphicsPath();
                path.AddRectangle(R(x, y, w, h));
                return path;
            }

            public GraphicsPath EllipsePath(float x, float y, float w, float h)
            {
                var path = new GraphicsPath();
                path.AddEllipse(R(x, y, w, h));
                return path;
            }

            // A long, thin opening centered at (cx, cy), turned by angle degrees.
            public GraphicsPath Slot(float cx, float cy, float length, float width, float angle)
            {
                double a = angle * Math.PI / 180;
                float ux = (float)Math.Cos(a) * length / 2, uy = (float)Math.Sin(a) * length / 2;
                float vx = -(float)Math.Sin(a) * width / 2, vy = (float)Math.Cos(a) * width / 2;
                return PolyPath(cx - ux - vx, cy - uy - vy, cx + ux - vx, cy + uy - vy,
                                cx + ux + vx, cy + uy + vy, cx - ux + vx, cy - uy + vy);
            }

            public GraphicsPath RoundRectPath(float x, float y, float w, float h, float radius)
            {
                var r = R(x, y, w, h);
                float d = L(radius) * 2;
                var path = new GraphicsPath();
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }

            public void Fill(GraphicsPath path, Color fill, Color line, float width)
            {
                if (fill != Color.Empty)
                    using (var b = new SolidBrush(fill)) G.FillPath(b, path);
                if (line != Color.Empty && width > 0) Outline(path, line, width);
            }

            public void Outline(GraphicsPath path, Color line, float width)
            {
                using (var p = Pen(line, width)) G.DrawPath(p, path);
            }

            public void Dashed(Color line, float width, GraphicsPath path)
            {
                using (var p = Pen(line, width))
                {
                    p.DashStyle = DashStyle.Dash;
                    p.StartCap = p.EndCap = LineCap.Flat;
                    G.DrawPath(p, path);
                }
            }

            public void Poly(Color fill, Color line, float width, params float[] xy)
            {
                Fill(PolyPath(xy), fill, line, width);
            }

            public void Rect(Color fill, Color line, float width, float x, float y, float w, float h)
            {
                Fill(RectPath(x, y, w, h), fill, line, width);
            }

            public void Ellipse(Color fill, Color line, float width, float x, float y, float w, float h)
            {
                Fill(EllipsePath(x, y, w, h), fill, line, width);
            }

            public void Line(Color color, float width, params float[] xy)
            {
                using (var p = Pen(color, width))
                    for (int i = 0; i + 3 < xy.Length; i += 2)
                        G.DrawLine(p, P(xy[i], xy[i + 1]), P(xy[i + 2], xy[i + 3]));
            }

            // A board or bar: dark edge with the color inside.
            public void Beam(Color color, float width, float x1, float y1, float x2, float y2)
            {
                var edge = ControlPaint.Dark(color, 0.35f);
                using (var outer = Pen(edge, width + 1.6f))
                using (var inner = Pen(color, width))
                {
                    outer.StartCap = outer.EndCap = inner.StartCap = inner.EndCap = LineCap.Flat;
                    G.DrawLine(outer, P(x1, y1), P(x2, y2));
                    G.DrawLine(inner, P(x1, y1), P(x2, y2));
                }
            }

            public void Chain(float x1, float y1, float x2, float y2)
            {
                using (var p = Pen(Iron, 1.7f))
                {
                    p.DashPattern = new[] { 1.4f, 0.9f };
                    p.StartCap = p.EndCap = LineCap.Flat;
                    G.DrawLine(p, P(x1, y1), P(x2, y2));
                }
            }

            public void Steel(GraphicsPath path, float lineWidth = 1.9f)
            {
                var bounds = path.GetBounds();
                bounds.Inflate(1, 1);
                using (var brush = new LinearGradientBrush(bounds, SteelLight, SteelDark, 55f))
                    G.FillPath(brush, path);
                Outline(path, SteelLine, lineWidth);
            }

            // Several shapes drawn as one silhouette: outlines first, then fills on top.
            public void SteelUnion(float lineWidth, params GraphicsPath[] parts)
            {
                var bounds = parts[0].GetBounds();
                foreach (var p in parts) bounds = RectangleF.Union(bounds, p.GetBounds());
                using (var pen = Pen(SteelLine, lineWidth * 2))
                    foreach (var p in parts) G.DrawPath(pen, p);
                using (var brush = new LinearGradientBrush(bounds, SteelLight, SteelDark, 55f))
                    foreach (var p in parts) G.FillPath(brush, p);
            }

            public void FillUnion(Color fill, Color line, float lineWidth, params GraphicsPath[] parts)
            {
                using (var pen = Pen(line, lineWidth * 2))
                    foreach (var p in parts) G.DrawPath(pen, p);
                using (var brush = new SolidBrush(fill))
                    foreach (var p in parts) G.FillPath(brush, p);
            }

            public void Shine(float x, float y, float w, float h)
            {
                using (var b = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
                    G.FillEllipse(b, R(x, y, w, h));
            }

            public void Shadow(float cx, float cy, float w, float h)
            {
                using (var b = new SolidBrush(Color.FromArgb(38, 15, 23, 42)))
                    G.FillEllipse(b, R(cx - w / 2, cy - h / 2, w, h));
            }

            public void Text(string text, Color color, float size, float x, float y, float w, float h)
            {
                using (var font = new Font("Segoe UI", L(size), FontStyle.Bold, GraphicsUnit.World))
                using (var brush = new SolidBrush(color))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    G.DrawString(text, font, brush, R(x, y, w, h), format);
            }
        }
    }
}
