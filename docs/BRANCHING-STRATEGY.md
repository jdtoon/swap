# 🌳 NetMX Branching Strategy & Workflow

This document defines our Git branching strategy and release workflow for the NetMX project.

## 📋 Branch Structure

We follow a **simplified Git Flow** strategy optimized for continuous development with controlled releases.

### Main Branches

```
master (protected)
  ├── Tag: v1.0.0, v1.1.0, etc. (production releases)
  └── Tag: v0.1.0-alpha, v0.2.0-beta (pre-releases)

develop (protected, default branch)
  └── Daily development work
```

#### `master` - Production & Release Branch
- **Purpose**: Stable, production-ready code
- **Protection**: Required reviews, status checks must pass
- **Commits**: Only via PR from `develop` or hotfix branches
- **Tags**: All releases (v1.0.0, v0.1.0-alpha, etc.)
- **CI/CD**: Automated deployments to NuGet triggered by tags

#### `develop` - Main Development Branch
- **Purpose**: Integration branch for ongoing development
- **Protection**: Status checks must pass (CI build)
- **Commits**: Feature branches merge here
- **Stability**: Should always build and pass tests
- **Default**: This is where you work day-to-day

### Supporting Branches

#### Feature Branches (`feature/*`)
- **Naming**: `feature/<description>` (e.g., `feature/audit-logging`)
- **Base**: Create from `develop`
- **Merge**: Back into `develop` via PR
- **Lifetime**: Deleted after merge
- **Example**:
  ```bash
  git checkout develop
  git pull
  git checkout -b feature/background-jobs
  # ... do work ...
  git push -u origin feature/background-jobs
  # Create PR: feature/background-jobs -> develop
  ```

#### Bugfix Branches (`bugfix/*`)
- **Naming**: `bugfix/<description>` (e.g., `bugfix/user-role-assignment`)
- **Base**: Create from `develop`
- **Merge**: Back into `develop` via PR
- **Lifetime**: Deleted after merge

#### Hotfix Branches (`hotfix/*`)
- **Naming**: `hotfix/<version>` (e.g., `hotfix/1.0.1`)
- **Base**: Create from `master` (for urgent production fixes)
- **Merge**: Into both `master` AND `develop`
- **Tag**: Create new patch version
- **Lifetime**: Deleted after merge
- **Example**:
  ```bash
  git checkout master
  git pull
  git checkout -b hotfix/1.0.1
  # ... fix critical bug ...
  git push -u origin hotfix/1.0.1
  # PR 1: hotfix/1.0.1 -> master
  # PR 2: hotfix/1.0.1 -> develop
  # Tag: v1.0.1 on master
  ```

## 🔄 Development Workflow

### Day-to-Day Development

```bash
# 1. Start your day - update develop
git checkout develop
git pull origin develop

# 2. Create a feature branch
git checkout -b feature/my-awesome-feature

# 3. Do your work, commit often
git add .
git commit -m "feat: Add awesome feature"

# 4. Push to remote
git push -u origin feature/my-awesome-feature

# 5. Create Pull Request on GitHub
# Base: develop <- Compare: feature/my-awesome-feature

# 6. After PR is merged, clean up
git checkout develop
git pull
git branch -d feature/my-awesome-feature
```

### Current State (Transitioning)

**Right now you're on `master`**. Let's create the `develop` branch structure:

```powershell
# Create develop branch from current master
git checkout -b develop
git push -u origin develop

# Set develop as default branch on GitHub:
# Go to: https://github.com/toonjd/netmx/settings/branches
# Change default branch to 'develop'

# From now on, work on develop
git checkout develop  # Your daily work branch
```

## 📦 Release Workflow

### Alpha/Beta Releases (Pre-releases)

Use this for testing, early feedback, and preview versions.

**Process:**

1. **Ensure `develop` is stable**
   ```bash
   git checkout develop
   git pull
   # Verify CI is green
   ```

2. **Merge `develop` into `master`** (for releases)
   ```bash
   git checkout master
   git pull
   git merge develop --no-ff -m "chore: Merge develop for v0.2.0-alpha release"
   git push
   ```

3. **Create pre-release on GitHub**
   - Go to: https://github.com/toonjd/netmx/releases/new
   - Tag: `v0.2.0-alpha` (or `v0.2.0-beta`)
   - Title: `NetMX v0.2.0-alpha - Description`
   - ✅ Check: **"Set as a pre-release"**
   - Publish

4. **Automated Deployment**
   - `publish-dev` workflow runs automatically
   - Publishes to **GitHub Packages** only
   - Adds `-beta` suffix to package versions
   - No manual approval needed

### Production Releases (Stable)

Use this for official, production-ready releases.

**Process:**

1. **Ensure `develop` is production-ready**
   - All tests pass
   - Documentation complete
   - No known critical bugs

2. **Update version numbers** (in `develop`)
   ```bash
   # Update all .csproj files: 0.2.0-alpha -> 1.0.0
   # Commit and push
   git add framework/**/*.csproj
   git commit -m "chore: Bump version to 1.0.0"
   git push
   ```

3. **Merge `develop` into `master`**
   ```bash
   git checkout master
   git pull
   git merge develop --no-ff -m "chore: Merge develop for v1.0.0 release"
   git push
   ```

4. **Create full release on GitHub**
   - Go to: https://github.com/toonjd/netmx/releases/new
   - Tag: `v1.0.0`
   - Title: `NetMX v1.0.0 - First Stable Release`
   - ❌ **Uncheck**: "Set as a pre-release"
   - ✅ **Check**: "Set as the latest release"
   - Publish

5. **Manual Deployment Approval**
   - `publish-production` workflow runs
   - **Waits for your approval** (environment protection rules)
   - Go to: https://github.com/toonjd/netmx/actions
   - Click "Review deployments"
   - Approve deployment to `nuget-production`
   - Wait 5 minutes (timer)
   - Publishes to **NuGet.org**

## 🔒 Branch Protection Rules

### Recommended Settings for `master`

Go to: https://github.com/toonjd/netmx/settings/branches

1. **Require a pull request before merging**
   - ✅ Require approvals: 1
   - ✅ Dismiss stale pull request approvals when new commits are pushed

2. **Require status checks to pass before merging**
   - ✅ Require branches to be up to date before merging
   - Required checks:
     - `build` (from CI Build & Test workflow)

3. **Do not allow bypassing the above settings**
   - ✅ Apply to administrators (optional, depends on team size)

### Recommended Settings for `develop`

1. **Require status checks to pass before merging**
   - ✅ Require branches to be up to date before merging
   - Required checks:
     - `build` (from CI Build & Test workflow)

2. **Do not require pull request reviews** (can merge directly for quick fixes)

## 🎯 Workflow Decision Tree

```
Need to add a feature?
├─ Create: feature/feature-name
├─ Base: develop
└─ Merge to: develop (via PR)

Found a bug?
├─ Is it in production (master)?
│  ├─ Yes (URGENT)
│  │  ├─ Create: hotfix/X.X.X
│  │  ├─ Base: master
│  │  └─ Merge to: master AND develop
│  └─ No (in develop)
│     ├─ Create: bugfix/bug-name
│     ├─ Base: develop
│     └─ Merge to: develop (via PR)

Ready to release?
├─ Is it stable/production-ready?
│  ├─ Yes
│  │  ├─ Merge develop -> master
│  │  ├─ Tag: vX.X.X (no suffix)
│  │  ├─ Uncheck "pre-release"
│  │  └─ Deploys to: NuGet.org (with approval)
│  └─ No (alpha/beta)
│     ├─ Merge develop -> master
│     ├─ Tag: vX.X.X-alpha or vX.X.X-beta
│     ├─ Check "pre-release"
│     └─ Deploys to: GitHub Packages (automatic)
```

## 📊 Workflow Examples

### Example 1: Adding Identity Module (Already Done)

```bash
# 1. Create feature branch
git checkout develop
git checkout -b feature/identity-module

# 2. Add module code, tests, documentation
# ... work ...

# 3. Commit and push
git add modules/Identity
git commit -m "feat(identity): Add user and role management module"
git push -u origin feature/identity-module

# 4. Create PR on GitHub: feature/identity-module -> develop
# 5. Review, approve, merge
# 6. Delete branch
```

### Example 2: First Alpha Release

```bash
# 1. Verify develop is stable
git checkout develop
git pull
# Check: https://github.com/toonjd/netmx/actions

# 2. Merge to master
git checkout master
git merge develop --no-ff

# 3. Push
git push

# 4. Create release on GitHub
#    Tag: v0.1.0-alpha
#    Check: "Set as a pre-release"
#    Publish

# 5. Verify deployment
#    Check: https://github.com/toonjd/netmx/actions
#    Verify: https://github.com/orgs/toonjd/packages
```

### Example 3: Hotfix for Production Bug

```bash
# 1. Create hotfix from master
git checkout master
git pull
git checkout -b hotfix/1.0.1

# 2. Fix the bug
# ... fix ...
git commit -m "fix: Critical security vulnerability in authentication"

# 3. Push
git push -u origin hotfix/1.0.1

# 4. Create TWO PRs:
#    PR 1: hotfix/1.0.1 -> master
#    PR 2: hotfix/1.0.1 -> develop

# 5. After merge, create release
#    Tag: v1.0.1
#    Based on: master
#    Uncheck: "pre-release"
#    Publish

# 6. Approve deployment to NuGet.org
```

## 🚦 Current Transition Plan

**You're currently on `master` with recent work.** Here's how to set up the new strategy:

### Step 1: Create Develop Branch
```powershell
cd c:\jd\netmx
git checkout -b develop
git push -u origin develop
```

### Step 2: Set Develop as Default (GitHub Web UI)
1. Go to: https://github.com/toonjd/netmx/settings
2. Click "Branches" in left sidebar
3. Under "Default branch", click switch button
4. Select `develop`
5. Click "Update"

### Step 3: Configure Branch Protection
1. Go to: https://github.com/toonjd/netmx/settings/branches
2. Add rule for `master` (protection rules above)
3. Add rule for `develop` (protection rules above)

### Step 4: Future Work
```powershell
# Always start from develop
git checkout develop
git pull

# Create feature branches
git checkout -b feature/my-feature

# Merge back to develop via PR
```

## 📝 Commit Message Convention

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks (CI, build, dependencies)
- `perf`: Performance improvements

**Examples:**
```
feat(identity): Add role-based authorization
fix(htmx): Correct response header handling
docs(roadmap): Update Phase 2 completion status
chore(deps): Update EF Core to 9.0.11
```

## 🎯 Summary

| Branch | Purpose | Protection | Merge Via | Deploy To |
|--------|---------|-----------|-----------|-----------|
| `master` | Stable releases | High | PR only | NuGet.org (on tag) |
| `develop` | Active development | Medium | PR or direct | GitHub Packages (on pre-release tag) |
| `feature/*` | New features | None | PR to develop | N/A |
| `bugfix/*` | Bug fixes | None | PR to develop | N/A |
| `hotfix/*` | Production fixes | None | PR to master & develop | NuGet.org (on tag) |

---

**Questions?** Check the examples above or refer to the [roadmap](ROADMAP.md) for project progress tracking.
