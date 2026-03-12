import axios, { AxiosInstance, AxiosError } from 'axios';

/**
 * Convert PascalCase keys to camelCase.
 * Handles nested objects and arrays recursively.
 *
 * @param obj The object to transform
 * @returns Transformed object with camelCase keys
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function toCamelCase(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj;
  }

  if (Array.isArray(obj)) {
    return obj.map(item => toCamelCase(item));
  }

  if (typeof obj === 'object' && obj.constructor === Object) {
    return Object.keys(obj).reduce((acc, key) => {
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
      acc[camelKey] = toCamelCase(obj[key]);
      return acc;
    }, {} as any);
  }

  return obj;
}

/**
 * Convert camelCase keys to PascalCase.
 * Handles nested objects and arrays recursively.
 *
 * @param obj The object to transform
 * @returns Transformed object with PascalCase keys
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function toPascalCase(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj;
  }

  if (Array.isArray(obj)) {
    return obj.map(item => toPascalCase(item));
  }

  if (typeof obj === 'object' && obj.constructor === Object) {
    return Object.keys(obj).reduce((acc, key) => {
      const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
      acc[pascalKey] = toPascalCase(obj[key]);
      return acc;
    }, {} as any);
  }

  return obj;
}

/**
 * Base API client configuration using axios.
 * Provides centralized configuration for all API requests including base URL,
 * timeout, headers, and error handling.
 * Automatically transforms PascalCase responses to camelCase and vice versa.
 */
class ApiClient {
  private client: AxiosInstance;

  constructor() {
    // Create axios instance with default configuration
    this.client = axios.create({
      baseURL: '/api', // Proxied through Vite to http://localhost:5000/api
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor for transforming request data and adding headers
    this.client.interceptors.request.use(
      (config) => {
        // Transform request data from camelCase to PascalCase
        if (config.data) {
          config.data = toPascalCase(config.data);
        }

        // Add any authentication tokens here if needed
        // const token = localStorage.getItem('token');
        // if (token) {
        //   config.headers.Authorization = `Bearer ${token}`;
        // }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor for transforming response data and handling errors
    this.client.interceptors.response.use(
      (response) => {
        // Transform response data from PascalCase to camelCase
        if (response.data) {
          response.data = toCamelCase(response.data);
        }
        return response;
      },
      (error: AxiosError) => {
        // Handle common errors
        if (error.response) {
          // Server responded with error status
          console.error('API Error:', error.response.status, error.response.data);
        } else if (error.request) {
          // Request made but no response received
          console.error('Network Error:', error.message);
        } else {
          // Something else happened
          console.error('Error:', error.message);
        }
        return Promise.reject(error);
      }
    );
  }

  /**
   * Get the axios instance for making requests.
   */
  public getInstance(): AxiosInstance {
    return this.client;
  }
}

// Export singleton instance
export const apiClient = new ApiClient().getInstance();

