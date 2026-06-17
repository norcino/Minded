import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  MenuItem,
  Paper,
  Snackbar,
  TextField,
  Typography,
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { tenantAdminService } from '../../api';
import { TenantInvite, TenantJoinRequest, User } from '../../types';
import { useUser } from '../../context/UserContext';

const TenantAdminPage: React.FC = () => {
  const { currentUser } = useUser();
  const [users, setUsers] = useState<User[]>([]);
  const [joinRequests, setJoinRequests] = useState<TenantJoinRequest[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [lastInvite, setLastInvite] = useState<TenantInvite | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const loadUsers = async () => {
    try {
      const data = await tenantAdminService.getUsers();
      setUsers(data);
    } catch {
      setError('Failed to load tenant users.');
    }
  };

  useEffect(() => {
    loadUsers();
    loadJoinRequests();
  }, []);

  const loadJoinRequests = async () => {
    try {
      const data = await tenantAdminService.getJoinRequests();
      setJoinRequests(data);
    } catch {
      setError('Failed to load pending join requests.');
    }
  };

  const handleCreateInvite = async () => {
    try {
      const invite = await tenantAdminService.createInvite(inviteEmail || undefined);
      setLastInvite(invite);
      setInviteEmail('');
      setSuccess('Invite created.');
    } catch {
      setError('Failed to create invite.');
    }
  };

  const handleRoleChange = async (userId: number, role: string) => {
    try {
      await tenantAdminService.updateUserRole(userId, role);
      setSuccess('Role updated.');
      await loadUsers();
    } catch {
      setError('Failed to update role.');
    }
  };

  const handleRemoveUser = async (userId: number) => {
    try {
      await tenantAdminService.removeUser(userId);
      setSuccess('User removed.');
      await loadUsers();
    } catch {
      setError('Failed to remove user.');
    }
  };

  const handleApprove = async (requestId: number) => {
    try {
      await tenantAdminService.approveJoinRequest(requestId);
      setSuccess('Join request approved.');
      await Promise.all([loadUsers(), loadJoinRequests()]);
    } catch {
      setError('Failed to approve join request.');
    }
  };

  const handleReject = async (requestId: number) => {
    try {
      await tenantAdminService.rejectJoinRequest(requestId);
      setSuccess('Join request rejected.');
      await loadJoinRequests();
    } catch {
      setError('Failed to reject join request.');
    }
  };

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Name', flex: 1, minWidth: 130 },
    { field: 'surname', headerName: 'Surname', flex: 1, minWidth: 130 },
    { field: 'email', headerName: 'Email', flex: 1.5, minWidth: 220 },
    {
      field: 'tenantRole',
      headerName: 'Tenant Role',
      flex: 1,
      minWidth: 160,
      renderCell: (params) => (
        <TextField
          select
          size="small"
          value={params.row.tenantRole || 'Member'}
          onChange={(e) => handleRoleChange(params.row.id, e.target.value)}
          disabled={params.row.id === currentUser?.id}
          sx={{ minWidth: 130 }}
        >
          <MenuItem value="Owner">Owner</MenuItem>
          <MenuItem value="Admin">Admin</MenuItem>
          <MenuItem value="Member">Member</MenuItem>
        </TextField>
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      minWidth: 150,
      renderCell: (params) => (
        <Button
          color="error"
          onClick={() => handleRemoveUser(params.row.id)}
          disabled={params.row.id === currentUser?.id}
        >
          Remove
        </Button>
      ),
    },
  ];

  const joinRequestColumns: GridColDef[] = [
    { field: 'name', headerName: 'Name', flex: 1, minWidth: 120 },
    { field: 'surname', headerName: 'Surname', flex: 1, minWidth: 120 },
    { field: 'email', headerName: 'Email', flex: 1.5, minWidth: 220 },
    {
      field: 'createdAtUtc',
      headerName: 'Requested At',
      flex: 1,
      minWidth: 180,
      valueFormatter: (value) => new Date(value).toLocaleString(),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      minWidth: 220,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button size="small" variant="contained" onClick={() => handleApprove(params.row.id)}>Approve</Button>
          <Button size="small" color="error" variant="outlined" onClick={() => handleReject(params.row.id)}>Reject</Button>
        </Box>
      ),
    },
  ];

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 2 }}>Tenant Admin</Typography>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Typography variant="h6" sx={{ mb: 1 }}>Create Invite</Typography>
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
          <TextField
            label="Invitee email (optional)"
            value={inviteEmail}
            onChange={(e) => setInviteEmail(e.target.value)}
            sx={{ minWidth: 300 }}
          />
          <Button variant="contained" onClick={handleCreateInvite}>Generate Invite</Button>
        </Box>
        {lastInvite && (
          <Box sx={{ mt: 2 }}>
            <Alert severity="info" sx={{ mb: 1 }}>
              Share this invite link: {lastInvite.inviteLink}
            </Alert>
            <Chip label={`Code: ${lastInvite.code}`} color="primary" />
          </Box>
        )}
      </Paper>

      <Paper sx={{ height: 540, p: 1 }}>
        <DataGrid rows={users} columns={columns} disableRowSelectionOnClick />
      </Paper>

      <Divider sx={{ my: 3 }} />

      <Typography variant="h6" sx={{ mb: 1 }}>Pending Join Requests</Typography>
      <Paper sx={{ height: 420, p: 1 }}>
        <DataGrid rows={joinRequests} columns={joinRequestColumns} disableRowSelectionOnClick />
      </Paper>

      <Snackbar open={!!error} autoHideDuration={6000} onClose={() => setError(null)}>
        <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>
      </Snackbar>

      <Snackbar open={!!success} autoHideDuration={3000} onClose={() => setSuccess(null)}>
        <Alert severity="success" onClose={() => setSuccess(null)}>{success}</Alert>
      </Snackbar>
    </Box>
  );
};

export default TenantAdminPage;
