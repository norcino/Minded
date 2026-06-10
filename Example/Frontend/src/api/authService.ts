import { apiClient } from './client';
import {
  AcceptInviteRequest,
  AuthResponse,
  ForgotPasswordRequest,
  InviteResolution,
  LoginRequest,
  PendingRegistrationResponse,
  RegisterRequest,
  ResetPasswordRequest,
} from '../types';

export class AuthService {
  private readonly endpoint = '/auth';

  async register(request: RegisterRequest): Promise<AuthResponse | PendingRegistrationResponse> {
    const response = await apiClient.post<AuthResponse | PendingRegistrationResponse>(`${this.endpoint}/register`, request);
    return response.data;
  }

  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>(`${this.endpoint}/login`, request);
    return response.data;
  }

  async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await apiClient.post(`${this.endpoint}/forgot-password`, request);
  }

  async resetPassword(request: ResetPasswordRequest): Promise<void> {
    await apiClient.post(`${this.endpoint}/reset-password`, request);
  }

  async acceptInvite(request: AcceptInviteRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>(`${this.endpoint}/accept-invite`, request);
    return response.data;
  }

  async me(): Promise<AuthResponse> {
    const response = await apiClient.get<AuthResponse>(`${this.endpoint}/me`);
    return response.data;
  }

  async getInviteDetails(token: string): Promise<InviteResolution> {
    const response = await apiClient.get<InviteResolution>(`${this.endpoint}/invite/${encodeURIComponent(token)}`);
    return response.data;
  }
}

export const authService = new AuthService();
