import React, { useState, useCallback } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Box,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
  Button,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import PeopleIcon from '@mui/icons-material/People';
import CategoryIcon from '@mui/icons-material/Category';
import ReceiptIcon from '@mui/icons-material/Receipt';
import SettingsIcon from '@mui/icons-material/Settings';
import LogoutIcon from '@mui/icons-material/Logout';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import SecurityIcon from '@mui/icons-material/Security';
import AssignmentIndIcon from '@mui/icons-material/AssignmentInd';
import GroupsIcon from '@mui/icons-material/Groups';
import { useUser } from '../../context/UserContext';
import LogConsole from '../logs/LogConsole';

const drawerWidth = 240;

/**
 * Navigation item interface.
 */
interface NavItem {
  text: string;
  icon: React.ReactElement;
  path: string;
}

/**
 * Layout component providing the main application structure.
 * Includes app bar, navigation drawer, and content area.
 * Displays current user impersonation status in the app bar.
 * 
 * @returns Layout component with navigation
 */
const Layout: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [consoleHeight, setConsoleHeight] = useState(300);
  const navigate = useNavigate();
  const location = useLocation();
  const { currentUser, tenantName, logout } = useUser();
  const isGlobalAdmin = !!currentUser?.isGlobalAdmin;
  const isTenantAdmin = !!currentUser && (
    currentUser.tenantRole === 'Owner' ||
    currentUser.tenantRole === 'Admin' ||
    currentUser.roles?.includes('TenantAdmin')
  );

  const handleConsoleResize = useCallback((newHeight: number) => {
    setConsoleHeight(newHeight);
  }, []);

  /**
   * Navigation items configuration.
   */
  const navItems: NavItem[] = isGlobalAdmin
    ? []
    : [
      { text: 'Users', icon: <PeopleIcon />, path: '/' },
      { text: 'Categories', icon: <CategoryIcon />, path: '/categories' },
      { text: 'Transactions', icon: <ReceiptIcon />, path: '/transactions' },
      { text: 'Configuration', icon: <SettingsIcon />, path: '/configuration' },
    ];

  /**
   * Admin navigation items.
   */
  const adminItems: NavItem[] = isGlobalAdmin
    ? [{ text: 'Tenants', icon: <GroupsIcon />, path: '/admin/global-tenants' }]
    : isTenantAdmin
      ? [
        { text: 'Tenant Admin', icon: <GroupsIcon />, path: '/admin/tenant' },
        { text: 'Roles', icon: <SecurityIcon />, path: '/admin/roles' },
        { text: 'User Roles', icon: <AssignmentIndIcon />, path: '/admin/user-roles' },
      ]
      : [];

  /**
   * Handle drawer toggle for mobile view.
   */
  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  /**
   * Handle navigation item click.
   */
  const handleNavigation = (path: string) => {
    navigate(path);
    setMobileOpen(false);
  };

  /**
   * Handle clearing user impersonation.
   */
  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  /**
   * Drawer content component.
   */
  const drawer = (
    <div>
      <Toolbar>
        <Typography variant="h6" noWrap component="div">
          Minded Example
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {navItems.map((item) => (
          <ListItem key={item.text} disablePadding>
            <ListItemButton
              selected={location.pathname === item.path}
              onClick={() => handleNavigation(item.path)}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
      {adminItems.length > 0 && (
        <>
          <Divider />
          <List>
            <ListItem disablePadding>
              <ListItemButton disabled>
                <ListItemIcon><AdminPanelSettingsIcon /></ListItemIcon>
                <ListItemText primary="Admin" primaryTypographyProps={{ fontWeight: 'bold', fontSize: '0.85rem' }} />
              </ListItemButton>
            </ListItem>
            {adminItems.map((item) => (
              <ListItem key={item.text} disablePadding>
                <ListItemButton
                  selected={location.pathname === item.path}
                  onClick={() => handleNavigation(item.path)}
                  sx={{ pl: 4 }}
                >
                  <ListItemIcon>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.text} />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </>
      )}
    </div>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { sm: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            {[...navItems, ...adminItems].find(item => item.path === location.pathname)?.text || 'Minded Example'}
          </Typography>
          {currentUser && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <Typography variant="body2">
                {currentUser.name} {currentUser.surname} {(!isGlobalAdmin && tenantName) ? `(${tenantName})` : isGlobalAdmin ? '(Application Admin)' : ''}
              </Typography>
              <Button
                color="inherit"
                startIcon={<LogoutIcon />}
                onClick={handleLogout}
                size="small"
              >
                Logout
              </Button>
            </Box>
          )}
        </Toolbar>
      </AppBar>
      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true, // Better open performance on mobile.
          }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          height: '100vh',
          overflow: 'auto',
          paddingBottom: `${consoleHeight + 20}px`,
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
      <LogConsole initialHeight={300} minHeight={100} maxHeight={800} onResize={handleConsoleResize} />
    </Box>
  );
};

export default Layout;

