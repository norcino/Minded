import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Switch,
  TextField,
  FormControlLabel,
  Button,
  Alert,
  CircularProgress,
  Grid,
  Chip,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import RestartAltIcon from '@mui/icons-material/RestartAlt';
import configurationService, { ConfigurationEntry } from '../../api/configurationService';

/**
 * Configuration page component for managing runtime configuration of Minded decorators.
 * Allows viewing and updating configuration options at runtime without application restart.
 */
const ConfigurationPage: React.FC = () => {
  const [configurations, setConfigurations] = useState<ConfigurationEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [expandedCategories, setExpandedCategories] = useState<string[]>(['System', 'Logging']);

  useEffect(() => {
    loadConfigurations();
  }, []);

  const loadConfigurations = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await configurationService.getAll();
      setConfigurations(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load configurations');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleCategory = (category: string) => {
    setExpandedCategories(prev =>
      prev.includes(category)
        ? prev.filter(c => c !== category)
        : [...prev, category]
    );
  };

  const handleValueChange = async (entry: ConfigurationEntry, newValue: any) => {
    try {
      setError(null);
      setSuccess(null);
      
      // Convert value based on type
      let convertedValue = newValue;
      if (entry.type === 'bool') {
        convertedValue = Boolean(newValue);
      } else if (entry.type === 'int') {
        convertedValue = parseInt(newValue, 10);
        if (isNaN(convertedValue)) {
          setError(`Invalid number value for ${entry.name}`);
          return;
        }
      }

      const updated = await configurationService.update(entry.key, convertedValue);
      
      // Update local state
      setConfigurations(prev =>
        prev.map(c => (c.key === entry.key ? updated : c))
      );
      
      setSuccess(`Updated ${entry.category}.${entry.name}`);
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError(err.message || `Failed to update ${entry.name}`);
    }
  };

  const handleReset = async (entry: ConfigurationEntry) => {
    try {
      setError(null);
      setSuccess(null);
      const updated = await configurationService.reset(entry.key, entry.defaultValue);
      
      // Update local state
      setConfigurations(prev =>
        prev.map(c => (c.key === entry.key ? updated : c))
      );
      
      setSuccess(`Reset ${entry.category}.${entry.name} to default`);
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError(err.message || `Failed to reset ${entry.name}`);
    }
  };

  const handleResetAll = async () => {
    try {
      setError(null);
      setSuccess(null);
      setLoading(true);
      const updated = await configurationService.resetAll();
      setConfigurations(updated);
      setSuccess('All configurations reset to defaults');
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to reset all configurations');
    } finally {
      setLoading(false);
    }
  };

  /**
   * Get dropdown options for enum-based configuration fields.
   */
  const getDropdownOptions = (key: string): string[] | null => {
    switch (key) {
      case 'System.MinimumLogLevel':
        return ['Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal'];
      case 'Logging.MinimumOutcomeSeverityLevel':
        return ['Info', 'Warning', 'Error'];
      case 'Transaction.DefaultTransactionScopeOption':
        return ['Required', 'RequiresNew', 'Suppress'];
      case 'Transaction.DefaultIsolationLevel':
        return ['ReadCommitted', 'ReadUncommitted', 'RepeatableRead', 'Serializable'];
      default:
        return null;
    }
  };

  const renderConfigControl = (entry: ConfigurationEntry) => {
    const isModified = entry.value !== entry.defaultValue;

    if (entry.type === 'bool') {
      return (
        <Box display="flex" alignItems="center" gap={2}>
          <FormControlLabel
            control={
              <Switch
                checked={Boolean(entry.value)}
                onChange={(e) => handleValueChange(entry, e.target.checked)}
                color="primary"
              />
            }
            label={entry.value ? 'Enabled' : 'Disabled'}
          />
          {isModified && (
            <Chip label="Modified" color="warning" size="small" />
          )}
          <Button
            size="small"
            startIcon={<RestartAltIcon />}
            onClick={() => handleReset(entry)}
            disabled={!isModified}
          >
            Reset
          </Button>
        </Box>
      );
    }

    if (entry.type === 'int') {
      return (
        <Box display="flex" alignItems="center" gap={2}>
          <TextField
            type="number"
            value={entry.value}
            onChange={(e) => handleValueChange(entry, e.target.value)}
            size="small"
            sx={{ width: 150 }}
          />
          {isModified && (
            <Chip label="Modified" color="warning" size="small" />
          )}
          <Button
            size="small"
            startIcon={<RestartAltIcon />}
            onClick={() => handleReset(entry)}
            disabled={!isModified}
          >
            Reset
          </Button>
        </Box>
      );
    }

    if (entry.type === 'string') {
      const dropdownOptions = getDropdownOptions(entry.key);

      // Use dropdown for enum-based fields
      if (dropdownOptions) {
        return (
          <Box display="flex" alignItems="center" gap={2}>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel>{entry.name}</InputLabel>
              <Select
                value={entry.value}
                label={entry.name}
                onChange={(e) => handleValueChange(entry, e.target.value)}
              >
                {dropdownOptions.map((option) => (
                  <MenuItem key={option} value={option}>
                    {option}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {isModified && (
              <Chip label="Modified" color="warning" size="small" />
            )}
            <Button
              size="small"
              startIcon={<RestartAltIcon />}
              onClick={() => handleReset(entry)}
              disabled={!isModified}
            >
              Reset
            </Button>
          </Box>
        );
      }

      // Use text field for other string fields
      return (
        <Box display="flex" alignItems="center" gap={2}>
          <TextField
            value={entry.value}
            onChange={(e) => handleValueChange(entry, e.target.value)}
            size="small"
            sx={{ width: 250 }}
          />
          {isModified && (
            <Chip label="Modified" color="warning" size="small" />
          )}
          <Button
            size="small"
            startIcon={<RestartAltIcon />}
            onClick={() => handleReset(entry)}
            disabled={!isModified}
          >
            Reset
          </Button>
        </Box>
      );
    }

    return <Typography>Unsupported type: {entry.type}</Typography>;
  };

  // Group configurations by category
  const groupedConfigurations = configurations.reduce((acc, entry) => {
    if (!acc[entry.category]) {
      acc[entry.category] = [];
    }
    acc[entry.category].push(entry);
    return acc;
  }, {} as Record<string, ConfigurationEntry[]>);

  if (loading && configurations.length === 0) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Runtime Configuration
        </Typography>
        <Button
          variant="outlined"
          color="warning"
          startIcon={<RestartAltIcon />}
          onClick={handleResetAll}
          disabled={loading}
        >
          Reset All to Defaults
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Configure Minded decorator options at runtime. Changes apply immediately without application restart.
      </Typography>

      {Object.entries(groupedConfigurations).map(([category, entries]) => (
        <Accordion
          key={category}
          expanded={expandedCategories.includes(category)}
          onChange={() => handleToggleCategory(category)}
          sx={{ mb: 1 }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Typography variant="h6">{category} Options</Typography>
            <Chip
              label={`${entries.length} option${entries.length !== 1 ? 's' : ''}`}
              size="small"
              sx={{ ml: 2 }}
            />
          </AccordionSummary>
          <AccordionDetails>
            <Grid container spacing={3}>
              {entries.map((entry) => (
                <Grid item xs={12} key={entry.key}>
                  <Paper sx={{ p: 2 }}>
                    <Typography variant="subtitle1" fontWeight="bold" gutterBottom>
                      {entry.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      {entry.description}
                    </Typography>
                    <Box display="flex" alignItems="center" gap={1} mb={1}>
                      <Typography variant="caption" color="text.secondary">
                        Type: {entry.type}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        • Default: {String(entry.defaultValue)}
                      </Typography>
                    </Box>
                    {renderConfigControl(entry)}
                  </Paper>
                </Grid>
              ))}
            </Grid>
          </AccordionDetails>
        </Accordion>
      ))}
    </Box>
  );
};

export default ConfigurationPage;

