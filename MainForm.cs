using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MsiAffinityUtility
{
    public partial class MainForm : Form
    {
        private Panel topPanel = null!;
        private Button btnRefresh = null!;
        private Button btnGenerate = null!;
        private Button btnExport = null!;
        private Button btnApply = null!;

        private Panel consoleContainer = null!;
        private TextBox txtConsole = null!;
        private Button btnCloseConsole = null!;
        private Label lblConsoleHeader = null!;
        private Button btnViewApply = null!;
        private Button btnViewRollback = null!;

        private DataGridView dgv = null!;
        private Panel bottomPanel = null!;
        private Label lblStatus = null!;

        private List<CoreInfo> cpuCores = new();
        private bool isScanning = false;
        private float scale = 1.0f;

        private string applyRegScript = "";
        private string rollbackRegScript = "";
        private bool isViewingRollback = false;

        public MainForm()
        {
            this.AutoScaleMode = AutoScaleMode.Dpi;
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            scale = this.DeviceDpi / 96.0f;
            if (scale <= 0) scale = 1.0f;

            this.Text = "MSI mode utility";
            int initWidth = Math.Min(Screen.PrimaryScreen?.WorkingArea.Width ?? 1400, (int)(1250 * scale));
            int initHeight = Math.Min(Screen.PrimaryScreen?.WorkingArea.Height ?? 750, (int)(600 * scale));
            this.Size = new Size(initWidth, initHeight);
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Font = SystemFonts.MessageBoxFont ?? new Font("MS UI Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = SystemColors.Control;

            int topHeight = (int)(34 * scale);
            int bottomHeight = (int)(26 * scale);
            int btnHeight = (int)(25 * scale);

            // 1. トップパネル (ボタン群)
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = topHeight,
                BackColor = SystemColors.Control
            };

            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point((int)(6 * scale), (int)(4 * scale)),
                Size = new Size((int)(75 * scale), btnHeight),
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                Font = this.Font
            };
            btnRefresh.Click += async (s, e) => await ScanDevicesAsync();

            btnGenerate = new Button
            {
                Text = "Generate",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(topPanel.Width - (int)(340 * scale), (int)(4 * scale)),
                Size = new Size((int)(85 * scale), btnHeight),
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                Font = this.Font
            };
            btnGenerate.Click += BtnGenerate_Click;

            btnExport = new Button
            {
                Text = "Export (Backup)",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(topPanel.Width - (int)(245 * scale), (int)(4 * scale)),
                Size = new Size((int)(155 * scale), btnHeight),
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                Font = this.Font,
                Visible = false
            };
            btnExport.Click += BtnExport_Click;

            btnApply = new Button
            {
                Text = "Apply",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(topPanel.Width - (int)(85 * scale), (int)(4 * scale)),
                Size = new Size((int)(75 * scale), btnHeight),
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                Font = this.Font,
                Visible = false
            };
            btnApply.Click += BtnApply_Click;

            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(btnGenerate);
            topPanel.Controls.Add(btnExport);
            topPanel.Controls.Add(btnApply);
            this.Controls.Add(topPanel);

            // 2. コンソール風・黒背景ウィンドウ (エキスパンドパネル)
            int consoleHeight = (int)(200 * scale);
            consoleContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = consoleHeight,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            var consoleHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = (int)(26 * scale),
                BackColor = Color.FromArgb(36, 36, 36)
            };

            lblConsoleHeader = new Label
            {
                Text = "  [Preview] Changes to Apply",
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point((int)(4 * scale), (int)(4 * scale)),
                AutoSize = true
            };

            btnCloseConsole = new Button
            {
                Text = "×",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(consoleHeaderPanel.Width - (int)(32 * scale), (int)(1 * scale)),
                Size = new Size((int)(28 * scale), (int)(22 * scale)),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(36, 36, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
            };
            btnCloseConsole.FlatAppearance.BorderSize = 0;
            btnCloseConsole.Click += (s, e) => { consoleContainer.Visible = false; };

            btnViewRollback = new Button
            {
                Text = "Rollback / Backup",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size((int)(130 * scale), (int)(20 * scale)),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.FromArgb(36, 36, 36)
            };
            btnViewRollback.FlatAppearance.BorderSize = 0;
            btnViewRollback.Click += (s, e) => SwitchConsoleView(true);

            btnViewApply = new Button
            {
                Text = "Apply Changes",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size((int)(105 * scale), (int)(20 * scale)),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            btnViewApply.FlatAppearance.BorderSize = 0;
            btnViewApply.Click += (s, e) => SwitchConsoleView(false);

            void LayoutConsoleHeader()
            {
                int w = consoleHeaderPanel.ClientSize.Width;
                int topY = (int)(2 * scale);
                int closeW = (int)(28 * scale);
                btnCloseConsole.Location = new Point(w - closeW - (int)(4 * scale), topY);
                btnViewRollback.Location = new Point(btnCloseConsole.Left - btnViewRollback.Width - (int)(6 * scale), topY);
                btnViewApply.Location = new Point(btnViewRollback.Left - btnViewApply.Width - (int)(4 * scale), topY);
            }
            consoleHeaderPanel.Resize += (s, e) => LayoutConsoleHeader();

            consoleHeaderPanel.Controls.Add(lblConsoleHeader);
            consoleHeaderPanel.Controls.Add(btnViewApply);
            consoleHeaderPanel.Controls.Add(btnViewRollback);
            consoleHeaderPanel.Controls.Add(btnCloseConsole);
            consoleContainer.Controls.Add(consoleHeaderPanel);
            LayoutConsoleHeader();

            txtConsole = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Consolas", 9.25F, FontStyle.Regular, GraphicsUnit.Point),
                BorderStyle = BorderStyle.None
            };
            consoleContainer.Controls.Add(txtConsole);
            txtConsole.BringToFront();

            this.Controls.Add(consoleContainer);

            // 3. ボトムパネル
            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = bottomHeight,
                BackColor = SystemColors.Control
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "",
                Font = this.Font
            };
            bottomPanel.Controls.Add(lblStatus);
            this.Controls.Add(bottomPanel);

            // 4. メイン DataGridView
            int rowHeight = (int)(23 * scale);
            int headerHeight = (int)(25 * scale);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 235, 235),
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Both,
                RowTemplate = { Height = rowHeight }
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.Font = this.Font;
            dgv.ColumnHeadersHeight = headerHeight;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;

            SetupColumns(rowHeight);

            this.Controls.Add(dgv);
            dgv.BringToFront();

            this.Shown += async (s, e) => await ScanDevicesAsync();
        }

        private void SetupColumns(int squareSize)
        {
            dgv.Columns.Clear();
            float s = scale;

            var colName = new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "name",
                MinimumWidth = (int)(260 * s),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            dgv.Columns.Add(colName);

            var colVen = new DataGridViewTextBoxColumn
            {
                Name = "colVen",
                HeaderText = "ven",
                Width = (int)(125 * s),
                ReadOnly = true
            };
            dgv.Columns.Add(colVen);

            var colIrq = new DataGridViewTextBoxColumn
            {
                Name = "colIrq",
                HeaderText = "irq",
                Width = (int)(46 * s),
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgv.Columns.Add(colIrq);

            var colMsi = new DataGridViewCheckBoxColumn
            {
                Name = "colMsi",
                HeaderText = "msi",
                Width = (int)(40 * s),
                FlatStyle = FlatStyle.Standard,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgv.Columns.Add(colMsi);

            var colLimit = new DataGridViewTextBoxColumn
            {
                Name = "colLimit",
                HeaderText = "limit",
                Width = (int)(48 * s),
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgv.Columns.Add(colLimit);

            var colMaxLimit = new DataGridViewTextBoxColumn
            {
                Name = "colMaxLimit",
                HeaderText = "max limit",
                Width = (int)(68 * s),
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgv.Columns.Add(colMaxLimit);

            var colSupported = new DataGridViewTextBoxColumn
            {
                Name = "colSupported",
                HeaderText = "supported modes",
                Width = (int)(138 * s),
                ReadOnly = true
            };
            dgv.Columns.Add(colSupported);

            var colPriority = new DataGridViewComboBoxColumn
            {
                Name = "colPriority",
                HeaderText = "interrupt priority",
                Width = (int)(118 * s),
                FlatStyle = FlatStyle.Flat
            };
            colPriority.Items.AddRange("Undefined", "High", "Normal", "Low");
            dgv.Columns.Add(colPriority);

            // コア列 (正方形セル)
            cpuCores = CpuTopology.GetCpuCores();
            Font coreHeaderFont = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point);

            foreach (var core in cpuCores)
            {
                var colCore = new DataGridViewCheckBoxColumn
                {
                    Name = $"colCore_{core.LogicalIndex}",
                    HeaderText = core.Label,
                    Width = squareSize,
                    Resizable = DataGridViewTriState.False,
                    FlatStyle = FlatStyle.Standard,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = coreHeaderFont } }
                };
                dgv.Columns.Add(colCore);
            }

            var colMask = new DataGridViewTextBoxColumn
            {
                Name = "colMask",
                HeaderText = "mask",
                Width = (int)(60 * s),
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            dgv.Columns.Add(colMask);
        }

        private async Task ScanDevicesAsync()
        {
            if (isScanning) return;
            isScanning = true;
            btnRefresh.Enabled = false;
            lblStatus.Text = "Scanning PCI devices...";
            dgv.Rows.Clear();

            await Task.Run(() =>
            {
                var devices = DeviceScanner.ScanPciDevices();
                foreach (var dev in devices)
                {
                    this.Invoke(new Action(() =>
                    {
                        AddDeviceRow(dev);
                    }));
                    System.Threading.Thread.Sleep(4);
                }
            });

            isScanning = false;
            btnRefresh.Enabled = true;

            if (dgv.Rows.Count > 0)
            {
                dgv.Rows[0].Selected = true;
                UpdateStatusLabel();
            }
            else
            {
                lblStatus.Text = "No devices found.";
            }
        }

        private void AddDeviceRow(PciDeviceInfo dev)
        {
            int rowIndex = dgv.Rows.Add();
            var row = dgv.Rows[rowIndex];

            row.Tag = dev;
            row.Cells["colName"].Value = dev.Name;
            row.Cells["colVen"].Value = dev.Vendor;
            row.Cells["colIrq"].Value = dev.Irq;
            row.Cells["colMsi"].Value = dev.Msi;
            row.Cells["colLimit"].Value = dev.Limit;
            row.Cells["colMaxLimit"].Value = dev.MaxLimit;
            row.Cells["colSupported"].Value = dev.SupportedModes;
            row.Cells["colPriority"].Value = dev.Priority;

            ulong mask = dev.AffinityMask;
            bool anyCoreChecked = false;
            for (int i = 0; i < cpuCores.Count; i++)
            {
                int coreIdx = cpuCores[i].LogicalIndex;
                bool isSet = dev.HasAffinityPolicy && ((mask & (1UL << coreIdx)) != 0);
                row.Cells[$"colCore_{coreIdx}"].Value = isSet;
                if (isSet) anyCoreChecked = true;
            }

            row.Cells["colMask"].Value = anyCoreChecked ? $"0x{mask:X}" : "-";
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgv.IsCurrentCellDirty)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgv.Rows[e.RowIndex];
            string colName = dgv.Columns[e.ColumnIndex].Name;

            if (colName.StartsWith("colCore_"))
            {
                ulong newMask = 0;
                bool anyChecked = false;
                foreach (var core in cpuCores)
                {
                    var val = row.Cells[$"colCore_{core.LogicalIndex}"].Value;
                    if (val is bool b && b)
                    {
                        newMask |= (1UL << core.LogicalIndex);
                        anyChecked = true;
                    }
                }
                row.Cells["colMask"].Value = anyChecked ? $"0x{newMask:X}" : "-";
            }
        }

        private void Dgv_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (dgv.SelectedRows.Count > 0)
            {
                var row = dgv.SelectedRows[0];
                if (row.Tag is PciDeviceInfo dev)
                {
                    lblStatus.Text = dev.InstancePath;
                }
            }
        }

        private (string applyScript, string rollbackScript, int changeCount) GenerateRegistryScripts(bool fromLaunchSnapshot = false)
        {
            var applySb = new StringBuilder();
            applySb.AppendLine("Windows Registry Editor Version 5.00");
            applySb.AppendLine();
            applySb.AppendLine("; ========================================================");
            applySb.AppendLine("; Apply Script (Changes to be applied)");
            applySb.AppendLine("; ========================================================");
            applySb.AppendLine();

            var rollbackSb = new StringBuilder();
            rollbackSb.AppendLine("Windows Registry Editor Version 5.00");
            rollbackSb.AppendLine();
            rollbackSb.AppendLine("; ========================================================");
            rollbackSb.AppendLine(fromLaunchSnapshot
                ? "; Backup / Rollback Script (Restore to initial launch state)"
                : "; Backup / Rollback Script (Restore before changes)");
            rollbackSb.AppendLine("; ========================================================");
            rollbackSb.AppendLine();

            int changeCount = 0;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Tag is not PciDeviceInfo dev) continue;

                bool curMsi = Convert.ToBoolean(row.Cells["colMsi"].Value ?? false);
                string curLimit = row.Cells["colLimit"].Value?.ToString()?.Trim() ?? "";
                string curPriority = row.Cells["colPriority"].Value?.ToString() ?? "Undefined";

                ulong curMask = 0;
                bool hasCheckedCore = false;
                foreach (var core in cpuCores)
                {
                    var cellVal = row.Cells[$"colCore_{core.LogicalIndex}"].Value;
                    if (cellVal is bool b && b)
                    {
                        curMask |= (1UL << core.LogicalIndex);
                        hasCheckedCore = true;
                    }
                }

                bool baseMsi = fromLaunchSnapshot ? dev.LaunchMsi : dev.OrigMsi;
                string baseLimit = fromLaunchSnapshot ? dev.LaunchLimit : dev.OrigLimit;
                string basePriority = fromLaunchSnapshot ? dev.LaunchPriority : dev.OrigPriority;
                ulong baseMask = fromLaunchSnapshot ? dev.LaunchAffinityMask : dev.OrigAffinityMask;
                bool baseHasAffinity = fromLaunchSnapshot ? dev.LaunchHasAffinityPolicy : dev.OrigHasAffinityPolicy;

                bool msiChanged = (curMsi != baseMsi);
                bool limitChanged = (!string.Equals(curLimit, baseLimit, StringComparison.OrdinalIgnoreCase));
                bool priChanged = (!string.Equals(curPriority, basePriority, StringComparison.OrdinalIgnoreCase));
                bool affinityChanged = (hasCheckedCore != baseHasAffinity) || (hasCheckedCore && curMask != baseMask);

                if (!msiChanged && !limitChanged && !priChanged && !affinityChanged)
                {
                    continue;
                }

                changeCount++;
                string devicePath = dev.InstancePath;

                // ----------------------------------------------------
                // 1. Apply Script
                // ----------------------------------------------------
                applySb.AppendLine($"; [Device] {dev.Name} ({dev.Vendor})");

                if (msiChanged || limitChanged)
                {
                    applySb.AppendLine($@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties]");
                    if (msiChanged)
                    {
                        applySb.AppendLine($"\"MSISupported\"=dword:{(curMsi ? 1 : 0):x8}");
                    }
                    if (limitChanged)
                    {
                        if (int.TryParse(curLimit, out int limVal) && limVal > 0)
                        {
                            applySb.AppendLine($"\"MessageNumberLimit\"=dword:{limVal:x8}");
                        }
                        else
                        {
                            applySb.AppendLine("\"MessageNumberLimit\"=-");
                        }
                    }
                    applySb.AppendLine();
                }

                if (priChanged || affinityChanged)
                {
                    if (!hasCheckedCore && curPriority == "Undefined")
                    {
                        applySb.AppendLine($@"[-HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\Affinity Policy]");
                        applySb.AppendLine();
                    }
                    else
                    {
                        applySb.AppendLine($@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\Affinity Policy]");
                        if (priChanged)
                        {
                            if (curPriority == "Undefined")
                            {
                                applySb.AppendLine("\"DevicePriority\"=-");
                            }
                            else
                            {
                                int pVal = curPriority switch { "Low" => 1, "Normal" => 2, "High" => 3, _ => 0 };
                                applySb.AppendLine($"\"DevicePriority\"=dword:{pVal:x8}");
                            }
                        }

                        if (affinityChanged)
                        {
                            if (hasCheckedCore && curMask > 0)
                            {
                                applySb.AppendLine("\"DevicePolicy\"=dword:00000004");
                                if (curMask <= uint.MaxValue)
                                {
                                    applySb.AppendLine($"\"AssignmentSetOverride\"=dword:{curMask:x8}");
                                }
                                else
                                {
                                    applySb.AppendLine($"\"AssignmentSetOverride\"=hex(b):{FormatHexQWord(curMask)}");
                                }
                            }
                            else
                            {
                                applySb.AppendLine("\"DevicePolicy\"=-");
                                applySb.AppendLine("\"AssignmentSetOverride\"=-");
                            }
                        }
                        applySb.AppendLine();
                    }
                }

                // ----------------------------------------------------
                // 2. Rollback / Backup Script
                // ----------------------------------------------------
                rollbackSb.AppendLine($"; [Device] {dev.Name} ({dev.Vendor})");

                if (msiChanged || limitChanged)
                {
                    rollbackSb.AppendLine($@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties]");
                    rollbackSb.AppendLine($"\"MSISupported\"=dword:{(baseMsi ? 1 : 0):x8}");
                    if (int.TryParse(baseLimit, out int origLim) && origLim > 0)
                    {
                        rollbackSb.AppendLine($"\"MessageNumberLimit\"=dword:{origLim:x8}");
                    }
                    else
                    {
                        rollbackSb.AppendLine("\"MessageNumberLimit\"=-");
                    }
                    rollbackSb.AppendLine();
                }

                if (priChanged || affinityChanged)
                {
                    if (!baseHasAffinity && basePriority == "Undefined")
                    {
                        rollbackSb.AppendLine($@"[-HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\Affinity Policy]");
                        rollbackSb.AppendLine();
                    }
                    else
                    {
                        rollbackSb.AppendLine($@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\{devicePath}\Device Parameters\Interrupt Management\Affinity Policy]");
                        if (basePriority == "Undefined")
                        {
                            rollbackSb.AppendLine("\"DevicePriority\"=-");
                        }
                        else
                        {
                            int pVal = basePriority switch { "Low" => 1, "Normal" => 2, "High" => 3, _ => 0 };
                            rollbackSb.AppendLine($"\"DevicePriority\"=dword:{pVal:x8}");
                        }

                        if (baseHasAffinity && baseMask > 0)
                        {
                            rollbackSb.AppendLine("\"DevicePolicy\"=dword:00000004");
                            if (baseMask <= uint.MaxValue)
                            {
                                rollbackSb.AppendLine($"\"AssignmentSetOverride\"=dword:{baseMask:x8}");
                            }
                            else
                            {
                                rollbackSb.AppendLine($"\"AssignmentSetOverride\"=hex(b):{FormatHexQWord(baseMask)}");
                            }
                        }
                        else
                        {
                            rollbackSb.AppendLine("\"DevicePolicy\"=-");
                            rollbackSb.AppendLine("\"AssignmentSetOverride\"=-");
                        }
                        rollbackSb.AppendLine();
                    }
                }
            }

            if (changeCount == 0)
            {
                applySb.AppendLine("; (No changes detected)");
                rollbackSb.AppendLine("; (No changes to rollback)");
            }

            return (applySb.ToString(), rollbackSb.ToString(), changeCount);
        }

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            var (apply, rollback, _) = GenerateRegistryScripts(fromLaunchSnapshot: false);
            applyRegScript = apply;
            rollbackRegScript = rollback;

            isViewingRollback = false;
            SwitchConsoleView(false);

            consoleContainer.Visible = true;
            btnExport.Visible = true;
            btnApply.Visible = true;

            AdjustButtonPositions();
        }

        private void SwitchConsoleView(bool viewRollback)
        {
            isViewingRollback = viewRollback;
            if (isViewingRollback)
            {
                txtConsole.Text = rollbackRegScript;
                lblConsoleHeader.Text = "  ● [Rollback / Backup Script] Restore state before changes";
                lblConsoleHeader.ForeColor = Color.FromArgb(255, 190, 80);

                // コンソール内タブボタン: Rollback を鮮やかなアンバーオレンジでハイライト
                btnViewRollback.BackColor = Color.FromArgb(202, 101, 16);
                btnViewRollback.ForeColor = Color.White;
                btnViewRollback.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);

                btnViewApply.BackColor = Color.FromArgb(36, 36, 36);
                btnViewApply.ForeColor = Color.FromArgb(160, 160, 160);
                btnViewApply.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);

                // トップパネルボタン: Export をハイライト
                btnExport.UseVisualStyleBackColor = false;
                btnExport.BackColor = Color.FromArgb(255, 230, 190);
                btnExport.ForeColor = Color.FromArgb(140, 50, 0);
                btnExport.Font = new Font(this.Font, FontStyle.Bold);

                btnApply.UseVisualStyleBackColor = true;
                btnApply.BackColor = SystemColors.Control;
                btnApply.ForeColor = SystemColors.ControlText;
                btnApply.Font = this.Font;
            }
            else
            {
                txtConsole.Text = applyRegScript;
                lblConsoleHeader.Text = "  ● [Apply Preview] Registry changes to be written";
                lblConsoleHeader.ForeColor = Color.FromArgb(90, 180, 255);

                // コンソール内タブボタン: Apply を Windows モダンブルーでハイライト
                btnViewApply.BackColor = Color.FromArgb(0, 120, 215);
                btnViewApply.ForeColor = Color.White;
                btnViewApply.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);

                btnViewRollback.BackColor = Color.FromArgb(36, 36, 36);
                btnViewRollback.ForeColor = Color.FromArgb(160, 160, 160);
                btnViewRollback.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);

                // トップパネルボタン: Apply をハイライト
                btnApply.UseVisualStyleBackColor = false;
                btnApply.BackColor = Color.FromArgb(210, 235, 255);
                btnApply.ForeColor = Color.FromArgb(0, 75, 160);
                btnApply.Font = new Font(this.Font, FontStyle.Bold);

                btnExport.UseVisualStyleBackColor = true;
                btnExport.BackColor = SystemColors.Control;
                btnExport.ForeColor = SystemColors.ControlText;
                btnExport.Font = this.Font;
            }
        }

        private void AdjustButtonPositions()
        {
            float s = scale;
            int rightMargin = (int)(10 * s);
            int gap = (int)(6 * s);

            btnApply.Location = new Point(topPanel.Width - btnApply.Width - rightMargin, (int)(4 * s));
            btnExport.Location = new Point(btnApply.Left - btnExport.Width - gap, (int)(4 * s));
            btnGenerate.Location = new Point(btnExport.Left - btnGenerate.Width - gap, (int)(4 * s));
        }

        private static string FormatHexQWord(ulong val)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            return string.Join(",", bytes.Select(b => b.ToString("x2")));
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            // Export を押したら即座に Rollback / Backup プレビューへ切り替えてハイライト
            SwitchConsoleView(true);

            // もし直近の差分がない場合（すでにApply済みなど）、LaunchSnapshot（起動時状態からの全差分）からロールバックを再生成！
            if (string.IsNullOrWhiteSpace(rollbackRegScript) || rollbackRegScript.Contains("(No changes to rollback)"))
            {
                var (_, launchRollback, launchCount) = GenerateRegistryScripts(fromLaunchSnapshot: true);
                if (launchCount > 0)
                {
                    rollbackRegScript = launchRollback;
                    txtConsole.Text = rollbackRegScript;
                }
                else
                {
                    MessageBox.Show("No changes detected to backup. Please modify settings and click Generate first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Resolve accurate Downloads folder (handles redirected paths like E:\YK-PC\Downloads)
            string downloadsPath = GetDownloadsFolderPath();

            // Default filename format: pure timestamp (e.g. 20260905_160728.reg)
            string defaultFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}.reg";

            using var sfd = new SaveFileDialog
            {
                Title = "Save Backup Registry Script (.reg)",
                Filter = "Registry Files (*.reg)|*.reg|All Files (*.*)|*.*",
                DefaultExt = "reg",
                InitialDirectory = downloadsPath,
                FileName = defaultFileName
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Encoding ansi = Encoding.GetEncoding(0);
                    File.WriteAllText(sfd.FileName, rollbackRegScript, ansi);
                    lblStatus.Text = $"Backup saved: {Path.GetFileName(sfd.FileName)}";
                    MessageBox.Show(
                        $"Backup registry script saved successfully:\r\n\r\n{sfd.FileName}",
                        "Backup Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save backup file:\r\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string GetDownloadsFolderPath()
        {
            // 1. Check User Shell Folders in Registry (handles redirected user folders)
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
                if (key != null)
                {
                    var val = key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}") ?? key.GetValue("{7D83EE9B-2244-4E70-B1F5-5393042AF1E4}");
                    if (val != null)
                    {
                        string path = Environment.ExpandEnvironmentVariables(val.ToString() ?? "");
                        if (Directory.Exists(path)) return path;
                    }
                }
            }
            catch { }

            // 2. Standard User Profile Downloads
            string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(userDownloads)) return userDownloads;

            // 3. Fallback
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            // Apply を押したら即座に Apply Changes プレビューへ切り替えてハイライト
            SwitchConsoleView(false);

            try
            {
                // 1. まず現在の変更に対するロールバックスクリプトを事前生成（適用前の値を安全に記録）
                var (applyText, rollbackText, changeCount) = GenerateRegistryScripts(fromLaunchSnapshot: false);
                if (changeCount == 0)
                {
                    MessageBox.Show("No changes detected to apply.", "MSI mode utility", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                applyRegScript = applyText;
                rollbackRegScript = rollbackText;

                // 2. 自動バックアップ .reg ファイルを保存（ダウンロードフォルダなどへ）
                string autoBackupPath = "";
                try
                {
                    string folder = GetDownloadsFolderPath();
                    if (!Directory.Exists(folder))
                    {
                        folder = AppDomain.CurrentDomain.BaseDirectory;
                    }
                    string backupFileName = $"MsiAffinity_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.reg";
                    autoBackupPath = Path.Combine(folder, backupFileName);
                    Encoding ansi = Encoding.GetEncoding(0);
                    File.WriteAllText(autoBackupPath, rollbackRegScript, ansi);
                }
                catch (Exception bEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-backup save warning: {bEx.Message}");
                }

                // 3. レジストリへ適用
                int modifiedCount = 0;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.Tag is not PciDeviceInfo dev) continue;

                    bool curMsi = Convert.ToBoolean(row.Cells["colMsi"].Value ?? false);
                    string curLimit = row.Cells["colLimit"].Value?.ToString()?.Trim() ?? "";
                    string curPriority = row.Cells["colPriority"].Value?.ToString() ?? "Undefined";

                    ulong curMask = 0;
                    bool hasCheckedCore = false;
                    foreach (var core in cpuCores)
                    {
                        var cellVal = row.Cells[$"colCore_{core.LogicalIndex}"].Value;
                        if (cellVal is bool b && b)
                        {
                            curMask |= (1UL << core.LogicalIndex);
                            hasCheckedCore = true;
                        }
                    }

                    bool msiChanged = (curMsi != dev.OrigMsi);
                    bool limitChanged = (!string.Equals(curLimit, dev.OrigLimit, StringComparison.OrdinalIgnoreCase));
                    bool priChanged = (!string.Equals(curPriority, dev.OrigPriority, StringComparison.OrdinalIgnoreCase));
                    bool affinityChanged = (hasCheckedCore != dev.OrigHasAffinityPolicy) || (hasCheckedCore && curMask != dev.OrigAffinityMask);

                    if (!msiChanged && !limitChanged && !priChanged && !affinityChanged)
                    {
                        continue;
                    }

                    string baseKeyPath = $@"SYSTEM\CurrentControlSet\Enum\{dev.InstancePath}\Device Parameters";
                    using var baseKey = Registry.LocalMachine.OpenSubKey(baseKeyPath, true);
                    if (baseKey == null) continue;

                    using var intKey = baseKey.CreateSubKey("Interrupt Management", true);
                    if (intKey == null) continue;

                    // 1. MSI
                    using (var msiKey = intKey.CreateSubKey("MessageSignaledInterruptProperties", true))
                    {
                        msiKey.SetValue("MSISupported", curMsi ? 1 : 0, RegistryValueKind.DWord);

                        if (int.TryParse(curLimit, out int limitVal) && limitVal > 0)
                        {
                            msiKey.SetValue("MessageNumberLimit", limitVal, RegistryValueKind.DWord);
                        }
                        else
                        {
                            try { msiKey.DeleteValue("MessageNumberLimit"); } catch { }
                        }
                    }

                    // 2. Affinity Policy
                    if (!hasCheckedCore && curPriority == "Undefined")
                    {
                        try
                        {
                            intKey.DeleteSubKeyTree("Affinity Policy", false);
                        }
                        catch { }
                    }
                    else
                    {
                        using var affKey = intKey.CreateSubKey("Affinity Policy", true);
                        if (curPriority == "Undefined")
                        {
                            try { affKey.DeleteValue("DevicePriority"); } catch { }
                        }
                        else
                        {
                            int priVal = curPriority switch
                            {
                                "Low" => 1,
                                "Normal" => 2,
                                "High" => 3,
                                _ => 0
                            };
                            affKey.SetValue("DevicePriority", priVal, RegistryValueKind.DWord);
                        }

                        if (hasCheckedCore && curMask > 0)
                        {
                            affKey.SetValue("DevicePolicy", 4, RegistryValueKind.DWord);
                            if (curMask <= uint.MaxValue)
                            {
                                affKey.SetValue("AssignmentSetOverride", (int)curMask, RegistryValueKind.DWord);
                            }
                            else
                            {
                                affKey.SetValue("AssignmentSetOverride", (long)curMask, RegistryValueKind.QWord);
                            }
                        }
                        else
                        {
                            try { affKey.DeleteValue("DevicePolicy"); } catch { }
                            try { affKey.DeleteValue("AssignmentSetOverride"); } catch { }
                        }
                    }

                    // 直近状態を更新（※dev.LaunchMsi などの起動時スナップショットは永久不変で保持）
                    dev.OrigMsi = curMsi;
                    dev.OrigLimit = curLimit;
                    dev.OrigPriority = curPriority;
                    dev.OrigAffinityMask = curMask;
                    dev.OrigHasAffinityPolicy = hasCheckedCore;

                    modifiedCount++;
                }

                consoleContainer.Visible = true;
                btnExport.Visible = true;
                AdjustButtonPositions();

                var msgSb = new StringBuilder();
                msgSb.AppendLine($"Settings successfully applied to registry. (Modified: {modifiedCount} devices)");
                msgSb.AppendLine();
                if (!string.IsNullOrEmpty(autoBackupPath) && File.Exists(autoBackupPath))
                {
                    msgSb.AppendLine("[Safety Auto-Backup Created]");
                    msgSb.AppendLine("A rollback script before these changes was automatically saved to:");
                    msgSb.AppendLine(autoBackupPath);
                    msgSb.AppendLine("(Double-click this .reg file anytime to restore previous settings)");
                    msgSb.AppendLine();
                    lblStatus.Text = $"Applied ({modifiedCount} devices). Auto-Backup: {Path.GetFileName(autoBackupPath)}";
                }
                msgSb.AppendLine("A system reboot is required for hardware changes to take effect.");

                MessageBox.Show(
                    msgSb.ToString(),
                    "MSI mode utility",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to apply settings to registry:\r\n{ex.Message}\r\n\r\nPlease ensure the utility is running as Administrator.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
