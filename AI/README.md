# AI Steering Files for Minded Framework

This folder contains canonical steering documents that teach AI coding tools how to contribute to and use the Minded framework correctly.

## Files

| File | Purpose |
|------|---------|
| `minded-contributing.md` | Guide for **extending the Minded framework** — adding new decorator extensions, maintaining framework internals |
| `minded-utilization.md` | Guide for **using Minded in applications** — Commands, Queries, Handlers, Validators, DI setup |

---

## Tool-Specific Configuration

Each AI tool has its own steering file format. The files in this repo are pre-configured for use within the Minded repository. For **consuming application repos**, follow the setup instructions below.

### GitHub Copilot

**This repo**: pre-configured via:
- `.github/copilot-instructions.md` — always-loaded repo overview
- `.github/instructions/minded-backend.instructions.md` — C# patterns for `Example/**/*.cs`
- `.github/instructions/minded-contributing.instructions.md` — patterns for `Framework/**`, `Extensions/**`, `Tests/**`

**Consuming repos**: create `.github/instructions/minded-utilization.instructions.md` with the following frontmatter, then paste the contents of `minded-utilization.md` below it:

```markdown
---
applyTo: "**/*.cs"
---

[paste minded-utilization.md content here]
```

### Kiro

**This repo**: pre-configured in `.kiro/steering/`.

**Consuming repos**: create `.kiro/steering/minded-utilization.md` with:

```yaml
---
description: Rules for writing Commands, Queries, Handlers and Validators using the Minded framework
globs:
  - "**/Commands/**"
  - "**/CommandHandlers/**"
  - "**/Queries/**"
  - "**/QueryHandlers/**"
  - "**/Validators/**"
alwaysApply: false
---

[paste minded-utilization.md content here]
```

### Claude Code

**This repo**: pre-configured with three files:
- `CLAUDE.md` — always-loaded concise overview + core invariants
- `.claude/rules/minded-contributing.md` — loads when editing `Framework/**`, `Extensions/**`, `Tests/**`; imports `AI/minded-contributing.md`
- `.claude/rules/minded-utilization.md` — loads when editing `Example/**`; imports `AI/minded-utilization.md`

**Consuming repos**: create `CLAUDE.md` at the root with a brief app description, then either:

**Option A — import** (cleanest, requires the `AI/` folder to be in the repo):
```markdown
# My Application

[Brief description]

@AI/minded-utilization.md
```

**Option B — path-scoped rules** (recommended for large apps):

Create `CLAUDE.md` with just an overview, then `.claude/rules/minded-utilization.md`:
```yaml
---
paths:
  - "**/Commands/**"
  - "**/CommandHandlers/**"
  - "**/Queries/**"
  - "**/QueryHandlers/**"
  - "**/Validators/**"
---

[paste minded-utilization.md content here]
```

### Augment Code

**This repo**: pre-configured in `.augment/rules/minded-framework.md`.

**Consuming repos**: create `.augment/rules/minded-utilization.md` with:

```markdown
---
type: "always_apply"
---

[paste minded-utilization.md content here]
```

---

## Keeping Files Up to Date

When the Minded framework gains new capabilities (new decorator extensions, new interfaces, new patterns), update both canonical files (`minded-contributing.md` and `minded-utilization.md`) and regenerate or update the tool-specific files that contain the same content.
