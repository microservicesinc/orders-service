# Tech Debt & Architecture Note: ASP.NET Core Dependency Injection Lifecycles

- **Date**: 2026-07-26
- **Topic**: Framework Architecture / App Lifecycle / Service Resolution
- **Context**: Explains why Query Handlers are registered via `builder.Services` while the Database Seeder manually resolves `IOrderRepository` using `CreateScope()`.

---

## 🧭 Concept 1: Service Registration (`builder.Services.Add...`)

### What It Means
You are defining the blueprints of the application graph. When you write `builder.Services.AddScoped<GetAllOrdersQueryHandler>()`, you are not instantiating the class yet. You are instructing .NET's Inversion of Control (IoC) container how to build it when an endpoint asks for it later during runtime.

### Why the Query Handler Uses It
Minimal API endpoints rely on **Automatic Parameter Injection**. When a user hits the `api/orders` HTTP route, the framework looks into the blueprint registry, builds the `GetAllOrdersQueryHandler` on the fly, passes it to the lambda endpoint, executes the code, and cleans it up.

### Reference Links
- [Dependency Injection in ASP.NET Core](https://microsoft.com)
- [Dependency Injection Service Lifetimes](https://microsoft.com#service-lifetimes)

---

## 🧭 Concept 2: Manual Service Resolution (`CreateScope()`)

### What It Means
This is **Explicit Service Resolution** (often referred to as the Service Locator pattern during app startup). Instead of waiting for an HTTP request to inject a class automatically, you are forcing the application to build and give you an instance right now during the initialization boot phase.

### Why the Database Seeder Uses It
1. **No Active HTTP Context**: At the root of `Program.cs`, the application is just warming up. There are no incoming web requests yet, so automatic parameter injection cannot occur.
2. **The Captive Dependency / Scoped Violation Trap**: The `IOrderRepository` is a **Scoped** service. Scoped services cannot be resolved directly from the root Application Provider (`app.Services`) because they require a boundary. Resolving a scoped service from the root throws a runtime validation error.
3. **The Solution**: Writing `using var scope = app.Services.CreateScope();` creates a temporary, artificial scope boundary. This allows you to resolve the scoped `IOrderRepository`, execute the `OrderSeeder.SeedAsync` database routine, and safely dispose of the repository database connections the moment the initialization block completes.

### Reference Links
- [Resolving Scoped Services on App Startup](https://microsoft.com#scope-validation)
- [Designing Services and Avoiding Captive Dependencies](https://microsoft.com#design-services-for-dependency-injection)
