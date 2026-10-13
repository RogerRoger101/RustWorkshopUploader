using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal sealed class LanguageBar : Control
    {
        private const int CodeWidth = 18;
        private const int Gap = 4;
        private const int PillWidth = 52;
        private const int PillHeight = 26;
        private const int PairGap = 6;
        private const int PairWidth = CodeWidth * 2 + Gap * 2 + PillWidth;
        private const float TrackInset = 2f;
        private const float KnobInset = 1.5f;
        private const float KnobSize = 19f;
        private const float MutedEdge = 7.25f;

        private static readonly int[][] Pairs = { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4, 5 } };

        private readonly float[] _knob = new float[Pairs.Length];
        private readonly Timer _timer = new Timer { Interval = 15 };
        private readonly ToolTip _tip = new ToolTip();
        private readonly Font _font;
        private int _selected;
        private bool _night;
        private int _hover = -1;

        public LanguageBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(PairWidth * Pairs.Length + PairGap * (Pairs.Length - 1), 30);
            TabStop = false;
            AccessibleName = "Language";

            var heavy = new Font("Segoe UI Black", 9f);
            if (heavy.Name == "Segoe UI Black")
            {
                _font = heavy;
            }
            else
            {
                heavy.Dispose();
                _font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }

            _timer.Tick += (sender, args) => Step();
            SnapKnobs();
        }

        public event EventHandler LanguageChanged;

        public int Selected
        {
            get { return _selected; }
            set
            {
                if (value < 0 || value >= Language.Codes.Length || value == _selected)
                    return;
                _selected = value;
                SnapKnobs();
                Invalidate();
            }
        }

        public bool Night
        {
            get { return _night; }
            set
            {
                _night = value;
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _tip.Dispose();
                _font.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var hit = HitTest(e.Location);
            Cursor = hit >= 0 ? Cursors.Hand : Cursors.Default;
            if (hit == _hover)
                return;
            _hover = hit;
            _tip.SetToolTip(this, hit >= 0 ? Language.Names[hit] : null);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            _tip.SetToolTip(this, null);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
                return;

            var hit = HitTest(e.Location);
            if (hit < 0 || hit == _selected)
                return;

            _selected = hit;
            _timer.Start();
            Invalidate();
            var handler = LanguageChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var back = BackColor.IsEmpty ? (Parent != null ? Parent.BackColor : SystemColors.Control) : BackColor;
            g.Clear(back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            for (var pair = 0; pair < Pairs.Length; pair++)
                DrawPair(g, pair, back);
        }

        private float Target(int pair)
        {
            if (_selected == Pairs[pair][0])
                return 0f;
            if (_selected == Pairs[pair][1])
                return 1f;
            return 0.5f;
        }

        private void SnapKnobs()
        {
            for (var pair = 0; pair < Pairs.Length; pair++)
                _knob[pair] = Target(pair);
        }

        private void Step()
        {
            const float speed = 1f / 12f;
            var moving = false;
            for (var pair = 0; pair < Pairs.Length; pair++)
            {
                var target = Target(pair);
                var delta = target - _knob[pair];
                if (Math.Abs(delta) <= speed)
                {
                    _knob[pair] = target;
                    continue;
                }

                _knob[pair] += Math.Sign(delta) * speed;
                moving = true;
            }

            if (!moving)
                _timer.Stop();
            Invalidate();
        }

        private int HitTest(Point point)
        {
            for (var pair = 0; pair < Pairs.Length; pair++)
            {
                if (Rectangle.Round(LeftCodeRect(pair)).Contains(point))
                    return Pairs[pair][0];
                if (Rectangle.Round(RightCodeRect(pair)).Contains(point))
                    return Pairs[pair][1];

                var pill = PillRect(pair);
                if (Rectangle.Round(pill).Contains(point))
                    return point.X < pill.Left + pill.Width / 2f ? Pairs[pair][0] : Pairs[pair][1];
            }

            return -1;
        }

        private static float PairLeft(int pair)
        {
            return pair * (PairWidth + PairGap);
        }

        private RectangleF LeftCodeRect(int pair)
        {
            return new RectangleF(PairLeft(pair), 0, CodeWidth, Height);
        }

        private RectangleF RightCodeRect(int pair)
        {
            return new RectangleF(PairLeft(pair) + CodeWidth + Gap * 2 + PillWidth, 0, CodeWidth, Height);
        }

        private RectangleF PillRect(int pair)
        {
            return new RectangleF(PairLeft(pair) + CodeWidth + Gap, (Height - PillHeight) / 2f, PillWidth, PillHeight);
        }

        private void DrawPair(Graphics g, int pair, Color back)
        {
            var left = Pairs[pair][0];
            var right = Pairs[pair][1];
            var active = _selected == left || _selected == right;
            var ring = PillRect(pair);
            DrawRing(g, ring, back);

            var track = RectangleF.Inflate(ring, -TrackInset, -TrackInset);
            using (var trackPath = Pill(track))
            {
                var state = g.Save();
                g.SetClip(trackPath, CombineMode.Intersect);

                if (active)
                {
                    var knobRight = _selected == right;
                    using (var brush = new SolidBrush(Color.FromArgb(6, 10, 32)))
                        g.FillPath(brush, trackPath);

                    var center = knobRight
                        ? track.Left + (track.Width - KnobInset - KnobSize) / 2f
                        : track.Left + (KnobInset + KnobSize + track.Width) / 2f;
                    Flags.Draw(g, Language.Labels[_selected], center, track, color => color);
                    DrawFade(g, track, knobRight);
                }
                else
                {
                    using (var brush = new SolidBrush(_night ? Color.FromArgb(26, 26, 26) : Color.FromArgb(230, 230, 236)))
                        g.FillPath(brush, trackPath);

                    var half = g.Save();
                    g.SetClip(new RectangleF(track.Left, track.Top, track.Width / 2f, track.Height), CombineMode.Intersect);
                    Flags.Draw(g, Language.Labels[left], track.Left + MutedEdge, track, Mute);
                    g.Restore(half);

                    half = g.Save();
                    g.SetClip(new RectangleF(track.Left + track.Width / 2f, track.Top, track.Width / 2f, track.Height), CombineMode.Intersect);
                    Flags.Draw(g, Language.Labels[right], track.Right - MutedEdge, track, Mute);
                    g.Restore(half);
                }

                DrawInnerShadow(g, track, active);
                g.Restore(state);
            }

            DrawKnob(g, track, _knob[pair]);

            var strong = _night ? Color.FromArgb(228, 228, 228) : Color.FromArgb(79, 81, 112);
            var faint = _night ? Color.FromArgb(92, 92, 102) : Color.FromArgb(196, 196, 207);
            DrawCode(g, LeftCodeRect(pair), Language.Labels[left], _selected == left ? strong : faint);
            DrawCode(g, RightCodeRect(pair), Language.Labels[right], _selected == right ? strong : faint);
        }

        private Color Mute(Color color)
        {
            var grey = _night ? Color.FromArgb(72, 72, 78) : Color.FromArgb(214, 214, 222);
            return Color.FromArgb(
                color.R + (grey.R - color.R) * 2 / 3,
                color.G + (grey.G - color.G) * 2 / 3,
                color.B + (grey.B - color.B) * 2 / 3);
        }

        private void DrawRing(Graphics g, RectangleF ring, Color back)
        {
            var shadow = _night ? Color.Black : Color.FromArgb(140, 140, 165);
            var shadowAlpha = _night ? new[] { 40, 60, 90 } : new[] { 22, 34, 52 };
            var lightAlpha = _night ? new[] { 4, 7, 10 } : new[] { 60, 110, 200 };

            for (var i = 0; i < 3; i++)
            {
                var spread = 1.8f - i * 0.6f;
                var shade = ring;
                shade.Offset(1.2f, 1.6f);
                shade.Inflate(spread, spread);
                using (var path = Pill(shade))
                using (var brush = new SolidBrush(Color.FromArgb(shadowAlpha[i], shadow)))
                    g.FillPath(brush, path);
            }

            for (var i = 0; i < 3; i++)
            {
                var spread = 1.8f - i * 0.6f;
                var shine = ring;
                shine.Offset(-1.2f, -1.2f);
                shine.Inflate(spread, spread);
                using (var path = Pill(shine))
                using (var brush = new SolidBrush(Color.FromArgb(lightAlpha[i], Color.White)))
                    g.FillPath(brush, path);
            }

            using (var path = Pill(ring))
            using (var brush = new SolidBrush(back))
                g.FillPath(brush, path);
        }

        private static void DrawFade(Graphics g, RectangleF track, bool knobRight)
        {
            var start = knobRight ? new PointF(track.Right + 1f, 0f) : new PointF(track.Left - 1f, 0f);
            var end = knobRight ? new PointF(track.Left - 1f, 0f) : new PointF(track.Right + 1f, 0f);
            var shade = Color.FromArgb(6, 10, 32);
            using (var brush = new LinearGradientBrush(start, end, Color.Black, Color.Black))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.36f, 0.6f, 1f },
                    Colors = new[]
                    {
                        Color.FromArgb(217, shade),
                        Color.FromArgb(140, shade),
                        Color.FromArgb(0, shade),
                        Color.FromArgb(0, shade)
                    }
                };
                g.FillRectangle(brush, track);
            }
        }

        private void DrawInnerShadow(Graphics g, RectangleF track, bool active)
        {
            var dark = active
                ? Color.FromArgb(140, 0, 0, 0)
                : _night ? Color.FromArgb(180, 0, 0, 0) : Color.FromArgb(150, 140, 140, 165);
            var light = active
                ? Color.FromArgb(80, 255, 255, 255)
                : _night ? Color.FromArgb(16, 255, 255, 255) : Color.FromArgb(230, 255, 255, 255);

            for (var i = 0; i < 2; i++)
            {
                var shifted = track;
                shifted.Offset(1.6f, 1.4f);
                using (var path = Pill(shifted))
                using (var pen = new Pen(Color.FromArgb(dark.A / (i + 1), dark), 3f + i * 2.5f))
                    g.DrawPath(pen, path);
            }

            var lifted = track;
            lifted.Offset(-1.2f, -1.2f);
            using (var path = Pill(lifted))
            using (var pen = new Pen(light, 2.2f))
                g.DrawPath(pen, path);
        }

        private void DrawKnob(Graphics g, RectangleF track, float position)
        {
            var x = track.Left + KnobInset + position * (track.Width - KnobInset * 2f - KnobSize);
            var y = track.Top + (track.Height - KnobSize) / 2f;
            var knob = new RectangleF(x, y, KnobSize, KnobSize);

            var shadowAlpha = _night ? new[] { 60, 110 } : new[] { 30, 55 };
            for (var i = 0; i < 2; i++)
            {
                var shade = knob;
                shade.Offset(0.8f, 1.6f);
                shade.Inflate(1.2f - i * 0.6f, 1.2f - i * 0.6f);
                using (var brush = new SolidBrush(Color.FromArgb(shadowAlpha[i], 20, 20, 40)))
                    g.FillEllipse(brush, shade);
            }

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(knob);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterPoint = new PointF(x + KnobSize * 0.35f, y + KnobSize * 0.3f);
                    brush.CenterColor = _night ? Color.FromArgb(242, 242, 244) : Color.White;
                    brush.SurroundColors = new[] { _night ? Color.FromArgb(169, 169, 178) : Color.FromArgb(220, 220, 227) };
                    g.FillPath(brush, path);
                }

                using (var pen = new Pen(Color.FromArgb(_night ? 90 : 45, 80, 80, 100), 0.7f))
                    g.DrawPath(pen, path);
            }
        }

        private void DrawCode(Graphics g, RectangleF bounds, string code, Color color)
        {
            TextRenderer.DrawText(g, code, _font, Rectangle.Round(bounds), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine | TextFormatFlags.NoClipping);
        }

        private static GraphicsPath Pill(RectangleF bounds)
        {
            var path = new GraphicsPath();
            var diameter = bounds.Height;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }

        private static class Flags
        {
            private static readonly Color UkBlue = Color.FromArgb(1, 33, 105);
            private static readonly Color UkRed = Color.FromArgb(200, 16, 46);
            private static readonly Color SpainRed = Color.FromArgb(170, 21, 27);
            private static readonly Color SpainYellow = Color.FromArgb(241, 191, 0);
            private static readonly Color RussiaBlue = Color.FromArgb(0, 57, 166);
            private static readonly Color RussiaRed = Color.FromArgb(213, 43, 30);
            private static readonly Color UkraineBlue = Color.FromArgb(0, 87, 183);
            private static readonly Color UkraineYellow = Color.FromArgb(255, 215, 0);
            private static readonly Color GermanyRed = Color.FromArgb(221, 0, 0);
            private static readonly Color GermanyGold = Color.FromArgb(255, 206, 0);
            private static readonly Color FranceBlue = Color.FromArgb(0, 85, 164);
            private static readonly Color FranceRed = Color.FromArgb(239, 65, 53);

            public static void Draw(Graphics g, string code, float centerX, RectangleF track, Func<Color, Color> tone)
            {
                var height = track.Height;
                var units = code == "EN" ? 30f : code == "DE" ? 36f : 40f;
                var width = height * 60f / units;

                var state = g.Save();
                g.TranslateTransform(centerX - width / 2f, track.Top);
                g.ScaleTransform(width / 60f, height / units);
                g.SetClip(new RectangleF(0f, 0f, 60f, units), CombineMode.Intersect);

                switch (code)
                {
                    case "EN":
                        DrawUnionJack(g, tone);
                        break;
                    case "ES":
                        Fill(g, tone(SpainRed), 0f, 0f, 60f, 40f);
                        Fill(g, tone(SpainYellow), 0f, 10f, 60f, 20f);
                        break;
                    case "RU":
                        Fill(g, tone(Color.White), 0f, 0f, 60f, 40f);
                        Fill(g, tone(RussiaBlue), 0f, 40f / 3f, 60f, 40f / 3f);
                        Fill(g, tone(RussiaRed), 0f, 80f / 3f, 60f, 40f / 3f);
                        break;
                    case "UA":
                        Fill(g, tone(UkraineBlue), 0f, 0f, 60f, 20f);
                        Fill(g, tone(UkraineYellow), 0f, 20f, 60f, 20f);
                        break;
                    case "DE":
                        Fill(g, tone(Color.Black), 0f, 0f, 60f, 12f);
                        Fill(g, tone(GermanyRed), 0f, 12f, 60f, 12f);
                        Fill(g, tone(GermanyGold), 0f, 24f, 60f, 12f);
                        break;
                    case "FR":
                        Fill(g, tone(FranceBlue), 0f, 0f, 20f, 40f);
                        Fill(g, tone(Color.White), 20f, 0f, 20f, 40f);
                        Fill(g, tone(FranceRed), 40f, 0f, 20f, 40f);
                        break;
                }

                g.Restore(state);
            }

            private static void DrawUnionJack(Graphics g, Func<Color, Color> tone)
            {
                Fill(g, tone(UkBlue), 0f, 0f, 60f, 30f);

                using (var white = new Pen(tone(Color.White), 6f))
                {
                    g.DrawLine(white, 0f, 0f, 60f, 30f);
                    g.DrawLine(white, 60f, 0f, 0f, 30f);
                }

                var state = g.Save();
                using (var counterchange = new GraphicsPath())
                {
                    counterchange.AddPolygon(new[] { new PointF(30f, 15f), new PointF(60f, 15f), new PointF(60f, 30f) });
                    counterchange.AddPolygon(new[] { new PointF(30f, 15f), new PointF(30f, 30f), new PointF(0f, 30f) });
                    counterchange.AddPolygon(new[] { new PointF(30f, 15f), new PointF(0f, 15f), new PointF(0f, 0f) });
                    counterchange.AddPolygon(new[] { new PointF(30f, 15f), new PointF(30f, 0f), new PointF(60f, 0f) });
                    g.SetClip(counterchange, CombineMode.Intersect);
                }

                using (var red = new Pen(tone(UkRed), 4f))
                {
                    g.DrawLine(red, 0f, 0f, 60f, 30f);
                    g.DrawLine(red, 60f, 0f, 0f, 30f);
                }
                g.Restore(state);

                Fill(g, tone(Color.White), 25f, 0f, 10f, 30f);
                Fill(g, tone(Color.White), 0f, 10f, 60f, 10f);
                Fill(g, tone(UkRed), 27f, 0f, 6f, 30f);
                Fill(g, tone(UkRed), 0f, 12f, 60f, 6f);
            }

            private static void Fill(Graphics g, Color color, float x, float y, float width, float height)
            {
                using (var brush = new SolidBrush(color))
                    g.FillRectangle(brush, x, y, width, height);
            }
        }
    }
}
