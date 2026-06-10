import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { appTenantService } from '../../api';
import { CreateTenantRequest, TenantSummary } from '../../types';

const initialTenantForm: CreateTenantRequest = {
  name: '',
  legalOwnerName: '',
  legalOwnerSurname: '',
  legalOwnerEmail: '',
  legalOwnerPassword: '',
};

const GlobalTenantManagementPage: React.FC = () => {
  const [tenants, setTenants] = useState<TenantSummary[]>([]);
  const [form, setForm] = useState<CreateTenantRequest>(initialTenantForm);
  const [deleteTarget, setDeleteTarget] = useState<TenantSummary | null>(null);
  const [confirmationName, setConfirmationName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const loadTenants = async () => {
    try {
      const data = await appTenantService.getAll();
      setTenants(data);
    } catch {
      setError('Failed to load tenants.');
    }
  };

  useEffect(() => {
    loadTenants();
  }, []);

  const updateField = (field: keyof CreateTenantRequest, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleCreateTenant = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);

    try {
      await appTenantService.create(form);
      setSuccess('Tenant created successfully.');
      setForm(initialTenantForm);
      await loadTenants();
    } catch {
      setError('Failed to create tenant.');
    }
  };

  const handleDeleteTenant = async () => {
    if (!deleteTarget) {
      return;
    }

    try {
      await appTenantService.delete(deleteTarget.id, confirmationName);
      setSuccess('Tenant deleted successfully.');
      setDeleteTarget(null);
      setConfirmationName('');
      await loadTenants();
    } catch {
      setError('Failed to delete tenant. Make sure the confirmation name matches.');
    }
  };

  const columns: GridColDef[] = [
    { field: 'name', headerName: 'Tenant', flex: 1.1, minWidth: 220 },
    { field: 'legalOwnerEmail', headerName: 'Legal Owner', flex: 1.3, minWidth: 240 },
    { field: 'activeUsersCount', headerName: 'Active Users', width: 130 },
    { field: 'categoriesCount', headerName: 'Categories', width: 120 },
    { field: 'transactionsCount', headerName: 'Transactions', width: 130 },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 130,
      sortable: false,
      renderCell: (params) => (
        <Button color="error" onClick={() => setDeleteTarget(params.row)}>
          Delete
        </Button>
      ),
    },
  ];

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 2 }}>Application Tenant Administration</Typography>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Typography variant="h6" sx={{ mb: 1 }}>Create Tenant</Typography>
        <Box component="form" onSubmit={handleCreateTenant}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mb: 1 }}>
            <TextField label="Tenant Name" required value={form.name} onChange={(e) => updateField('name', e.target.value)} fullWidth />
            <TextField label="Owner First Name" required value={form.legalOwnerName} onChange={(e) => updateField('legalOwnerName', e.target.value)} fullWidth />
            <TextField label="Owner Surname" required value={form.legalOwnerSurname} onChange={(e) => updateField('legalOwnerSurname', e.target.value)} fullWidth />
          </Stack>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1}>
            <TextField label="Owner Email" required type="email" value={form.legalOwnerEmail} onChange={(e) => updateField('legalOwnerEmail', e.target.value)} fullWidth />
            <TextField label="Owner Password" required type="password" value={form.legalOwnerPassword} onChange={(e) => updateField('legalOwnerPassword', e.target.value)} fullWidth />
            <Button type="submit" variant="contained" sx={{ minWidth: 180 }}>Create Tenant</Button>
          </Stack>
        </Box>
      </Paper>

      <Paper sx={{ height: 560, p: 1 }}>
        <DataGrid rows={tenants} columns={columns} disableRowSelectionOnClick />
      </Paper>

      <Dialog open={!!deleteTarget} onClose={() => setDeleteTarget(null)}>
        <DialogTitle>Delete Tenant</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 1 }}>
            This will permanently delete all users, categories, transactions, invites, and requests for this tenant.
          </Typography>
          <Typography sx={{ mb: 2 }}>
            Type <strong>{deleteTarget?.name}</strong> to confirm.
          </Typography>
          <TextField
            fullWidth
            label="Tenant name confirmation"
            value={confirmationName}
            onChange={(e) => setConfirmationName(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="error" variant="contained" onClick={handleDeleteTenant}>Delete</Button>
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

export default GlobalTenantManagementPage;
