# Quick Start Guide

## Prerequisites

1. **Start the Backend API**
   - Navigate to `Example/Application.Api`
   - Run the API (it should be available at `http://localhost:5000`)
   - Ensure the database is seeded with sample data

2. **Install Node.js**
   - Ensure Node.js v18+ is installed
   - Verify: `node --version`

## Running the Frontend

### First Time Setup

```bash
# Navigate to the Frontend folder
cd Example/Frontend

# Install dependencies
npm install
```

### Start Development Server

```bash
npm run dev
```

The application will open at `http://localhost:3000`

## Using the Application

### 1. User Management (Home Page)

- **View Users**: See all users in a data grid with sorting and filtering
- **Create User**: Click "Add User" button
  - Fill in Name, Surname, and Email
  - Click "Save"
- **Edit User**: Click the edit icon (pencil) on any user row
- **Delete User**: Click the delete icon (trash) on any user row
- **Impersonate User**: Click the person icon to impersonate a user
  - This allows you to manage their categories and transactions

### 2. Category Management

**Note**: You must impersonate a user first!

- Navigate to "Categories" from the sidebar
- **Switch Views**: Toggle between Tree View (default) and Grid View
  - **Tree View**: Visual hierarchy with drag-and-drop
  - **Grid View**: Traditional table with sorting/filtering

#### Tree View (Recommended)
- **Create Root Category**: Click "Add Root Category"
  - Enter Name and Description
  - Toggle Active status
- **Create Subcategory**: Click the "+" icon on any category
  - Automatically sets the parent category
- **Move Categories**: Drag and drop categories to reorganize
  - Drop on another category to make it a child
  - Drop at root level to remove parent
  - Cannot create circular references (moving a category under itself)
- **Expand/Collapse**: Click folder icons or use expand/collapse all buttons
- **Edit/Delete**: Use the action icons on each category

#### Grid View
- **Create Category**: Click "Add Category"
  - Enter Name and Description
  - Optionally select a Parent Category from dropdown
  - Toggle Active status
- **Edit/Delete**: Use the action icons in the grid

### 3. Transaction Management

**Note**: You must impersonate a user first!

- Navigate to "Transactions" from the sidebar
- **Create Transaction**: Click "Add Transaction"
  - Select Date using the date picker
  - Enter Description
  - Select Category from dropdown
  - Enter either Credit (income) OR Debit (expense)
  - Click "Save"
- **Edit/Delete**: Use the action icons in the grid
- **View**: Transactions are sorted by date (newest first)

## Features Demonstrated

### Material-UI Components Used

- **DataGrid**: Advanced table with sorting, filtering, pagination
- **TreeView**: Hierarchical tree structure with expand/collapse
- **DatePicker**: Calendar-based date selection
- **Dialog**: Modal forms for create/edit operations
- **Drawer**: Responsive navigation sidebar
- **AppBar**: Top navigation bar
- **TextField**: Form inputs with validation
- **Select/MenuItem**: Dropdown selections
- **Switch**: Toggle controls
- **Chip**: Status indicators
- **Alert/Snackbar**: Success and error notifications
- **ToggleButtonGroup**: View mode switcher (Tree/Grid)

### Best Practices Implemented

1. **TypeScript**: Full type safety across the application
2. **Component Architecture**: Reusable, well-documented components
3. **API Service Layer**: Centralized API communication
4. **Context API**: Global state management for user impersonation
5. **Form Validation**: Client-side validation with error messages
6. **Error Handling**: Graceful error handling with user feedback
7. **Responsive Design**: Mobile-friendly interface
8. **Code Documentation**: Comprehensive JSDoc comments

## Troubleshooting

### API Connection Issues

If you see "Failed to load" errors:
1. Verify the backend API is running on `http://localhost:5000`
2. Check the browser console for CORS errors
3. Ensure the API endpoints are accessible

### Build Errors

If you encounter build errors:
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Port Already in Use

If port 3000 is already in use:
```bash
# Edit vite.config.ts and change the port number
server: {
  port: 3001,  // Change to any available port
  ...
}
```

## Next Steps

- Read the full [README.md](./README.md) for detailed documentation
- Explore the source code in `src/` folder
- Customize the theme in `src/theme/theme.ts`
- Add new features following the extension guide in README.md

