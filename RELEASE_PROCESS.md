# Minded Framework - Release Process

This document describes the release process for Minded Framework NuGet packages, including versioning strategy, branch conventions, and step-by-step workflows.

## Table of Contents

- [Overview](#overview)
- [Branch-Based Versioning Strategy](#branch-based-versioning-strategy)
- [Version Management](#version-management)
- [Release Workflows](#release-workflows)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## Overview

Minded Framework uses a **branch-based versioning strategy** with automatic NuGet package publishing through Azure DevOps pipelines. The version is controlled by `.csproj` files, and the pipeline automatically applies appropriate pre-release suffixes based on the branch name.

### Publishing Targets

- **Internal Azure DevOps Feed**: All branches (for internal testing)
- **NuGet.org**: Only branches following naming conventions (stable and pre-release packages)

---

## Branch-Based Versioning Strategy

The Azure DevOps pipeline automatically determines the version suffix and publishing strategy based on the branch name:

| Branch Pattern | Version Suffix | Package Type | Push to NuGet.org | Use Case |
|----------------|----------------|--------------|-------------------|----------|
| `master` | None | Stable | ✅ Yes | Production releases |
| `dev` or `develop` | `-preview.{buildNumber}` | Pre-release | ✅ Yes | Development snapshots |
| `release/*` | `-rc.{buildNumber}` | Pre-release | ✅ Yes | Release candidates |
| `feature/*` or `features/*` | `-alpha.{buildNumber}` | Pre-release | ✅ Yes | Feature testing |
| **Other** | `-alpha.{buildNumber}` | Pre-release | ❌ No | Internal only |

### Version Format

Packages follow [Semantic Versioning 2.0](https://semver.org/):

```
{Major}.{Minor}.{Patch}[-{PreReleaseLabel}.{BuildNumber}]
```

**Examples:**
- Stable: `1.2.0`
- Preview: `1.2.0-preview.20250211.1`
- Release Candidate: `1.2.0-rc.20250211.1`
- Alpha: `1.2.0-alpha.20250211.1`

---

## Version Management

### Updating Package Versions

Versions are controlled in each project's `.csproj` file using the `VersionPrefix` property:

```xml
<PropertyGroup>
  <AssemblyVersion>1.2.0</AssemblyVersion>
  <FileVersion>1.2.0</FileVersion>
  <VersionPrefix>1.2.0</VersionPrefix>
  <VersionSuffix></VersionSuffix>
</PropertyGroup>
```

**Important:**
- ✅ Update `VersionPrefix`, `AssemblyVersion`, and `FileVersion` together
- ✅ Leave `VersionSuffix` empty (pipeline handles this automatically)
- ✅ Follow [Semantic Versioning](https://semver.org/) rules:
  - **Major**: Breaking changes
  - **Minor**: New features (backward compatible)
  - **Patch**: Bug fixes (backward compatible)

### Which Projects to Update

When releasing, you typically need to update versions in:

1. **Changed packages** - Projects with actual code changes
2. **Dependent packages** - Projects that reference changed packages (if API changes)

**Example:** If you update `Minded.Framework.CQRS`, you may also need to update:
- `Minded.Extensions.CQRS.EntityFrameworkCore` (depends on CQRS)
- `Minded.Extensions.CQRS.OData` (depends on CQRS)

---

## Release Workflows

### 1. Development Preview Release

**Use Case:** Share development progress with early adopters

**Steps:**

```bash
# 1. Create or switch to develop branch
git checkout -b develop
# or
git checkout develop

# 2. Make your changes and commit
git add .
git commit -m "Add new feature X"

# 3. Push to trigger pipeline
git push origin develop
```

**Result:**
- Package: `Minded.Framework.CQRS.1.2.0-preview.20250211.1.nupkg`
- Published to: Internal feed + NuGet.org (as pre-release)

---

### 2. Feature Alpha Release

**Use Case:** Test a specific feature in isolation

**Steps:**

```bash
# 1. Create feature branch from develop or master
git checkout -b feature/add-caching-support

# 2. Update version in .csproj if needed (usually bump minor or patch)
# Edit: Framework/Minded.Framework.CQRS/Minded.Framework.CQRS.csproj
# Change: <VersionPrefix>1.2.0</VersionPrefix> to <VersionPrefix>1.3.0</VersionPrefix>

# 3. Implement feature and commit
git add .
git commit -m "Implement caching support"

# 4. Push to trigger pipeline
git push origin feature/add-caching-support
```

**Result:**
- Package: `Minded.Framework.CQRS.1.3.0-alpha.20250211.1.nupkg`
- Published to: Internal feed + NuGet.org (as pre-release)

---

### 3. Release Candidate

**Use Case:** Prepare for production release, final testing

**Steps:**

```bash
# 1. Create release branch from develop
git checkout develop
git checkout -b release/v1.3.0

# 2. Ensure versions are correct in all .csproj files
# All packages should have the target version (e.g., 1.3.0)

# 3. Update Changelog.md with release notes
# Document all changes, breaking changes, new features, bug fixes

# 4. Commit and push
git add .
git commit -m "Prepare release 1.3.0"
git push origin release/v1.3.0
```

**Result:**
- Package: `Minded.Framework.CQRS.1.3.0-rc.20250211.1.nupkg`
- Published to: Internal feed + NuGet.org (as pre-release)

**Testing:**
- Install RC packages in test projects
- Run integration tests
- Validate in real-world scenarios

**If issues found:**
```bash
# Fix issues in release branch
git checkout release/v1.3.0
# Make fixes
git commit -m "Fix issue in caching logic"
git push origin release/v1.3.0
# New RC will be created: 1.3.0-rc.20250211.2
```

---

### 4. Stable Production Release

**Use Case:** Official production release

**Steps:**

```bash
# 1. Merge release branch to master
git checkout master
git merge release/v1.3.0

# 2. Create Git tag
git tag -a v1.3.0 -m "Release version 1.3.0"

# 3. Push master and tags
git push origin master
git push origin v1.3.0
```

**Result:**
- Package: `Minded.Framework.CQRS.1.3.0.nupkg` (stable, no suffix)
- Published to: Internal feed + NuGet.org (as stable release)

**Post-Release:**
```bash
# 4. Merge back to develop
git checkout develop
git merge master

# 5. Bump version for next development cycle
# Edit .csproj files: <VersionPrefix>1.4.0</VersionPrefix>
git commit -am "Bump version to 1.4.0-dev"
git push origin develop

# 6. Delete release branch (optional)
git branch -d release/v1.3.0
git push origin --delete release/v1.3.0
```

---

### 5. Hotfix Release

**Use Case:** Critical bug fix for production

**Steps:**

```bash
# 1. Create hotfix branch from master
git checkout master
git checkout -b release/v1.3.1

# 2. Update version in affected .csproj files
# Change: <VersionPrefix>1.3.0</VersionPrefix> to <VersionPrefix>1.3.1</VersionPrefix>

# 3. Fix the bug
git add .
git commit -m "Fix critical security issue in validation"

# 4. Push to create RC
git push origin release/v1.3.1
# Creates: 1.3.1-rc.20250211.1

# 5. Test RC, then merge to master
git checkout master
git merge release/v1.3.1
git tag -a v1.3.1 -m "Hotfix release 1.3.1"
git push origin master
git push origin v1.3.1
# Creates: 1.3.1 (stable)

# 6. Merge back to develop
git checkout develop
git merge master
git push origin develop
```

---

## Examples

### Example 1: New Feature Development

**Scenario:** Adding retry logic to the Mediator package

```bash
# Start from develop
git checkout develop
git pull origin develop

# Create feature branch
git checkout -b feature/mediator-retry-logic

# Update version (minor bump for new feature)
# Edit: Framework/Minded.Framework.Mediator/Minded.Framework.Mediator.csproj
# Change: <VersionPrefix>1.1.0</VersionPrefix> → <VersionPrefix>1.2.0</VersionPrefix>

# Implement feature
# ... code changes ...

# Commit and push
git add .
git commit -m "Add retry logic to mediator"
git push origin feature/mediator-retry-logic

# Pipeline creates: Minded.Framework.Mediator.1.2.0-alpha.20250211.1
```

**Testing the alpha package:**

```bash
# In a test project
dotnet add package Minded.Framework.Mediator --version 1.2.0-alpha.20250211.1
```

**When ready to release:**

```bash
# Merge to develop
git checkout develop
git merge feature/mediator-retry-logic
git push origin develop
# Creates: 1.2.0-preview.20250211.2

# Create release branch
git checkout -b release/v1.2.0
git push origin release/v1.2.0
# Creates: 1.2.0-rc.20250211.1

# After testing, merge to master
git checkout master
git merge release/v1.2.0
git tag -a v1.2.0 -m "Release 1.2.0 - Add retry logic"
git push origin master --tags
# Creates: 1.2.0 (stable)
```

---

### Example 2: Multi-Package Release

**Scenario:** Updating CQRS framework and dependent extensions

**Packages to update:**
1. `Minded.Framework.CQRS` (1.0.7 → 1.1.0)
2. `Minded.Extensions.CQRS.EntityFrameworkCore` (depends on CQRS)
3. `Minded.Extensions.CQRS.OData` (depends on CQRS)

```bash
# Create release branch
git checkout -b release/v1.1.0

# Update all affected .csproj files
# 1. Framework/Minded.Framework.CQRS/Minded.Framework.CQRS.csproj
#    <VersionPrefix>1.1.0</VersionPrefix>
# 2. Extensions/Minded.Extensions.CQRS.EntityFrameworkCore/...csproj
#    <VersionPrefix>1.1.0</VersionPrefix>
# 3. Extensions/Minded.Extensions.CQRS.OData/...csproj
#    <VersionPrefix>1.1.0</VersionPrefix>

# Update Changelog.md
# Document changes for all three packages

# Commit and push
git add .
git commit -m "Release 1.1.0 - CQRS improvements"
git push origin release/v1.1.0
# Creates RC for all three packages:
# - Minded.Framework.CQRS.1.1.0-rc.20250211.1
# - Minded.Extensions.CQRS.EntityFrameworkCore.1.1.0-rc.20250211.1
# - Minded.Extensions.CQRS.OData.1.1.0-rc.20250211.1

# Test, then merge to master
git checkout master
git merge release/v1.1.0
git tag -a v1.1.0 -m "Release 1.1.0"
git push origin master --tags
# Creates stable versions of all three packages
```

---

### Example 3: Working on Non-Standard Branch

**Scenario:** You're on branch `Upgrade202511` (doesn't follow convention)

```bash
# Current branch doesn't follow convention
git branch
# * Upgrade202511

# Push triggers build
git push origin Upgrade202511

# Pipeline output:
# ⚠️ Warning: Branch does not follow naming convention
# Package will NOT be pushed to NuGet.org
# Creates: Minded.Framework.CQRS.1.0.7-alpha.20250211.1
# Published to: Internal feed ONLY

# To publish to NuGet.org, rename branch:
git checkout -b feature/upgrade-202511
git push origin feature/upgrade-202511
# Now creates alpha and pushes to NuGet.org
```

---

## Troubleshooting

### Issue: Package not appearing on NuGet.org

**Possible causes:**

1. **Branch doesn't follow naming convention**
   - Check branch name matches: `master`, `dev`, `develop`, `release/*`, `feature/*`
   - Pipeline logs will show: "Branch type: NON-STANDARD (alpha, internal only)"

2. **Build failed**
   - Check Azure DevOps pipeline logs
   - Look for compilation errors or test failures

3. **NuGet.org API key expired**
   - Verify `$(NugetApiKey)` variable in Azure DevOps
   - Regenerate key at https://www.nuget.org/account/apikeys

4. **Package already exists with same version**
   - NuGet.org doesn't allow overwriting published packages
   - Bump version in `.csproj` file
   - Or wait for next build (build number will increment)

### Issue: Wrong version suffix applied

**Check:**
- Branch name is correct
- Pipeline logs show correct branch detection
- `$(Build.BuildNumber)` variable is set correctly

### Issue: Pre-release package shows as stable

**Cause:** Missing version suffix in pipeline

**Solution:**
- Verify branch is not `master`
- Check pipeline variable `$(VersionSuffix)` is set
- Review pipeline logs for "Determine Version Suffix" step

---

## Best Practices

### 1. Branch Naming

✅ **DO:**
- Use lowercase for branch names: `feature/add-caching`
- Use hyphens for spaces: `feature/user-authentication`
- Be descriptive: `feature/add-retry-decorator`
- Use version in release branches: `release/v1.3.0`

❌ **DON'T:**
- Use spaces: `feature/add caching`
- Use underscores: `feature/add_caching`
- Use vague names: `feature/fix`, `feature/update`
- Forget version in release: `release/next`

### 2. Version Bumping

✅ **DO:**
- Bump **major** for breaking changes (1.x.x → 2.0.0)
- Bump **minor** for new features (1.1.x → 1.2.0)
- Bump **patch** for bug fixes (1.1.1 → 1.1.2)
- Update `AssemblyVersion`, `FileVersion`, and `VersionPrefix` together
- Document changes in `Changelog.md`

❌ **DON'T:**
- Skip versions (1.1.0 → 1.3.0)
- Reuse versions
- Forget to update dependent packages

### 3. Release Workflow

✅ **DO:**
- Test alpha/preview packages before RC
- Test RC packages before stable release
- Create Git tags for stable releases
- Merge release branches back to develop
- Update changelog with each release

❌ **DON'T:**
- Push directly to master
- Skip RC phase for major releases
- Delete release branches before merging back
- Forget to bump version after release

### 4. Testing Pre-Release Packages

```bash
# Install specific pre-release version
dotnet add package Minded.Framework.CQRS --version 1.2.0-preview.20250211.1

# Or allow any pre-release
dotnet add package Minded.Framework.CQRS --prerelease
```

**In NuGet Package Manager:**
- Check "Include prerelease" checkbox
- Filter by version suffix (preview, rc, alpha)

### 5. Changelog Management

Keep `Changelog.md` updated with each release:

```markdown
## [1.3.0] - 2025-02-11

### Added
- Retry logic in mediator for transient failures
- New caching decorator for queries

### Changed
- Improved validation error messages
- Updated dependencies to .NET 8

### Fixed
- Memory leak in command handler disposal
- Race condition in concurrent query execution

### Breaking Changes
- Removed deprecated `ILegacyMediator` interface
- Changed `ICommandHandler.Handle` signature
```

---

## Quick Reference

### Branch → Package Version

```
master                    → 1.2.0
develop                   → 1.2.0-preview.20250211.1
release/v1.2.0           → 1.2.0-rc.20250211.1
feature/add-caching      → 1.2.0-alpha.20250211.1
Upgrade202511            → 1.2.0-alpha.20250211.1 (internal only)
```

### Common Commands

```bash
# Create and push feature branch
git checkout -b feature/my-feature
git push origin feature/my-feature

# Create release candidate
git checkout -b release/v1.2.0
git push origin release/v1.2.0

# Release to production
git checkout master
git merge release/v1.2.0
git tag -a v1.2.0 -m "Release 1.2.0"
git push origin master --tags

# Install pre-release package
dotnet add package Minded.Framework.CQRS --version 1.2.0-rc.20250211.1
```

---

## Additional Resources

- [Semantic Versioning 2.0](https://semver.org/)
- [NuGet Package Versioning](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning)
- [Git Flow Workflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [Azure DevOps Pipeline Documentation](https://docs.microsoft.com/en-us/azure/devops/pipelines/)

---

**Questions or Issues?**

If you encounter any issues with the release process, please:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review Azure DevOps pipeline logs
3. Open an issue on GitHub with pipeline logs and branch information

