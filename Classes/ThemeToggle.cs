using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal sealed class ThemeToggle : Control
    {
        private const int PillWidth = 52;
        private const int PillHeight = 26;
        private const float TrackInset = 2f;
        private const float KnobInset = 1.5f;
        private const float KnobSize = 19f;

        private readonly Timer _timer = new Timer { Interval = 15 };
        private float _knob;
        private bool _night;

        public ThemeToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(PillWidth, 30);
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleName = "Theme";
            _timer.Tick += (sender, args) => Step();
            SnapKnob();
        }

        public bool Night
        {
            get { return _night; }
            set
            {
                if (_night == value)
                    return;
                _night = value;
                SnapKnob();
                Invalidate();
            }
        }

        public event EventHandler NightChanged;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !PillRect().Contains(e.Location))
                return;

            _night = !_night;
            _timer.Start();
            Invalidate();
            var handler = NightChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var back = Parent != null ? Parent.BackColor : SystemColors.Control;
            e.Graphics.Clear(back);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var ring = PillRect();
            DrawRing(g, ring, back);

            var track = RectangleF.Inflate(ring, -TrackInset, -TrackInset);
            using (var trackPath = Pill(track))
            {
                var state = g.Save();
                g.SetClip(trackPath, CombineMode.Intersect);

                using (var brush = new SolidBrush(Color.FromArgb(6, 10, 32)))
                    g.FillPath(brush, trackPath);

                var center = _night
                    ? track.Left + (track.Width - KnobInset - KnobSize) / 2f
                    : track.Left + (KnobInset + KnobSize + track.Width) / 2f;
                DrawThemeIcon(g, center, track, _night, false);
                DrawFade(g, track, _night);
                DrawInnerShadow(g, track);
                g.Restore(state);
            }

            DrawKnob(g, track, _knob);
        }

        private void SnapKnob()
        {
            _knob = _night ? 1f : 0f;
            _timer.Stop();
        }

        private void Step()
        {
            const float speed = 1f / 12f;
            var target = _night ? 1f : 0f;
            var delta = target - _knob;
            if (Math.Abs(delta) <= speed)
            {
                _knob = target;
                _timer.Stop();
            }
            else
            {
                _knob += Math.Sign(delta) * speed;
            }
            Invalidate();
        }

        private RectangleF PillRect()
        {
            return new RectangleF(0, (Height - PillHeight) / 2f, PillWidth, PillHeight);
        }

        private void DrawRing(Graphics g, RectangleF ring, Color back)
        {
            var night = Parent != null && Parent.BackColor.GetBrightness() < 0.4f;
            var shadow = night ? Color.Black : Color.FromArgb(140, 140, 165);
            var shadowAlpha = night ? new[] { 40, 60, 90 } : new[] { 22, 34, 52 };
            var lightAlpha = night ? new[] { 4, 7, 10 } : new[] { 60, 110, 200 };

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

        private void DrawInnerShadow(Graphics g, RectangleF track)
        {
            var dark = Color.FromArgb(140, 0, 0, 0);
            var light = Color.FromArgb(80, 255, 255, 255);

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
            var night = Parent != null && Parent.BackColor.GetBrightness() < 0.4f;
            var x = track.Left + KnobInset + position * (track.Width - KnobInset * 2f - KnobSize);
            var y = track.Top + (track.Height - KnobSize) / 2f;
            var knob = new RectangleF(x, y, KnobSize, KnobSize);

            var shadowAlpha = night ? new[] { 60, 110 } : new[] { 30, 55 };
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
                    brush.CenterColor = night ? Color.FromArgb(242, 242, 244) : Color.White;
                    brush.SurroundColors = new[] { night ? Color.FromArgb(169, 169, 178) : Color.FromArgb(220, 220, 227) };
                    g.FillPath(brush, path);
                }

                using (var pen = new Pen(Color.FromArgb(night ? 90 : 45, 80, 80, 100), 0.7f))
                    g.DrawPath(pen, path);
            }
        }

        private static void DrawThemeIcon(Graphics g, float centerX, RectangleF track, bool night, bool muted)
        {
            var area = new RectangleF(centerX - 8f, track.Top + (track.Height - 16f) / 2f, 16f, 16f);
            var color = muted
                ? Color.FromArgb(140, 180, 180, 190)
                : night ? Color.FromArgb(246, 246, 246) : Color.FromArgb(255, 214, 90);
            if (night)
                DrawMoon(g, area, color);
            else
                DrawSun(g, area, color);
        }

        private static void DrawSun(Graphics g, RectangleF area, Color color)
        {
            var cx = area.Left + area.Width / 2f;
            var cy = area.Top + area.Height / 2f;
            var radius = Math.Min(area.Width, area.Height) * 0.18f;
            using (var pen = new Pen(color, 1.35f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2f, radius * 2f);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4d;
                    var inner = radius + 1.4f;
                    var outer = radius + 3.2f;
                    g.DrawLine(pen,
                        cx + (float)Math.Cos(angle) * inner,
                        cy + (float)Math.Sin(angle) * inner,
                        cx + (float)Math.Cos(angle) * outer,
                        cy + (float)Math.Sin(angle) * outer);
                }
            }
        }

        private static void DrawMoon(Graphics g, RectangleF area, Color color)
        {
            var cx = area.Left + area.Width / 2f - 0.8f;
            var cy = area.Top + area.Height / 2f;
            var size = Math.Min(area.Width, area.Height) * 0.72f;
            var moon = new RectangleF(cx - size / 2f, cy - size / 2f, size, size);
            using (var body = new GraphicsPath())
            using (var bite = new GraphicsPath())
            {
                body.AddEllipse(moon);
                bite.AddEllipse(moon.X + size * 0.35f, moon.Y - size * 0.12f, size * 0.92f, size * 0.92f);
                using (var region = new Region(body))
                using (var brush = new SolidBrush(color))
                {
                    region.Exclude(bite);
                    g.FillRegion(brush, region);
                }
            }
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
    }
}
