import React, { useMemo, useState } from 'react';
import { Alert, Box, Button, Link, Paper, TextField, Typography } from '@mui/material';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import { authService } from '../../api';

const ResetPasswordPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [password, setPassword] = useState('');
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const token = useMemo(() => searchParams.get('token') ?? '', [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      await authService.resetPassword({ token, newPassword: password });
      setSuccess(true);
    } catch {
      setError('Reset link is invalid or expired.');
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 420, p: 3 }}>
        <Typography variant="h5" sx={{ mb: 2 }}>Reset Password</Typography>
        {success && <Alert severity="success" sx={{ mb: 2 }}>Password updated successfully.</Alert>}
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField label="New Password" type="password" fullWidth margin="normal" required value={password} onChange={e => setPassword(e.target.value)} />
          <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }} disabled={!token}>
            Reset Password
          </Button>
        </Box>
        <Box sx={{ mt: 2 }}>
          <Link component={RouterLink} to="/login">Back to login</Link>
        </Box>
      </Paper>
    </Box>
  );
};

export default ResetPasswordPage;
