import { apiClient } from './client';
import { User, UserFormData } from '../types';

/**
 * User service for managing user-related API operations.
 * Provides CRUD operations for users with support for OData queries.
 */
export class UserService {
  private readonly endpoint = '/users';

  /**
   * Get all users with optional OData query parameters.
   * @param queryParams Optional OData query string (e.g., "$filter=name eq 'John'&$orderby=surname")
   * @returns Promise with array of users
   */
  async getAll(queryParams?: string): Promise<User[]> {
    const url = queryParams ? `${this.endpoint}?${queryParams}` : this.endpoint;
    const response = await apiClient.get<User[]>(url);
    return response.data;
  }

  /**
   * Get a single user by ID.
   * @param id User ID
   * @returns Promise with user data
   */
  async getById(id: number): Promise<User> {
    const response = await apiClient.get<User>(`${this.endpoint}/${id}`);
    return response.data;
  }

  /**
   * Create a new user.
   * @param user User data to create
   * @returns Promise with created user
   */
  async create(user: UserFormData): Promise<User> {
    const response = await apiClient.post<User>(this.endpoint, user);
    return response.data;
  }

  /**
   * Update an existing user.
   * Ensures the ID is set in both the URL and the payload to match backend expectations.
   * @param id User ID to update
   * @param user Updated user data
   * @returns Promise with updated user
   */
  async update(id: number, user: UserFormData): Promise<User> {
    const response = await apiClient.put<User>(`${this.endpoint}/${id}`, { ...user, id });
    return response.data;
  }

  /**
   * Delete a user by ID.
   * @param id User ID to delete
   * @returns Promise that resolves when deletion is complete
   */
  async delete(id: number): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${id}`);
  }
}

// Export singleton instance
export const userService = new UserService();

