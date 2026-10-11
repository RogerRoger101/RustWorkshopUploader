using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal sealed class RoundedButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public RoundedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            TextAlign = ContentAlignment.MiddleCenter;
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var around = Parent != null ? Parent.BackColor : SystemColors.Control;
            using (var clear = new SolidBrush(around))
                g.FillRectangle(clear, ClientRectangle);

            var fill = BackColor;
            if (!Enabled)
                fill = SystemColors.Control;
            else if (_pressed)
                fill = FlatAppearance.MouseDownBackColor;
            else if (_hover)
                fill = FlatAppearance.MouseOverBackColor;

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var path = Pill(rect))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(FlatAppearance.BorderColor.IsEmpty ? SystemColors.ControlDark : FlatAppearance.BorderColor, 1.2f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            var text = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
    }
}
