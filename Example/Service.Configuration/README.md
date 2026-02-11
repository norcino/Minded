# Service.Configuration

This service implements runtime configuration management using the Minded CQRS pattern. It provides a REST API for viewing and updating application configuration values at runtime without requiring application restart.

## Overview

The Configuration service manages application settings across multiple categories:
- **System** - Core system settings (logging level)
- **Logging** - Logging configuration (templates, sensitive data, outcome entries)
- **Exception** - Exception handling settings
- **Retry** - Retry policy configuration
- **DataProtection** - Data protection and sanitization settings
- **Transaction** - Database transaction settings

## Architecture

This service follows the Minded framework's CQRS pattern with complete separation of concerns:

```
Service.Configuration/
├── Command/
│   └── UpdateConfigurationCommand.cs       # Command to update configuration value
├── CommandHandler/
│   └── UpdateConfigurationCommandHandler.cs # Handles configuration updates
├── Query/
│   ├── GetAllConfigurationsQuery.cs        # Query to retrieve all configurations
│   └── GetConfigurationByKeyQuery.cs       # Query to retrieve specific configuration
├── QueryHandler/
│   ├── GetAllConfigurationsQueryHandler.cs # Handles retrieving all configurations
│   └── GetConfigurationByKeyQueryHandler.cs # Handles retrieving single configuration
└── Validator/
    ├── GetConfigurationByKeyQueryValidator.cs # Validates configuration key exists
    └── UpdateConfigurationCommandValidator.cs # Validates update requests
```

## Features

### ✅ Runtime Configuration Updates
- Update configuration values without application restart
- Type-safe value conversion (bool, int, string)
- Immediate effect for supported settings

### ✅ Special Handling
- **Serilog Logging Level** - Dynamically updates the logging level via `LoggingLevelSwitch`
- **Type Conversion** - Automatic conversion and validation of configuration values

### ✅ Validation
- Key existence validation (returns 404 if configuration key not found)
- Type compatibility validation (ensures value can be converted to expected type)
- Null value validation

### ✅ Metadata Provider
- Centralized configuration metadata (name, type, description, default value)
- Category-based organization
- Supports dynamic value providers for runtime-calculated defaults

## API Endpoints

### Get All Configurations
```http
GET /api/configurations
```

**Response:**
```json
[
  {
    "key": "System.MinimumLogLevel",
    "category": "System",
    "name": "Minimum Log Level",
    "type": "string",
    "value": "Information",
    "defaultValue": "Information",
    "description": "Minimum log level for Serilog (Verbose, Debug, Information, Warning, Error, Fatal)"
  },
  {
    "key": "Logging.Enabled",
    "category": "Logging",
    "name": "Logging Enabled",
    "type": "bool",
    "value": true,
    "defaultValue": true,
    "description": "Enable or disable logging decorator"
  }
]
```

### Get Configuration by Key
```http
GET /api/configurations/{key}
```

**Example:**
```http
GET /api/configurations/System.MinimumLogLevel
```

**Response:**
```json
{
  "key": "System.MinimumLogLevel",
  "category": "System",
  "name": "Minimum Log Level",
  "type": "string",
  "value": "Information",
  "defaultValue": "Information",
  "description": "Minimum log level for Serilog (Verbose, Debug, Information, Warning, Error, Fatal)"
}
```

**Error Response (404):**
```json
{
  "successful": false,
  "outcomeEntries": [
    {
      "propertyName": "Key",
      "message": "Configuration key 'InvalidKey' not found",
      "errorCode": "404",
      "severity": "Error"
    }
  ]
}
```

### Update Configuration
```http
PUT /api/configurations/{key}
Content-Type: application/json

{
  "value": "Debug"
}
```

**Example:**
```http
PUT /api/configurations/System.MinimumLogLevel
Content-Type: application/json

{
  "value": "Debug"
}
```

**Response:**
```json
{
  "successful": true,
  "result": {
    "key": "System.MinimumLogLevel",
    "category": "System",
    "name": "Minimum Log Level",
    "type": "string",
    "value": "Debug",
    "defaultValue": "Information",
    "description": "Minimum log level for Serilog (Verbose, Debug, Information, Warning, Error, Fatal)"
  }
}
```

**Error Response (400 - Invalid Type):**
```json
{
  "successful": false,
  "outcomeEntries": [
    {
      "propertyName": "Value",
      "message": "Value 'invalid' cannot be converted to type 'bool'",
      "errorCode": "400",
      "severity": "Error"
    }
  ]
}
```

## Configuration Categories

### System Configuration
- `System.MinimumLogLevel` - Serilog minimum log level (Verbose, Debug, Information, Warning, Error, Fatal)

### Logging Configuration
- `Logging.Enabled` - Enable/disable logging decorator
- `Logging.LogMessageTemplateData` - Include message template data in logs
- `Logging.LogOutcomeEntries` - Include outcome entries in logs
- `Logging.LogResult` - Include command/query results in logs

### Exception Configuration
- `Exception.Serialize` - Serialize exceptions in error responses

### Retry Configuration
- `Retry.DefaultRetryCount` - Default number of retry attempts
- `Retry.DefaultDelay1` - First retry delay (ms)
- `Retry.DefaultDelay2` - Second retry delay (ms)
- `Retry.DefaultDelay3` - Third retry delay (ms)
- `Retry.DefaultDelay4` - Fourth retry delay (ms)
- `Retry.DefaultDelay5` - Fifth retry delay (ms)
- `Retry.DefaultDelay6` - Sixth retry delay (ms)

### Data Protection Configuration
- `DataProtection.ShowSensitiveData` - Show/hide sensitive data in logs

### Transaction Configuration
- `Transaction.DefaultIsolationLevel` - Default transaction isolation level
- `Transaction.DefaultTimeoutSeconds` - Default transaction timeout
- `Transaction.EnableDistributedTransactions` - Enable distributed transactions
- `Transaction.MaxRetryAttempts` - Maximum transaction retry attempts
- `Transaction.RetryDelayMilliseconds` - Delay between transaction retries

## Implementation Details

### Commands

#### UpdateConfigurationCommand
Updates a configuration value with type conversion and special handling.

```csharp
[ValidateCommand]
public class UpdateConfigurationCommand : ICommand<ConfigurationEntry>, ILoggable
{
    public string Key { get; }
    public UpdateConfigurationRequest Request { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
    
    public string LoggingTemplate => "Updating configuration: {Key} = {Value}";
    public string[] LoggingProperties => new[] { nameof(Key), "Request.Value" };
}
```

**Handler Features:**
- Type conversion (bool, int, string)
- Special handling for `System.MinimumLogLevel` - updates Serilog `LoggingLevelSwitch`
- Updates `RuntimeConfigurationStore` for persistence

### Queries

#### GetAllConfigurationsQuery
Retrieves all configuration entries with current values.

```csharp
public class GetAllConfigurationsQuery : IQuery<IQueryResponse<IEnumerable<ConfigurationEntry>>>, ILoggable
{
    public Guid TraceId { get; } = Guid.NewGuid();
    
    public string LoggingTemplate => "Retrieving all configuration entries";
    public string[] LoggingProperties => Array.Empty<string>();
}
```

**Handler Features:**
- Combines metadata from `ConfigurationMetadataProvider`
- Retrieves current values from `RuntimeConfigurationStore`
- Returns complete configuration entries with all metadata

#### GetConfigurationByKeyQuery
Retrieves a specific configuration entry by key.

```csharp
[ValidateQuery]
public class GetConfigurationByKeyQuery : IQuery<ConfigurationEntry>, ILoggable
{
    public string Key { get; }
    public Guid TraceId { get; } = Guid.NewGuid();
    
    public string LoggingTemplate => "ConfigurationKey: {Key}";
    public string[] LoggingProperties => new[] { nameof(Key) };
}
```

**Validation:**
- Key must not be empty
- Key must exist in metadata (returns 404 if not found)

### Validators

#### UpdateConfigurationCommandValidator
Validates configuration update requests.

**Validation Rules:**
1. Key must not be empty
2. Key must exist in metadata (404 if not found)
3. Value must not be null
4. Value must be convertible to the expected type

#### GetConfigurationByKeyQueryValidator
Validates configuration retrieval requests.

**Validation Rules:**
1. Key must not be empty
2. Key must exist in metadata (404 if not found)

## Dependencies

- **Minded.Framework.CQRS** - CQRS abstractions and implementations
- **Minded.Extensions.Validation** - Validation decorator and abstractions
- **Minded.Extensions.Logging** - Logging decorator
- **Minded.Extensions.Exception** - Exception handling decorator
- **Common.Configuration** - Shared configuration services (`RuntimeConfigurationStore`, `ConfigurationMetadataProvider`)
- **Data.Entity** - Entity models (`ConfigurationEntry`, `UpdateConfigurationRequest`)
- **Serilog** - For `LoggingLevelSwitch` integration

## Testing

### Unit Tests
Located in `Tests/Service.Configuration.Tests/`:
- `UpdateConfigurationCommandValidatorTest` - Validation logic tests
- `GetConfigurationByKeyQueryValidatorTest` - Query validation tests

### E2E Tests
Located in `Tests/Application.Api.E2ETests/ConfigurationE2ETests.cs`:
- GET all configurations
- GET configuration by key (success and 404)
- PUT update configuration (success, 404, and type validation)

## Usage Example

### Changing Log Level at Runtime

```bash
# Get current log level
curl -X GET https://localhost:5001/api/configurations/System.MinimumLogLevel

# Update to Debug level
curl -X PUT https://localhost:5001/api/configurations/System.MinimumLogLevel \
  -H "Content-Type: application/json" \
  -d '{"value": "Debug"}'

# Verify change
curl -X GET https://localhost:5001/api/configurations/System.MinimumLogLevel
```

### Enabling Sensitive Data Logging

```bash
# Show sensitive data in logs (for debugging)
curl -X PUT https://localhost:5001/api/configurations/DataProtection.ShowSensitiveData \
  -H "Content-Type: application/json" \
  -d '{"value": true}'

# Hide sensitive data in logs (for production)
curl -X PUT https://localhost:5001/api/configurations/DataProtection.ShowSensitiveData \
  -H "Content-Type: application/json" \
  -d '{"value": false}'
```

## Learn More

- **Minded Framework Documentation**: [../../README.md](../../README.md)
- **Example Application**: [../README.md](../README.md)
- **Configuration Metadata**: [../Common.Configuration/ConfigurationMetadataProvider.cs](../Common.Configuration/ConfigurationMetadataProvider.cs)
- **Runtime Configuration Store**: [../Common.Configuration/RuntimeConfigurationStore.cs](../Common.Configuration/RuntimeConfigurationStore.cs)

