# How to Contribute

* [Getting Started](#getting-started)
* [Reporting Issues](#reporting-issues)
* [Feature Requests](#feature-requests)
* [Contributing Code](#contributing-code)
* [General Contribution Guidelines](#general-contribution-guidelines)
* [Governance Model](#governance-model)

## Getting Started

One of the easiest ways to contribute is to participate in discussions, provide feedback or simply submit issues when you encounter them.

Of course, we also accept code contributions by submitting pull requests (PR) with code changes.

The Steeltoe team is available on the [Steeltoe Slack channel](https://slack.steeltoe.io), so if you want to start a general discussion, provide feedback or have a question about using Steeltoe, feel free to contact us on Slack.  Also, if you have a desire to get involved, but don't know what to do, reach out to us on Slack. We are glad to help!

Remember, one of the great things about Slack is that you can search existing channel content to see if your idea or question has been discussed already.

By using Slack for questions, discussions and help, that basically leaves the GitHub issue tracker for use in reporting bugs and feature requests only.

## Reporting Issues

If you want to report an issue for a specific Steeltoe package (e.g. Configuration, CircuitBreaker, etc. ) then please open an issue in the appropriate [SteeltoeOSS GitHub repository](https://github.com/SteeltoeOSS).

When reporting issues, please use our [issue reporting template](bug-template).

You will find that this is the best way to get your issue addressed. Please be as detailed as you can be about the problem. Providing a minimal project with code that illustrates the problem along with a description of the steps to reproduce the problem is ideal.

Before reporting an issue, go through the list below and ask yourself these questions.  This will make sure you're not missing any important information before opening up an issue.

1. Did you read the [documentation](https://steeltoe.io/docs/)?
1. Did you look at one of the [samples](https://github.com/SteeltoeOSS/Samples) to see if it provides answers for your issue.
1. Do you have the snippet of broken code or a sample project for the issue?
1. What are the *EXACT* steps to reproduce this problem?
1. What package versions are you using (you can see these in the `.csproj` file)?
1. Is the problem still there in the latest version of the package?
1. What operating system are you having the problem on?
1. What version of cloud applications platform (e.g. Cloud Foundry 1.11, etc), if any, are you running on.
1. If the problem is related to a back-end service (e.g. Redis, MySql, etc) what version are you using.

Make sure before you submit the issue to check the formatting of the content. Remember that GitHub supports [markdown](https://help.github.com/articles/github-flavored-markdown/) formatting in the issue content.

## Feature Requests

The best way to get new feature requests implemented is by contributing code. That said, if you don't feel you have time or the ability to contribute code, then please feel free to start a discussion around your idea or new feature suggestion in the [Steeltoe Slack channel](https://slack.steeltoe.io).

## Contributing Code

The Steeltoe team welcomes code contributions from the community and as a part of that we recommend the following guidelines for you to follow to ensure your contributions get proper consideration:

* [Workflow](contributing-workflow.md) - We pulled together a workflow for how to go about contributing code to Steeltoe. We recommend you read through it first and follow it to get the best results.
* [Licensing](contributing-license.md) - There are some licensing requirements for code contributions, so please follow through with those.
* [Code Management](contributing-code-management.md) - We try to maintain a consistent structure for our code; the repositories they are in and how we mange building and publishing it.  Please try to follow our lead on this.
* [Coding](contributing-code-style.md) - While we might not be completely consistent throughout our code base, we are striving to be better, so please try to follow our coding guidelines.

Please note, when you want to contribute code to a specific Steeltoe package (e.g. Configuration, CircuitBreaker, etc. ) then please follow the above guidelines in the appropriate [SteeltoeOSS GitHub repository](https://github.com/SteeltoeOSS). Specifically, open issues, carry on contribution discussions, and submit PRs, in the repository to which your contribution will be made.

## General Contribution Guidelines

* Please try to follow our [coding guidelines](contributing-code-style.md) but give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* Please include tests when adding new features. We recommend when fixing bugs, start with adding a test that highlights how the current behavior is broken.
* Please keep all discussions focused. When a new or related topic comes up it's often better to create new thread of discussion than to side track the discussion.
* Please blog and tweet (or whatever) about your contributions, frequently!
* Please don't surprise us with big pull requests. Instead, file an issue and start a discussion so we can agree on a direction before you invest a large amount of time.
* Please don't commit code that you didn't write. If you find code that you think is a good fit, file an issue and start a discussion before proceeding.
* Please don't submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.
* Please don't add API additions without filing an issue and discussing with us first.
* Please tag any users that should know about and/or review the changes you are wanting to contribute.

## Governance Model

As a member of the [.NET Foundation](https://dotnetfoundation.org/), the Steeltoe project has adopted a [project governance model](https://github.com/dotnet/home/blob/master/governance/project-governance.md) in line with that recommended by the .NET Foundation.

Specifically, the Steeltoe project recognizes the following roles within the project:

* Project Lead
* Project Maintainer
* Contributor
* User

See [code maintainers](code-maintainers.md) to find the current project members and the roles they play within the project.

In addition, as we fully support and desire the involvement from the larger open source community, all decision making will be conducted in the open and based on a model outlined in the .NET Foundation project governance document mentioned above. When project decisions need to be made, it will be the responsibility of the project lead(s) to make those final decisions, with the help and guidance from the project maintainers, contributors and the larger open source community.
