using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RustWorkshopUploader.Classes;
using RustWorkshopUploader.Localization;
using Steamworks;
using Steamworks.Ugc;

namespace RustWorkshopUploader
{
    public partial class FrmMain : Form
    {
        private CustomSkin _editing;
        private string _folderPath;

        public FrmMain()
        {
            InitializeComponent();

            txtWorkshopId.Maximum = ulong.MaxValue;
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CustomSkins");
            LoadImagesFromExeFolder();
            InstallCaption();

            var themeToggle = new ThemeToggle { Night = NightTheme.Load() };
            themeToggle.NightChanged += (s, args) =>
            {
                NightTheme.SetEnabled(themeToggle.Night);
                NightTheme.Apply(this);
                ApplyWorkshopIdStyle();
                FitBrowseButton();
                themeToggle.BackColor = menuStrip1.BackColor;
                themeToggle.Invalidate();
            };
            menuStrip1.Items.Add(new ToolStripControlHost(themeToggle)
            {
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                Size = new Size(88, 26),
                Margin = new Padding(0, 0, 6, 0),
                ToolTipText = "Switch between light and dark"
            });
            NightTheme.Apply(this);
            themeToggle.BackColor = menuStrip1.BackColor;
            ApplyWorkshopIdStyle();
            HoldInputs(false);
            FitBrowseButton();
        }

        private void InstallCaption()
        {
            var client = ClientSize;
            FormBorderStyle = FormBorderStyle.None;
            var caption = new CaptionBar(this);
            Controls.Add(caption);
            foreach (Control control in Controls)
            {
                if (control == caption || control.Dock != DockStyle.None)
                    continue;
                control.Top += caption.Height;
            }
            ClientSize = new Size(client.Width, client.Height + caption.Height);
        }

        private void FitBrowseButton()
        {
            var measured = TextRenderer.MeasureText(btnSelectFolder.Text, btnSelectFolder.Font);
            var height = measured.Height + 16;
            var width = measured.Width + height + 8;
            btnSelectFolder.Size = new Size(width, height);
            btnSelectFolder.Top = txtFolder.Top + (txtFolder.Height - height) / 2;

            var limit = ClientSize.Width - 8;
            if (btnSelectFolder.Right > limit)
                btnSelectFolder.Left -= btnSelectFolder.Right - limit;

            var folderRight = btnSelectFolder.Left - 6;
            if (txtFolder.Right > folderRight)
                txtFolder.Width = Math.Max(40, folderRight - txtFolder.Left);
        }

        private void LoadImagesFromExeFolder()
        {
            var images = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            TrySetImage(pictureBox1, Path.Combine(images, "logo.png"));
            TrySetImage(pictureBox2, Path.Combine(images, "picture-01-512.png"));

            var iconPath = Path.Combine(images, "favicon.ico");
            if (!File.Exists(iconPath))
                return;

            try
            {
                using (var stream = new MemoryStream(File.ReadAllBytes(iconPath)))
                using (var icon = new Icon(stream))
                    Icon = (Icon)icon.Clone();
            }
            catch
            {
            }
        }

        private static void TrySetImage(PictureBox box, string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                box.Image = FromFile(path);
            }
            catch
            {
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var help = new Form())
            {
                help.Text = "How to use";
                help.FormBorderStyle = FormBorderStyle.None;
                help.StartPosition = FormStartPosition.CenterParent;
                help.MinimizeBox = false;
                help.MaximizeBox = false;
                help.ShowInTaskbar = false;
                help.ClientSize = new Size(480, 412);

                var instructions = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    Dock = DockStyle.Fill,
                    ScrollBars = ScrollBars.None,
                    HideSelection = true,
                    TabStop = false,
                    Font = new Font("Segoe UI", 9f),
                    Text =
                        "Steam has to be running before you open this program. It searches every fixed drive for the Steam client." + Environment.NewLine + Environment.NewLine +
                        "1. Click Browse and choose a PNG image. The image must be 512 x 512." + Environment.NewLine + Environment.NewLine +
                        "2. Enter a workshop name and a workshop description." + Environment.NewLine + Environment.NewLine +
                        "3. Click Upload. When the upload finishes, you can open the Steam Workshop page." + Environment.NewLine + Environment.NewLine +
                        "4. To change an item you already uploaded, enter its Workshop ID. The button changes to Update. The ID is saved with the image, so choosing that image again fills it in." + Environment.NewLine + Environment.NewLine +
                        "5. Click N to clear the Workshop ID and upload the image as a new item." + Environment.NewLine + Environment.NewLine +
                        "6. Double-click the Workshop ID to open that item on Steam." + Environment.NewLine + Environment.NewLine +
                        "The banner at the top is Images\\logo.png in the same folder as this program. Replace that file, then start the program again. The window icon is Images\\favicon.ico." + Environment.NewLine + Environment.NewLine +
                        "The sun and moon button at the right of the menu bar switches the theme. The sun is light and the moon is dark."
                };

                var ok = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Dock = DockStyle.Bottom,
                    Height = 32
                };

                help.Controls.Add(instructions);
                help.Controls.Add(ok);
                help.Controls.Add(new CaptionBar(help, false, false));
                help.AcceptButton = ok;
                help.CancelButton = ok;
                help.ActiveControl = ok;
                NightTheme.Apply(help);
                help.Shown += (s, args) =>
                {
                    instructions.SelectionStart = 0;
                    instructions.SelectionLength = 0;
                    ok.Focus();
                };
                help.ShowDialog(this);
            }
        }

        private string ManifestPath => _folderPath + Path.DirectorySeparatorChar + "manifest.txt";

        private void SetStatus(bool editing)
        {
            txtWorkshopId.Enabled = editing;
            btnDo.Enabled = editing;
            button1.Enabled = editing;
            HoldInputs(editing);
        }

        private void HoldInputs(bool editing)
        {
            txtWorkshopName.Enabled = true;
            txtWorkshopName.ReadOnly = !editing;
            txtWorkshopDesc.Enabled = true;
            txtWorkshopDesc.ReadOnly = !editing;
            NightTheme.RefreshInputs(txtWorkshopName, txtWorkshopDesc);
        }

        private static Image FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            return img;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog {Filter = @"Image Files(*.PNG)|*.PNG"};

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            Image image;
            try
            {
                image = FromFile(openDialog.FileName);
            }
            catch
            {
                MessageBox.Show(strings.Select_UnableToOpen, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetStatus(false);
                return;
            }

            if (image.Width != 512 && image.Height != 512)
            {
                MessageBox.Show(strings.Select_WrongResolution, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetStatus(false);
                return;
            }

            pictureBox2.Image = image;
            pictureBox2.Invalidate();

            _folderPath = string.Format("{1}{0}CustomSkins{0}Skin_{2}", Path.DirectorySeparatorChar,
                Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(openDialog.FileName));

            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
            if (File.Exists(_folderPath + Path.DirectorySeparatorChar + "icon.png"))
                File.Delete(_folderPath + Path.DirectorySeparatorChar + "icon.png");
            File.Copy(openDialog.FileName, _folderPath + Path.DirectorySeparatorChar + "icon.png");

            var dataPath = GetDataPath(_folderPath);
            _editing = File.Exists(dataPath) ? CustomSkin.FromFile(dataPath) : new CustomSkin();
            _editing.FilePath = dataPath;
            SetStatus(true);
            UpdateTexts();
        }


        private string GetDataPath(string path)
        {
            var folderName = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            var projectName = Path.GetFileName(folderName);
            return Path.GetFullPath(Path.Combine(path, $"..\\{projectName}.data"));
        }

        private void btnDo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_folderPath))
            {
                MessageBox.Show(strings.Do_NoImage, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtWorkshopName.Text))
            {
                MessageBox.Show(strings.Do_NoName, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtWorkshopDesc.Text))
            {
                MessageBox.Show(strings.Do_NoDescription, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(ManifestPath))
                File.WriteAllText(ManifestPath, _editing.ManifestText);

            PublishToSteam();
        }

        private async void PublishToSteam()
        {
            ProgressBar.Value = 0;
            SetStatus(false);

            var editor = _editing.ItemId == 0UL ? Editor.NewMicrotransactionFile : new Editor(_editing.ItemId);

            ProgressBar.Value = 20;

            editor = editor.ForAppId(Program.RustAppId).WithContent(_folderPath)
                .WithPreviewFile(_folderPath + Path.DirectorySeparatorChar + "icon.png")
                .WithTitle(_editing.Title).WithTag("Version3").WithTag("Skin")
                .WithPublicVisibility().WithDescription(_editing.Description);

            ProgressBar.Value = 40;
            var publishResult = await editor.SubmitAsync();
            ProgressBar.Value = 60;

            if (!publishResult.Success)
            {
                ProgressBar.Value = 0;
                MessageBox.Show(string.Format(strings.ErrorText, publishResult.Result), strings.Message_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus(true);
                return;
            }

            ProgressBar.Value = 85;

            var item = await SteamUGC.QueryFileAsync(publishResult.FileId);
            if (item == null)
            {
                ProgressBar.Value = 0;
                MessageBox.Show(strings.Publish_RetreiveFailed, strings.Message_Error, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetStatus(true);
                return;
            }

            ProgressBar.Value = 100;
            _editing.Title = item.Value.Title;
            _editing.Description = item.Value.Description;
            _editing.ItemId = item.Value.Id;
            _editing.Save();
            UpdateTexts();
            var result = MessageBox.Show(strings.Publish_OpenBrowser, strings.Message_Success, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (result == DialogResult.Yes)
                Process.Start("http://steamcommunity.com/sharedfiles/filedetails/?id=" + _editing.ItemIdString);
            SetStatus(true);
        }

        private void txtWorkshopId_DoubleClick(object sender, EventArgs e)
        {
            if (txtWorkshopId.Value == 0) return;
            Process.Start("http://steamcommunity.com/sharedfiles/filedetails/?id=" + txtWorkshopId.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtWorkshopId.Value = 0ul;
            btnDo.Text = strings.BtnDo_Upload;
        }

        private void btnDo_EnabledChanged(object sender, EventArgs e)
        {
            if (btnDo.Enabled && _editing != null)
            {
                if (_editing.ItemId > 0)
                    btnDo.Text = strings.BtnDo_Update;
                else
                    btnDo.Text = strings.BtnDo_Upload;
                return;
            }

            btnDo.Text = strings.BtnDo_Uploading;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #region Field updates

        private bool _updating;

        private void UpdateTexts()
        {
            _updating = true;
            txtWorkshopId.Value = _editing.ItemId;
            txtWorkshopName.Text = _editing.Title;
            txtWorkshopDesc.Text = _editing.Description;
            NightTheme.StyleRich(txtWorkshopDesc, true);
            txtFolder.Text = _folderPath;

            if (_editing.ItemId > 0)
                btnDo.Text = strings.BtnDo_Update;
            else
                btnDo.Text = strings.BtnDo_Upload;
            _updating = false;
        }

        private void txtWorkshopId_ValueChanged(object sender, EventArgs e)
        {
            ApplyWorkshopIdStyle();

            if (!_updating)
                _editing.ItemId = decimal.ToUInt64(txtWorkshopId.Value);
        }

        private void ApplyWorkshopIdStyle()
        {
            var linked = txtWorkshopId.Value != 0;
            var color = NightTheme.IdColor(linked);
            txtWorkshopId.ForeColor = color;
            txtWorkshopId.Font = new Font("Microsoft Sans Serif", 8, linked ? FontStyle.Underline : FontStyle.Regular);
            foreach (Control child in txtWorkshopId.Controls)
            {
                var text = child as TextBoxBase;
                if (text != null)
                    text.ForeColor = color;
            }
        }

        private void txtWorkshopDesc_TextChanged(object sender, EventArgs e)
        {
            if (!_updating && _editing != null)
                _editing.Description = txtWorkshopDesc.Text;
        }

        private void txtWorkshopName_TextChanged(object sender, EventArgs e)
        {
            if (!_updating && _editing != null)
                _editing.Title = txtWorkshopName.Text;
        }

        #endregion
    }
}