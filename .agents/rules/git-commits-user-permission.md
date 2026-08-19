# Rule: Strict Explicit User Consent for Git Commits & Pushes

## 🚫 Prohibition of Autonomous Commits and Pushes

1. **Subagents and AI Assistants MUST NEVER run `git commit` or `git push` automatically.**
2. **Every Git commit or push requires an explicit, direct prompt directive from the user** in the current conversation turn (e.g. `/git-commit`, `/git-commit-many-by`, or an explicit request like "faça o commit").
3. **No Automatic Commits After Fixes**: When resolving lints, compilation warnings, or SonarCloud code smells, the agent MUST NOT run `git commit` or `git push` automatically. The agent must leave the changes in the working tree and inform the user.
