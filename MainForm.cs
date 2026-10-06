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

        // Card Container Panels
        private Panel pnlPaths = null!;
        private Panel pnlFolders = null!;
        private Panel pnlOptions = null!;
        private Panel pnlActions = null!;
        private Panel pnlOverall = null!;
        private Panel pnlProgress = null!;
        private ToolTip toolTip = null!;

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
            this.Text = "Robocopy GUI Wrapper";
            this.Size = new Size(960, 800);
            this.MinimumSize = new Size(960, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(244, 246, 249);

            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 200,
                ReshowDelay = 100,
                ShowAlways = true
            };

            // 1. TOP-LEFT CARD: Path Inputs (Narrowed to half-width, matching Subfolder Selection)
            pnlPaths = new Panel
            {
                Location = new Point(25, 20),
                Size = new Size(435, 118),
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlPaths.Paint += DrawCardBorder;

            var lblSource = new Label
            {
                Text = "Source:",
                Location = new Point(12, 16),
                Size = new Size(60, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            txtSource = new TextBox
            {
                Location = new Point(76, 16),
                Size = new Size(305, 26),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(this.Font, FontStyle.Regular)
            };
            txtSource.TextChanged += TxtSource_TextChanged;

            btnBrowseSource = new Button
            {
                Location = new Point(386, 14),
                Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 249, 250),
                Image = CreateFolderIcon(),
                Cursor = Cursors.Hand
            };
            btnBrowseSource.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnBrowseSource.Click += BtnBrowseSource_Click;
            toolTip.SetToolTip(btnBrowseSource, "Browse Source Folder");

            var lblDest = new Label
            {
                Text = "Target:",
                Location = new Point(12, 64),
                Size = new Size(60, 26),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            txtDestination = new TextBox
            {
                Location = new Point(76, 64),
                Size = new Size(305, 26),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(this.Font, FontStyle.Regular)
            };

            btnBrowseDest = new Button
            {
                Location = new Point(386, 62),
                Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 249, 250),
                Image = CreateFolderIcon(),
                Cursor = Cursors.Hand
            };
            btnBrowseDest.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnBrowseDest.Click += BtnBrowseDest_Click;
            toolTip.SetToolTip(btnBrowseDest, "Browse Target Folder");

            pnlPaths.Controls.AddRange(new Control[] { lblSource, txtSource, btnBrowseSource, lblDest, txtDestination, btnBrowseDest });
            this.Controls.Add(pnlPaths);

            // 2. BOTTOM-LEFT CARD: Subfolder Selection (Directly below Path inputs)
            pnlFolders = new Panel
            {
                Location = new Point(25, 148),
                Size = new Size(435, 256),
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlFolders.Paint += DrawCardBorder;

            var lblFoldersTitle = new Label
            {
                Text = "Subfolder Selection (Uncheck to Exclude)",
                Location = new Point(12, 10),
                Size = new Size(410, 22),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            tvFolders = new TreeView
            {
                Location = new Point(12, 36),
                Size = new Size(411, 206),
                CheckBoxes = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            tvFolders.BeforeExpand += TvFolders_BeforeExpand;
            tvFolders.AfterCheck += TvFolders_AfterCheck;
            pnlFolders.Controls.AddRange(new Control[] { lblFoldersTitle, tvFolders });
            this.Controls.Add(pnlFolders);

            // 3. TOP-RIGHT CARD: Essential Flags & Settings (Moved UP to top-right)
            pnlOptions = new Panel
            {
                Location = new Point(475, 20),
                Size = new Size(460, 254),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlOptions.Paint += DrawCardBorder;

            var lblOptionsTitle = new Label
            {
                Text = "Essential Flags & Settings",
                Location = new Point(14, 10),
                Size = new Size(430, 22),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            chkUnbuffered = new CheckBox
            {
                Text = "Unbuffered I/O (/J - recommendation for large files)",
                Checked = true,
                Location = new Point(16, 38),
                Size = new Size(420, 24)
            };
            chkRestartable = new CheckBox
            {
                Text = "Restartable Mode (/Z - resumes transfer if network cuts)",
                Checked = true,
                Location = new Point(16, 68),
                Size = new Size(420, 24)
            };

            var lblRetries = new Label
            {
                Text = "Retries on Failure (/R):",
                Location = new Point(16, 104),
                Size = new Size(170, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            numRetries = new NumericUpDown
            {
                Value = 3,
                Minimum = 0,
                Maximum = 1000,
                Location = new Point(190, 104),
                Size = new Size(75, 24),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblWait = new Label
            {
                Text = "Wait time (seconds, /W):",
                Location = new Point(16, 140),
                Size = new Size(170, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            numWait = new NumericUpDown
            {
                Value = 2,
                Minimum = 0,
                Maximum = 1000,
                Location = new Point(190, 140),
                Size = new Size(75, 24),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblFilter = new Label
            {
                Text = "File Pattern (e.g. *.zip):",
                Location = new Point(16, 176),
                Size = new Size(170, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            txtFileFilter = new TextBox
            {
                Text = "*.*",
                Location = new Point(190, 176),
                Size = new Size(240, 24),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlOptions.Controls.AddRange(new Control[] { lblOptionsTitle, chkUnbuffered, chkRestartable, lblRetries, numRetries, lblWait, numWait, lblFilter, txtFileFilter });
            this.Controls.Add(pnlOptions);

            // 4. MIDDLE-RIGHT CARD: Action Controls (Start/Play, Stop, Save/Disk)
            pnlActions = new Panel
            {
                Location = new Point(475, 284),
                Size = new Size(460, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlActions.Paint += DrawCardBorder;

            var lblActionsTitle = new Label
            {
                Text = "Transfer Controls",
                Location = new Point(14, 10),
                Size = new Size(430, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            var lblActionsHint = new Label
            {
                Text = "Hover over an icon button to see its action:",
                Location = new Point(14, 30),
                Size = new Size(430, 18),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular)
            };

            // Play Button (Start)
            btnStart = new Button
            {
                Location = new Point(16, 54),
                Size = new Size(76, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 253, 244),
                Image = CreatePlayIcon(),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);
            btnStart.FlatAppearance.BorderSize = 1;
            btnStart.Click += BtnStart_Click;
            toolTip.SetToolTip(btnStart, "Start Copying (Play)");

            // Stop Button (Stop/Cancel)
            btnStop = new Button
            {
                Location = new Point(102, 54),
                Size = new Size(76, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(243, 244, 246),
                Image = CreateStopIcon(),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnStop.FlatAppearance.BorderSize = 1;
            btnStop.Click += BtnStop_Click;
            toolTip.SetToolTip(btnStop, "Stop / Cancel Transfer (Stop)");

            // Save Button (Disk)
            btnSaveConfig = new Button
            {
                Location = new Point(188, 54),
                Size = new Size(76, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(239, 246, 255),
                Image = CreateDiskIcon(),
                Cursor = Cursors.Hand
            };
            btnSaveConfig.FlatAppearance.BorderColor = Color.FromArgb(59, 130, 246);
            btnSaveConfig.FlatAppearance.BorderSize = 1;
            btnSaveConfig.Click += BtnSaveConfig_Click;
            toolTip.SetToolTip(btnSaveConfig, "Save Settings to config.json (Disk)");

            pnlActions.Controls.AddRange(new Control[] { lblActionsTitle, lblActionsHint, btnStart, btnStop, btnSaveConfig });
            this.Controls.Add(pnlActions);

            // 5. BOTTOM SECTION: Overall Progress Card (Full Width)
            pnlOverall = new Panel
            {
                Location = new Point(25, 414),
                Size = new Size(910, 68),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlOverall.Paint += DrawCardBorder;

            lblOverallProgress = new Label
            {
                Text = "Overall Progress: Ready",
                Location = new Point(14, 10),
                Size = new Size(882, 20),
                Font = new Font(this.Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            overallProgressBar = new ProgressBar
            {
                Location = new Point(14, 34),
                Size = new Size(882, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlOverall.Controls.AddRange(new Control[] { lblOverallProgress, overallProgressBar });
            this.Controls.Add(pnlOverall);

            // 6. BOTTOM SECTION: Current File & Live Log (Full Width)
            pnlProgress = new Panel
            {
                Location = new Point(25, 492),
                Size = new Size(910, 252),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlProgress.Paint += DrawCardBorder;

            lblStatus = new Label
            {
                Text = "Status: Idle",
                Location = new Point(14, 10),
                Size = new Size(882, 20),
                Font = new Font(this.Font, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lblCurrentFile = new Label
            {
                Text = "Current File: None",
                Location = new Point(14, 30),
                Size = new Size(882, 18),
                AutoEllipsis = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            fileProgressBar = new ProgressBar
            {
                Location = new Point(14, 50),
                Size = new Size(882, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtLog = new TextBox
            {
                Location = new Point(14, 66),
                Size = new Size(882, 172),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(24, 24, 27),
                ForeColor = Color.FromArgb(228, 228, 231),
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.None
            };

            pnlProgress.Controls.AddRange(new Control[] { lblStatus, lblCurrentFile, fileProgressBar, txtLog });
            this.Controls.Add(pnlProgress);
        }

        private static void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is Panel panel)
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
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

            btnStart.BackColor = !running ? Color.FromArgb(240, 253, 244) : Color.FromArgb(243, 244, 246);
            btnStart.FlatAppearance.BorderColor = !running ? Color.FromArgb(34, 197, 94) : Color.FromArgb(209, 213, 219);

            btnStop.BackColor = running ? Color.FromArgb(254, 242, 242) : Color.FromArgb(243, 244, 246);
            btnStop.FlatAppearance.BorderColor = running ? Color.FromArgb(239, 68, 68) : Color.FromArgb(209, 213, 219);

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
            var bmp = new Bitmap(20, 20);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var tabBrush = new SolidBrush(Color.FromArgb(217, 119, 6)); // Amber dark
            using var bodyBrush = new SolidBrush(Color.FromArgb(245, 158, 11)); // Amber vibrant
            using var flapBrush = new SolidBrush(Color.FromArgb(251, 191, 36)); // Amber light
            using var pen = new Pen(Color.FromArgb(180, 83, 9), 1);

            g.FillRectangle(tabBrush, 2, 2, 7, 4);
            g.FillRectangle(bodyBrush, 2, 5, 16, 12);
            g.FillPolygon(flapBrush, new Point[] {
                new Point(2, 8),
                new Point(18, 8),
                new Point(16, 17),
                new Point(2, 17)
            });
            g.DrawRectangle(pen, 2, 5, 15, 11);
            return bmp;
        }

        private static Bitmap CreatePlayIcon()
        {
            var bmp = new Bitmap(24, 24);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(22, 163, 74)); // Emerald green
            PointF[] points = {
                new PointF(7, 4),
                new PointF(19, 12),
                new PointF(7, 20)
            };
            g.FillPolygon(brush, points);
            return bmp;
        }

        private static Bitmap CreateStopIcon()
        {
            var bmp = new Bitmap(24, 24);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(220, 38, 38)); // Rose red
            g.FillRectangle(brush, 6, 6, 12, 12);
            return bmp;
        }

        private static Bitmap CreateDiskIcon()
        {
            var bmp = new Bitmap(24, 24);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bodyBrush = new SolidBrush(Color.FromArgb(37, 99, 235)); // Royal blue
            using var whiteBrush = new SolidBrush(Color.White);
            using var shutterBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
            using var pen = new Pen(Color.FromArgb(30, 64, 175), 1);

            g.FillRectangle(bodyBrush, 3, 3, 18, 18);
            g.FillRectangle(shutterBrush, 7, 3, 9, 7);
            g.DrawRectangle(pen, 9, 4, 3, 4);
            g.FillRectangle(whiteBrush, 6, 13, 12, 7);
            return bmp;
        }
    }
}

