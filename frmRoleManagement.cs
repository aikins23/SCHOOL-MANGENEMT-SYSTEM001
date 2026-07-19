using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using kingdom_Preparatory_School_Management_System.Common;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// UI for Directors and Administrators to manage roles and their permissions dynamically.
    /// System roles cannot be deleted but their permissions can be adjusted.
    /// Custom roles can be created for school-specific positions.
    /// </summary>
    public class frmRoleManagement : Form
    {
        private readonly IPermissionRepository _repo;
        private ListBox lstRoles;
        private TreeView tvPermissions;
        private TextBox txtRoleName;
        private TextBox txtRoleDescription;
        private Button btnNewRole;
        private Button btnSaveRole;
        private Button btnDeleteRole;
        private Button btnSavePermissions;
        private Label lblStatus;
        private List<Role> _roles = new List<Role>();
        private List<Permission> _allPermissions = new List<Permission>();
        private Role _selectedRole;

        public frmRoleManagement() : this(new PermissionRepository()) { }

        public frmRoleManagement(IPermissionRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            InitializeComponent();
            this.Load += async (s, e) => await LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Role & Permission Management";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = Color.White;

            var lblTitle = new Label
            {
                Text = "Roles & Permissions",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(400, 30)
            };
            this.Controls.Add(lblTitle);

            var lblRoles = new Label
            {
                Text = "Roles",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(20, 55),
                Size = new Size(300, 22)
            };
            this.Controls.Add(lblRoles);

            lstRoles = new ListBox
            {
                Location = new Point(20, 80),
                Size = new Size(280, 350),
                Font = new Font("Segoe UI", 9F)
            };
            lstRoles.SelectedIndexChanged += LstRoles_SelectedIndexChanged;
            this.Controls.Add(lstRoles);

            btnNewRole = new Button
            {
                Text = "+ New Role",
                Location = new Point(20, 440),
                Size = new Size(135, 32),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNewRole.Click += BtnNewRole_Click;
            this.Controls.Add(btnNewRole);

            btnDeleteRole = new Button
            {
                Text = "Delete",
                Location = new Point(165, 440),
                Size = new Size(135, 32),
                BackColor = Color.FromArgb(198, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDeleteRole.Click += BtnDeleteRole_Click;
            this.Controls.Add(btnDeleteRole);

            var lblName = new Label
            {
                Text = "Role Name:",
                Location = new Point(20, 485),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblName);
            txtRoleName = new TextBox
            {
                Location = new Point(20, 508),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(txtRoleName);

            var lblDesc = new Label
            {
                Text = "Description:",
                Location = new Point(20, 540),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblDesc);
            txtRoleDescription = new TextBox
            {
                Location = new Point(20, 563),
                Size = new Size(280, 60),
                Multiline = true,
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(txtRoleDescription);

            btnSaveRole = new Button
            {
                Text = "Save Role Info",
                Location = new Point(20, 630),
                Size = new Size(280, 32),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSaveRole.Click += BtnSaveRole_Click;
            this.Controls.Add(btnSaveRole);

            var lblPerms = new Label
            {
                Text = "Permissions for Selected Role",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(320, 55),
                Size = new Size(400, 22)
            };
            this.Controls.Add(lblPerms);

            tvPermissions = new TreeView
            {
                Location = new Point(320, 80),
                Size = new Size(740, 540),
                CheckBoxes = true,
                Font = new Font("Segoe UI", 9F),
                ShowLines = true,
                ShowPlusMinus = true
            };
            tvPermissions.AfterCheck += TvPermissions_AfterCheck;
            this.Controls.Add(tvPermissions);

            btnSavePermissions = new Button
            {
                Text = "Save Permissions",
                Location = new Point(320, 630),
                Size = new Size(200, 32),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSavePermissions.Click += BtnSavePermissions_Click;
            this.Controls.Add(btnSavePermissions);

            lblStatus = new Label
            {
                Location = new Point(540, 635),
                Size = new Size(520, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblStatus);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _allPermissions = await _repo.GetAllPermissionsAsync();
                _roles = await _repo.GetAllRolesAsync();
                RefreshRoleList();
                BuildPermissionTree();
                SetStatus("Loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load roles/permissions: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshRoleList()
        {
            lstRoles.Items.Clear();
            foreach (var r in _roles)
            {
                string tag = r.IsSystemRole ? " (System)" : " (Custom)";
                lstRoles.Items.Add(r.Name + tag);
            }
        }

        private void BuildPermissionTree()
        {
            tvPermissions.Nodes.Clear();
            var modules = _allPermissions.GroupBy(p => p.Module).OrderBy(g => g.Key);
            foreach (var mod in modules)
            {
                var modNode = new TreeNode(mod.Key) { Tag = "MODULE" };
                modNode.NodeFont = new Font("Segoe UI", 9F, FontStyle.Bold);
                foreach (var perm in mod.OrderBy(p => p.Category).ThenBy(p => p.Name))
                {
                    var label = $"[{perm.Category}] {perm.Name}";
                    var permNode = new TreeNode(label) { Tag = perm };
                    if (!string.IsNullOrEmpty(perm.Description))
                        permNode.ToolTipText = perm.Description;
                    modNode.Nodes.Add(permNode);
                }
                tvPermissions.Nodes.Add(modNode);
            }
            tvPermissions.ExpandAll();
        }

        private async void LstRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRoles.SelectedIndex < 0 || lstRoles.SelectedIndex >= _roles.Count) return;
            _selectedRole = _roles[lstRoles.SelectedIndex];
            txtRoleName.Text = _selectedRole.Name;
            txtRoleDescription.Text = _selectedRole.Description ?? string.Empty;
            txtRoleName.ReadOnly = _selectedRole.IsSystemRole;
            btnDeleteRole.Enabled = !_selectedRole.IsSystemRole;
            await LoadRolePermissionsAsync(_selectedRole.RoleId);
        }

        private async Task LoadRolePermissionsAsync(int roleId)
        {
            try
            {
                var assigned = await _repo.GetRolePermissionsAsync(roleId);
                var assignedIds = new HashSet<int>(assigned.Select(p => p.PermissionId));
                foreach (TreeNode modNode in tvPermissions.Nodes)
                {
                    foreach (TreeNode permNode in modNode.Nodes)
                    {
                        if (permNode.Tag is Permission p)
                            permNode.Checked = assignedIds.Contains(p.PermissionId);
                    }
                }
                SetStatus($"Showing permissions for '{_selectedRole.Name}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load role permissions: " + ex.Message);
            }
        }

        private void TvPermissions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;
            if (e.Node.Tag is string s && s == "MODULE")
            {
                foreach (TreeNode child in e.Node.Nodes)
                    child.Checked = e.Node.Checked;
            }
        }

        private void BtnNewRole_Click(object sender, EventArgs e)
        {
            _selectedRole = null;
            lstRoles.ClearSelected();
            txtRoleName.Text = string.Empty;
            txtRoleDescription.Text = string.Empty;
            txtRoleName.ReadOnly = false;
            foreach (TreeNode modNode in tvPermissions.Nodes)
                foreach (TreeNode permNode in modNode.Nodes)
                    permNode.Checked = false;
            txtRoleName.Focus();
            SetStatus("Creating new role - enter name and click Save Role Info");
        }

        private async void BtnSaveRole_Click(object sender, EventArgs e)
        {
            var name = txtRoleName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Role name is required.");
                return;
            }
            try
            {
                if (_selectedRole == null)
                {
                    var role = new Role
                    {
                        Name = name,
                        Description = txtRoleDescription.Text.Trim(),
                        IsActive = true,
                        IsSystemRole = false,
                        CreatedBy = AuthService.CurrentUser?.Username ?? "SYSTEM"
                    };
                    role.RoleId = await _repo.CreateRoleAsync(role);
                    _roles.Add(role);
                    _selectedRole = role;
                    SetStatus($"Created role '{name}'");
                }
                else
                {
                    _selectedRole.Name = name;
                    _selectedRole.Description = txtRoleDescription.Text.Trim();
                    await _repo.UpdateRoleAsync(_selectedRole);
                    SetStatus($"Updated role '{name}'");
                }
                RefreshRoleList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save role: " + ex.Message);
            }
        }

        private async void BtnDeleteRole_Click(object sender, EventArgs e)
        {
            if (_selectedRole == null) return;
            if (_selectedRole.IsSystemRole)
            {
                MessageBox.Show("System roles cannot be deleted.");
                return;
            }
            var result = MessageBox.Show($"Delete role '{_selectedRole.Name}'? Users assigned to this role will lose its permissions.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
            try
            {
                await _repo.DeleteRoleAsync(_selectedRole.RoleId);
                _roles.Remove(_selectedRole);
                _selectedRole = null;
                RefreshRoleList();
                SetStatus("Role deleted.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to delete role: " + ex.Message);
            }
        }

        private async void BtnSavePermissions_Click(object sender, EventArgs e)
        {
            if (_selectedRole == null)
            {
                MessageBox.Show("Select a role first.");
                return;
            }
            try
            {
                var selectedPermissionIds = new List<int>();
                foreach (TreeNode modNode in tvPermissions.Nodes)
                    foreach (TreeNode permNode in modNode.Nodes)
                        if (permNode.Checked && permNode.Tag is Permission p)
                            selectedPermissionIds.Add(p.PermissionId);

                var who = AuthService.CurrentUser?.Username ?? "SYSTEM";
                await _repo.SetRolePermissionsAsync(_selectedRole.RoleId, selectedPermissionIds, who);
                DynamicPermissionService.ClearCache();
                SetStatus($"Saved {selectedPermissionIds.Count} permissions for '{_selectedRole.Name}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save permissions: " + ex.Message);
            }
        }

        private void SetStatus(string msg)
        {
            lblStatus.Text = msg;
        }
    }
}
