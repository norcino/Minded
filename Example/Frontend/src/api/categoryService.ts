import { apiClient } from './client';
import { Category, CategoryFormData } from '../types';

/**
 * Category service for managing category-related API operations.
 * Provides CRUD operations for categories with support for OData queries and hierarchical structures.
 */
export class CategoryService {
  private readonly endpoint = '/category';

  /**
   * Get all categories with optional OData query parameters.
   * @param queryParams Optional OData query string (e.g., "$filter=active eq true&$orderby=name")
   * @returns Promise with array of categories
   */
  async getAll(queryParams?: string): Promise<Category[]> {
    const url = queryParams ? `${this.endpoint}?${queryParams}` : this.endpoint;
    const response = await apiClient.get<Category[]>(url);
    return response.data;
  }

  /**
   * Get categories for a specific user.
   * @param userId User ID to filter categories
   * @returns Promise with array of categories
   */
  async getByUserId(userId: number): Promise<Category[]> {
    const response = await apiClient.get<Category[]>(`${this.endpoint}?$filter=userId eq ${userId}`);
    return response.data;
  }

  /**
   * Get a single category by ID.
   * @param id Category ID
   * @returns Promise with category data
   */
  async getById(id: number): Promise<Category> {
    const response = await apiClient.get<Category>(`${this.endpoint}/${id}`);
    return response.data;
  }

  /**
   * Create a new category.
   * @param category Category data to create
   * @returns Promise with created category
   */
  async create(category: CategoryFormData): Promise<Category> {
    const response = await apiClient.post<Category>(this.endpoint, category);
    return response.data;
  }

  /**
   * Update an existing category.
   * Ensures the ID is set in both the URL and the payload to match backend expectations.
   * @param id Category ID to update
   * @param category Updated category data
   * @returns Promise with updated category
   */
  async update(id: number, category: CategoryFormData): Promise<Category> {
    const response = await apiClient.put<Category>(`${this.endpoint}/${id}`, { ...category, id });
    return response.data;
  }

  /**
   * Delete a category by ID.
   * @param id Category ID to delete
   * @returns Promise that resolves when deletion is complete
   */
  async delete(id: number): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${id}`);
  }
}

// Export singleton instance
export const categoryService = new CategoryService();

