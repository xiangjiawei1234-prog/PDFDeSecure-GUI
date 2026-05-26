using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFDeSecure
{
    public partial class PDFDeSecure : Form
    {
        private PdfDocument pdf;
        private PdfDocument outpdf;
        private string sourceFilePath;

        private Panel dropPanel;
        private Label dropLabel;
        private Label fileLabel;
        private ProgressBar progressBar;
        private Button unlockBtn;
        private Label statusLabel;

        public PDFDeSecure()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "PDFDeSecure — PDF Unlocker";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.FromArgb(22, 24, 29);
            this.ForeColor = Color.White;
            this.AllowDrop = true;
            string uiFontName = GetUiFontName();
            this.Font = new Font(uiFontName, 9F, FontStyle.Regular);
            this.AutoScaleMode = AutoScaleMode.None;

            // Manual DPI scaling: pt-based fonts render larger on high-DPI screens,
            // so we scale all pixel measurements (heights, paddings, margins) to match.
            float scale = this.DeviceDpi / 96f;
            int S(int v) => (int)(v * scale);

            var wa = Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(S(860), wa.Width * 9 / 10);
            int h = Math.Min(S(620), wa.Height * 9 / 10);
            this.ClientSize = new Size(w, h);
            this.MinimumSize = new Size(S(620), S(520));
            this.Padding = new Padding(S(28));

            // Use a table layout for clean arrangement
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // header (auto to fit text)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // drop zone
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // file info
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // progress + status
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // button

            this.Controls.Add(layout);

            // Row 0 — header
            var title = new Label
            {
                Text = "PDFDeSecure",
                Font = new Font(uiFontName, 24F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, S(2))
            };

            var subtitle = new Label
            {
                Text = "拖入 PDF，移除打印、复制、编辑限制",
                Font = new Font(uiFontName, 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(163, 172, 187),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, S(6))
            };

            var headerPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 0, S(10)),
                WrapContents = false
            };
            headerPanel.MinimumSize = new Size(0, S(86));
            headerPanel.Controls.Add(title);
            headerPanel.Controls.Add(subtitle);

            // Row 1 — drop zone
            dropPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 37, 46),
                BorderStyle = BorderStyle.None,
                AllowDrop = true,
                Margin = new Padding(0, S(4), 0, S(14)),
                Padding = new Padding(S(18))
            };
            dropPanel.MinimumSize = new Size(0, S(250));
            dropPanel.Paint += (s, e) =>
            {
                var rc = dropPanel.ClientRectangle;
                e.Graphics.Clear(dropPanel.BackColor);
                using (var pen = new Pen(Color.FromArgb(88, 102, 124), Math.Max(1, S(1))))
                {
                    pen.DashPattern = new float[] { 8, 5 };
                    rc.Inflate(-1, -1);
                    e.Graphics.DrawRectangle(pen, rc);
                }
            };

            dropLabel = new Label
            {
                Text = "拖拽 PDF 文件到此处\r\nDrop your PDF file here\r\n输出文件会自动保存为 *_unlocked.pdf",
                Font = new Font(uiFontName, 14F, FontStyle.Regular),
                ForeColor = Color.FromArgb(215, 221, 231),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            dropPanel.Controls.Add(dropLabel);

            dropPanel.DragEnter += OnDragEnter;
            dropPanel.DragDrop += OnDragDrop;
            dropPanel.DragLeave += OnDragLeave;
            this.DragEnter += OnDragEnter;
            this.DragDrop += OnDragDrop;
            this.DragLeave += OnDragLeave;

            // Row 2 — file info
            fileLabel = new Label
            {
                Text = "No file selected",
                Font = new Font(uiFontName, 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(181, 190, 205),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0)
            };

            var filePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(29, 33, 41),
                Height = S(46),
                Margin = new Padding(0, 0, 0, S(10)),
                Padding = new Padding(S(14), 0, S(14), 0)
            };
            filePanel.Controls.Add(fileLabel);

            // Row 3 — progress + status
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                Dock = DockStyle.Top,
                Height = S(16),
                Margin = new Padding(0)
            };

            statusLabel = new Label
            {
                Text = "",
                Font = new Font(uiFontName, 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 210, 138),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0)
            };

            var infoPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Height = S(42),
                Margin = new Padding(0, 0, 0, S(14)),
                Padding = new Padding(0)
            };
            infoPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            infoPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            infoPanel.Controls.Add(progressBar, 0, 0);
            infoPanel.Controls.Add(statusLabel, 0, 1);

            // Row 4 — button
            unlockBtn = new Button
            {
                Text = "解锁 PDF  /  Unlock PDF",
                Font = new Font(uiFontName, 13F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Height = S(64),
                MinimumSize = new Size(0, S(64)),
                Margin = new Padding(0)
            };
            unlockBtn.FlatAppearance.BorderSize = 0;
            unlockBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68);
            unlockBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(185, 28, 28);
            unlockBtn.Click += UnlockBtn_Click;

            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(dropPanel, 0, 1);
            layout.Controls.Add(filePanel, 0, 2);
            layout.Controls.Add(infoPanel, 0, 3);
            layout.Controls.Add(unlockBtn, 0, 4);
        }

        private static string GetUiFontName()
        {
            const string preferredFont = "Microsoft YaHei UI";

            using (var fonts = new System.Drawing.Text.InstalledFontCollection())
            {
                foreach (var family in fonts.Families)
                {
                    if (string.Equals(family.Name, preferredFont, StringComparison.OrdinalIgnoreCase))
                    {
                        return preferredFont;
                    }
                }
            }

            return "Segoe UI";
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                dropPanel.BackColor = Color.FromArgb(43, 54, 69);
                dropPanel.Invalidate();
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, EventArgs e)
        {
            dropPanel.BackColor = Color.FromArgb(32, 37, 46);
            dropPanel.Invalidate();
        }

        private async void OnDragDrop(object sender, DragEventArgs e)
        {
            dropPanel.BackColor = Color.FromArgb(32, 37, 46);
            dropPanel.Invalidate();

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;

            string filePath = files[0];
            if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                statusLabel.Text = "Please drop a PDF file";
                statusLabel.ForeColor = Color.FromArgb(255, 100, 100);
                return;
            }

            sourceFilePath = filePath;
            fileLabel.Text = $"已选择 / Selected: {Path.GetFileName(filePath)}";
            unlockBtn.Enabled = false;
            unlockBtn.Text = "Processing...";
            progressBar.Visible = true;
            progressBar.Value = 0;
            statusLabel.Text = "";

            try
            {
                var progress = new Progress<int>(v => progressBar.Value = v);
                await Task.Run(() =>
                {
                    outpdf = new PdfDocument();
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        pdf = PdfReader.Open(fileStream, PdfDocumentOpenMode.Import);
                        int current = 0;
                        int total = pdf.PageCount;
                        foreach (PdfPage page in pdf.Pages)
                        {
                            outpdf.AddPage(page);
                            current++;
                            ((IProgress<int>)progress).Report(current * 100 / total);
                        }
                    }
                });

                unlockBtn.Enabled = true;
                unlockBtn.Text = "解锁 PDF  /  Unlock PDF";
                statusLabel.Text = $"Ready — {outpdf.PageCount} pages loaded";
                statusLabel.ForeColor = Color.FromArgb(100, 200, 100);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                statusLabel.ForeColor = Color.FromArgb(255, 100, 100);
                unlockBtn.Enabled = false;
                unlockBtn.Text = "解锁 PDF  /  Unlock PDF";
                progressBar.Visible = false;
            }
        }

        private async void UnlockBtn_Click(object sender, EventArgs e)
        {
            if (outpdf == null) return;

            string outputPath = Path.Combine(
                Path.GetDirectoryName(sourceFilePath),
                Path.GetFileNameWithoutExtension(sourceFilePath) + "_unlocked.pdf"
            );

            unlockBtn.Enabled = false;
            unlockBtn.Text = "Saving...";
            statusLabel.Text = "";

            try
            {
                await Task.Run(() =>
                {
                    outpdf.Save(outputPath);
                    outpdf.Dispose();
                    pdf?.Dispose();
                });

                statusLabel.Text = $"已保存 / Saved: {Path.GetFileName(outputPath)}";
                statusLabel.ForeColor = Color.FromArgb(100, 200, 100);
                unlockBtn.Text = "解锁 PDF  /  Unlock PDF";
                progressBar.Visible = false;
                fileLabel.Text = "No file selected";

                MessageBox.Show(
                    $"PDF unlocked and saved!\n\n{outputPath}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                statusLabel.ForeColor = Color.FromArgb(255, 100, 100);
                unlockBtn.Enabled = true;
                unlockBtn.Text = "解锁 PDF  /  Unlock PDF";
            }
        }
    }
}
