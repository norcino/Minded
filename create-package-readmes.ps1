# Package README configurations
$packages = @(
    @{
        Path = "Framework/Minded.Framework.CQRS.Abstractions"
        Name = "Minded.Framework.CQRS.Abstractions"
        Description = "Core abstractions for CQRS pattern implementation including ICommand, IQuery, ICommandHandler, IQueryHandler interfaces and response types."
        Features = @(
            "Command and Query interfaces",
            "Handler abstractions",
            "Response types with success/failure tracking",
            "Outcome tracking for validation and business rules"
        )
    },
    @{
        Path = "Framework/Minded.Framework.CQRS"
        Name = "Minded.Framework.CQRS"
        Description = "Core CQRS implementation with command and query processing, validation, and business rules support."
        Features = @(
            "Command and Query base classes",
            "Validation rule processing",
            "Business rule evaluation",
            "Response outcome management"
        )
    },
    @{
        Path = "Framework/Minded.Framework.Decorator"
        Name = "Minded.Framework.Decorator"
        Description = "Base classes and utilities for implementing the Decorator pattern in CQRS pipelines."
        Features = @(
            "Decorator base classes for commands and queries",
            "Pipeline composition support",
            "Cross-cutting concern infrastructure"
        )
    },
    @{
        Path = "Framework/Minded.Framework.Mediator.Abstractions"
        Name = "Minded.Framework.Mediator.Abstractions"
        Description = "Mediator pattern abstractions for decoupled command and query dispatching."
        Features = @(
            "IMediator interface",
            "Command and Query dispatching contracts",
            "Async/await support with CancellationToken"
        )
    },
    @{
        Path = "Framework/Minded.Framework.Mediator"
        Name = "Minded.Framework.Mediator"
        Description = "Mediator pattern implementation for dispatching commands and queries to their handlers."
        Features = @(
            "Automatic handler resolution",
            "Decorator chain execution",
            "Dependency injection integration"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Configuration"
        Name = "Minded.Extensions.Configuration"
        Description = "Configuration infrastructure for Minded Framework including MindedBuilder for fluent decorator registration."
        Features = @(
            "Fluent decorator registration API",
            "Service collection extensions",
            "Assembly scanning utilities"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Validation"
        Name = "Minded.Extensions.Validation"
        Description = "Validation decorator for automatic command and query validation before handler execution."
        Features = @(
            "Automatic validation execution",
            "FluentValidation integration",
            "Validation result aggregation",
            "Configurable validation behavior"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Validation.Abstractions"
        Name = "Minded.Extensions.Validation.Abstractions"
        Description = "Validation abstractions including IValidator interface and validation attributes."
        Features = @(
            "IValidator interface",
            "Validation attributes",
            "Validation result types"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Exception"
        Name = "Minded.Extensions.Exception"
        Description = "Exception handling decorator with automatic error logging and graceful failure handling."
        Features = @(
            "Automatic exception catching and logging",
            "OperationCanceledException special handling",
            "Configurable error responses",
            "Integration with Microsoft.Extensions.Logging"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Logging"
        Name = "Minded.Extensions.Logging"
        Description = "Logging decorator for automatic request/response logging with configurable detail levels and outcome tracking."
        Features = @(
            "Request and response logging",
            "Outcome entry logging with severity filtering",
            "Dynamic configuration via providers (feature flags support)",
            "Template data logging control"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Retry"
        Name = "Minded.Extensions.Retry"
        Description = "Retry decorator for handling transient failures with configurable retry policies and exponential backoff."
        Features = @(
            "Configurable retry count and delays",
            "Exponential backoff support",
            "Per-command/query retry configuration",
            "Detailed retry attempt logging"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Transaction"
        Name = "Minded.Extensions.Transaction"
        Description = "Transaction decorator for automatic database transaction management with support for nested transactions and configurable isolation levels."
        Features = @(
            "Automatic transaction scope management",
            "Nested transaction support",
            "Configurable isolation levels",
            "Automatic rollback on failure",
            "Timeout configuration"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Caching.Abstractions"
        Name = "Minded.Extensions.Caching.Abstractions"
        Description = "Caching abstractions including cache key generation interfaces and attributes."
        Features = @(
            "IGenerateCacheKey interface",
            "Cache attribute definitions",
            "Cache key generation contracts"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.Caching.Memory"
        Name = "Minded.Extensions.Caching.Memory"
        Description = "In-memory caching decorator for query result caching with automatic cache key generation."
        Features = @(
            "Automatic query result caching",
            "Configurable cache duration",
            "Memory cache integration",
            "Cache key generation"
        )
    },
    @{
        Path = "Extensions/Minded.Extensions.WebApi"
        Name = "Minded.Extensions.WebApi"
        Description = "ASP.NET Core Web API integration with RestMediator for automatic HTTP status code mapping and response formatting."
        Features = @(
            "RestMediator for HTTP-aware command/query processing",
            "Automatic HTTP status code mapping",
            "Validation error formatting",
            "Cancellation token support (HTTP 499)"
        )
    }
)

$readmeTemplate = @"
# {0}

{1}

## Features

{2}

## Installation

``````bash
dotnet add package {0}
``````

## Usage

See the [main documentation](https://github.com/norcino/Minded) for comprehensive usage examples.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/norcino/Minded/blob/master/LICENSE) file for details.

## Links

- [GitHub Repository](https://github.com/norcino/Minded)
- [NuGet Package](https://www.nuget.org/packages/{0})
- [Documentation](https://github.com/norcino/Minded#readme)
- [Changelog](https://github.com/norcino/Minded/blob/master/Changelog.md)
"@

foreach ($pkg in $packages) {
    $features = ($pkg.Features | ForEach-Object { "- $_" }) -join "`n"
    $readmeContent = $readmeTemplate -f $pkg.Name, $pkg.Description, $features
    
    $readmePath = Join-Path $pkg.Path "README.md"
    Set-Content -Path $readmePath -Value $readmeContent
    Write-Host "Created README for $($pkg.Name)" -ForegroundColor Green
    
    # Update .csproj to include PackageReadme
    $csprojPath = Join-Path $pkg.Path "$($pkg.Name).csproj"
    if (Test-Path $csprojPath) {
        $csprojContent = Get-Content $csprojPath -Raw
        
        # Add PackageReadme if not already present
        if ($csprojContent -notmatch "<PackageReadme>") {
            # Find the PropertyGroup with package metadata and add PackageReadme
            $csprojContent = $csprojContent -replace "(<PackageIcon>.*?</PackageIcon>)", "`$1`n    <PackageReadme>README.md</PackageReadme>"
            Set-Content -Path $csprojPath -Value $csprojContent -NoNewline
            Write-Host "  Added PackageReadme to $($pkg.Name).csproj" -ForegroundColor Cyan
        }
        
        # Add README.md to ItemGroup if not already present
        if ($csprojContent -notmatch 'Include="README.md"') {
            $csprojContent = Get-Content $csprojPath -Raw
            $csprojContent = $csprojContent -replace '(<None Include="Minded-128.png".*?/>)', "`$1`n    <None Include=`"README.md`" Pack=`"true`" PackagePath=`"\`" />"
            Set-Content -Path $csprojPath -Value $csprojContent -NoNewline
            Write-Host "  Added README.md to ItemGroup in $($pkg.Name).csproj" -ForegroundColor Cyan
        }
    }
}

Write-Host "`nAll package READMEs created and .csproj files updated!" -ForegroundColor Green

