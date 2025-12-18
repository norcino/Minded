import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Paper,
  Typography,
  Alert,
  Snackbar,
  Chip,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
} from '@mui/material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import ViewListIcon from '@mui/icons-material/ViewList';
import AccountTreeIcon from '@mui/icons-material/AccountTree';
import { Category } from '../../types';
import { categoryService } from '../../api';
import { useUser } from '../../context/UserContext';
import CategoryDialog from './CategoryDialog';
import CategoryTreeView from './CategoryTreeView';
import DeleteConfirmDialog from '../common/DeleteConfirmDialog';

/**
 * CategoryList component displays categories in either a data grid or tree view.
 * Provides functionality to create, edit, and delete categories.
 * Filters categories based on the currently impersonated user.
 * Supports hierarchical category structure with parent-child relationships.
 * Tree view allows drag-and-drop to reorganize category hierarchy.
 */
const CategoryList: React.FC = () => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);
  const [parentCategory, setParentCategory] = useState<Category | null>(null);
  const [viewMode, setViewMode] = useState<'grid' | 'tree'>('tree');
  const { currentUser } = useUser();

  // Load categories when component mounts or user changes
  useEffect(() => {
    if (currentUser) {
      loadCategories();
    } else {
      setCategories([]);
    }
  }, [currentUser]);

  /**
   * Load categories for the current user from the API.
   */
  const loadCategories = async () => {
    if (!currentUser) return;

    setLoading(true);
    setError(null);
    try {
      const data = await categoryService.getByUserId(currentUser.id);
      setCategories(data);
    } catch (err) {
      setError('Failed to load categories. Please try again.');
      console.error('Error loading categories:', err);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Handle opening the create category dialog.
   */
  const handleCreate = () => {
    setSelectedCategory(null);
    setParentCategory(null);
    setDialogOpen(true);
  };

  /**
   * Handle opening the create subcategory dialog.
   */
  const handleAddChild = (parent: Category | null) => {
    setSelectedCategory(null);
    setParentCategory(parent);
    setDialogOpen(true);
  };

  /**
   * Handle opening the edit category dialog.
   */
  const handleEdit = (category: Category) => {
    setSelectedCategory(category);
    setParentCategory(null);
    setDialogOpen(true);
  };

  /**
   * Handle opening the delete confirmation dialog.
   */
  const handleDeleteClick = (category: Category) => {
    setSelectedCategory(category);
    setDeleteDialogOpen(true);
  };

  /**
   * Handle confirming category deletion.
   */
  const handleDeleteConfirm = async () => {
    if (!selectedCategory) return;

    try {
      await categoryService.delete(selectedCategory.id);
      setSuccess('Category deleted successfully');
      await loadCategories();
    } catch (err) {
      setError('Failed to delete category. Please try again.');
      console.error('Error deleting category:', err);
    } finally {
      setDeleteDialogOpen(false);
      setSelectedCategory(null);
    }
  };

  /**
   * Handle saving a category (create or update).
   */
  const handleSave = async () => {
    setDialogOpen(false);
    await loadCategories();
    setSuccess(selectedCategory ? 'Category updated successfully' : 'Category created successfully');
  };

  // Define columns for the data grid
  const columns: GridColDef[] = [
    { field: 'id', headerName: 'ID', width: 70 },
    { field: 'name', headerName: 'Name', width: 200, flex: 1 },
    { field: 'description', headerName: 'Description', width: 300, flex: 2 },
    {
      field: 'parentId',
      headerName: 'Parent Category',
      width: 180,
      flex: 1,
      renderCell: (params) => {
        const parentCategory = categories.find(c => c.id === params.value);
        return parentCategory ? (
          <Chip
            label={parentCategory.name}
            size="small"
            variant="outlined"
            color="primary"
          />
        ) : (
          <Chip label="Root" size="small" variant="outlined" color="default" />
        );
      },
    },
    {
      field: 'active',
      headerName: 'Status',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'default'}
          size="small"
        />
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
          icon={<EditIcon />}
          label="Edit"
          onClick={() => handleEdit(params.row as Category)}
          showInMenu={false}
        />,
        <GridActionsCellItem
          key="delete"
          icon={<DeleteIcon />}
          label="Delete"
          onClick={() => handleDeleteClick(params.row as Category)}
          showInMenu={false}
        />,
      ],
    },
  ];

  if (!currentUser) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Please select a user to impersonate from the Users page to manage categories.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}>
        <Typography variant="h4" component="h1">
          Categories
        </Typography>

        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={(_, newMode) => newMode && setViewMode(newMode)}
            size="small"
          >
            <ToggleButton value="tree">
              <Tooltip title="Tree View">
                <AccountTreeIcon />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="grid">
              <Tooltip title="Grid View">
                <ViewListIcon />
              </Tooltip>
            </ToggleButton>
          </ToggleButtonGroup>

          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleCreate}
          >
            {viewMode === 'tree' ? 'Add Root Category' : 'Add Category'}
          </Button>
        </Box>
      </Box>

      <Alert severity="info" sx={{ mb: 2 }}>
        Managing categories for: {currentUser.name} {currentUser.surname}
        {viewMode === 'tree' && ' • Drag categories to reorganize hierarchy'}
      </Alert>

      {viewMode === 'grid' ? (
        <Paper sx={{ height: 600, width: '100%' }}>
          <DataGrid
            rows={categories}
            columns={columns}
            loading={loading}
            pageSizeOptions={[5, 10, 25, 50]}
            initialState={{
              pagination: { paginationModel: { pageSize: 10 } },
            }}
            disableRowSelectionOnClick
          />
        </Paper>
      ) : (
        <CategoryTreeView
          categories={categories}
          loading={loading}
          onEdit={handleEdit}
          onDelete={handleDeleteClick}
          onAddChild={handleAddChild}
          onRefresh={loadCategories}
        />
      )}

      <CategoryDialog
        open={dialogOpen}
        category={selectedCategory}
        userId={currentUser.id}
        categories={categories}
        parentCategoryId={parentCategory?.id}
        onClose={() => {
          setDialogOpen(false);
          setParentCategory(null);
        }}
        onSave={handleSave}
      />

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        title="Delete Category"
        message={`Are you sure you want to delete "${selectedCategory?.name}"?`}
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

export default CategoryList;

