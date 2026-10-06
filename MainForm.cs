using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging; // Add this for Icon related operations
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobocopyGui
{
    // Helper class for rounded rectangle drawing
    public static class GraphicsExtensions
    {
        public static void AddRoundRect(this GraphicsPath path, Rectangle rect, int radius)
        {
            float diameter = radius * 2;
            SizeF size = new SizeF(diameter, diameter);

            RectangleF arc = new RectangleF(rect.Location, size);
            path.AddArc(arc, 180, 90);

            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
        }
    }

    // Custom control for rounded textboxes
    public class RoundedTextBox : TextBox
    {
        private int _borderRadius = 4;
        private Color _borderColor = Color.FromArgb(210, 214, 220);

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set { _borderRadius = value; this.Invalidate(); }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; this.Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw rounded rectangle border
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRoundRect(new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height), _borderRadius);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(_borderColor, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    // Custom control for rounded buttons
    public class RoundedButton : Button
    {
        private int _borderRadius = 4;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set { _borderRadius = value; this.Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            // Draw rounded rectangle background
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRoundRect(new Rectangle(0, 0, this.Width, this.Height), _borderRadius);
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    pevent.Graphics.FillPath(brush, path);
                }

                // Draw border if needed
                if (this.FlatStyle == FlatStyle.Flat && this.FlatAppearance.BorderSize > 0)
                {
                    using (Pen pen = new Pen(this.FlatAppearance.BorderColor, this.FlatAppearance.BorderSize))
                    {
                        pevent.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }
    }

    public class MainForm : Form
    {
        private readonly AppConfig _config;
        private readonly RobocopyRunner _runner;
        private bool _isRunning = false;

        // Form Controls
        private TextBox txtSource = null!;
        private TextBox txtDestination = null!;
        private Button btnBrowseSource = null!;
        private Button btnBrowseDest = null!;

        private TreeView tvFolders = null!;

        private CheckBox chkUnbuffered = null!;
        private CheckBox chkRestartable = null!;
        private NumericUpDown numRetries = null!;
        private NumericUpDown numWait = null!;
        private TextBox txtFileFilter = null!;

        private Button btnStart = null!;
        private Button btnStop = null!;
        private Button btnSaveConfig = null!;

        private ProgressBar fileProgressBar = null!;
        private ProgressBar overallProgressBar = null!;
        private Label lblStatus = null!;
        private Label lblCurrentFile = null!;
        private Label lblOverallProgress = null!;
        private TextBox txtLog = null!;

        // Panels to replace GroupBoxes for modern look
        private Panel pnlFolders = null!;
        private Panel pnlOptions = null!;
        private Panel pnlProgress = null!;

        public MainForm()
        {
            _config = AppConfig.Load();
            _runner = new RobocopyRunner();

            // Wire up runner events
            _runner.ProgressChanged += Runner_ProgressChanged;
            _runner.OverallProgressChanged += Runner_OverallProgressChanged;
            _runner.OutputReceived += Runner_OutputReceived;
            _runner.StatusChanged += Runner_StatusChanged;

            InitializeFormComponents();
            LoadConfigToUi();
        }

        private void InitializeFormComponents()
        {
            // Main Window Settings
            this.Text = "Robocopy GUI Wrapper (Phase 1)";
            this.Size = new Size(950, 780); // Slightly larger for more whitespace
            this.MinimumSize = new Size(850, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 246, 248); // Modern light-gray background

            // Top Section: Path inputs
            var lblSource = new Label { Text = "Source Folder:", Location = new Point(30, 25), Size = new Size(110, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            txtSource = new RoundedTextBox { Location = new Point(150, 25), Size = new Size(650, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderStyle = BorderStyle.None, BorderRadius = 6, Font = new Font(this.Font, FontStyle.Regular) };
            txtSource.TextChanged += TxtSource_TextChanged;
            btnBrowseSource = new RoundedButton { Text = "", Location = new Point(810, 25), Size = new Size(30, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(230, 230, 230), BorderRadius = 6 };
            btnBrowseSource.Image = CreateFolderIcon();
            btnBrowseSource.ImageAlign = ContentAlignment.MiddleCenter;
            btnBrowseSource.TextAlign = ContentAlignment.MiddleCenter;
            btnBrowseSource.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 220);
            btnBrowseSource.Click += BtnBrowseSource_Click;

            var lblDest = new Label { Text = "Target Folder:", Location = new Point(30, 65), Size = new Size(110, 25), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            txtDestination = new RoundedTextBox { Location = new Point(150, 65), Size = new Size(650, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderStyle = BorderStyle.None, BorderRadius = 6, Font = new Font(this.Font, FontStyle.Regular) };
            btnBrowseDest = new RoundedButton { Text = "", Location = new Point(810, 65), Size = new Size(30, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(230, 230, 230), BorderRadius = 6 };
            btnBrowseDest.Image = CreateFolderIcon();
            btnBrowseDest.ImageAlign = ContentAlignment.MiddleCenter;
            btnBrowseDest.TextAlign = ContentAlignment.MiddleCenter;
            btnBrowseDest.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 220);
            btnBrowseDest.Click += BtnBrowseDest_Click;

            this.Controls.AddRange(new Control[] { lblSource, txtSource, btnBrowseSource, lblDest, txtDestination, btnBrowseDest });

            // Panelled Sections (Card Style)
            pnlFolders = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(425, 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            var lblFoldersTitle = new Label { Text = "Subfolder Selection (Uncheck to Exclude)", Location = new Point(12, 10), Size = new Size(400, 22), AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            tvFolders = new TreeView
            {
                Location = new Point(12, 35),
                Size = new Size(400, 210),
                CheckBoxes = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            tvFolders.BeforeExpand += TvFolders_BeforeExpand;
            tvFolders.AfterCheck += TvFolders_AfterCheck;
            pnlFolders.Controls.AddRange(new Control[] { lblFoldersTitle, tvFolders });
            this.Controls.Add(pnlFolders);

            pnlOptions = new Panel
            {
                Location = new Point(475, 100),
                Size = new Size(430, 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            var lblOptionsTitle = new Label { Text = "Essential Flags & Settings", Location = new Point(12, 10), Size = new Size(400, 22), AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };

            chkUnbuffered = new CheckBox { Text = "Unbuffered I/O (/J - recommendation for large files)", Checked = true, Location = new Point(15, 40), Size = new Size(390, 23) };
            chkRestartable = new CheckBox { Text = "Restartable Mode (/Z - resumes transfer if network cuts)", Checked = true, Location = new Point(15, 70), Size = new Size(390, 23) };

            var lblRetries = new Label { Text = "Retries on Failure (/R):", Location = new Point(15, 105), Size = new Size(160, 23), TextAlign = ContentAlignment.MiddleLeft };
            numRetries = new NumericUpDown { Value = 3, Minimum = 0, Maximum = 1000, Location = new Point(185, 105), Size = new Size(80, 23), BorderStyle = BorderStyle.FixedSingle };

            var lblWait = new Label { Text = "Wait time (seconds, /W):", Location = new Point(15, 140), Size = new Size(160, 23), TextAlign = ContentAlignment.MiddleLeft };
            numWait = new NumericUpDown { Value = 2, Minimum = 0, Maximum = 1000, Location = new Point(185, 140), Size = new Size(80, 23), BorderStyle = BorderStyle.FixedSingle };

            var lblFilter = new Label { Text = "File Pattern (e.g. *.zip):", Location = new Point(15, 175), Size = new Size(160, 23), TextAlign = ContentAlignment.MiddleLeft };
            txtFileFilter = new TextBox { Text = "*.*", Location = new Point(185, 175), Size = new Size(200, 23), BorderStyle = BorderStyle.FixedSingle };

            pnlOptions.Controls.AddRange(new Control[] { lblOptionsTitle, chkUnbuffered, chkRestartable, lblRetries, numRetries, lblWait, numWait, lblFilter, txtFileFilter });
            this.Controls.Add(pnlOptions);

            // Overall Progress Card (Prominent & Clean)
            var pnlOverall = new Panel
            {
                Location = new Point(30, 375),
                Size = new Size(875, 65),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            lblOverallProgress = new Label { Text = "Overall Progress: Ready", Location = new Point(12, 10), Size = new Size(850, 20), Font = new Font(this.Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            overallProgressBar = new ProgressBar { Location = new Point(12, 33), Size = new Size(850, 18), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlOverall.Controls.AddRange(new Control[] { lblOverallProgress, overallProgressBar });
            this.Controls.Add(pnlOverall);

            // Control Buttons (Start, Stop, Save)
            btnStart = new Button { Text = "Start Copying", Location = new Point(30, 455), Size = new Size(160, 38), BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White, Font = new Font(this.Font, FontStyle.Bold), Anchor = AnchorStyles.Bottom | AnchorStyles.Left, FlatStyle = FlatStyle.Flat };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;

            btnStop = new Button { Text = "Stop/Cancel", Location = new Point(205, 455), Size = new Size(160, 38), BackColor = Color.FromArgb(198, 40, 40), ForeColor = Color.White, Font = new Font(this.Font, FontStyle.Bold), Enabled = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, FlatStyle = FlatStyle.Flat };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += BtnStop_Click;

            btnSaveConfig = new Button { Text = "Save Settings", Location = new Point(745, 455), Size = new Size(160, 38), BackColor = Color.White, ForeColor = Color.FromArgb(33, 150, 243), Font = new Font(this.Font, FontStyle.Bold), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, FlatStyle = FlatStyle.Flat };
            btnSaveConfig.FlatAppearance.BorderColor = Color.FromArgb(33, 150, 243);
            btnSaveConfig.FlatAppearance.BorderSize = 1;
            btnSaveConfig.Click += BtnSaveConfig_Click;

            this.Controls.AddRange(new Control[] { btnStart, btnStop, btnSaveConfig });

            // Lower Section: Current File & Log (Card style)
            pnlProgress = new Panel
            {
                Location = new Point(30, 505),
                Size = new Size(875, 220),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            lblStatus = new Label { Text = "Status: Idle", Location = new Point(12, 10), Size = new Size(850, 20), Font = new Font(this.Font, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblCurrentFile = new Label { Text = "Current File: None", Location = new Point(12, 32), Size = new Size(850, 18), AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            fileProgressBar = new ProgressBar { Location = new Point(12, 53), Size = new Size(850, 10), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            txtLog = new TextBox
            {
                Location = new Point(12, 70),
                Size = new Size(850, 138),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(24, 24, 27), // Modern dark slate
                ForeColor = Color.FromArgb(228, 228, 231),
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.None
            };

            pnlProgress.Controls.AddRange(new Control[] { lblStatus, lblCurrentFile, fileProgressBar, txtLog });
            this.Controls.Add(pnlProgress);
        }

        private void LoadConfigToUi()
        {
            txtSource.Text = _config.SourcePath;
            txtDestination.Text = _config.DestinationPath;
            chkUnbuffered.Checked = _config.UseUnbufferedIo;
            chkRestartable.Checked = _config.UseRestartableMode;
            numRetries.Value = _config.Retries;
            numWait.Value = _config.WaitTime;
            txtFileFilter.Text = _config.FileFilter;

            if (Directory.Exists(_config.SourcePath))
            {
                PopulateSourceTree(_config.SourcePath);
            }
        }

        private void SyncUiToConfig()
        {
            _config.SourcePath = txtSource.Text;
            _config.DestinationPath = txtDestination.Text;
            _config.UseUnbufferedIo = chkUnbuffered.Checked;
            _config.UseRestartableMode = chkRestartable.Checked;
            _config.Retries = (int)numRetries.Value;
            _config.WaitTime = (int)numWait.Value;
            _config.FileFilter = txtFileFilter.Text;
        }

        private void BtnBrowseSource_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (Directory.Exists(txtSource.Text))
            {
                dialog.InitialDirectory = txtSource.Text;
            }
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtSource.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseDest_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (Directory.Exists(txtDestination.Text))
            {
                dialog.InitialDirectory = txtDestination.Text;
            }
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtDestination.Text = dialog.SelectedPath;
            }
        }

        private void TxtSource_TextChanged(object? sender, EventArgs e)
        {
            PopulateSourceTree(txtSource.Text);
        }

        private void PopulateSourceTree(string sourcePath)
        {
            tvFolders.Nodes.Clear();
            string cleanPath = sourcePath.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(cleanPath) || !Directory.Exists(cleanPath))
                return;

            try
            {
                var rootNode = new TreeNode(Path.GetFileName(cleanPath) == "" ? cleanPath : Path.GetFileName(cleanPath))
                {
                    Tag = cleanPath,
                    Checked = true
                };
                tvFolders.Nodes.Add(rootNode);

                AddSubdirectoryNodes(rootNode, cleanPath);
                rootNode.Expand();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error scanning source: {ex.Message}";
            }
        }

        private static void AddSubdirectoryNodes(TreeNode parentNode, string path)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(dir);
                    var node = new TreeNode(dirName)
                    {
                        Tag = dir,
                        Checked = parentNode.Checked
                    };

                    // Check if sub-subdirectories exist to add a dummy node for lazy expansion
                    try
                    {
                        if (Directory.GetDirectories(dir).Length > 0)
                        {
                            node.Nodes.Add(new TreeNode("...") { Tag = "DUMMY" });
                        }
                    }
                    catch
                    {
                        // Ignore access denied errors during pre-scan
                    }

                    parentNode.Nodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding subdirectories for {path}: {ex.Message}");
            }
        }

        private void TvFolders_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag as string == "DUMMY")
            {
                e.Node.Nodes.Clear();
                if (e.Node.Tag is string dirPath && Directory.Exists(dirPath))
                {
                    AddSubdirectoryNodes(e.Node, dirPath);
                }
            }
        }

        private bool _isUpdatingCheckState = false;

        private void TvFolders_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_isUpdatingCheckState || e.Node == null) return;

            _isUpdatingCheckState = true;
            try
            {
                // Propagate check state to all loaded descendants
                SetChildNodesCheckState(e.Node, e.Node.Checked);
            }
            finally
            {
                _isUpdatingCheckState = false;
            }
        }

        private void SetChildNodesCheckState(TreeNode parentNode, bool isChecked)
        {
            foreach (TreeNode child in parentNode.Nodes)
            {
                if (child.Tag as string == "DUMMY") continue;

                child.Checked = isChecked;
                SetChildNodesCheckState(child, isChecked);
            }
        }

        private List<string> GetExcludedDirectories()
        {
            var excludedDirs = new List<string>();
            if (tvFolders.Nodes.Count > 0)
            {
                CollectUncheckedDirectories(tvFolders.Nodes[0], excludedDirs);
            }
            return excludedDirs;
        }

        private void CollectUncheckedDirectories(TreeNode node, List<string> excludedDirs)
        {
            // Root node itself should not be excluded, check children
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Tag as string == "DUMMY") continue;

                if (!child.Checked)
                {
                    // Top-most unchecked directory: add to exclusion list
                    // Robocopy's /XD excludes this directory AND all its subdirectories automatically!
                    if (child.Tag is string path)
                    {
                        excludedDirs.Add(path);
                    }
                    // Do NOT recurse into unchecked children - they are already implicitly excluded!
                }
                else
                {
                    // If directory is checked, inspect its children for any unchecked sub-items
                    CollectUncheckedDirectories(child, excludedDirs);
                }
            }
        }

        private void BtnSaveConfig_Click(object? sender, EventArgs e)
        {
            SyncUiToConfig();
            if (_config.Save(out string? error))
            {
                MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Failed to save settings: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            string source = txtSource.Text.Trim().Trim('"');
            string dest = txtDestination.Text.Trim().Trim('"');

            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                MessageBox.Show("Please select a valid source folder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(dest))
            {
                MessageBox.Show("Please select a destination folder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Guard to prevent copying a folder to itself or its child
            try
            {
                string fullSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullDest = Path.GetFullPath(dest).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (fullDest.StartsWith(fullSource, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Destination folder cannot be the source folder itself or one of its child directories.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid path format: {ex.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create target directory if it doesn't exist
            try
            {
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create destination folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Identify excluded subdirectories from TreeView
            var excludedDirs = GetExcludedDirectories();

            // Save state before run
            SyncUiToConfig();
            _config.Save();

            // Set running state
            SetUiRunningState(true);
            txtLog.Clear();
            fileProgressBar.Value = 0;
            overallProgressBar.Value = 0;
            lblCurrentFile.Text = "Starting transfer...";
            lblOverallProgress.Text = "Overall Progress: Starting...";

            try
            {
                // Execute robocopy
                int exitCode = await _runner.RunAsync(source, dest, _config, excludedDirs.ToArray(), Array.Empty<string>());

                if (exitCode >= 8)
                {
                    MessageBox.Show($"Robocopy completed with errors (Exit Code {exitCode}).\n\n{RobocopyRunner.MapExitCode(exitCode)}", "Execution Finished", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show($"Robocopy completed successfully (Exit Code {exitCode}).\n\n{RobocopyRunner.MapExitCode(exitCode)}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during execution: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiRunningState(false);
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the running Robocopy process?", "Confirm Stop", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _runner.Stop();
            }
        }

        private void SetUiRunningState(bool running)
        {
            _isRunning = running;
            btnStart.Enabled = !running;
            btnStop.Enabled = running;

            txtSource.Enabled = !running;
            txtDestination.Enabled = !running;
            btnBrowseSource.Enabled = !running;
            btnBrowseDest.Enabled = !running;
            tvFolders.Enabled = !running;

            chkUnbuffered.Enabled = !running;
            chkRestartable.Enabled = !running;
            numRetries.Enabled = !running;
            numWait.Enabled = !running;
            txtFileFilter.Enabled = !running;
            btnSaveConfig.Enabled = !running;

            if (!running)
            {
                overallProgressBar.Style = ProgressBarStyle.Blocks;
            }
        }

        private void Runner_ProgressChanged(object? sender, RobocopyProgressEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Runner_ProgressChanged(sender, e)));
                return;
            }

            fileProgressBar.Value = Math.Clamp(e.Percentage, 0, 100);
            if (!string.IsNullOrEmpty(e.CurrentFile))
            {
                lblCurrentFile.Text = $"File: {e.CurrentFile} ({e.Percentage}%)";
            }
        }

        private void Runner_OverallProgressChanged(object? sender, RobocopyOverallProgressEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Runner_OverallProgressChanged(sender, e)));
                return;
            }

            if (e.TotalFiles > 0)
            {
                overallProgressBar.Style = ProgressBarStyle.Blocks;
                overallProgressBar.Value = Math.Clamp(e.OverallPercentage, 0, 100);
                lblOverallProgress.Text = $"Overall Progress: {e.CopiedFiles} of {e.TotalFiles} files copied ({e.OverallPercentage}%)";
            }
            else
            {
                overallProgressBar.Style = ProgressBarStyle.Marquee;
                lblOverallProgress.Text = e.CopiedFiles > 0
                    ? $"Overall Progress: {e.CopiedFiles} file(s) copied..."
                    : "Overall Progress: Transfer in progress...";
            }
        }

        private void Runner_OutputReceived(object? sender, RobocopyOutputEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Runner_OutputReceived(sender, e)));
                return;
            }

            txtLog.AppendText(e.Line + Environment.NewLine);
        }

        private void Runner_StatusChanged(object? sender, string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => Runner_StatusChanged(sender, status)));
                return;
            }

            lblStatus.Text = status;
        }

        private static Bitmap CreateFolderIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var tabBrush = new SolidBrush(Color.FromArgb(202, 138, 4));
                using var bodyBrush = new SolidBrush(Color.FromArgb(234, 179, 8));
                g.FillRectangle(tabBrush, 1, 2, 6, 3);
                g.FillRectangle(bodyBrush, 1, 4, 14, 10);
            }
            return bmp;
        }
    }
}

