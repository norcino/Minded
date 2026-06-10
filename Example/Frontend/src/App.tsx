import { BrowserRouter, Navigate, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { UserProvider } from './context/UserContext';
import { useUser } from './context/UserContext';
import { theme } from './theme/theme';
import Layout from './components/layout/Layout';
import UserList from './components/users/UserList';
import CategoryList from './components/categories/CategoryList';
import TransactionList from './components/transactions/TransactionList';
import ConfigurationPage from './components/configuration/ConfigurationPage';
import RoleManagement from './components/admin/RoleManagement';
import UserRoleAssignment from './components/admin/UserRoleAssignment';
import TenantAdminPage from './components/admin/TenantAdminPage';
import RequireAuth from './components/auth/RequireAuth';
import LoginPage from './components/auth/LoginPage';
import RegisterPage from './components/auth/RegisterPage';
import ForgotPasswordPage from './components/auth/ForgotPasswordPage';
import ResetPasswordPage from './components/auth/ResetPasswordPage';
import AcceptInvitePage from './components/auth/AcceptInvitePage';
import GlobalTenantManagementPage from './components/admin/GlobalTenantManagementPage';

const HomePage: React.FC = () => {
  const { currentUser } = useUser();

  if (currentUser?.isGlobalAdmin) {
    return <Navigate to="/admin/global-tenants" replace />;
  }

  return <UserList />;
};

/**
 * Main App component.
 * Sets up routing, theming, and global context providers.
 * Provides navigation between Users, Categories, Transactions, and Configuration pages.
 */
function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <UserProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/accept-invite" element={<AcceptInvitePage />} />
            <Route element={<RequireAuth />}>
              <Route path="/" element={<Layout />}>
                <Route index element={<HomePage />} />
                <Route path="categories" element={<CategoryList />} />
                <Route path="transactions" element={<TransactionList />} />
                <Route path="configuration" element={<ConfigurationPage />} />
                <Route path="admin/roles" element={<RoleManagement />} />
                <Route path="admin/user-roles" element={<UserRoleAssignment />} />
                <Route path="admin/tenant" element={<TenantAdminPage />} />
                <Route path="admin/global-tenants" element={<GlobalTenantManagementPage />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </UserProvider>
    </ThemeProvider>
  );
}

export default App;

