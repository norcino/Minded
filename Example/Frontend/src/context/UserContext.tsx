import React, { createContext, useContext, useState, ReactNode } from 'react';
import { User } from '../types';

/**
 * User context interface defining the shape of the context.
 * Provides user impersonation functionality to allow viewing the application
 * as different users without authentication.
 */
interface UserContextType {
  currentUser: User | null;
  setCurrentUser: (user: User | null) => void;
  isImpersonating: boolean;
}

/**
 * User context for managing the currently impersonated user.
 * This allows the application to filter categories and transactions
 * based on the selected user.
 */
const UserContext = createContext<UserContextType | undefined>(undefined);

/**
 * Props for the UserProvider component.
 */
interface UserProviderProps {
  children: ReactNode;
}

/**
 * UserProvider component that wraps the application and provides
 * user impersonation functionality.
 * 
 * @param props Component props containing children
 * @returns Provider component with user context
 */
export const UserProvider: React.FC<UserProviderProps> = ({ children }) => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  const value: UserContextType = {
    currentUser,
    setCurrentUser,
    isImpersonating: currentUser !== null,
  };

  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
};

/**
 * Custom hook to access the user context.
 * Must be used within a UserProvider.
 * 
 * @returns User context value
 * @throws Error if used outside of UserProvider
 */
export const useUser = (): UserContextType => {
  const context = useContext(UserContext);
  if (context === undefined) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return context;
};

