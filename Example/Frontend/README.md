# Minded Frontend - User & Transaction Management Application

A modern React TypeScript application built with Vite and Material-UI (MUI) for managing users, categories, and transactions. This application demonstrates integration with the Minded Example REST API using best practices for frontend development.

## Features

- **User Management**: Create, edit, delete, and impersonate users
- **Category Management**: Organize transactions with hierarchical categories
  - **Tree View**: Visual hierarchy with drag-and-drop to reorganize
  - **Grid View**: Traditional table view with sorting and filtering
  - Move categories to root level or under other categories
  - Prevent circular parent-child relationships
- **Transaction Management**: Track financial transactions with credit/debit entries
- **User Impersonation**: Switch between users to manage their categories and transactions
- **Advanced Data Grid**: Sorting, filtering, and pagination for all data tables
- **Drag-and-Drop**: Intuitive category reorganization in tree view
- **Responsive Design**: Mobile-friendly interface with drawer navigation
- **Form Validation**: Client-side validation for all forms
- **Date Picker**: Easy date selection for transactions
- **Material Design**: Professional UI using MUI components

## Prerequisites

- Node.js (v18 or higher)
- npm (v9 or higher)
- Minded Example API running on `http://localhost:5000`

## How to Build and Run

### 1. Install Dependencies

```bash
npm install
```

### 2. Start the Development Server

```bash
npm run dev
```

The application will start on `http://localhost:3000` and automatically proxy API requests to `http://localhost:5000`.

### 3. Build for Production

```bash
npm run build
```

The production build will be created in the `dist` folder.

### 4. Preview Production Build

```bash
npm run preview
```

## Project Structure

```
src/
├── api/                    # API client and service modules
│   ├── client.ts          # Axios configuration and interceptors
│   ├── userService.ts     # User API operations
│   ├── categoryService.ts # Category API operations
│   ├── transactionService.ts # Transaction API operations
│   └── index.ts           # Barrel export
├── components/            # React components
│   ├── common/           # Reusable components
│   │   └── DeleteConfirmDialog.tsx
│   ├── layout/           # Layout components
│   │   └── Layout.tsx    # Main layout with navigation
│   ├── users/            # User management components
│   │   ├── UserList.tsx
│   │   └── UserDialog.tsx
│   ├── categories/       # Category management components
│   │   ├── CategoryList.tsx
│   │   └── CategoryDialog.tsx
│   └── transactions/     # Transaction management components
│       ├── TransactionList.tsx
│       └── TransactionDialog.tsx
├── context/              # React context providers
│   └── UserContext.tsx   # User impersonation context
├── theme/                # MUI theme configuration
│   └── theme.ts
├── types/                # TypeScript type definitions
│   └── index.ts
├── App.tsx               # Main application component
├── main.tsx              # Application entry point
└── index.css             # Global styles
```

## How to Extend the Application

### Adding a New Entity

1. **Define Types**: Add entity and form data interfaces in `src/types/index.ts`

```typescript
export interface MyEntity {
  id: number;
  name: string;
  // ... other fields
}

export interface MyEntityFormData {
  name: string;
  // ... other fields
}
```

2. **Create Service**: Add a new service in `src/api/myEntityService.ts`

```typescript
import { apiClient } from './client';
import { MyEntity, MyEntityFormData } from '../types';

export class MyEntityService {
  private readonly endpoint = '/myentity';

  async getAll(): Promise<MyEntity[]> {
    const response = await apiClient.get<MyEntity[]>(this.endpoint);
    return response.data;
  }

  async create(data: MyEntityFormData): Promise<MyEntity> {
    const response = await apiClient.post<MyEntity>(this.endpoint, data);
    return response.data;
  }

  // ... other CRUD operations
}

export const myEntityService = new MyEntityService();
```

3. **Create Components**: Add list and dialog components in `src/components/myentity/`
   - Follow the pattern used in `UserList.tsx` and `UserDialog.tsx`
   - Use MUI DataGrid for lists
   - Use MUI Dialog for forms

4. **Add Route**: Update `src/App.tsx` to include the new route

```typescript
<Route path="myentity" element={<MyEntityList />} />
```

5. **Add Navigation**: Update `src/components/layout/Layout.tsx` to add navigation item

```typescript
const navItems: NavItem[] = [
  // ... existing items
  { text: 'My Entity', icon: <MyIcon />, path: '/myentity' },
];
```

### Customizing the Theme

Edit `src/theme/theme.ts` to customize colors, typography, and component styles:

```typescript
export const theme = createTheme({
  palette: {
    primary: {
      main: '#your-color',
    },
    // ... other palette options
  },
  // ... other theme options
});
```

### Adding Form Validation

Use the validation pattern from existing dialogs:

```typescript
const validate = (): boolean => {
  const newErrors: Partial<MyFormData> = {};

  if (!formData.field.trim()) {
    newErrors.field = 'Field is required';
  }

  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};
```

### Customizing API Client

Edit `src/api/client.ts` to add authentication, custom headers, or error handling:

```typescript
// Add authentication token
this.client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

**Note**: The API client automatically transforms property names between PascalCase (backend) and camelCase (frontend):
- **Requests**: camelCase → PascalCase (e.g., `userId` → `UserId`)
- **Responses**: PascalCase → camelCase (e.g., `Id` → `id`)

This ensures the frontend uses JavaScript conventions while the backend uses C# conventions.

## Dependencies

### Core Dependencies

- **react** (^18.3.1): JavaScript library for building user interfaces
- **react-dom** (^18.3.1): React package for working with the DOM
- **react-router-dom** (^6.x): Declarative routing for React applications

### UI Framework

- **@mui/material** (^6.x): Material-UI component library (MIT License)
- **@mui/icons-material** (^6.x): Material Design icons (MIT License)
- **@emotion/react** (^11.x): CSS-in-JS library required by MUI (MIT License)
- **@emotion/styled** (^11.x): Styled components for Emotion (MIT License)

### MUI X Components

- **@mui/x-data-grid** (^8.x): Advanced data grid component (MIT License - Community version)
- **@mui/x-date-pickers** (^8.x): Date and time picker components (MIT License)
- **@mui/x-tree-view** (^8.x): Tree view component for hierarchical data (MIT License)

### Drag-and-Drop

- **@dnd-kit/core** (^6.x): Modern drag-and-drop toolkit (MIT License)
- **@dnd-kit/sortable** (^8.x): Sortable preset for dnd-kit (MIT License)
- **@dnd-kit/utilities** (^3.x): Utility functions for dnd-kit (MIT License)

### HTTP Client

- **axios** (^1.x): Promise-based HTTP client (MIT License)

### Date Utilities

- **date-fns** (^4.x): Modern JavaScript date utility library (MIT License)

### Build Tools

- **vite** (^6.x): Next-generation frontend build tool (MIT License)
- **@vitejs/plugin-react** (^4.x): Official Vite plugin for React (MIT License)

### Development Dependencies

- **typescript** (^5.6.x): TypeScript language (Apache 2.0 License)
- **@types/react** (^18.x): TypeScript definitions for React
- **@types/react-dom** (^18.x): TypeScript definitions for React DOM
- **@types/node** (^22.x): TypeScript definitions for Node.js
- **eslint** (^9.x): JavaScript linter (MIT License)
- **@typescript-eslint/eslint-plugin** (^8.x): TypeScript ESLint plugin
- **@typescript-eslint/parser** (^8.x): TypeScript parser for ESLint

## API Integration

The application integrates with the Minded Example REST API with the following endpoints:

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

- `GET /api/category` - Get all categories
- `GET /api/category/{id}` - Get category by ID
- `POST /api/category` - Create category
- `PUT /api/category/{id}` - Update category
- `DELETE /api/category/{id}` - Delete category

- `GET /api/transaction` - Get all transactions
- `GET /api/transaction/{id}` - Get transaction by ID
- `POST /api/transaction` - Create transaction
- `PUT /api/transaction/{id}` - Update transaction
- `DELETE /api/transaction/{id}` - Delete transaction

All endpoints support OData query parameters for filtering, sorting, and pagination.

## License

This project uses only MIT and Apache 2.0 licensed dependencies.

