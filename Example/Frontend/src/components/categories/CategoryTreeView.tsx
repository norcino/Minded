import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  Tooltip,
  Chip,
  Alert,
  Snackbar,
} from '@mui/material';
import { SimpleTreeView } from '@mui/x-tree-view/SimpleTreeView';
import { TreeItem } from '@mui/x-tree-view/TreeItem';
import { ChevronRight, ExpandMore } from '@mui/icons-material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import FolderIcon from '@mui/icons-material/Folder';
import FolderOpenIcon from '@mui/icons-material/FolderOpen';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
  DragOverEvent,
  closestCenter,
  useDraggable,
  useDroppable,
} from '@dnd-kit/core';
import { Category } from '../../types';
import { categoryService } from '../../api';
import { useUser } from '../../context/UserContext';

/**
 * Extended Category type with children for tree structure.
 */
interface CategoryNode extends Category {
  children: CategoryNode[];
}

/**
 * Props for DraggableTreeItem component.
 */
interface DraggableTreeItemProps {
  category: CategoryNode;
  onEdit: (category: Category) => void;
  onDelete: (category: Category) => void;
  onAddChild: (parentCategory: Category) => void;
  expandedNodes: string[];
  onNodeToggle: (nodeId: string) => void;
  isOver?: boolean;
}

/**
 * Draggable tree item component with drag-and-drop functionality.
 * Allows dragging categories and dropping them onto other categories to create hierarchy.
 */
const DraggableTreeItem: React.FC<DraggableTreeItemProps> = ({
  category,
  onEdit,
  onDelete,
  onAddChild,
  expandedNodes,
  onNodeToggle,
  isOver = false,
}) => {
  // Make this item draggable
  const { attributes, listeners, setNodeRef: setDragRef, isDragging } = useDraggable({
    id: `drag-${category.id}`,
    data: { category },
  });

  // Make this item droppable (can receive other categories)
  const { setNodeRef: setDropRef, isOver: isOverCurrent } = useDroppable({
    id: `drop-${category.id}`,
    data: { category },
  });

  // Combine refs
  const setRefs = (element: HTMLDivElement | null) => {
    setDragRef(element);
    setDropRef(element);
  };

  const isExpanded = expandedNodes.includes(category.id.toString());

  /**
   * Custom label with actions for each tree node.
   */
  const renderLabel = () => (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1,
        py: 0.5,
        pr: 1,
        bgcolor: isOverCurrent ? 'action.hover' : 'transparent',
        borderRadius: 1,
        transition: 'background-color 0.2s',
        '&:hover .action-buttons': {
          opacity: 1,
        },
      }}
    >
      <Box
        {...listeners}
        {...attributes}
        sx={{
          cursor: isDragging ? 'grabbing' : 'grab',
          display: 'flex',
          alignItems: 'center',
          opacity: isDragging ? 0.5 : 1,
        }}
      >
        <DragIndicatorIcon fontSize="small" sx={{ color: 'text.secondary' }} />
      </Box>

      {isExpanded ? <FolderOpenIcon color="primary" /> : <FolderIcon color="action" />}

      <Typography variant="body1" sx={{ flexGrow: 1 }}>
        {category.name}
      </Typography>

      {!category.active && (
        <Chip label="Inactive" size="small" color="default" />
      )}

      {isOverCurrent && (
        <Chip label="Drop here" size="small" color="primary" />
      )}

      <Box
        className="action-buttons"
        sx={{
          display: 'flex',
          gap: 0.5,
          opacity: { xs: 1, sm: 0 },
          transition: 'opacity 0.2s',
        }}
      >
        <Tooltip title="Add subcategory">
          <IconButton
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              onAddChild(category);
            }}
          >
            <AddIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="Edit">
          <IconButton
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              onEdit(category);
            }}
          >
            <EditIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="Delete">
          <IconButton
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              onDelete(category);
            }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>
    </Box>
  );

  return (
    <div ref={setRefs}>
      <TreeItem
        itemId={category.id.toString()}
        label={renderLabel()}
        onClick={() => onNodeToggle(category.id.toString())}
      >
        {category.children.length > 0 &&
          category.children.map((child) => (
            <DraggableTreeItem
              key={child.id}
              category={child}
              onEdit={onEdit}
              onDelete={onDelete}
              onAddChild={onAddChild}
              expandedNodes={expandedNodes}
              onNodeToggle={onNodeToggle}
            />
          ))}
      </TreeItem>
    </div>
  );
};

/**
 * Props for CategoryTreeView component.
 */
interface CategoryTreeViewProps {
  categories: Category[];
  loading: boolean;
  onEdit: (category: Category) => void;
  onDelete: (category: Category) => void;
  onAddChild: (parentCategory: Category | null) => void;
  onRefresh: () => void;
}

/**
 * CategoryTreeView component displays categories in a hierarchical tree structure.
 * Supports drag-and-drop to reorganize categories and move them between levels.
 * Provides actions to add, edit, and delete categories at any level.
 *
 * @param props Component props
 * @returns Tree view component with drag-and-drop functionality
 */
const CategoryTreeView: React.FC<CategoryTreeViewProps> = ({
  categories,
  loading,
  onEdit,
  onDelete,
  onAddChild,
  onRefresh,
}) => {
  const [treeData, setTreeData] = useState<CategoryNode[]>([]);
  const [expandedNodes, setExpandedNodes] = useState<string[]>([]);
  const [activeCategory, setActiveCategory] = useState<Category | null>(null);
  const [overCategory, setOverCategory] = useState<Category | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const { currentUser } = useUser();

  // Rebuild tree when categories change
  useEffect(() => {
    const tree = buildCategoryTree(categories);
    setTreeData(tree);
  }, [categories]);

  /**
   * Build hierarchical tree structure from flat category list.
   *
   * @param flatCategories Flat array of categories
   * @returns Array of root-level category nodes with nested children
   */
  const buildCategoryTree = (flatCategories: Category[]): CategoryNode[] => {
    const categoryMap = new Map<number, CategoryNode>();
    const rootCategories: CategoryNode[] = [];

    // First pass: create map with all categories
    flatCategories.forEach((cat) => {
      categoryMap.set(cat.id, { ...cat, children: [] });
    });

    // Second pass: build tree structure
    flatCategories.forEach((cat) => {
      const node = categoryMap.get(cat.id)!;
      if (cat.parentId && categoryMap.has(cat.parentId)) {
        const parent = categoryMap.get(cat.parentId)!;
        parent.children.push(node);
      } else {
        // Root level category (no parent or parent not found)
        rootCategories.push(node);
      }
    });

    return rootCategories;
  };

  /**
   * Find a category by ID in the tree.
   */
  const findCategory = (id: number, nodes: CategoryNode[] = treeData): CategoryNode | null => {
    for (const node of nodes) {
      if (node.id === id) return node;
      const found = findCategory(id, node.children);
      if (found) return found;
    }
    return null;
  };

  /**
   * Check if moving a category would create a circular reference.
   * A category cannot be moved under itself or any of its descendants.
   */
  const wouldCreateCircularReference = (categoryId: number, newParentId: number | null): boolean => {
    if (newParentId === null) return false;
    if (categoryId === newParentId) return true;

    const isDescendant = (nodeId: number, ancestorId: number): boolean => {
      const node = findCategory(nodeId);
      if (!node) return false;

      for (const child of node.children) {
        if (child.id === ancestorId) return true;
        if (isDescendant(child.id, ancestorId)) return true;
      }
      return false;
    };

    return isDescendant(categoryId, newParentId);
  };

  /**
   * Handle drag start event.
   */
  const handleDragStart = (event: DragStartEvent) => {
    const category = event.active.data.current?.category as Category;
    setActiveCategory(category);
  };

  /**
   * Handle drag over event - track which category is being hovered.
   */
  const handleDragOver = (event: DragOverEvent) => {
    const { over } = event;
    if (over) {
      const category = over.data.current?.category as Category;
      setOverCategory(category);
    } else {
      setOverCategory(null);
    }
  };

  /**
   * Handle drag end event - update category parent.
   */
  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveCategory(null);
    setOverCategory(null);

    if (!over) return;

    const draggedCategory = active.data.current?.category as Category;

    // Check if dropped on root zone
    if (over.id === 'root-zone') {
      // Move to root level (remove parent)
      if (draggedCategory.parentId === null || draggedCategory.parentId === undefined) {
        return; // Already at root
      }

      try {
        await categoryService.update(draggedCategory.id, {
          ...draggedCategory,
          parentId: undefined,
        });

        setSuccess(`"${draggedCategory.name}" moved to root level`);
        onRefresh();
      } catch (err) {
        setError('Failed to move category. Please try again.');
        console.error('Error moving category:', err);
      }
      return;
    }

    const targetCategory = over.data.current?.category as Category;

    // Don't do anything if dropped on itself
    if (draggedCategory.id === targetCategory.id) return;

    // Check for circular reference
    if (wouldCreateCircularReference(draggedCategory.id, targetCategory.id)) {
      setError('Cannot move a category under itself or its descendants');
      return;
    }

    // If already the parent, don't do anything
    if (draggedCategory.parentId === targetCategory.id) {
      return;
    }

    try {
      // Update the category's parent to the target category
      await categoryService.update(draggedCategory.id, {
        ...draggedCategory,
        parentId: targetCategory.id,
      });

      setSuccess(`"${draggedCategory.name}" moved under "${targetCategory.name}"`);
      onRefresh();
    } catch (err) {
      setError('Failed to move category. Please try again.');
      console.error('Error moving category:', err);
    }
  };

  /**
   * Handle node toggle (expand/collapse).
   */
  const handleNodeToggle = (nodeId: string) => {
    setExpandedNodes((prev) =>
      prev.includes(nodeId)
        ? prev.filter((id) => id !== nodeId)
        : [...prev, nodeId]
    );
  };

  /**
   * Expand all nodes.
   */
  const handleExpandAll = () => {
    const allIds: string[] = [];
    const traverse = (node: CategoryNode) => {
      allIds.push(node.id.toString());
      node.children.forEach(traverse);
    };
    treeData.forEach(traverse);
    setExpandedNodes(allIds);
  };

  /**
   * Collapse all nodes.
   */
  const handleCollapseAll = () => {
    setExpandedNodes([]);
  };

  if (!currentUser) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="warning">
          Please select a user to impersonate from the Users page to manage categories.
        </Alert>
      </Box>
    );
  }

  if (loading) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading categories...</Typography>
      </Box>
    );
  }

  if (categories.length === 0) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="info">
          No categories found. Click "Add Root Category" to create your first category.
        </Alert>
      </Box>
    );
  }

  // Root droppable zone component
  const RootDropZone = () => {
    const { setNodeRef, isOver } = useDroppable({
      id: 'root-zone',
    });

    return (
      <Box
        ref={setNodeRef}
        sx={{
          p: 1,
          mb: 2,
          border: 2,
          borderStyle: 'dashed',
          borderColor: isOver ? 'primary.main' : 'divider',
          borderRadius: 1,
          bgcolor: isOver ? 'action.hover' : 'transparent',
          transition: 'all 0.2s',
          textAlign: 'center',
        }}
      >
        <Typography variant="body2" color={isOver ? 'primary' : 'text.secondary'}>
          {isOver ? 'Drop here to move to root level' : 'Drop categories here to remove parent (move to root)'}
        </Typography>
      </Box>
    );
  };

  return (
    <Box sx={{ height: '100%', width: '100%' }}>
      <Box sx={{ mb: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        <Tooltip title="Expand all categories">
          <IconButton size="small" onClick={handleExpandAll}>
            <ExpandMore />
          </IconButton>
        </Tooltip>
        <Tooltip title="Collapse all categories">
          <IconButton size="small" onClick={handleCollapseAll}>
            <ChevronRight />
          </IconButton>
        </Tooltip>
        <Typography variant="body2" sx={{ alignSelf: 'center', ml: 1, color: 'text.secondary' }}>
          Drag and drop categories onto other categories to create hierarchy
        </Typography>
      </Box>

      <Paper sx={{ p: 2, minHeight: 400 }}>
        <DndContext
          collisionDetection={closestCenter}
          onDragStart={handleDragStart}
          onDragOver={handleDragOver}
          onDragEnd={handleDragEnd}
        >
          <RootDropZone />

          <SimpleTreeView
            expandedItems={expandedNodes}
            slots={{
              collapseIcon: ExpandMore,
              expandIcon: ChevronRight,
            }}
            sx={{
              flexGrow: 1,
              overflowY: 'auto',
            }}
          >
            {treeData.map((category) => (
              <DraggableTreeItem
                key={category.id}
                category={category}
                onEdit={onEdit}
                onDelete={onDelete}
                onAddChild={onAddChild}
                expandedNodes={expandedNodes}
                onNodeToggle={handleNodeToggle}
              />
            ))}
          </SimpleTreeView>

          <DragOverlay>
            {activeCategory ? (
              <Box
                sx={{
                  p: 1,
                  bgcolor: 'background.paper',
                  border: 2,
                  borderColor: 'primary.main',
                  borderRadius: 1,
                  boxShadow: 3,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                }}
              >
                <FolderIcon color="primary" />
                <Typography fontWeight="medium">
                  {activeCategory.name}
                </Typography>
              </Box>
            ) : null}
          </DragOverlay>
        </DndContext>
      </Paper>

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

export default CategoryTreeView;

