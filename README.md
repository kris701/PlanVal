
<p align="center">
    <img src="https://github.com/kris701/PlanVal/assets/22596587/3ae337c0-c3f7-401c-bb9c-61876c85e8ea" width="200" height="200" />
</p>

[![Build and Publish](https://github.com/kris701/PlanVal/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/kris701/PlanVal/actions/workflows/dotnet-desktop.yml)
![Nuget](https://img.shields.io/nuget/v/PlanVal)
![Nuget](https://img.shields.io/nuget/dt/PlanVal)
![GitHub last commit (branch)](https://img.shields.io/github/last-commit/kris701/PlanVal/main)
![GitHub commit activity (branch)](https://img.shields.io/github/commit-activity/m/kris701/PlanVal)
![Static Badge](https://img.shields.io/badge/Platform-Windows-blue)
![Static Badge](https://img.shields.io/badge/Platform-Linux-blue)
![Static Badge](https://img.shields.io/badge/Framework-dotnet--8.0-green)

# PlanVal

This is a simple project, that can take in a domain+problem file as well as a plan file in the [Fast Downward](https://www.fast-downward.org/) format, and validate if it is a correct plan or not.
You can use the CLI tool as follows:
```
dotnet run --domain domain.pddl --problem problem.pddl --plan plan.plan
```

Or you can find PlanVal as a package on the [NuGet Package Manager](https://www.nuget.org/packages/PlanVal).

## Examples
```csharp
IErrorListener listener = new ErrorListener();
IParser<ActionPlan> parser = new FastDownwardPlanParser(listener);
ActionPlan plan = parser.Parse(new FileInfo("planFile"));

PDDLDecl declaration = new PDDLDecl(...);
IPlanValidator validator = new PlanValidator();
bool isValid = validator.Validate(plan, declaration);
```