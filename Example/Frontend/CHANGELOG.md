# Changelog

## [1.1.0] - 2025-12-12

### Added
- **Category Tree View with Drag-and-Drop**: New hierarchical tree view for categories
  - Visual hierarchy display with expand/collapse functionality
  - Drag-and-drop to move categories between levels
  - Move categories to root level or under other categories/subcategories
  - Circular reference validation prevents invalid parent-child relationships
  - Add subcategory directly from parent node
  - Toggle between Tree View and Grid View
  - Real-time hierarchy updates with visual feedback
- **@dnd-kit Integration**: Added drag-and-drop library for tree reorganization
  - @dnd-kit/core: Core drag-and-drop functionality
  - @dnd-kit/sortable: Sortable tree items
  - @dnd-kit/utilities: CSS transform utilities

### Changed
- **CategoryList Component**: Enhanced with view mode toggle
  - Tree View (default): Hierarchical display with drag-and-drop
  - Grid View: Traditional data grid with sorting/filtering
  - "Add Category" button changes to "Add Root Category" in tree view
  - Parent category can be pre-selected when adding subcategories
- **CategoryDialog Component**: Added support for parent category pre-selection
  - New `parentCategoryId` prop to pre-fill parent when adding subcategories
  - Automatically sets parent when "Add Subcategory" is clicked from tree view

### Technical Details
- Tree structure built from flat category list using parent-child relationships
- Drag-and-drop updates category's `parentId` via API
- Validation prevents moving a category under itself or its descendants
- Expand/collapse state managed locally for better UX
- Sortable context includes all category IDs for proper drag-and-drop behavior

## [1.0.1] - 2025-12-12

### Fixed
- **API Property Name Transformation**: Added automatic transformation between PascalCase (C# backend) and camelCase (JavaScript frontend)
  - Backend returns: `{ "Id": 1, "Name": "John", "Surname": "Doe", "Email": "john.doe@example.com" }`
  - Frontend receives: `{ "id": 1, "name": "John", "surname": "Doe", "email": "john.doe@example.com" }`
  - This fixes the MUI DataGrid error: "The Data Grid component requires all rows to have a unique `id` property"
- **React Key Props Warning**: Added unique `key` props to all `GridActionsCellItem` components in DataGrid action columns
  - Fixed warning: "Each child in a list should have a unique 'key' prop"
  - Applied to UserList, CategoryList, and TransactionList components
  
### Changed
- Updated `src/api/client.ts` with request/response interceptors:
  - Request interceptor: Transforms camelCase to PascalCase before sending to backend
  - Response interceptor: Transforms PascalCase to camelCase after receiving from backend
  - Handles nested objects and arrays recursively

### Technical Details
The transformation functions ensure seamless integration between:
- **Frontend conventions**: JavaScript/TypeScript uses camelCase (e.g., `userId`, `firstName`)
- **Backend conventions**: C#/.NET uses PascalCase (e.g., `UserId`, `FirstName`)

This is transparent to developers - you can continue using camelCase in your TypeScript code, and the API client handles the conversion automatically.

## [1.0.0] - 2025-12-12

### Added
- Initial release of Minded Frontend application
- User management with CRUD operations
- Category management with hierarchical structure
- Transaction management with credit/debit tracking
- User impersonation feature
- Material-UI components (DataGrid, DatePicker, Dialog, etc.)
- React Router navigation
- Comprehensive documentation (README.md, QUICKSTART.md)

