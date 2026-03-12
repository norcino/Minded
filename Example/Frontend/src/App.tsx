import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { UserProvider } from './context/UserContext';
import { theme } from './theme/theme';
import Layout from './components/layout/Layout';
import UserList from './components/users/UserList';
import CategoryList from './components/categories/CategoryList';
import TransactionList from './components/transactions/TransactionList';
import ConfigurationPage from './components/configuration/ConfigurationPage';

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
            <Route path="/" element={<Layout />}>
              <Route index element={<UserList />} />
              <Route path="categories" element={<CategoryList />} />
              <Route path="transactions" element={<TransactionList />} />
              <Route path="configuration" element={<ConfigurationPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </UserProvider>
    </ThemeProvider>
  );
}

export default App;

