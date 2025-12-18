import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  Alert,
  MenuItem,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { Transaction, TransactionFormData, Category } from '../../types';
import { transactionService, categoryService } from '../../api';

/**
 * Props for the TransactionDialog component.
 */
interface TransactionDialogProps {
  open: boolean;
  transaction: Transaction | null;
  userId: number;
  onClose: () => void;
  onSave: () => void;
}

/**
 * TransactionDialog component for creating and editing transactions.
 * Provides a form with validation for transaction data entry.
 * Includes date picker for transaction date and category dropdown.
 * 
 * @param props Component props
 * @returns Dialog component with transaction form
 */
const TransactionDialog: React.FC<TransactionDialogProps> = ({
  open,
  transaction,
  userId,
  onClose,
  onSave,
}) => {
  const [formData, setFormData] = useState<TransactionFormData>({
    recorded: new Date().toISOString(),
    credit: 0,
    debit: 0,
    description: '',
    categoryId: 0,
    userId: userId,
  });
  const [categories, setCategories] = useState<Category[]>([]);
  const [errors, setErrors] = useState<Partial<Record<keyof TransactionFormData, string>>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load categories when dialog opens
  useEffect(() => {
    if (open) {
      loadCategories();
    }
  }, [open, userId]);

  // Initialize form data when transaction changes
  useEffect(() => {
    if (transaction) {
      setFormData({
        recorded: transaction.recorded,
        credit: transaction.credit,
        debit: transaction.debit,
        description: transaction.description,
        categoryId: transaction.categoryId,
        userId: transaction.userId,
      });
    } else {
      setFormData({
        recorded: new Date().toISOString(),
        credit: 0,
        debit: 0,
        description: '',
        categoryId: categories.length > 0 ? categories[0].id : 0,
        userId: userId,
      });
    }
    setErrors({});
    setError(null);
  }, [transaction, userId, open]);

  /**
   * Load categories for the current user.
   */
  const loadCategories = async () => {
    try {
      const data = await categoryService.getByUserId(userId);
      setCategories(data.filter(c => c.active));
    } catch (err) {
      console.error('Error loading categories:', err);
      setError('Failed to load categories');
    }
  };

  /**
   * Validate form data.
   * @returns True if form is valid, false otherwise
   */
  const validate = (): boolean => {
    const newErrors: Partial<Record<keyof TransactionFormData, string>> = {};

    if (!formData.description.trim()) {
      newErrors.description = 'Description is required';
    }

    if (formData.credit < 0) {
      newErrors.credit = 'Credit cannot be negative';
    }

    if (formData.debit < 0) {
      newErrors.debit = 'Debit cannot be negative';
    }

    if (formData.credit === 0 && formData.debit === 0) {
      newErrors.credit = 'Either credit or debit must be greater than 0';
      newErrors.debit = 'Either credit or debit must be greater than 0';
    }

    if (formData.credit > 0 && formData.debit > 0) {
      newErrors.credit = 'Cannot have both credit and debit';
      newErrors.debit = 'Cannot have both credit and debit';
    }

    if (!formData.categoryId || formData.categoryId === 0) {
      newErrors.categoryId = 'Category is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  /**
   * Handle form submission.
   */
  const handleSubmit = async () => {
    if (!validate()) return;

    setSaving(true);
    setError(null);

    try {
      if (transaction) {
        await transactionService.update(transaction.id, formData);
      } else {
        await transactionService.create(formData);
      }
      onSave();
      onClose();
    } catch (err) {
      setError('Failed to save transaction. Please try again.');
      console.error('Error saving transaction:', err);
    } finally {
      setSaving(false);
    }
  };

  /**
   * Handle input change.
   */
  const handleChange = (field: keyof TransactionFormData) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = field === 'credit' || field === 'debit'
      ? parseFloat(event.target.value) || 0
      : event.target.value;
    setFormData({ ...formData, [field]: value });
    // Clear error for this field
    if (errors[field]) {
      setErrors({ ...errors, [field]: undefined });
    }
  };

  if (categories.length === 0 && open) {
    return (
      <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
        <DialogTitle>Cannot Create Transaction</DialogTitle>
        <DialogContent>
          <Alert severity="warning">
            You must create at least one active category before creating transactions.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{transaction ? 'Edit Transaction' : 'Create Transaction'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <LocalizationProvider dateAdapter={AdapterDateFns}>
            <DatePicker
              label="Date"
              value={new Date(formData.recorded)}
              onChange={(newValue) => {
                if (newValue) {
                  setFormData({ ...formData, recorded: newValue.toISOString() });
                }
              }}
              slotProps={{
                textField: {
                  fullWidth: true,
                  required: true,
                },
              }}
            />
          </LocalizationProvider>

          <TextField
            label="Description"
            value={formData.description}
            onChange={handleChange('description')}
            error={!!errors.description}
            helperText={errors.description}
            fullWidth
            required
            multiline
            rows={2}
          />

          <TextField
            select
            label="Category"
            value={formData.categoryId}
            onChange={(e) => {
              setFormData({ ...formData, categoryId: Number(e.target.value) });
              if (errors.categoryId) {
                setErrors({ ...errors, categoryId: undefined });
              }
            }}
            error={!!errors.categoryId}
            helperText={errors.categoryId}
            fullWidth
            required
          >
            {categories.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label="Credit"
            type="number"
            value={formData.credit}
            onChange={handleChange('credit')}
            error={!!errors.credit}
            helperText={errors.credit || 'Enter amount for income/credit'}
            fullWidth
            inputProps={{ min: 0, step: 0.01 }}
          />

          <TextField
            label="Debit"
            type="number"
            value={formData.debit}
            onChange={handleChange('debit')}
            error={!!errors.debit}
            helperText={errors.debit || 'Enter amount for expense/debit'}
            fullWidth
            inputProps={{ min: 0, step: 0.01 }}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={saving}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={saving}>
          {saving ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default TransactionDialog;

