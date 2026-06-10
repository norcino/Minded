import React, { useState } from 'react';
import { Alert, Box, Button, IconButton, InputAdornment, Link, Paper, TextField, Typography } from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { authService } from '../../api';
import { useUser } from '../../context/UserContext';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { setCurrentUser, setAccessToken } = useUser();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const response = await authService.login({ email, password });
      if (response.accessToken) {
        setAccessToken(response.accessToken);
      }
      setCurrentUser(response.user);
      localStorage.setItem('currentUser', JSON.stringify(response.user));
      if (response.tenant?.name) {
        localStorage.setItem('tenantName', response.tenant.name);
      }
      navigate('/');
    } catch {
      setError('Invalid email or password.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 460, p: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
          <Box
            component="img"
            src="/Minded-128.png"
            alt="Minded logo"
            sx={{ width: 40, height: 40, borderRadius: 1 }}
          />
          <Box>
            <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
              Minded Example Application
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Sign in to continue
            </Typography>
          </Box>
        </Box>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            label="Email"
            type="email"
            fullWidth
            margin="normal"
            required
            value={email}
            onChange={e => setEmail(e.target.value)}
          />
          <TextField
            label="Password"
            type={showPassword ? 'text' : 'password'}
            fullWidth
            margin="normal"
            required
            value={password}
            onChange={e => setPassword(e.target.value)}
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      edge="end"
                      onClick={() => setShowPassword(prev => !prev)}
                      onMouseDown={e => e.preventDefault()}
                      aria-label={showPassword ? 'Hide password' : 'Show password'}
                    >
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                )
              }
            }}
          />
          <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }} disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In'}
          </Button>
        </Box>
        <Alert severity="info" sx={{ mt: 2 }}>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            Default system admin credentials
          </Typography>
          <Typography variant="body2">Email: admin@example.com</Typography>
          <Typography variant="body2">Password: Admin1!</Typography>
        </Alert>
        <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between' }}>
          <Link component={RouterLink} to="/register">Sign up</Link>
          <Link component={RouterLink} to="/forgot-password">Forgot password?</Link>
        </Box>
      </Paper>
    </Box>
  );
};

export default LoginPage;
