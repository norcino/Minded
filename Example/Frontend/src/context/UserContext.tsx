import React, { createContext, useContext, useEffect, useMemo, useState, ReactNode } from 'react';
import { authService } from '../api/authService';
import { AuthResponse, User } from '../types';

interface UserContextType {
  currentUser: User | null;
  accessToken: string | null;
  tenantName: string | null;
  isAuthenticated: boolean;
  setCurrentUser: (user: User | null) => void;
  setAccessToken: (token: string | null) => void;
  refreshCurrentUser: () => Promise<void>;
  logout: () => void;
}

const UserContext = createContext<UserContextType | undefined>(undefined);

interface UserProviderProps {
  children: ReactNode;
}

export const UserProvider: React.FC<UserProviderProps> = ({ children }) => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [accessToken, setAccessTokenState] = useState<string | null>(localStorage.getItem('accessToken'));
  const [tenantName, setTenantName] = useState<string | null>(localStorage.getItem('tenantName'));

  const setAccessToken = (token: string | null) => {
    setAccessTokenState(token);
    if (token) {
      localStorage.setItem('accessToken', token);
    } else {
      localStorage.removeItem('accessToken');
    }
  };

  const applyAuthResponse = (response: AuthResponse) => {
    setCurrentUser(response.user);
    setTenantName(response.tenant?.name || null);
    if (response.accessToken) {
      setAccessToken(response.accessToken);
    }
    localStorage.setItem('currentUser', JSON.stringify(response.user));
    if (response.tenant?.name) {
      localStorage.setItem('tenantName', response.tenant.name);
    } else {
      localStorage.removeItem('tenantName');
    }
  };

  const refreshCurrentUser = async () => {
    if (!localStorage.getItem('accessToken')) {
      return;
    }

    const me = await authService.me();
    applyAuthResponse(me);
  };

  const logout = () => {
    setCurrentUser(null);
    setTenantName(null);
    setAccessToken(null);
    localStorage.removeItem('currentUser');
    localStorage.removeItem('tenantName');
  };

  useEffect(() => {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
      try {
        setCurrentUser(JSON.parse(savedUser));
      } catch {
        localStorage.removeItem('currentUser');
      }
    }

    refreshCurrentUser().catch(() => {
      logout();
    });
  }, []);

  const value = useMemo<UserContextType>(() => ({
    currentUser,
    accessToken,
    tenantName,
    isAuthenticated: !!currentUser && !!accessToken,
    setCurrentUser,
    setAccessToken,
    refreshCurrentUser,
    logout,
  }), [currentUser, accessToken, tenantName]);

  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
};

export const useUser = (): UserContextType => {
  const context = useContext(UserContext);
  if (context === undefined) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return context;
};
