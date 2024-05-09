# PlanVal

This is a simple project, that can take in a domain+problem file as well as a plan file in the [Fast Downward](https://www.fast-downward.org/) format, and validate if it is a correct plan or not.

## Examples
```csharp
IErrorListener listener = new ErrorListener();
IParser<ActionPlan> parser = new FastDownwardPlanParser(listener);
ActionPlan plan = parser.Parse(new FileInfo("planFile"));

PDDLDecl declaration = new PDDLDecl(...);
IPlanValidator validator = new PlanValidator();
bool isValid = validator.Validate(plan, declaration);
```