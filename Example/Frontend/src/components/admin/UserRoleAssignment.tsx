import React, { useEffect, useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Alert,
  Snackbar,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Checkbox,
  FormControlLabel,
  FormGroup,
  Tooltip,
} from '@mui/material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import EditIcon from '@mui/icons-material/Edit';
import { User, RoleDto } from '../../types';
import { roleService } from '../../api';

const UserRoleAssignment: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [selectedRoleNames, setSelectedRoleNames] = useState<string[]>([]);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [usersData, rolesData] = await Promise.all([
        roleService.getUsersWithRoles(),
        roleService.getAll(),
      ]);
      setUsers(usersData);
      setRoles(rolesData);
    } catch {
      setError('Failed to load users and roles.');
    } finally {
      setLoading(false);
    }
  };

  const handleEditRoles = (user: User) => {
    setSelectedUser(user);
    setSelectedRoleNames(user.roles || []);
    setDialogOpen(true);
  };

  const handleToggleRole = (roleName: string) => {
    setSelectedRoleNames(prev =>
      prev.includes(roleName) ? prev.filter(n => n !== roleName) : [...prev, roleName]
    );
  };

  const handleSaveRoles = async () => {
    if (!selectedUser) return;
    try {
      await roleService.assignRolesToUser(selectedUser.id, selectedRoleNames);
      setSuccess(`Roles updated for ${selectedUser.name} ${selectedUser.surname}`);
      setDialogOpen(false);
      await loadData();
    } catch {
      setError('Failed to assign roles.');
    }
  };

  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 70 },
    { field: 'name', headerName: 'Name', width: 150, flex: 1 },
    { field: 'surname', headerName: 'Surname', width: 150, flex: 1 },
    { field: 'email', headerName: 'Email', width: 200, flex: 1 },
    {
      field: 'roles',
      headerName: 'Roles',
      width: 250,
      flex: 1,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', py: 0.5 }}>
          {(params.row.roles || []).map((r: string) => (
            <Chip key={r} label={r} size="small" color="primary" variant="outlined" />
          ))}
          {(!params.row.roles || params.row.roles.length === 0) && (
            <Typography variant="body2" color="text.secondary">No roles</Typography>
          )}
        </Box>
      ),
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 100,
      getActions: (params) => [
        <GridActionsCellItem
          key="edit"
          icon={<Tooltip title="Assign roles to this user"><EditIcon /></Tooltip>}
          label="Edit Roles"
          onClick={() => handleEditRoles(params.row as User)}
        />,
      ],
    },
  ];

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2 }}>
        <Typography variant="h4" component="h1">User Role Assignment</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          Assign roles to users. Only roles can be assigned directly — permissions are managed through roles.
        </Typography>
      </Box>

      <Paper sx={{ height: 500, width: '100%' }}>
        <DataGrid
          rows={users}
          columns={columns}
          loading={loading}
          pageSizeOptions={[5, 10, 25]}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          disableRowSelectionOnClick
          getRowHeight={() => 'auto'}
        />
      </Paper>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Assign Roles to {selectedUser?.name} {selectedUser?.surname}</DialogTitle>
        <DialogContent>
          <FormGroup>
            {roles.map(r => (
              <FormControlLabel
                key={r.name}
                control={
                  <Checkbox
                    checked={selectedRoleNames.includes(r.name)}
                    onChange={() => handleToggleRole(r.name)}
                  />
                }
                label={r.name}
              />
            ))}
          </FormGroup>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSaveRoles} variant="contained">Save</Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={!!error} autoHideDuration={6000} onClose={() => setError(null)}>
        <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>
      </Snackbar>
      <Snackbar open={!!success} autoHideDuration={3000} onClose={() => setSuccess(null)}>
        <Alert severity="success" onClose={() => setSuccess(null)}>{success}</Alert>
      </Snackbar>
    </Box>
  );
};

export default UserRoleAssignment;
