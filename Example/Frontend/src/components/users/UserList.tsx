import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Paper,
  Typography,
  Alert,
  Snackbar,
} from '@mui/material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import PersonIcon from '@mui/icons-material/Person';
import AddIcon from '@mui/icons-material/Add';
import { User } from '../../types';
import { userService } from '../../api';
import { useUser } from '../../context/UserContext';
import UserDialog from './UserDialog';
import DeleteConfirmDialog from '../common/DeleteConfirmDialog';

/**
 * UserList component displays a list of users in a data grid.
 * Provides functionality to create, edit, delete, and impersonate users.
 * Uses MUI DataGrid for advanced features like sorting and filtering.
 */
const UserList: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const { setCurrentUser, currentUser } = useUser();

  // Load users on component mount
  useEffect(() => {
    loadUsers();
  }, []);

  /**
   * Load all users from the API.
   */
  const loadUsers = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await userService.getAll();
      setUsers(data);
    } catch (err) {
      setError('Failed to load users. Please try again.');
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Handle opening the create user dialog.
   */
  const handleCreate = () => {
    setSelectedUser(null);
    setDialogOpen(true);
  };

  /**
   * Handle opening the edit user dialog.
   */
  const handleEdit = (user: User) => {
    setSelectedUser(user);
    setDialogOpen(true);
  };

  /**
   * Handle opening the delete confirmation dialog.
   */
  const handleDeleteClick = (user: User) => {
    setSelectedUser(user);
    setDeleteDialogOpen(true);
  };

  /**
   * Handle confirming user deletion.
   */
  const handleDeleteConfirm = async () => {
    if (!selectedUser) return;

    try {
      await userService.delete(selectedUser.id);
      setSuccess('User deleted successfully');
      // If deleted user was impersonated, clear impersonation
      if (currentUser?.id === selectedUser.id) {
        setCurrentUser(null);
      }
      await loadUsers();
    } catch (err) {
      setError('Failed to delete user. Please try again.');
      console.error('Error deleting user:', err);
    } finally {
      setDeleteDialogOpen(false);
      setSelectedUser(null);
    }
  };

  /**
   * Handle impersonating a user.
   */
  const handleImpersonate = (user: User) => {
    setCurrentUser(user);
    setSuccess(`Now impersonating ${user.name} ${user.surname}`);
  };

  /**
   * Handle saving a user (create or update).
   */
  const handleSave = async () => {
    setDialogOpen(false);
    await loadUsers();
    setSuccess(selectedUser ? 'User updated successfully' : 'User created successfully');
  };

  // Define columns for the data grid
  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 70 },
    { field: 'name', headerName: 'Name', width: 150, flex: 1 },
    { field: 'surname', headerName: 'Surname', width: 150, flex: 1 },
    { field: 'email', headerName: 'Email', width: 200, flex: 1 },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 150,
      getActions: (params) => [
        <GridActionsCellItem
          key="impersonate"
          icon={<PersonIcon />}
          label="Impersonate"
          onClick={() => handleImpersonate(params.row as User)}
          showInMenu={false}
        />,
        <GridActionsCellItem
          key="edit"
          icon={<EditIcon />}
          label="Edit"
          onClick={() => handleEdit(params.row as User)}
          showInMenu={false}
        />,
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon />}
          label="Delete"
          onClick={() => handleDeleteClick(params.row as User)}
          showInMenu={false}
        />,
      ],
    },
  ];

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          Users
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleCreate}
        >
          Add User
        </Button>
      </Box>

      {currentUser && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Currently impersonating: {currentUser.name} {currentUser.surname} ({currentUser.email})
        </Alert>
      )}

      <Paper sx={{ height: 600, width: '100%' }}>
        <DataGrid
          rows={users}
          columns={columns}
          loading={loading}
          pageSizeOptions={[5, 10, 25, 50]}
          initialState={{
            pagination: { paginationModel: { pageSize: 10 } },
          }}
          disableRowSelectionOnClick
        />
      </Paper>

      <UserDialog
        open={dialogOpen}
        user={selectedUser}
        onClose={() => setDialogOpen(false)}
        onSave={handleSave}
      />

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        title="Delete User"
        message={`Are you sure you want to delete ${selectedUser?.name} ${selectedUser?.surname}?`}
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteDialogOpen(false)}
      />

      <Snackbar
        open={!!error}
        autoHideDuration={6000}
        onClose={() => setError(null)}
      >
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      </Snackbar>

      <Snackbar
        open={!!success}
        autoHideDuration={3000}
        onClose={() => setSuccess(null)}
      >
        <Alert severity="success" onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default UserList;

