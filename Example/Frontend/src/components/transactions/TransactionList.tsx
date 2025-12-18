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
import AddIcon from '@mui/icons-material/Add';
import { Transaction } from '../../types';
import { transactionService } from '../../api';
import { useUser } from '../../context/UserContext';
import TransactionDialog from './TransactionDialog';
import DeleteConfirmDialog from '../common/DeleteConfirmDialog';

/**
 * TransactionList component displays a list of transactions in a data grid.
 * Provides functionality to create, edit, and delete transactions.
 * Filters transactions based on the currently impersonated user.
 * Supports advanced features like sorting, filtering, and pagination.
 */
const TransactionList: React.FC = () => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const { currentUser } = useUser();

  // Load transactions when component mounts or user changes
  useEffect(() => {
    if (currentUser) {
      loadTransactions();
    } else {
      setTransactions([]);
    }
  }, [currentUser]);

  /**
   * Load transactions for the current user from the API.
   */
  const loadTransactions = async () => {
    if (!currentUser) return;

    setLoading(true);
    setError(null);
    try {
      const data = await transactionService.getByUserId(currentUser.id);
      setTransactions(data);
    } catch (err) {
      setError('Failed to load transactions. Please try again.');
      console.error('Error loading transactions:', err);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Handle opening the create transaction dialog.
   */
  const handleCreate = () => {
    setSelectedTransaction(null);
    setDialogOpen(true);
  };

  /**
   * Handle opening the edit transaction dialog.
   */
  const handleEdit = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
    setDialogOpen(true);
  };

  /**
   * Handle opening the delete confirmation dialog.
   */
  const handleDeleteClick = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
    setDeleteDialogOpen(true);
  };

  /**
   * Handle confirming transaction deletion.
   */
  const handleDeleteConfirm = async () => {
    if (!selectedTransaction) return;

    try {
      await transactionService.delete(selectedTransaction.id);
      setSuccess('Transaction deleted successfully');
      await loadTransactions();
    } catch (err) {
      setError('Failed to delete transaction. Please try again.');
      console.error('Error deleting transaction:', err);
    } finally {
      setDeleteDialogOpen(false);
      setSelectedTransaction(null);
    }
  };

  /**
   * Handle saving a transaction (create or update).
   */
  const handleSave = async () => {
    setDialogOpen(false);
    await loadTransactions();
    setSuccess(selectedTransaction ? 'Transaction updated successfully' : 'Transaction created successfully');
  };

  /**
   * Format currency value for display.
   */
  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(value);
  };

  /**
   * Format date for display.
   */
  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  // Define columns for the data grid
  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 70 },
    {
      field: 'recorded',
      headerName: 'Date',
      width: 120,
      valueFormatter: (value) => formatDate(value),
    },
    { field: 'description', headerName: 'Description', width: 250, flex: 1 },
    {
      field: 'credit',
      headerName: 'Credit',
      width: 120,
      type: 'number',
      valueFormatter: (value) => value > 0 ? formatCurrency(value) : '-',
    },
    {
      field: 'debit',
      headerName: 'Debit',
      width: 120,
      type: 'number',
      valueFormatter: (value) => value > 0 ? formatCurrency(value) : '-',
    },
    {
      field: 'category',
      headerName: 'Category',
      width: 150,
      valueGetter: (_value, row) => row.category?.name || 'N/A',
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Actions',
      width: 100,
      getActions: (params) => [
        <GridActionsCellItem
          key="edit"
          icon={<EditIcon />}
          label="Edit"
          onClick={() => handleEdit(params.row as Transaction)}
          showInMenu={false}
        />,
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon />}
          label="Delete"
          onClick={() => handleDeleteClick(params.row as Transaction)}
          showInMenu={false}
        />,
      ],
    },
  ];

  if (!currentUser) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Please select a user to impersonate from the Users page to manage transactions.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4" component="h1">
          Transactions
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleCreate}
        >
          Add Transaction
        </Button>
      </Box>

      <Alert severity="info" sx={{ mb: 2 }}>
        Managing transactions for: {currentUser.name} {currentUser.surname}
      </Alert>

      <Paper sx={{ height: 600, width: '100%' }}>
        <DataGrid
          rows={transactions}
          columns={columns}
          loading={loading}
          pageSizeOptions={[5, 10, 25, 50]}
          initialState={{
            pagination: { paginationModel: { pageSize: 10 } },
            sorting: { sortModel: [{ field: 'recorded', sort: 'desc' }] },
          }}
          disableRowSelectionOnClick
        />
      </Paper>

      <TransactionDialog
        open={dialogOpen}
        transaction={selectedTransaction}
        userId={currentUser.id}
        onClose={() => setDialogOpen(false)}
        onSave={handleSave}
      />

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        title="Delete Transaction"
        message={`Are you sure you want to delete this transaction?`}
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

export default TransactionList;

