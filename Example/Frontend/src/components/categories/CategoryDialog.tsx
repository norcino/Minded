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
  FormControlLabel,
  Switch,
  MenuItem,
} from '@mui/material';
import { Category, CategoryFormData } from '../../types';
import { categoryService } from '../../api';

/**
 * Props for the CategoryDialog component.
 */
interface CategoryDialogProps {
  open: boolean;
  category: Category | null;
  userId: number;
  categories: Category[];
  parentCategoryId?: number;
  onClose: () => void;
  onSave: () => void;
}

/**
 * CategoryDialog component for creating and editing categories.
 * Provides a form with validation for category data entry.
 * Supports hierarchical category structure with parent category selection.
 * 
 * @param props Component props
 * @returns Dialog component with category form
 */
const CategoryDialog: React.FC<CategoryDialogProps> = ({
  open,
  category,
  userId,
  categories,
  parentCategoryId,
  onClose,
  onSave,
}) => {
  const [formData, setFormData] = useState<CategoryFormData>({
    name: '',
    description: '',
    active: true,
    userId: userId,
    parentId: null,
  });
  const [errors, setErrors] = useState<Partial<Record<keyof CategoryFormData, string>>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Initialize form data when category, userId, or parentCategoryId changes
  useEffect(() => {
    if (category) {
      setFormData({
        name: category.name,
        description: category.description,
        active: category.active,
        userId: category.userId,
        parentId: category.parentId || null,
      });
    } else {
      setFormData({
        name: '',
        description: '',
        active: true,
        userId: userId,
        parentId: parentCategoryId || null,
      });
    }
    setErrors({});
    setError(null);
  }, [category, userId, parentCategoryId, open]);

  /**
   * Validate form data.
   * @returns True if form is valid, false otherwise
   */
  const validate = (): boolean => {
    const newErrors: Partial<Record<keyof CategoryFormData, string>> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    }

    if (!formData.description.trim()) {
      newErrors.description = 'Description is required';
    }

    // Prevent circular parent-child relationships
    if (category && formData.parentId === category.id) {
      newErrors.parentId = 'A category cannot be its own parent';
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
      if (category) {
        await categoryService.update(category.id, formData);
      } else {
        await categoryService.create(formData);
      }
      onSave();
      onClose();
    } catch (err) {
      setError('Failed to save category. Please try again.');
      console.error('Error saving category:', err);
    } finally {
      setSaving(false);
    }
  };

  /**
   * Handle input change.
   */
  const handleChange = (field: keyof CategoryFormData) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = field === 'active' ? event.target.checked : event.target.value;
    setFormData({ ...formData, [field]: value });
    // Clear error for this field
    if (errors[field]) {
      setErrors({ ...errors, [field]: undefined });
    }
  };

  /**
   * Get available parent categories (excluding the current category and its descendants).
   */
  const getAvailableParentCategories = (): Category[] => {
    if (!category) return categories;
    
    // Filter out the current category to prevent self-reference
    return categories.filter(c => c.id !== category.id);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{category ? 'Edit Category' : 'Create Category'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <TextField
            label="Name"
            value={formData.name}
            onChange={handleChange('name')}
            error={!!errors.name}
            helperText={errors.name}
            fullWidth
            required
          />

          <TextField
            label="Description"
            value={formData.description}
            onChange={handleChange('description')}
            error={!!errors.description}
            helperText={errors.description}
            fullWidth
            required
            multiline
            rows={3}
          />

          <TextField
            select
            label="Parent Category"
            value={formData.parentId || ''}
            onChange={(e) => {
              const value = e.target.value === '' ? null : Number(e.target.value);
              setFormData({ ...formData, parentId: value });
            }}
            error={!!errors.parentId}
            helperText={errors.parentId || 'Optional: Select a parent category for hierarchical organization'}
            fullWidth
          >
            <MenuItem value="">
              <em>None (Top Level)</em>
            </MenuItem>
            {getAvailableParentCategories().map((cat) => (
              <MenuItem key={cat.id} value={cat.id}>
                {cat.name}
              </MenuItem>
            ))}
          </TextField>

          <FormControlLabel
            control={
              <Switch
                checked={formData.active}
                onChange={handleChange('active')}
              />
            }
            label="Active"
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

export default CategoryDialog;

