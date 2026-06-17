/**
 * User entity representing a user in the system.
 * Contains personal information and relationships to categories and transactions.
 */
export interface User {
  id: number;
  tenantId?: number | null;
  name: string;
  surname: string;
  email: string;
  tenantRole?: string;
  isGlobalAdmin?: boolean;
  categories?: Category[];
  transactions?: Transaction[];
  roles?: string[];
}

export interface Tenant {
  id: number;
  name: string;
}

export interface AuthResponse {
  accessToken: string | null;
  user: User;
  tenant: Tenant | null;
}

export interface RegisterRequest {
  name: string;
  surname: string;
  email: string;
  password: string;
  tenantName: string;
  mode?: 'create-tenant' | 'join-tenant';
  inviteToken?: string;
}

export interface PendingRegistrationResponse {
  pendingApproval: boolean;
  message: string;
}

export interface InviteResolution {
  tenantName: string;
  email?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface AcceptInviteRequest {
  codeOrToken: string;
  name: string;
  surname: string;
  email: string;
  password: string;
}

export interface TenantInvite {
  id: number;
  email: string;
  code: string;
  token: string;
  inviteLink: string;
  expiresAtUtc: string;
}

export interface TenantSummary {
  id: number;
  name: string;
  legalOwnerUserId?: number | null;
  legalOwnerEmail?: string;
  activeUsersCount: number;
  categoriesCount: number;
  transactionsCount: number;
}

export interface CreateTenantRequest {
  name: string;
  legalOwnerName: string;
  legalOwnerSurname: string;
  legalOwnerEmail: string;
  legalOwnerPassword: string;
}

export interface TenantJoinRequest {
  id: number;
  tenantId: number;
  name: string;
  surname: string;
  email: string;
  createdAtUtc: string;
}

/**
 * Role DTO with name and associated permissions.
 * Roles are string-based, stored in join tables.
 */
export interface RoleDto {
  name: string;
  permissions: string[];
}

/**
 * Grouped permissions returned by the API.
 * Keys are group names (e.g. "Categories"), values are permission name arrays.
 */
export type PermissionGroups = Record<string, string[]>;

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
