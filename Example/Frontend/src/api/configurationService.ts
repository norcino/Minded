import { apiClient } from './client';

/**
 * Represents a single configuration entry with metadata.
 */
export interface ConfigurationEntry {
  key: string;
  category: string;
  name: string;
  type: string;
  value: any;
  defaultValue: any;
  description: string;
}

/**
 * Request model for updating a configuration value.
 */
export interface UpdateConfigurationRequest {
  value: any;
}

/**
 * Service for managing runtime configuration of Minded decorators.
 */
class ConfigurationService {
  private readonly baseUrl = '/configurations';

  /**
   * Gets all configuration entries.
   */
  async getAll(): Promise<ConfigurationEntry[]> {
    const response = await apiClient.get<ConfigurationEntry[]>(this.baseUrl);
    return response.data;
  }

  /**
   * Gets a specific configuration entry by key.
   */
  async getByKey(key: string): Promise<ConfigurationEntry> {
    const response = await apiClient.get<ConfigurationEntry>(`${this.baseUrl}/${encodeURIComponent(key)}`);
    return response.data;
  }

  /**
   * Updates a configuration value by key.
   */
  async update(key: string, value: any): Promise<ConfigurationEntry> {
    const request: UpdateConfigurationRequest = { value };
    const response = await apiClient.put<ConfigurationEntry>(`${this.baseUrl}/${encodeURIComponent(key)}`, request);
    return response.data;
  }

  /**
   * Resets a configuration value to its default.
   */
  async reset(key: string, defaultValue: any): Promise<ConfigurationEntry> {
    return this.update(key, defaultValue);
  }

  /**
   * Resets all configuration values to their defaults.
   */
  async resetAll(): Promise<ConfigurationEntry[]> {
    const entries = await this.getAll();
    const resetPromises = entries.map(entry => this.reset(entry.key, entry.defaultValue));
    return Promise.all(resetPromises);
  }
}

export default new ConfigurationService();

