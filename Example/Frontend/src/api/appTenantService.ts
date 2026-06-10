import { apiClient } from './client';
import { CreateTenantRequest, TenantSummary } from '../types';

export class AppTenantService {
  private readonly endpoint = '/tenants';

  async getAll(): Promise<TenantSummary[]> {
    const response = await apiClient.get<TenantSummary[]>(this.endpoint);
    return response.data;
  }

  async create(request: CreateTenantRequest): Promise<void> {
    await apiClient.post(this.endpoint, request);
  }

  async delete(tenantId: number, confirmationName: string): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${tenantId}`, {
      data: { confirmationName },
    });
  }
}

export const appTenantService = new AppTenantService();
