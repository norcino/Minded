import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Box, Button, FormControl, InputLabel, Link, MenuItem, Paper, Select, TextField, Typography } from '@mui/material';
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '../../api';
import { useUser } from '../../context/UserContext';
import { AuthResponse } from '../../types';

const RegisterPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [name, setName] = useState('');
  const [surname, setSurname] = useState('');
  const [tenantName, setTenantName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [mode, setMode] = useState<'create-tenant' | 'join-tenant'>('create-tenant');
  const [pendingMessage, setPendingMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { setCurrentUser, setAccessToken } = useUser();

  const inviteToken = useMemo(() => searchParams.get('inviteToken')?.trim() ?? '', [searchParams]);

  useEffect(() => {
    if (!inviteToken) {
      return;
    }

    setLoading(true);
    authService.getInviteDetails(inviteToken)
      .then((invite) => {
        setMode('join-tenant');
        setTenantName(invite.tenantName ?? '');
        if (invite.email) {
          setEmail(invite.email);
        }
      })
      .catch(() => {
        setError('Invite is invalid or expired.');
      })
      .finally(() => {
        setLoading(false);
      });
  }, [inviteToken]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setPendingMessage(null);
    setLoading(true);

    try {
      const response = await authService.register({
        name,
        surname,
        tenantName,
        email,
        password,
        mode,
        inviteToken: inviteToken || undefined,
      });

      if ('pendingApproval' in response && response.pendingApproval) {
        setPendingMessage(response.message || 'Registration submitted and pending tenant admin approval.');
        return;
      }

      const authResponse = response as AuthResponse;
      if (authResponse.accessToken) {
        setAccessToken(authResponse.accessToken);
      }
      setCurrentUser(authResponse.user);
      localStorage.setItem('currentUser', JSON.stringify(authResponse.user));
      if (authResponse.tenant?.name) {
        localStorage.setItem('tenantName', authResponse.tenant.name);
      }
      navigate('/');
    } catch {
      setError('Failed to register. Please review the data and retry.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 460, p: 3 }}>
        <Typography variant="h5" sx={{ mb: 2 }}>{inviteToken ? 'Join Tenant' : 'Create Account'}</Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {pendingMessage && <Alert severity="success" sx={{ mb: 2 }}>{pendingMessage}</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          {!inviteToken && (
            <FormControl fullWidth margin="normal">
              <InputLabel id="register-mode-label">Registration Type</InputLabel>
              <Select
                labelId="register-mode-label"
                value={mode}
                label="Registration Type"
                onChange={(e) => setMode(e.target.value as 'create-tenant' | 'join-tenant')}
              >
                <MenuItem value="create-tenant">Create a new tenant</MenuItem>
                <MenuItem value="join-tenant">Join an existing tenant</MenuItem>
              </Select>
            </FormControl>
          )}
          <TextField label="First name" fullWidth margin="normal" required value={name} onChange={e => setName(e.target.value)} />
          <TextField label="Surname" fullWidth margin="normal" required value={surname} onChange={e => setSurname(e.target.value)} />
          <TextField
            label="Tenant name"
            fullWidth
            margin="normal"
            required
            value={tenantName}
            onChange={e => setTenantName(e.target.value)}
            disabled={!!inviteToken}
          />
          <TextField label="Email" type="email" fullWidth margin="normal" required value={email} onChange={e => setEmail(e.target.value)} />
          <TextField label="Password" type="password" fullWidth margin="normal" required value={password} onChange={e => setPassword(e.target.value)} />
          <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }} disabled={loading}>
            {loading ? 'Creating account...' : mode === 'join-tenant' ? 'Submit Request' : 'Create Account'}
          </Button>
        </Box>
        <Box sx={{ mt: 2 }}>
          <Link component={RouterLink} to="/login">Already have an account? Sign in</Link>
        </Box>
      </Paper>
    </Box>
  );
};

export default RegisterPage;
