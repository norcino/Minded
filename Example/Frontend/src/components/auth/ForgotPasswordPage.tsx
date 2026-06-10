import React, { useState } from 'react';
import { Alert, Box, Button, Link, Paper, TextField, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { authService } from '../../api';

const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await authService.forgotPassword({ email });
    setSent(true);
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 420, p: 3 }}>
        <Typography variant="h5" sx={{ mb: 2 }}>Forgot Password</Typography>
        {sent && <Alert severity="success" sx={{ mb: 2 }}>If the account exists, reset instructions have been issued.</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField label="Email" type="email" fullWidth margin="normal" required value={email} onChange={e => setEmail(e.target.value)} />
          <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }}>
            Send Reset Link
          </Button>
        </Box>
        <Box sx={{ mt: 2 }}>
          <Link component={RouterLink} to="/login">Back to login</Link>
        </Box>
      </Paper>
    </Box>
  );
};

export default ForgotPasswordPage;
