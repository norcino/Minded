/**
 * User entity representing a user in the system.
 * Contains personal information and relationships to categories and transactions.
 */
export interface User {
  id: number;
  name: string;
  surname: string;
  email: string;
  categories?: Category[];
  transactions?: Transaction[];
}

/**
 * Category entity for organizing transactions.
 * Each category belongs to a user and can have multiple transactions.
 * Supports hierarchical structure with parent-child relationships.
 */
export interface Category {
  id: number;
  name: string;
  description: string;
  active: boolean;
  userId: number;
  parentId?: number | null;
  user?: User;
  transactions?: Transaction[];
  children?: Category[];
  parent?: Category;
}

/**
 * Transaction entity representing a financial transaction.
 * Each transaction belongs to a user and a category.
 */
export interface Transaction {
  id: number;
  recorded: string; // ISO date string
  credit: number;
  debit: number;
  description: string;
  categoryId: number;
  userId: number;
  category?: Category;
  user?: User;
}

/**
 * Form data for creating/editing a user.
 */
export interface UserFormData {
  name: string;
  surname: string;
  email: string;
}

/**
 * Form data for creating/editing a category.
 */
export interface CategoryFormData {
  name: string;
  description: string;
  active: boolean;
  userId: number;
  parentId?: number | null;
}

/**
 * Form data for creating/editing a transaction.
 */
export interface TransactionFormData {
  recorded: string;
  credit: number;
  debit: number;
  description: string;
  categoryId: number;
  userId: number;
}

/**
 * API response wrapper for OData queries.
 */
export interface ODataResponse<T> {
  value: T[];
  '@odata.count'?: number;
}

