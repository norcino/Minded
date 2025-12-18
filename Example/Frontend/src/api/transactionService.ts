import { apiClient } from './client';
import { Transaction, TransactionFormData } from '../types';

/**
 * Transaction service for managing transaction-related API operations.
 * Provides CRUD operations for transactions with support for OData queries.
 */
export class TransactionService {
  private readonly endpoint = '/transaction';

  /**
   * Get all transactions with optional OData query parameters.
   * @param queryParams Optional OData query string (e.g., "$filter=userId eq 1&$orderby=recorded desc")
   * @returns Promise with array of transactions
   */
  async getAll(queryParams?: string): Promise<Transaction[]> {
    const url = queryParams ? `${this.endpoint}?${queryParams}` : this.endpoint;
    const response = await apiClient.get<Transaction[]>(url);
    return response.data;
  }

  /**
   * Get transactions for a specific user.
   * @param userId User ID to filter transactions
   * @returns Promise with array of transactions
   */
  async getByUserId(userId: number): Promise<Transaction[]> {
    const response = await apiClient.get<Transaction[]>(`${this.endpoint}?$filter=userId eq ${userId}&$expand=category`);
    return response.data;
  }

  /**
   * Get a single transaction by ID.
   * @param id Transaction ID
   * @returns Promise with transaction data
   */
  async getById(id: number): Promise<Transaction> {
    const response = await apiClient.get<Transaction>(`${this.endpoint}/${id}`);
    return response.data;
  }

  /**
   * Create a new transaction.
   * @param transaction Transaction data to create
   * @returns Promise with created transaction
   */
  async create(transaction: TransactionFormData): Promise<Transaction> {
    const response = await apiClient.post<Transaction>(this.endpoint, transaction);
    return response.data;
  }

  /**
   * Update an existing transaction.
   * Ensures the ID is set in both the URL and the payload to match backend expectations.
   * @param id Transaction ID to update
   * @param transaction Updated transaction data
   * @returns Promise with updated transaction
   */
  async update(id: number, transaction: TransactionFormData): Promise<Transaction> {
    const response = await apiClient.put<Transaction>(`${this.endpoint}/${id}`, { ...transaction, id });
    return response.data;
  }

  /**
   * Delete a transaction by ID.
   * @param id Transaction ID to delete
   * @returns Promise that resolves when deletion is complete
   */
  async delete(id: number): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${id}`);
  }
}

// Export singleton instance
export const transactionService = new TransactionService();

