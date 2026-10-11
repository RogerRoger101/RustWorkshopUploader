using System.Drawing;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal sealed class CaptionBar : Panel
    {
        private readonly Form _form;
        private readonly PictureBox _icon;
        private readonly Label _title;
        private readonly Button _minimize;
        private readonly Button _close;
        private Point _dragStart;
        private bool _dragging;

        public CaptionBar(Form form, bool showMinimize = true, bool showIcon = true)
        {
            _form = form;
            Height = 32;
            Dock = DockStyle.Top;

            if (showIcon)
            {
                _icon = new PictureBox
                {
                    Dock = DockStyle.Left,
                    Width = 28,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = form.Icon == null ? null : form.Icon.ToBitmap()
                };
            }

            _title = new Label
            {
                Dock = DockStyle.Fill,
                Text = form.Text,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Padding = new Padding(showIcon ? 2 : 10, 0, 0, 0)
            };
            if (showMinimize)
            {
                _minimize = MakeButton("\u2014");
                _minimize.Click += (sender, args) => _form.WindowState = FormWindowState.Minimized;
            }

            _close = MakeButton("\u2715");
            _close.Click += (sender, args) => _form.Close();

            Controls.Add(_title);
            if (_icon != null)
                Controls.Add(_icon);
            if (_minimize != null)
                Controls.Add(_minimize);
            Controls.Add(_close);

            WireDrag(this);
            if (_icon != null)
                WireDrag(_icon);
            WireDrag(_title);
        }

        public void ApplyTheme(bool dark)
        {
            var back = dark ? Color.FromArgb(24, 24, 24) : SystemColors.Control;
            var text = dark ? Color.FromArgb(228, 228, 228) : SystemColors.ControlText;
            var hover = dark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(220, 220, 220);
            var closeHover = Color.FromArgb(196, 43, 28);
            BackColor = back;
            _title.BackColor = back;
            _title.ForeColor = text;
            if (_icon != null)
                _icon.BackColor = back;
            if (_minimize != null)
                StyleButton(_minimize, back, text, hover);
            StyleButton(_close, back, text, closeHover);
        }

        private static Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Right,
                Width = 46,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
        }

        private static void StyleButton(Button button, Color back, Color text, Color hover)
        {
            button.BackColor = back;
            button.ForeColor = text;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor = hover;
        }

        private void WireDrag(Control control)
        {
            control.MouseDown += (sender, args) =>
            {
                if (args.Button != MouseButtons.Left)
                    return;
                _dragging = true;
                _dragStart = args.Location;
            };
            control.MouseMove += (sender, args) =>
            {
                if (!_dragging || args.Button != MouseButtons.Left)
                    return;
                var screen = ((Control)sender).PointToScreen(args.Location);
                var origin = ((Control)sender).PointToScreen(_dragStart);
                _form.Location = new Point(_form.Left + screen.X - origin.X, _form.Top + screen.Y - origin.Y);
            };
            control.MouseUp += (sender, args) => _dragging = false;
        }
    }
}
