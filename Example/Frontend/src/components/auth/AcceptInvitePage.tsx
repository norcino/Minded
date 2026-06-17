import React, { useEffect } from 'react';
import { Alert, Box, Paper, Typography } from '@mui/material';
import { useNavigate, useSearchParams } from 'react-router-dom';

const AcceptInvitePage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token') ?? '';

  useEffect(() => {
    if (!token) {
      navigate('/register', { replace: true });
      return;
    }

    navigate(`/register?inviteToken=${encodeURIComponent(token)}`, { replace: true });
  }, [navigate, token]);

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 460, p: 3 }}>
        <Typography variant="h5" sx={{ mb: 2 }}>Preparing invitation</Typography>
        <Alert severity="info">Redirecting you to registration...</Alert>
      </Paper>
    </Box>
  );
};

export default AcceptInvitePage;
