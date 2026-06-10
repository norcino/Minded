import { apiClient } from './client';
import { TenantInvite, TenantJoinRequest, User } from '../types';

export class TenantAdminService {
  private readonly endpoint = '/tenant-admin';

  async getUsers(): Promise<User[]> {
    const response = await apiClient.get<User[]>(`${this.endpoint}/users`);
    return response.data;
  }

  async createInvite(email?: string): Promise<TenantInvite> {
    const response = await apiClient.post<TenantInvite>(`${this.endpoint}/invites`, { email });
    return response.data;
  }

  async updateUserRole(userId: number, role: string): Promise<void> {
    await apiClient.put(`${this.endpoint}/users/${userId}/role`, { role });
  }

  async removeUser(userId: number): Promise<void> {
    await apiClient.delete(`${this.endpoint}/users/${userId}`);
  }

  async getJoinRequests(): Promise<TenantJoinRequest[]> {
    const response = await apiClient.get<TenantJoinRequest[]>(`${this.endpoint}/join-requests`);
    return response.data;
  }

  async approveJoinRequest(requestId: number): Promise<void> {
    await apiClient.post(`${this.endpoint}/join-requests/${requestId}/approve`);
  }

  async rejectJoinRequest(requestId: number): Promise<void> {
    await apiClient.post(`${this.endpoint}/join-requests/${requestId}/reject`);
  }
}

export const tenantAdminService = new TenantAdminService();
