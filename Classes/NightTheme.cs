using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RustWorkshopUploader.Classes
{
    internal static class NightTheme
    {
        private static readonly Color Window = Color.FromArgb(24, 24, 24);
        private static readonly Color Menu = Color.FromArgb(30, 30, 30);
        private static readonly Color Input = Color.FromArgb(42, 42, 42);
        private static readonly Color Text = Color.FromArgb(228, 228, 228);
        private static readonly Color Muted = Color.FromArgb(139, 139, 139);
        private static readonly Color ButtonFace = Color.FromArgb(60, 60, 60);
        private static readonly Color ButtonBorder = Color.FromArgb(85, 85, 85);
        private static readonly Color Link = Color.FromArgb(110, 168, 255);
        private static readonly HashSet<RichTextBox> RichHooks = new HashSet<RichTextBox>();
        private static bool _stylingRich;

        public static bool Enabled { get; private set; }

        public static Color IdColor(bool linked)
        {
            if (Enabled)
                return linked ? Link : Text;
            return linked ? Color.Blue : Color.Black;
        }

        public static bool Load()
        {
            try
            {
                Enabled = File.Exists(SettingsPath) && File.ReadAllText(SettingsPath).Trim() == "1";
            }
            catch
            {
                Enabled = false;
            }

            return Enabled;
        }

        public static void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, enabled ? "1" : "0");
            }
            catch
            {
            }
        }

        public static void Apply(Form form)
        {
            Paint(form, Enabled);
        }

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RustWorkshopUploader",
                    "nightmode.txt");
            }
        }

        private static void Paint(Control control, bool dark)
        {
            if (control is CaptionBar caption)
            {
                caption.ApplyTheme(dark);
                return;
            }
            if (control is MenuStrip menu)
                PaintMenu(menu, dark);
            else if (control is Button button)
                PaintButton(button, dark);
            else if (control is ProgressBar bar)
                PaintProgress(bar, dark);
            else if (control is RichTextBox rich)
                StyleRich(rich, true);
            else if (control is TextBoxBase text)
                PaintText(text, dark);
            else if (control is NumericUpDown number)
                PaintNumber(number, dark);
            else if (control is Label label)
                PaintLabel(label, dark);
            else if (control is PictureBox picture)
                PaintPicture(picture, dark);
            else if (control is Form form)
            {
                form.BackColor = dark ? Window : SystemColors.Control;
                form.ForeColor = dark ? Text : SystemColors.ControlText;
            }

            foreach (Control child in control.Controls)
                Paint(child, dark);
        }

        private static void PaintMenu(MenuStrip menu, bool dark)
        {
            if (dark)
            {
                menu.BackColor = Window;
                menu.ForeColor = Text;
                menu.RenderMode = ToolStripRenderMode.Professional;
                menu.Renderer = new CursorDarkRenderer();
            }
            else
            {
                menu.RenderMode = ToolStripRenderMode.System;
                menu.BackColor = SystemColors.Control;
                menu.ForeColor = SystemColors.ControlText;
            }

            foreach (ToolStripItem item in menu.Items)
                PaintItem(item, dark);

            menu.Invalidate(true);
            menu.Refresh();
        }

        private static void PaintItem(ToolStripItem item, bool dark)
        {
            item.ForeColor = dark ? Text : SystemColors.ControlText;
            var menuItem = item as ToolStripMenuItem;
            if (menuItem == null)
                return;

            foreach (ToolStripItem child in menuItem.DropDownItems)
                PaintItem(child, dark);
        }

        private static void PaintButton(Button button, bool dark)
        {
            button.Padding = Padding.Empty;
            button.TextAlign = ContentAlignment.MiddleCenter;
            if (button is RoundedButton)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.FlatAppearance.BorderSize = 0;
                button.BackColor = dark ? ButtonFace : SystemColors.Control;
                button.ForeColor = dark ? Text : SystemColors.ControlText;
                button.FlatAppearance.BorderColor = dark ? ButtonBorder : SystemColors.ControlDark;
                button.FlatAppearance.MouseOverBackColor = dark ? Color.FromArgb(74, 74, 74) : Color.FromArgb(220, 220, 220);
                button.FlatAppearance.MouseDownBackColor = dark ? Color.FromArgb(48, 48, 48) : Color.FromArgb(200, 200, 200);
                button.Invalidate();
                return;
            }

            button.UseVisualStyleBackColor = !dark;
            if (dark)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = ButtonFace;
                button.ForeColor = Text;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = ButtonBorder;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(74, 74, 74);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(48, 48, 48);
            }
            else
            {
                button.FlatStyle = FlatStyle.Standard;
                button.BackColor = SystemColors.Control;
                button.ForeColor = SystemColors.ControlText;
            }
        }

        public static void RefreshInputs(TextBox nameBox, RichTextBox description)
        {
            if (nameBox != null)
                PaintText(nameBox, Enabled);
            if (description != null)
                StyleRich(description, true);
        }

        public static void StyleRich(RichTextBox box, bool recolorText)
        {
            if (box == null || _stylingRich)
                return;

            _stylingRich = true;
            try
            {
                var back = Enabled ? Input : SystemColors.Window;
                var fore = Enabled ? Text : SystemColors.WindowText;
                if (box.BorderStyle != BorderStyle.None)
                    box.BorderStyle = Enabled ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                if (!box.IsHandleCreated)
                    box.CreateControl();

                try
                {
                    box.BackColor = back;
                }
                catch (ArgumentException)
                {
                }

                SendMessage(box.Handle, 0x443, IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(back));
                box.ForeColor = fore;
                EnsureRichHook(box);

                if (!recolorText || box.TextLength == 0)
                    return;

                var start = box.SelectionStart;
                var length = box.SelectionLength;
                box.SelectAll();
                box.SelectionColor = fore;
                box.SelectionBackColor = back;
                box.Select(start, length);
            }
            finally
            {
                _stylingRich = false;
            }
        }

        private static void EnsureRichHook(RichTextBox box)
        {
            if (!RichHooks.Add(box))
                return;

            box.TextChanged += (sender, args) => StyleRich((RichTextBox)sender, false);
            box.HandleCreated += (sender, args) => StyleRich((RichTextBox)sender, false);
        }

        private static void PaintText(TextBoxBase text, bool dark)
        {
            var borderless = text.BorderStyle == BorderStyle.None;
            if (!borderless)
                text.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;

            text.BackColor = dark ? (borderless ? Window : Input) : SystemColors.Window;
            text.ForeColor = dark ? Text : SystemColors.WindowText;
        }

        private static void PaintNumber(NumericUpDown number, bool dark)
        {
            number.BackColor = dark ? Input : SystemColors.Window;
            number.ForeColor = dark ? Text : SystemColors.WindowText;
            foreach (Control child in number.Controls)
            {
                var text = child as TextBoxBase;
                if (text == null)
                    continue;
                text.BackColor = number.BackColor;
                text.ForeColor = number.ForeColor;
            }
        }

        private static void PaintLabel(Label label, bool dark)
        {
            label.BackColor = dark ? Window : SystemColors.Control;
            label.ForeColor = dark
                ? (label.Name == "label6" ? Muted : Text)
                : SystemColors.ControlText;
        }

        private static void PaintPicture(PictureBox picture, bool dark)
        {
            picture.BackColor = dark ? Window : SystemColors.Control;
            if (picture.Name == "pictureBox2")
                picture.BorderStyle = dark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
        }

        private static void PaintProgress(ProgressBar bar, bool dark)
        {
            try
            {
                SetWindowTheme(bar.Handle, dark ? "" : null, dark ? "" : null);
            }
            catch
            {
            }

            bar.BackColor = dark ? Input : SystemColors.Control;
            bar.ForeColor = dark ? Link : SystemColors.Highlight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        private sealed class CursorDarkRenderer : ToolStripProfessionalRenderer
        {
            public CursorDarkRenderer()
                : base(new CursorDarkColorTable())
            {
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var brush = new SolidBrush(Window))
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                var color = e.Item.Selected || e.Item.Pressed ? Color.FromArgb(47, 47, 47) : Window;
                using (var brush = new SolidBrush(color))
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Text;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                var bounds = e.ImageRectangle;
                bounds.Inflate(-4, -4);
                using (var pen = new Pen(Text, 2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawLines(pen, new[]
                    {
                        new Point(bounds.Left, bounds.Top + bounds.Height / 2),
                        new Point(bounds.Left + bounds.Width / 3, bounds.Bottom),
                        new Point(bounds.Right, bounds.Top)
                    });
                }
            }
        }

        private sealed class CursorDarkColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin { get { return Window; } }
            public override Color MenuStripGradientEnd { get { return Window; } }
            public override Color ToolStripGradientBegin { get { return Window; } }
            public override Color ToolStripGradientEnd { get { return Window; } }
            public override Color ToolStripDropDownBackground { get { return Menu; } }
            public override Color ImageMarginGradientBegin { get { return Menu; } }
            public override Color ImageMarginGradientMiddle { get { return Menu; } }
            public override Color ImageMarginGradientEnd { get { return Menu; } }
            public override Color MenuItemSelected { get { return Color.FromArgb(47, 47, 47); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(47, 47, 47); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(47, 47, 47); } }
            public override Color MenuItemPressedGradientBegin { get { return Menu; } }
            public override Color MenuItemPressedGradientMiddle { get { return Menu; } }
            public override Color MenuItemPressedGradientEnd { get { return Menu; } }
            public override Color MenuItemBorder { get { return Color.FromArgb(47, 47, 47); } }
            public override Color MenuBorder { get { return ButtonBorder; } }
            public override Color SeparatorDark { get { return ButtonBorder; } }
            public override Color SeparatorLight { get { return ButtonBorder; } }
            public override Color ToolStripBorder { get { return Window; } }
            public override Color CheckBackground { get { return Menu; } }
            public override Color CheckSelectedBackground { get { return Color.FromArgb(47, 47, 47); } }
            public override Color CheckPressedBackground { get { return Menu; } }
        }
    }
}
