# Sentry Breadcrumb Leak Repro

## Requirements

* Azure Service Bus ConnectionString
* Sentry Dsn

## Running the app

* Update the appsettings.json file

## Reproducing the leak

Comment the line in `Comsumer.cs` on line 46

```csharp
// Comment this line to 'introduce' the leak
_hub.ConfigureScope(s => s.Clear());
```

You will see logs appearing:

```
Breadcrumbs should be empty, but they are not
```