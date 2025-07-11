## Project Template Overview

This project template is designed to streamline the setup of small-to-medium .NET projects with a strong focus on CI/CD, diagnostics, and analyzers. It incorporates the following key features:

- **Automated Build & CI/CD with NUKE**
   Uses [NUKE Build](https://nuke.build) to orchestrate tasks such as compilation, testing, packaging, and publishing. Includes built-in targets for local development and GitHub Actions workflows for CI.
- **Versioning via NBGV (Nerdbank.GitVersioning)**
   Automatically manages SemVer-compliant versioning based on Git history. Integrates with build and publish steps.
- **Main/Release Branching Strategy**
   Optimized for smaller repositories. CI runs from the `main` branch, while the `release` branch triggers NuGet package creation and Git tagging.
- **GitHub Repo Bootstrapping**
   On first launch, if a `GITHUB_TOKEN`-style OAuth credential is set in the environment, the template can automatically:
  - Create the GitHub repository
  - Push the project
  - Set up GitHub Actions workflows
  - Copy your local NuGet API key as a secret
- **GitHub Actions Integration**
   Preconfigured workflows call NUKE targets for CI tasks and generate TRX test reports for use in GitHub UI.
- **Test Framework**
   Uses [TUnit](https://github.com/TUnit) combined with FluentAssertions 7.2.0. A custom MSBuild target enforces the version pin, preventing accidental upgrades to v8+.
- **Roslyn Analyzer & Source Generator Project**
   Includes a separate `MyProject.Analyzers` project with:
  - Sample diagnostic analyzer and source generator
  - Authoring utilities for writing analyzers (e.g., indentation helpers, syntax utilities)
  - Support for packaging analyzers via NuGet
  - Debug-friendly configuration for local testing and dependency resolution
- **Preconfigured Build Targets**
   Centralized `Directory.Build.props` and `Directory.Build.targets` configure consistent build behavior across projects.
- **Application Boilerplate**
   The main application is scaffolded with:
  - Generic host builder
  - Serilog for structured logging