import { apiClient } from './client';
import { RoleDto, PermissionGroups } from '../types';

/**
 * Role service for managing roles, permissions, and user-role assignments.
 */
export class RoleService {
  private readonly endpoint = '/roles';

  async getAll(): Promise<RoleDto[]> {
    const response = await apiClient.get<RoleDto[]>(this.endpoint);
    return response.data;
  }

  async create(name: string): Promise<void> {
    await apiClient.post(this.endpoint, { name });
  }

  async delete(name: string): Promise<void> {
    await apiClient.delete(`${this.endpoint}/${encodeURIComponent(name)}`);
  }

  async getPermissions(): Promise<PermissionGroups> {
    const response = await apiClient.get<PermissionGroups>('/permissions');
    return response.data;
  }

  async updateRolePermissions(roleName: string, permissionNames: string[]): Promise<void> {
    await apiClient.put(`${this.endpoint}/${encodeURIComponent(roleName)}/permissions`, permissionNames);
  }

  async assignRolesToUser(userId: number, roleNames: string[]): Promise<void> {
    await apiClient.put(`/users/${userId}/roles`, roleNames);
  }

  async getUsersWithRoles() {
    const response = await apiClient.get('/users-with-roles');
    return response.data;
  }

  async resetToDefault(): Promise<void> {
    await apiClient.post(`${this.endpoint}/reset-to-default`);
  }
}

export const roleService = new RoleService();
