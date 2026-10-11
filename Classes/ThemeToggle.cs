using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal sealed class ThemeToggle : Control
    {
        private bool _night;

        public ThemeToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(84, 24);
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleName = "Theme";
        }

        public bool Night
        {
            get { return _night; }
            set
            {
                if (_night == value)
                    return;
                _night = value;
                Invalidate();
            }
        }

        public event EventHandler NightChanged;

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null)
                BackColor = Parent.BackColor;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            var color = Parent != null ? Parent.BackColor : SystemColors.Control;
            using (var brush = new SolidBrush(color))
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location))
                return;

            var wantNight = e.X >= Width / 2;
            if (wantNight == _night)
                return;

            _night = wantNight;
            Invalidate();
            var handler = NightChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            OnPaintBackground(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var pill = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var shape = Pill(pill))
            {
                var mid = pill.Left + pill.Width / 2;
                g.SetClip(shape);
                using (var day = new SolidBrush(_night ? Color.FromArgb(58, 58, 58) : Color.FromArgb(246, 246, 246)))
                using (var night = new SolidBrush(_night ? Color.FromArgb(18, 18, 18) : Color.FromArgb(42, 42, 42)))
                {
                    g.FillRectangle(day, pill.Left, pill.Top, mid - pill.Left, pill.Height);
                    g.FillRectangle(night, mid, pill.Top, pill.Right - mid, pill.Height);
                }

                g.ResetClip();
                var outline = Parent != null && Parent.BackColor.GetBrightness() < 0.4f
                    ? Color.FromArgb(92, 92, 92)
                    : Color.FromArgb(196, 196, 196);
                using (var pen = new Pen(outline, 1.4f))
                    g.DrawPath(pen, shape);
            }

            var sunColor = _night ? Color.FromArgb(150, 150, 150) : Color.FromArgb(32, 32, 32);
            var moonColor = _night ? Color.FromArgb(246, 246, 246) : Color.FromArgb(170, 170, 170);
            DrawSun(g, new Rectangle(pill.Left, pill.Top, pill.Width / 2, pill.Height), sunColor);
            DrawMoon(g, new Rectangle(pill.Left + pill.Width / 2, pill.Top, pill.Width / 2, pill.Height), moonColor);
        }

        private static GraphicsPath Pill(Rectangle bounds)
        {
            var path = new GraphicsPath();
            var diameter = bounds.Height;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }

        private static void DrawSun(Graphics g, Rectangle area, Color color)
        {
            var cx = area.Left + area.Width / 2f;
            var cy = area.Top + area.Height / 2f;
            const float radius = 3.2f;
            using (var pen = new Pen(color, 1.35f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2f, radius * 2f);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4d;
                    var inner = radius + 1.6f;
                    var outer = radius + 3.5f;
                    g.DrawLine(pen,
                        cx + (float)Math.Cos(angle) * inner,
                        cy + (float)Math.Sin(angle) * inner,
                        cx + (float)Math.Cos(angle) * outer,
                        cy + (float)Math.Sin(angle) * outer);
                }
            }
        }

        private static void DrawMoon(Graphics g, Rectangle area, Color color)
        {
            var cx = area.Left + area.Width / 2f - 1f;
            var cy = area.Top + area.Height / 2f;
            var moon = new RectangleF(cx - 6f, cy - 6f, 12f, 12f);
            using (var body = new GraphicsPath())
            using (var bite = new GraphicsPath())
            {
                body.AddEllipse(moon);
                bite.AddEllipse(moon.X + 4.2f, moon.Y - 1.4f, 11f, 11f);
                using (var region = new Region(body))
                using (var brush = new SolidBrush(color))
                {
                    region.Exclude(bite);
                    g.FillRegion(brush, region);
                }
            }
        }
    }
}
