# Category Tree View Guide

## Overview

The Category Tree View provides an intuitive, visual way to manage hierarchical categories with drag-and-drop functionality. This guide explains how to use the tree view and how it works under the hood.

## Features

### Visual Hierarchy
- **Folder Icons**: Closed folders for collapsed nodes, open folders for expanded nodes
- **Indentation**: Clear visual hierarchy showing parent-child relationships
- **Expand/Collapse**: Click on any category to expand or collapse its children
- **Expand/Collapse All**: Buttons to quickly expand or collapse the entire tree

### Drag-and-Drop
- **Move Categories**: Drag any category to a new location
- **Change Parent**: Drop a category on another to make it a child
- **Move to Root**: Drop a category at the root level to remove its parent
- **Visual Feedback**: Dragged item shows with a border and shadow
- **Validation**: Prevents circular references (can't move a category under itself or its descendants)

### Actions
- **Add Subcategory**: Click the "+" icon to add a child category
- **Edit**: Click the pencil icon to edit category details
- **Delete**: Click the trash icon to delete a category
- **Hover Actions**: Action buttons appear on hover (always visible on mobile)

### Status Indicators
- **Active/Inactive**: Inactive categories show a gray "Inactive" chip
- **Drag Handle**: Grip icon indicates draggable items

## How to Use

### Creating Categories

#### Root Category
1. Click "Add Root Category" button
2. Fill in Name and Description
3. Toggle Active status if needed
4. Click "Save"

#### Subcategory
1. Find the parent category in the tree
2. Click the "+" icon on the parent
3. Fill in the form (parent is pre-selected)
4. Click "Save"

### Reorganizing Categories

#### Move to Different Parent
1. Click and hold the drag handle (grip icon) on the category
2. Drag to the desired parent category
3. Drop on the parent category
4. Category is now a child of the new parent

#### Move to Root Level
1. Click and hold the drag handle
2. Drag to an empty area or the root level
3. Drop to remove the parent
4. Category is now a root-level category

#### Reorder Categories
1. Drag a category up or down within the same level
2. Drop at the desired position
3. Categories are reordered

### Validation Rules

The tree view enforces these rules:
- **No Circular References**: Cannot move a category under itself
- **No Descendant Parents**: Cannot move a category under any of its descendants
- **Error Messages**: Clear error messages when validation fails

## Technical Implementation

### Tree Structure

Categories are stored flat in the database with a `parentId` field:
```json
{
  "id": 1,
  "name": "Income",
  "parentId": null  // Root category
}
{
  "id": 2,
  "name": "Salary",
  "parentId": 1     // Child of "Income"
}
```

The tree view builds a hierarchical structure from this flat list:
```typescript
interface CategoryNode {
  id: number;
  name: string;
  parentId: number | null;
  children: CategoryNode[];  // Nested children
}
```

### Drag-and-Drop Flow

1. **Drag Start**: Record which category is being dragged
2. **Drag Over**: Highlight valid drop targets
3. **Drop**: Validate the move
4. **Update**: Call API to update `parentId`
5. **Refresh**: Reload categories to show new structure

### API Calls

When you move a category:
```typescript
// Update the category's parent
PUT /api/category/{id}
{
  "name": "Salary",
  "parentId": 5,  // New parent ID
  // ... other fields
}
```

### Circular Reference Prevention

Before allowing a move, the tree view checks:
```typescript
function wouldCreateCircularReference(categoryId, newParentId) {
  // Can't be your own parent
  if (categoryId === newParentId) return true;
  
  // Can't move under any descendant
  if (isDescendant(categoryId, newParentId)) return true;
  
  return false;
}
```

## Comparison: Tree View vs Grid View

| Feature | Tree View | Grid View |
|---------|-----------|-----------|
| Visual Hierarchy | ✅ Clear parent-child display | ❌ Flat list |
| Drag-and-Drop | ✅ Intuitive reorganization | ❌ Not available |
| Add Subcategory | ✅ One click from parent | ⚠️ Manual parent selection |
| Sorting | ❌ Hierarchical order only | ✅ Sort by any column |
| Filtering | ❌ Not available | ✅ Filter by any field |
| Pagination | ❌ Shows all categories | ✅ Paginated view |
| Best For | Managing hierarchy | Bulk operations |

## Tips and Best Practices

### Organization
- **Use Root Categories** for main types (Income, Expenses, Savings)
- **Use Subcategories** for specific items (Salary, Groceries, Emergency Fund)
- **Limit Depth** to 2-3 levels for better usability

### Performance
- Tree view loads all categories at once
- For large category lists (100+), consider using Grid View for better performance
- Expand only the branches you need to work with

### Workflow
1. **Plan Structure**: Sketch your category hierarchy first
2. **Create Root Categories**: Start with top-level categories
3. **Add Subcategories**: Build out the tree from top to bottom
4. **Reorganize**: Use drag-and-drop to refine the structure
5. **Switch to Grid**: Use Grid View for bulk edits or searching

## Keyboard Shortcuts

- **Click Category**: Expand/collapse
- **Drag Handle**: Click and drag to move
- **Expand All**: Click expand all button
- **Collapse All**: Click collapse all button

## Troubleshooting

### Category Won't Move
- **Check for circular reference**: Can't move under itself or descendants
- **Verify permissions**: Ensure you're impersonating the correct user
- **Check network**: Ensure API is accessible

### Tree Not Updating
- **Refresh**: The tree auto-refreshes after moves
- **Check console**: Look for API errors in browser console
- **Reload page**: Force a full refresh if needed

### Missing Categories
- **Check filters**: Grid view may have filters applied
- **Verify user**: Ensure you're impersonating the right user
- **Check active status**: Inactive categories still show in tree view

## Future Enhancements

Potential improvements for future versions:
- Multi-select drag-and-drop
- Keyboard navigation (arrow keys)
- Search/filter in tree view
- Bulk operations in tree view
- Export tree structure
- Import from CSV/JSON

