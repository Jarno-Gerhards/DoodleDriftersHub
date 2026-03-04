# Git Conventions

## Branching

### Structure

Our branch structure follows the GitLab Flow. We work with three main branch types: main, develop, and feature.

-   `main` branch: This branch contains all stable releases of the game. It can only be updated through merge requests, and direct pushes are not allowed.

-   `develop` branch: All completed feature branches are merged into this branch. While it may be less stable than main, it should always remain functional—broken code should not be pushed. Direct pushes to develop are discouraged but not as critical as pushing directly to main.

-   `feature` branches: These branches are used to develop new features in isolation. They ensure that work in progress does not affect main or develop. Developers are free to push, delete, and modify these branches as needed.

### Naming

To prevent issues, branch names follow strict rules:

-   All branch names must be written in lowercase to avoid capitalization errors.
-   Spaces are not allowed; use - instead to separate words.
-   feature branches can have different prefixes based on their purpose:
    -   `feature/`: For new features
    -   `fix/`: For bug fixes
    -   `hotfix/`: For urgent fixes

Examples:

`feature/player-movement `

`fix/sprite-rendering`

## Commit Messages

Commit messages should be clear and descriptive. Follow these guidelines:

-   Use an active scentence structure: fixed, added, changed, removed.
-   Keep messages concise yet informative.

We are also using [conventional commit](https://www.conventionalcommits.org/en/v1.0.0/), Conventional Commits specification is a lightweight convention on top of commit messages. It provides an easy set of rules for creating an explicit commit history; which makes it easier to write automated tools on top of.

Example:

`feat: added player movement mechanics`

`docs(GDD): added formal elements anlysis`

`fix: resolved sprite rendering issue in battle mode`

## Merge Requests

To ensure code quality and maintainability, all changes must be submitted via pull requests / merge requests:

-   Review Process: At least one other developer must review the PR before it is merged.
-   Code Quality: Code should be clean, and follow coding standards.
-   Testing: Ensure that all changes are working, and tested before submitting an PR.
-   Approval: PRs must be approved before merging into the `develop` branch.
