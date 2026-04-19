import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Paper,
  Typography,
  Alert,
  Snackbar,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Checkbox,
  FormControlLabel,
  FormGroup,
  Tooltip,
  Divider,
} from '@mui/material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import DeleteIcon from '@mui/icons-material/Delete';
import SecurityIcon from '@mui/icons-material/Security';
import AddIcon from '@mui/icons-material/Add';
import RestoreIcon from '@mui/icons-material/Restore';
import { RoleDto, PermissionGroups } from '../../types';
import { roleService } from '../../api';
import DeleteConfirmDialog from '../common/DeleteConfirmDialog';

// Protected permissions that cannot be removed from the Admin role
const PROTECTED_ADMIN_PERMISSIONS = [
  'CanManageRoles', 'CanAssignRoles', 'CanCreateRole', 'CanDeleteRole', 'CanUpdateRolePermissions',
];

const ADMIN_ROLE = 'Admin';

const RoleManagement: React.FC = () => {
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissionGroups, setPermissionGroups] = useState<PermissionGroups>({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [roleDialogOpen, setRoleDialogOpen] = useState(false);
  const [roleName, setRoleName] = useState('');

  const [permDialogOpen, setPermDialogOpen] = useState(false);
  const [permRole, setPermRole] = useState<RoleDto | null>(null);
  const [selectedPermNames, setSelectedPermNames] = useState<string[]>([]);

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteRole, setDeleteRole] = useState<RoleDto | null>(null);

  useEffect(() => { loadData(); }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [rolesData, permsData] = await Promise.all([
        roleService.getAll(),
        roleService.getPermissions(),
      ]);
      setRoles(rolesData);
      setPermissionGroups(permsData);
    } catch {
      setError('Failed to load roles and permissions.');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateRole = () => { setRoleName(''); setRoleDialogOpen(true); };

  const handleSaveRole = async () => {
    try {
      await roleService.create(roleName);
      setSuccess('Role created successfully');
      setRoleDialogOpen(false);
      await loadData();
    } catch { setError('Failed to create role.'); }
  };

  const handleDeleteClick = (role: RoleDto) => { setDeleteRole(role); setDeleteDialogOpen(true); };

  const handleDeleteConfirm = async () => {
    if (!deleteRole) return;
    try {
      await roleService.delete(deleteRole.name);
      setSuccess('Role deleted successfully');
      await loadData();
    } catch { setError('Failed to delete role.'); }
    finally { setDeleteDialogOpen(false); setDeleteRole(null); }
  };

  const handleEditPermissions = (role: RoleDto) => {
    setPermRole(role);
    setSelectedPermNames(role.permissions || []);
    setPermDialogOpen(true);
  };

  const isPermissionProtected = (permName: string) => {
    return permRole?.name === ADMIN_ROLE && PROTECTED_ADMIN_PERMISSIONS.includes(permName);
  };

  const handleTogglePermission = (permName: string) => {
    if (isPermissionProtected(permName)) return;
    setSelectedPermNames(prev =>
      prev.includes(permName) ? prev.filter(n => n !== permName) : [...prev, permName]
    );
  };

  const handleToggleGroup = (groupPerms: string[]) => {
    const toggleablePerms = permRole?.name === ADMIN_ROLE
      ? groupPerms.filter(p => !PROTECTED_ADMIN_PERMISSIONS.includes(p))
      : groupPerms;
    if (toggleablePerms.length === 0) return;
    const allSelected = toggleablePerms.every(p => selectedPermNames.includes(p));
    if (allSelected) {
      setSelectedPermNames(prev => prev.filter(p => !toggleablePerms.includes(p)));
    } else {
      setSelectedPermNames(prev => [...new Set([...prev, ...toggleablePerms])]);
    }
  };

  const handleSavePermissions = async () => {
    if (!permRole) return;
    try {
      await roleService.updateRolePermissions(permRole.name, selectedPermNames);
      setSuccess('Permissions updated successfully');
      setPermDialogOpen(false);
      await loadData();
    } catch { setError('Failed to update permissions.'); }
  };

  const handleResetToDefault = async () => {
    try {
      await roleService.resetToDefault();
      setSuccess('Roles reset to default successfully');
      await loadData();
    } catch { setError('Failed to reset roles to default.'); }
  };

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', width: 150, flex: 1 },
    {
      field: 'permissions',
      headerName: 'Permissions',
      width: 400,
      flex: 3,
      renderCell: (params) => {
        const rolePerms: string[] = params.row.permissions || [];
        return (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5, py: 0.5 }}>
            {Object.entries(permissionGroups)
              .filter(([, perms]) => perms.some(p => rolePerms.includes(p)))
              .map(([group, perms]) => (
                <Box key={group} sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexWrap: 'wrap' }}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 'bold', minWidth: 90 }}>
                    {group}
                  </Typography>
                  {perms.filter(p => rolePerms.includes(p)).map(p => (
                    <Chip key={p} label={p} size="small" variant="outlined" />
                  ))}
                </Box>
              ))}
          </Box>
        );
      },
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 120,
      getActions: (params) => {
        const isAdmin = params.row.name === ADMIN_ROLE;
        return [
          <GridActionsCellItem
            key="permissions"
            icon={<Tooltip title="Manage permissions for this role"><SecurityIcon /></Tooltip>}
            label="Permissions"
            onClick={() => handleEditPermissions(params.row as RoleDto)}
          />,
          <GridActionsCellItem
            key="delete"
            icon={<Tooltip title={isAdmin ? "The Admin role cannot be deleted" : "Delete this role"}>
              <DeleteIcon color={isAdmin ? "disabled" : undefined} />
            </Tooltip>}
            label="Delete"
            onClick={() => handleDeleteClick(params.row as RoleDto)}
            disabled={isAdmin}
          />,
        ];
      },
    },
  ];

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" component="h1">Roles</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Manage roles and their permissions. Assign permissions to roles, then assign roles to users.
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button variant="outlined" startIcon={<RestoreIcon />} onClick={handleResetToDefault}>
            Reset to Default
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleCreateRole}>
            Add Role
          </Button>
        </Box>
      </Box>

      <Paper sx={{ height: 500, width: '100%' }}>
        <DataGrid
          rows={roles}
          columns={columns}
          loading={loading}
          pageSizeOptions={[5, 10, 25]}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          disableRowSelectionOnClick
          getRowHeight={() => 'auto'}
          getRowId={(row) => row.name}
        />
      </Paper>

      {/* Create Role Dialog */}
      <Dialog open={roleDialogOpen} onClose={() => setRoleDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create Role</DialogTitle>
        <DialogContent>
          <TextField autoFocus margin="dense" label="Name" fullWidth value={roleName} onChange={(e) => setRoleName(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRoleDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveRole} variant="contained" disabled={!roleName.trim()}>Save</Button>
        </DialogActions>
      </Dialog>

      {/* Permissions Dialog — grouped by category */}
      <Dialog open={permDialogOpen} onClose={() => setPermDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Permissions for {permRole?.name}</DialogTitle>
        <DialogContent>
          {Object.entries(permissionGroups).map(([group, perms], idx) => {
            const allSelected = perms.every(p => selectedPermNames.includes(p));
            const someSelected = perms.some(p => selectedPermNames.includes(p));
            return (
              <Box key={group}>
                {idx > 0 && <Divider sx={{ my: 1 }} />}
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={allSelected}
                      indeterminate={someSelected && !allSelected}
                      onChange={() => handleToggleGroup(perms)}
                    />
                  }
                  label={<Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{group}</Typography>}
                />
                <FormGroup sx={{ pl: 4 }}>
                  {perms.map(p => {
                    const isProtected = isPermissionProtected(p);
                    return (
                      <FormControlLabel
                        key={p}
                        control={<Checkbox checked={selectedPermNames.includes(p)} onChange={() => handleTogglePermission(p)} size="small" disabled={isProtected} />}
                        label={
                          <Typography variant="body2">
                            {p}{isProtected ? ' (protected)' : ''}
                          </Typography>
                        }
                      />
                    );
                  })}
                </FormGroup>
              </Box>
            );
          })}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPermDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSavePermissions} variant="contained">Save</Button>
        </DialogActions>
      </Dialog>

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        title="Delete Role"
        message={`Are you sure you want to delete the role "${deleteRole?.name}"?`}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteDialogOpen(false)}
      />

      <Snackbar open={!!error} autoHideDuration={6000} onClose={() => setError(null)}>
        <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>
      </Snackbar>
      <Snackbar open={!!success} autoHideDuration={3000} onClose={() => setSuccess(null)}>
        <Alert severity="success" onClose={() => setSuccess(null)}>{success}</Alert>
      </Snackbar>
    </Box>
  );
};

export default RoleManagement;
