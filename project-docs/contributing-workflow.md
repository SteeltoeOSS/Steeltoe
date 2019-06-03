# Contribution Workflow

First, thank you for your interest in contributing to Steeltoe. We really appreciate help from the community.

If you would like to contribute code to one of our repositories, first identify the scale of what you would like to contribute. If it is small (e.g. grammar/spelling) feel free to start working on a fix and submit a PR with your changes.

If you are submitting a feature or substantial code contribution, please follow the recommended workflow below and discuss your ideas with the maintainers to ensure it fits with the direction of the project. See the [code maintainers](code-maintainers.md) document for who to reach out to.

## Suggested Workflow

We want to keep it as easy as possible for you to contribute changes, so here is a workflow that you can follow to ensure your changes or issues get proper consideration.

1. Create an issue for your work. You can skip this step for trivial changes or reuse an existing issue if there is one. Use [code maintainers](code-maintainers.md) to find relevant project maintainers and @ mention them to ask for feedback on your issue.
1. Get agreement from the team and the community that your proposed changes are good.
1. Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you.
1. Create a personal fork of the repository on GitHub.
1. Create a branch off of branch you wish your change to be in (i.e. 2.x or master). Name the branch so that it clearly communicates your intentions.
1. Make and commit your changes. Please follow our commit message guidance below.
1. Add any new tests which corresponds to your change.
1. Add or update any Sample applications that illustrate how to use your new feature or change.
1. Build the repository with your changes. Make sure that the builds are clean and make sure that the tests are all passing, including your new tests.
1. Create a pull request (PR) against the upstream repository's branch. Push your changes to your fork on GitHub (if you haven't already).

Note: It is OK for your PR to include a large number of commits. Once your change is accepted, you will be asked to squash your commits into one or some appropriately small number of commits before your PR is merged.

You should always send a pull request from a remote branch that you have created your local branch from.

## Handling Updates from Upstream

While you're working away in your branch it's quite possible that the upstream `dev` branch will be updated. If this happens you should:

1. [Stash](https://git-scm.com/book/en/v2/Git-Tools-Stashing-and-Cleaning) any un-committed changes you need to save
1. `git checkout 2.x`
1. `git pull upstream 2.x`
1. `git rebase 2.x myBranch`

This ensures that your history is "clean" i.e. you have one branch off from `2.x` followed by your changes in a straight line.

If you're working on a long running feature then you may want to do this quite often, rather than run the risk of potential merge issues further down the line.

## Pull Request - Feedback

The Steeltoe team and community members will provide feedback on your change. All community feedback is highly valued.

It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach during the feedback process.

One or more of the Steeltoe maintainers will review and approve every PR prior to merge.

## Pull Request - CI Process

The systems we use for CI (i.e. Azure Devops) will automatically perform the required builds and run the unit tests (including the ones you are expected to run) for all PRs. We have added required checks for each pull request:

* Must pass CI builds
* Must pass code coverage percentage range
* Must have at least one reviewer approve the request

## Commit Messages

Please format commit messages as follows (based on [A Note About Git Commit Messages](https://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```text
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Addresses #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same commit, and not too small with the same small change applied N times in N different commits.
