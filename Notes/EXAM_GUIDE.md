# Web 3 Practical Exam Guide - Any ASP.NET Core Repository

**Use this guide when the exam gives you a different repository or domain.** Inspect what exists, record the required behavior, and apply the actual exam's requirements. The Product/Order examples below explain reusable patterns; their names, fields, permissions, and rules are not exam requirements.

**Practice reference:** [Library API walkthrough and all eight original PDF answers](LIBRARY_API_REFERENCE.md). Use that file with the supplied `Example` project. This main guide explains how to transfer the method to another repository.

<a id="navigation"></a>
## Quick navigation

| I need to... | Open | Ctrl+F keyword |
|---|---|---|
| Run the unfamiliar API and Swagger | [Run / Swagger](#swagger) | `RUN SWAGGER` |
| Answer review questions | [All eight PDF themes, generalized](#answers) | `REVIEW ANSWER` |
| Understand a new repo | [First inspection](#inspect) | `FIRST 10 MINUTES` |
| Decide what is required | [Requirement matrix](#requirements) | `REQUIREMENT MATRIX` |
| Know where code belongs | [Architecture](#architecture) | `LAYER MAP` |
| Implement changes in order | [Refactoring recipe](#recipe) | `IMPLEMENTATION ORDER` |
| Write LINQ / joins / groups / paging | [Query cookbook](#queries) | `QUERY COOKBOOK` |
| Add repositories, factory, UoW | [Persistence patterns](#persistence) | `REPOSITORY FACTORY` |
| Add validation / audit / logging | [Validation / audit](#validation) | `VALIDATION AND AUDIT` |
| Add JWT / scopes | [Security](#security) | `JWT AND SCOPES` |
| Write tests | [Testing](#tests) | `TEST STRATEGY` |
| Fix errors | [Troubleshooting](#troubleshooting) | `ERROR LOOKUP` |
| Rehearse before the exam | [Practice exercise](#practice) | `PRACTICE PLAN` |
| Finish and submit | [Final checklist](#submission) | `SUBMISSION` |
| Choose between friends' approaches | [Notes comparison](#friends) | `FRIENDS NOTES` |

Open this file in VS Code and press **Ctrl+Shift+V** for preview. Use **Ctrl+F** for class names, routes, errors, or the keywords above. Replace all example names and capitalized command placeholders before using them.

<a id="swagger"></a>
## RUN SWAGGER - whichever repository you receive

### 1. Find the correct startup project

Open a terminal in the new repository root. Find the solution and project files. The API host usually uses `Microsoft.NET.Sdk.Web`, has Program.cs and appsettings files, and maps controllers/endpoints. Do not run a class library or test project.

```powershell
Get-ChildItem -File -Filter *.sln*
Get-ChildItem -Recurse -File -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-Object -ExpandProperty FullName
Get-Content -LiteralPath '.\PATH\TO\YourApi.csproj'
Get-Content -LiteralPath '.\PATH\TO\Program.cs'
```

**Replace `PATH/TO/YourApi.csproj` with the real API project path.** Keep the starter's target framework/provider/package versions unless the sheet asks for a change.

### 2. Restore, build, run

```powershell
dotnet --list-sdks
dotnet restore '.\PATH\TO\YourApi.csproj'
dotnet build '.\PATH\TO\YourApi.csproj'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project '.\PATH\TO\YourApi.csproj' --no-launch-profile --urls http://localhost:5090
```

This chooses a predictable Development HTTP address for practice. `--no-launch-profile` bypasses profile-specific settings too: use the supplied launch profile instead if it provides required configuration. For multiple services/containers, follow the repository's documented startup sequence.

Wait for **Now listening on...**. Keep that terminal running; use another terminal/browser for requests. **Ctrl+C stops the server.** If startup fails, read the first meaningful exception before opening Swagger.

### 3. Identify the documentation stack already present

| Program.cs contains | What to do |
|---|---|
| AddSwaggerGen, UseSwagger, UseSwaggerUI | Usually open `/swagger`; inspect actual configured route/document URL |
| AddOpenApi, MapOpenApi, UseSwaggerUI | Usually `/swagger`; built-in JSON commonly `/openapi/v1.json` |
| AddOpenApi and MapOpenApi only | JSON document exists; interactive Swagger UI has not been added |
| AddOpenApi but no MapOpenApi | Generator registered but document endpoint not mapped |
| Neither stack | Use existing `.http` requests first, or add the requested compatible documentation setup |

Do not assume `/swagger` exists just because the API builds. Read the configured routes and **Now listening on** address.

### 4. Add UI only when needed

For a .NET 10 starter like the supplied practice example, which already uses built-in OpenAPI generation, Jainder adds the **SwaggerUI-only** package. Keep the built-in document:

```csharp
// Before builder.Build(): keep the existing registration.
builder.Services.AddOpenApi();

// After build: expand the existing Development block, rather than duplicating it.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o =>
        o.SwaggerEndpoint("/openapi/v1.json", "API v1"));
}
```

Install a compatible UI package in the actual host project. For the supplied .NET 10 example, the local solution uses:

```powershell
dotnet add '.\PATH\TO\YourApi.csproj' package Swashbuckle.AspNetCore.SwaggerUI --version 11.0.1
```

For a different framework or existing Swashbuckle setup, preserve its compatible approach. Do not paste this version or add a second generator blindly. Swagger is a convenience unless the actual rubric requires it.

### 5. Make a request / authorize

1. Open `http://localhost:5090/swagger` if that is your configured address.
2. Expand a real GET route, click **Try it out**, supply parameters, and **Execute**.
3. Read response code, body, and request URL. For creates, inspect **Location** too.
4. For protected calls with a documented HTTP bearer scheme, click **Authorize** and normally paste the access token itself; the UI adds Bearer. Follow the scheme's actual instructions if different.
5. Real calls need the correct issuer/audience/token/scope. Test-handler headers only work inside the matching integration-test host.

If Authorize is missing, document the bearer security scheme using the existing OpenAPI approach. This only configures the UI; actual JWT authentication/policies must also exist. Do not remove authorization to make a manual call succeed.

If 5090 is occupied, choose another port and open its `/swagger`. A 404 means check UI mapping, Development environment, RoutePrefix and port.

**Local practice shortcut:** exact commands for Jainder's existing API are in [the practice reference](LIBRARY_API_REFERENCE.md#swagger-start).

<a id="answers"></a>
## Review questions - all eight PDF themes, adapted to any repository

The original questions name library classes/routes. Their reasoning transfers to other domains. Use these answers as templates, then name what you actually found/implemented. [Original library-specific answers](LIBRARY_API_REFERENCE.md#answers) remain available for rehearsal.

### REVIEW ANSWER 1 - Which reads filter/group/count in memory, and why move them to IQueryable?

**Answer:** I found read methods that loaded entities before filtering, sorting, grouping, counting, or summing. I moved those operations into EF query expressions executed inside repositories. The database returns the required rows, columns, and aggregates instead of transferring whole tables/collections for C# processing. Read-only queries use scalar DTO projections or AsNoTracking; paging has deterministic ordering before Skip/Take.

**Adapt:** name the actual methods. Look for ToListAsync followed by Where/GroupBy/Sum, or Include followed by a collection count. Do not claim a new starter has the library's exact mistakes. A list's Count used only to format an already-required list is not automatically an inefficient query; explain the actual requirement.

### REVIEW ANSWER 2 - Why commit related writes with one SaveChangesAsync?

**Answer:** The changes represent one business operation. Repositories stage them on the same scoped DbContext and the Unit of Work commits once. With a relational provider's transaction support, the batch is saved atomically: if it fails, none of that batch persists. Separate commits could leave only part of the operation completed.

**Adapt:** e.g. create an Order and reduce Product.Stock, or add Enrollment and change seat availability. One save guarantees atomic persistence, not protection against every concurrent request race.

### REVIEW ANSWER 3 - Why cache summaries instead of entities?

**Answer:** An immutable scalar summary contains exactly the cached endpoint's fields without mutable entity graphs, tracking assumptions, or lazy-loading dependencies. A cached entity outlives the request that loaded it and can expose stale/shared mutable state. I project summaries through a read-only query and cache only that endpoint for its required lifetime.

**Adapt:** name your record, fields, endpoint, and lifetime. A plain entity does not always retain a DbContext reference. Records are only as immutable as their members; avoid mutable nested data and do not mutate the cached collection.

### REVIEW ANSWER 4 - Why is a normal dictionary acceptable for the factory?

**Answer:** The factory is scoped to the current request/Unit of Work and used sequentially. Dictionary<Type, object> stores one repository per entity type, cast back to the requested interface. There is no concurrent dictionary access in that usage. Sharing the factory across requests or concurrent access requires a redesigned lifetime/concurrency model. ConcurrentDictionary alone does not make DbContext thread-safe.

**Adapt:** name your scoped factory and specific-repository branches. If no factory is required/present in the new exam, describe the direct scoped injection design instead of inventing a dictionary.

### REVIEW ANSWER 5 - Why generic CRUD for one resource but specific actions for another?

**Answer:** Simple reference data fits reusable list/get/create/update/delete behavior with small uniqueness/delete hooks. A workflow resource has business operations spanning several entities, ordered checks, or state transitions. Specific actions keep those operations explicit without inventing inappropriate generic update/delete endpoints.

**Adapt:** e.g. generic Category CRUD versus Order.Place/Cancel; generic Course CRUD versus Enrollment.Register/Withdraw. Follow the actual required generic resource and service count.

### REVIEW ANSWER 6 - Why 401 without authentication, but 403 with the wrong scope?

**Answer:** A protected action needs an authenticated identity. Without valid authentication, the framework challenges with 401. A valid token authenticates the caller but may lack the policy's permission, producing 403. Correct permission lets the caller reach the action; business rules can still reject input with 400/404/409.

**Adapt:** substitute the real endpoint and scope. Do not use loans.write in an unrelated API. Keep actual public endpoints public.

### REVIEW ANSWER 7 - Why are creation timestamps server-owned and preserved?

**Answer:** Requests do not expose audit fields and mappings never copy client timestamps into entities. The server sets CreatedAtUtc on insert, with UpdatedAtUtc initially null. On update it sets UpdatedAtUtc and excludes CreatedAtUtc from modification, preserving the original creation time.

**Adapt:** name your DTO and save override/interceptor. Extra JSON can physically be sent; default settings may ignore it and stricter settings may reject it. The guarantee is that it does not control stored audit timestamps through the API.

### REVIEW ANSWER 8 - Logs vs timestamps vs an immutable audit trail?

**Answer:** Request logs are operational telemetry such as method/path/status/duration and explicit ID-based events. Row timestamps show creation and latest update, without full actor/old/new history. Logs can be lost and ordinary columns can be edited, so neither is an immutable audit trail. Such a trail needs separately protected append-only history and integrity/access controls.

**Adapt:** describe the actual sinks, columns, and limits. Installing Serilog does not prove who changed every field or provide tamper-proof history.

<a id="inspect"></a>
## FIRST 10 MINUTES - understand an unfamiliar repository

[Back to navigation](#navigation)

**Goal:** identify starting behavior before changing architecture.

| Order | Read | Record |
|---|---|---|
| 1 | Actual exam sheet and README | Required deliverables, branch, constraints, commands |
| 2 | Solution, csproj, global.json | Host/test projects, SDK, provider, versions |
| 3 | Program.cs and settings | DI, auth, logging, middleware, migration/seed |
| 4 | Entities and DbContext/configurations | Keys, relationships, indexes, audit/tracking |
| 5 | Controllers / endpoint mappings | Routes, verbs, inputs, responses, status codes |
| 6 | DTOs, services, repositories | Existing boundaries, validation, business rules, saves |
| 7 | Tests, .http files, seed | Expected values/errors, IDs, persistence assumptions |

In VS Code use Ctrl+Shift+F, excluding bin/obj. Search:

`DbContext`, `SaveChanges`, `EnsureCreated`, `Migrate`, `ToList`, `AsEnumerable`, `Include`, `GroupBy`, `Sum`, `Update(`, `[Authorize`, `AddScoped`, `AddSingleton`.

### Fill three small tables first

**A. Relationship map** - read entities AND fluent configuration:

| Entity | PK | FK -> parent | Navigations | Delete behavior | Unique fields |
|---|---|---|---|---|---|
| Actual entity | ... | ... | ... | ... | ... |

**B. HTTP contract** - one row per endpoint:

| Method + route | Input/query fields | Success/body/Location | Failure + exact detail | Public/policy |
|---|---|---|---|---|
| Actual route | ... | ... | ... | ... |

**C. Workflow** - preserve order and transaction boundary:

| Operation | Ordered checks | Entities changed | Commit boundary | Cache invalidation |
|---|---|---|---|---|
| Actual operation | ... | ... | ... | ... |

Example: an inactive customer requests a missing product. Which failure should win? The sheet/starter's check order decides; do not reorder because another order seems cleaner.

Run existing build/tests and record the baseline. Missing external credentials/services are setup issues; separate them from compilation and refactoring. Use isolated integration tests where allowed.

<a id="requirements"></a>
## REQUIREMENT MATRIX - choose the correct design

[Back to navigation](#navigation)

Mark each feature **already present**, **must add/change**, or **not requested**. Do not remove required behavior because an alternative guide calls it optional.

| If the sheet requires... | Implement/check | Do not assume... |
|---|---|---|
| Same API behavior | Frozen routes/DTOs/status/errors | Refactoring permits different JSON |
| Services | Rules in services, HTTP mapping in controllers | Always one service per table |
| Exactly three services | Requested capability boundaries | Copy six library services |
| Generic repository | Shared query/staging contract | All query shapes must use one generic method |
| Specific repositories | Named specialized queries | Every entity needs a specific repository |
| Dictionary factory | Scoped type->instance cache | Direct injection fulfills an explicitly required factory |
| UoW | Shared context + designated commit | UoW must always resolve repositories |
| No factory / direct injection | Scoped constructor-injected repos | Add a factory anyway |
| Migrations | Matching Design package, committed/applicable migrations | EnsureCreated evolves schemas |
| SQL-side LINQ | Required operations before materialization | Correct JSON proves SQL translation |
| Generic controller | CRUD for named suitable resource | Workflows belong in generic CRUD |
| Cache | Exact projection/lifetime/invalidation | Cache every GET |
| JWT/scopes | Configured validation/policies/401/403 | Make all endpoints private |
| Audit/validation/logging | Required fields/guards/events | Logs are immutable history |
| Particular provider | Preserve/configure it | SQLite is universal for every exam |

The supplied Library practice PDF requires the full factory/cache/generic/security/audit stack. A new exam may change that. **The actual sheet decides**, not a guessed simplification or the new domain name.

<a id="architecture"></a>
## LAYER MAP - what goes where

[Back to navigation](#navigation)

| Layer | Responsibility | Avoid |
|---|---|---|
| Controller | Bind HTTP, call service, map response/status/Location, action policy | DbContext/repos/UoW or business decisions |
| Request DTO | Allowed client input and annotations | Client audit/derived fields |
| Response DTO | Stable output contract | Entity navigation graphs |
| Service | Rules, workflow, results/mapping, one commit | MVC IActionResult; independent saves per row |
| Repository | Build/execute EF queries, stage changes | Returning IQueryable; saving |
| Factory if required | One repository per type within scope | Singleton context capture |
| UoW | Shared commit and post-success invalidation | Separate context per related repository |
| DbContext | Model/tracking; stamp/validate/save | Concurrent operations on the same context |
| Entity | Persisted state/invariants | Being the HTTP contract |

**Lifetimes:** DbContext, repositories, factory, UoW and dependent services are scoped. IMemoryCache is shared/singleton; stateless mappers can be singleton. Scoped means per DI scope (normally request), not one fixed OS thread.

### Translate the pattern, not library names

| Pattern | Library practice | Possible different domain |
|---|---|---|
| Simple lookup CRUD | Category | Department / Brand / RoomType |
| Searchable main resource | Book | Product / Course / Event |
| Available resource | Copy | Stock / Seat / Room |
| Person/account | Patron | Customer / Student / Guest |
| Multi-entity operation | Checkout/return | Place/cancel order, enroll/withdraw, reserve/cancel |
| Derived monetary data | Fine | Charge / Invoice / Penalty |

These are analogies, not schemas. Product.Stock as an integer differs from a separate Copy table. Inspect real relationships before writing code.

<a id="recipe"></a>
## IMPLEMENTATION ORDER - repeatable refactoring recipe

[Back to navigation](#navigation)

### Step 0. Baseline and Git

Run restore/build/tests, try a read/write, record contract tables, and create the branch named by the exam. Preserve the starter; ignore runtime databases/build output/secrets. Do not initialize a second repo inside an existing one accidentally.

### Step 1. Persistence model and migrations

If requested, move fluent rules into separate `IEntityTypeConfiguration<T>` classes. Preserve actual indexes, FK/delete rules, lengths, requiredness, precision. Add requested audit properties before generating a migration, or create a later migration for them.

```csharp
// Example schema: Product has Name, Sku, CategoryId, Category;
// Category has Products. Adapt every name/limit/rule to the actual task.
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(p => p.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(p => p.Sku).IsUnique();
        b.HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

120/Sku/Restrict are illustrative, not new requirements. In DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

Preserve any other model setup/base calls needed by the real context, especially framework-specific contexts. Imports include Microsoft.EntityFrameworkCore and Microsoft.EntityFrameworkCore.Metadata.Builders. SQLite does not enforce all SQL Server-style length/decimal-precision limits simply because metadata is configured; keep validation too.

Substitute real paths and matching versions:

```powershell
dotnet add '.\PATH\TO\YourApi.csproj' package Microsoft.EntityFrameworkCore.Design --version YOUR_EF_VERSION
dotnet tool install --global dotnet-ef --version YOUR_EF_VERSION
# Already installed? Use tool update instead of install.
dotnet ef migrations add DescribeYourChange --project '.\PATH\TO\YourApi.csproj' --startup-project '.\PATH\TO\YourApi.csproj' --output-dir Data/Migrations
dotnet ef database update --project '.\PATH\TO\YourApi.csproj' --startup-project '.\PATH\TO\YourApi.csproj'
```

With a separate persistence project, --project is that migrations project and --startup-project is the host. Extend existing migrations; do not regenerate InitialCreate unnecessarily. Generate migrations for schema changes, not ordinary DTO/controller changes.

An old disposable EnsureCreated database can conflict with migrations. Verify the exact path, stop the API, and back up/reset only when permitted. Never delete an unfamiliar real DB as a shortcut. Runtime seed and migration application are separate unless explicitly combined by the project.

### Step 2. DTO contracts and input validation

Preserve response property names and expected shapes. Annotate request fields using actual limits/ranges; replace corresponding handwritten checks. Exclude server-owned fields. Dynamic ranges such as next calendar year need custom validation; attribute arguments cannot use a runtime expression as a Range constant.

```csharp
public sealed record CreateProductRequest
{
    [Required, StringLength(120)] // example limit, replace from actual contract
    public string Name { get; init; } = "";
    public int CategoryId { get; init; }
}
```

Import System.ComponentModel.DataAnnotations. **Preserve failure semantics:** an ID Range annotation could turn a required missing-row 404 into automatic 400 before the service runs.

### Step 3. Repositories / factory / UoW

Add required nontracking reads/staging methods, specialized queries, and factory if named. Queries execute inside repositories; UoW commits. See [persistence patterns](#persistence).

### Step 4. Services own decisions

Move checks in their required order, with exact error details. Load changed entities as tracked, stage every insert/update/delete, commit once. Choose one outcome design:

- **Result values:** domain outcomes mapped to HTTP at the boundary.
- **Exceptions:** specific expected domain exceptions explicitly translated at the boundary.

Do not return MVC types from services. Generic UseExceptionHandler alone does not know your business status mapping. Do not translate every InvalidOperationException into 409; that can hide EF/programming errors.

### Step 5. Thin controllers / generic CRUD

Each action binds input -> calls service -> maps outcome. Normal helpers: Ok, CreatedAtAction, NoContent, Problem. Match the actual code/body requirements: a workflow can return 200 even though it updates data.

If requested, build generic CRUD service/interface/abstract controller and derive the named simple resource. Keep type-parameter order consistent. Concrete controllers own resource route prefixes; base actions provide verb/suffix templates. Complex workflows keep specific actions unless instructed otherwise.

`CreatedAtAction(nameof(Get), new { id = created.Id }, created)` requires a real matching Get action/route. Preserve another required Location pattern when no individual GET exists.

### Step 6. Requested caching/security/audit/logging/validation

Use the sections below. Do not omit graded requirements because they were optional in a hypothetical guide. Do not spend time building unrelated features absent from the new task.

### Step 7. Verify and document

Build after coherent changes; run meaningful existing/new tests. Include an HTTP write and a later read. Check SQL placement and boundaries manually too. Finish actual README, review answers, and submission branch.

<a id="queries"></a>
## QUERY COOKBOOK - apply to actual entities

[Back to navigation](#navigation)

These snippets assume fictional Product/Category/Order/Customer properties. EF queries belong inside repositories. Adapt entities, DTO constructors, rules, and ordering to the actual contract.

### A. Filter -> stable sort -> page -> project -> execute

```csharp
IQueryable<Product> q = db.Products.AsNoTracking();
if (!string.IsNullOrWhiteSpace(search))
{
    var term = search.Trim();
    q = q.Where(p => p.Name.Contains(term));
}
if (categoryId is int id) q = q.Where(p => p.CategoryId == id);
// Normalize paging using the ACTUAL contract before this query.
var rows = await q.OrderBy(p => p.Name).ThenBy(p => p.Id)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(p => new ProductResponse(p.Id, p.Name, p.Category.Name))
    .ToListAsync(token);
```

Do not copy the library page-size clamps blindly. Stable unique ordering before paging matters across domains.

### B. Count related rows without loading them

```csharp
var rows = await db.Categories.AsNoTracking()
    .OrderBy(c => c.Name)
    .Select(c => new { c.Id, c.Name, ProductCount = c.Products.Count() })
    .ToListAsync(token);
```

Use a real DTO for public output. Include then collection Count in memory loads unnecessary entities and can violate the query requirement.

### C. Group and count

```csharp
var groups = await db.Products.AsNoTracking()
    .GroupBy(p => new { p.CategoryId, p.Category.Name })
    .OrderBy(g => g.Key.Name)
    .Select(g => new { g.Key.CategoryId, g.Key.Name, Count = g.Count() })
    .ToListAsync(token);
```

This returns categories represented by products. Starting from Categories and projecting counts also returns empty categories: different output semantics. Preserve the specified behavior.

### D. Group and sum

```csharp
var totals = await db.Orders.AsNoTracking()
    .Where(o => !o.IsPaid)
    .GroupBy(o => new { o.CustomerId, o.Customer.Name })
    .OrderBy(g => g.Key.Name)
    .Select(g => new { g.Key.CustomerId, g.Key.Name, Total = g.Sum(o => o.Total) })
    .ToListAsync(token);
```

Sum stays before materialization. Provider/version/query shape matter. The practice EF Core 10 SQLite provider supports decimal Sum via ef_sum, so the old in-memory workaround in one friend's solution is inappropriate there. For a different stack, inspect actual support rather than changing money to double. For no-match correlated sums that should be zero, use `.Sum(x => (decimal?)x.Amount) ?? 0m` as appropriate.

### E. Explicit joins

```csharp
var rows = await (
    from order in db.Orders.AsNoTracking()
    join customer in db.Customers on order.CustomerId equals customer.Id
    where !order.IsPaid
    orderby order.Id
    select new { order.Id, CustomerName = customer.Name, order.Total })
    .ToListAsync(token);
```

Add the actual additional joins requested. Navigation projection can also generate joins; explicit Join clearly demonstrates an explicitly requested join pattern.

### F. Existence / duplicates

```csharp
var exists = await db.Products.AnyAsync(p => p.Id == id, token);
var duplicate = await db.Products.AnyAsync(
    p => p.Sku == normalizedSku && p.Id != updatedId, token);
```

Exclude the current row on update. Preserve trim/case rules. Unique indexes protect against two requests both passing the precheck.

### G. Tracked updates

Repository loads a tracked entity. Service checks missing row, assigns only allowed fields, then saves through UoW. Do not load it AsNoTracking if you rely on snapshot tracking. Do not mark every property modified with blanket Update/Modified.

| Check | Good answer |
|---|---|
| Where does the query execute? | Terminal operation inside repository |
| Filters/groups/sums after ToList/AsEnumerable? | Move required database operations before execution |
| Does DTO projection contain entities? | Those can still track; prefer scalar values |
| Is Include necessary? | Use when the operation needs related entities, not just scalar counts |
| Does paging order break ties? | Appropriate unique ThenBy before Skip/Take |
| Need to confirm translation? | Inspect ToQueryString in repository/debug code; test actual provider |

IQueryable builds expressions; not every expression translates. Avoid arbitrary custom helper methods inside database filters/order/groups. Final DTO formatting is different from performing required filtering in memory.

<a id="persistence"></a>
## REPOSITORY FACTORY AND UNIT OF WORK - reusable shapes

[Back to navigation](#navigation)

### A. Generic repository

Illustrative interface; match required signatures/entity key types:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(int id, CancellationToken token = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken token = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

- Tracked getter for edit workflows; nontracking list/read projections.
- Read endpoints still return DTOs, not entities exposed by generic Get.
- Add/Remove stage changes; tracked field assignments are detected by EF.
- Update must follow a clear tracked/detached contract. Attaching an already-mutated detached object alone does not reconstruct original values or changed fields.
- No repository SaveChanges or IQueryable return.
- Specialized query methods return DTOs/lists. If other entities must remain generic, a query-shaping callback can be executed internally and return materialized results.

### B. Factory if explicitly required

```csharp
public sealed class RepositoryFactory(AppDbContext db) : IRepositoryFactory
{
    private readonly Dictionary<Type, object> repositories = [];
    public IRepository<T> Get<T>() where T : class
    {
        var key = typeof(T);
        if (!repositories.TryGetValue(key, out var repository))
        {
            // Replace with entity types requiring specific repositories.
            repository = key == typeof(Product)
                ? (object)new ProductRepository(db)
                : new Repository<T>(db);
            repositories[key] = repository;
        }
        return (IRepository<T>)repository;
    }
}
```

Assumes ProductRepository implements IRepository<Product> through its specific/base interfaces. Supply extra dependencies if its constructor needs them. Keep factory scoped; repeated type resolution returns the same object.

### C. Direct injection if no factory is required

```csharp
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

Services explicitly inject repositories and UoW. They can still share one scoped DbContext. If IRepository<Product> must resolve to the same specialized/decorated instance as IProductRepository, register that closed alias too; open generic resolution otherwise may bypass it.

### D. Commit and invalidation

```csharp
public async Task<int> SaveChangesAsync(CancellationToken token = default)
{
    var written = await db.SaveChangesAsync(token);
    cache.Remove(SummaryCache.Key); // only if this task has the required cache
    return written;
}
```

Invalidate after successful save; a thrown save never reaches it. Whether to invalidate on every successful save or only writes follows the task's contract. Parent changes can affect summary fields too. Do not invalidate before save/in finally when failure must retain the entry.

### E. Multi-entity operation

Illustrative service pseudocode. Fail is your own domain-result helper, not a framework method:

```csharp
var product = await products.GetAsync(request.ProductId, token);
if (product is null) return Fail("not-found", "Exact required message.");
if (product.Stock < request.Quantity) return Fail("conflict", "Exact required message.");

product.Stock -= request.Quantity;
var order = new Order { ProductId = product.Id, Quantity = request.Quantity };
orders.Add(order); // constructing it alone does not stage insertion

await uow.SaveChangesAsync(token);
logger.LogInformation("Order {OrderId} created", order.Id);
```

Real invariants must also validate positive quantity and all required fields. The actual domain can require customer checks, other rows, and concurrency controls. This miniature example is not a complete order specification.

### F. Cached projection

Define exact cached endpoint/fields/order/lifetime/invalidation triggers. Project scalar summary records with nontracking queries. Use absolute expiration for a maximum age; sliding-only expiration can keep popular entries indefinitely. Cache read logic can be a service or repository decorator; invalidate at the successful commit boundary.

Shared memory cache does not guarantee distributed consistency or eliminate concurrent fill/write races. Do not claim it solves every concurrency problem.

<a id="validation"></a>
## VALIDATION AND AUDIT

[Back to navigation](#navigation)

| Location | Protects | Typical result |
|---|---|---|
| Request annotations | Input shape/length/range | ApiController -> 400 ValidationProblemDetails |
| Service checks | Missing rows/conflicts/workflow order | Required domain outcomes -> 404/409/etc. |
| Entity invariants | Domain state independent of HTTP | Validate/IsValid report failures |
| Save guard | Actually runs entity validation | Throws before base save; invalid batch not persisted |
| DB constraints | Relationships/uniqueness despite bypass/race | Provider constraint failure; recognized conflict mapping |

The practice PDF's three locations are request, entity and save boundary. Service checks/DB constraints are additional; do not omit entity methods or guard execution by relabeling the layers.

### Audit/save pattern if required

Add requested audit fields and migrate. With the practice convention, insert sets CreatedAtUtc now and UpdatedAtUtc null; update preserves CreatedAtUtc and sets UpdatedAtUtc.

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken token = default)
{
    var changed = ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
    var now = DateTime.UtcNow;
    foreach (var entry in changed)
    {
        if (entry.Entity is not IAuditable audit) continue;
        if (entry.State == EntityState.Added)
        {
            audit.CreatedAtUtc = now;
            audit.UpdatedAtUtc = null;
        }
        else
        {
            audit.UpdatedAtUtc = now;
            entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
        }
    }
    foreach (var entry in changed)
        Validator.ValidateObject(entry.Entity,
            new ValidationContext(entry.Entity), validateAllProperties: true);
    return await base.SaveChangesAsync(token);
}
```

Imports: Microsoft.EntityFrameworkCore and System.ComponentModel.DataAnnotations. IAuditable is your actual interface. This local ToList snapshots tracker entries; it is not premature materialization of a route query.

Required entities implement IValidatableObject.Validate with the **new domain's rules**. If IsValid is also required, reuse the same rules. EF Core does not automatically run ValidateObject before every save; the explicit call matters.

Cover application commit paths. Synchronous saves/other overloads/raw SQL/ExecuteUpdate can bypass a narrowly overridden async method. Failed validation can leave objects staged in memory; verify persisted state in a fresh context.

### Logging

If required, configure compatible Serilog host/console packages, builder.Host.UseSerilog before build and app.UseSerilogRequestLogging early in the pipeline. Log successful events after commit:

```csharp
logger.LogInformation("Resource {ResourceId} updated", resource.Id);
```

Named templates preserve structured fields; interpolation produces text. Exclude tokens/secrets/bodies/personal fields as required. Logs/timestamps are not immutable history.

<a id="security"></a>
## JWT AND SCOPES - derive from the new route table

[Back to navigation](#navigation)

1. Inspect existing auth; preserve compatible versions/configuration.
2. For required JWT bearer, read issuer/Authority and Audience from actual config keys. Fail fast for missing settings if requested; never commit secrets/signing keys.
3. Register policies with the **new exam's scope names**.
4. Keep public actions public; protect the correct actions/inherited generic actions.
5. Authenticate before authorizing.
6. Test anonymous, wrong-permission, allowed-permission cases.

Example scope assertion, using a placeholder policy:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("resources.write", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.Claims
            .Where(c => c.Type is "scope" or "scp")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains("resources.write", StringComparer.Ordinal)));
});
```

Accept single space-separated/multiple scope claims if specified. Match whole scope tokens, not substrings. Supporting scp too is common; follow actual issued claims.

Authority identifies trusted issuer/metadata/signing keys; Audience is the API expected in aud. MapInboundClaims=false preserves original claim names for predictable scope checks. Nonempty placeholders do not authenticate real tokens.

Typical order; adapt around existing routing/CORS/etc. requirements:

```csharp
app.UseExceptionHandler();
app.UseSerilogRequestLogging(); // if configured
// Development OpenAPI / Swagger
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Required startup migration/seed runs once before Run, not per request.
app.Run();
public partial class Program; // if needed for WebApplicationFactory
```

Do not paste away required middleware in the new host. Class-level Authorize on mixed public/protected controllers can break public reads.

<a id="tests"></a>
## TEST STRATEGY - adapt by purpose, not library IDs

[Back to navigation](#navigation)

Use tests specified by the actual sheet. Transfer the practice tests' **purposes**:

| Purpose | Generic verification |
|---|---|
| Filter/page/order | Known rows -> exact ordered page/filter result |
| Group/count | Known relationships -> expected groups/counts, including empty semantics |
| Multi-entity create | POST -> success/Location -> later GET verifies changed related state |
| Rule rejection | Each failure/detail; simultaneous failures obey specified check order |
| State transition | First succeeds, repeated action produces required conflict |
| Aggregate | Known totals and no-match case if relevant |
| Delete guard | Dependent rows cause required conflict |
| Authorization | Public success; protected anonymous 401; wrong scope 403; allowed reaches action |
| Input validation | Invalid DTO -> ValidationProblemDetails and field errors |
| Save guard | Invalid entity save throws; fresh context shows unchanged DB |
| Audit | Created/null Updated; later Created unchanged/Updated set |
| Cache | Reuse proved; successful mutation refreshes; failed save retains |

### WebApplicationFactory setup

- Tests reference the actual host; use compatible xUnit/Mvc.Testing setup.
- Expose public partial Program if top-level Program needs visibility.
- Supply required config before startup checks.
- Replace database registrations/options with an isolated DB using the required provider.
- SQLite options: unique temp file or open in-memory connection per factory; keep alive across requests and dispose/clean up.
- Apply migrations/seed to test DB, never the developer DB.
- Test-only auth replaces authenticate/challenge/forbid defaults as appropriate. Match its actual header format; never register that scheme in production.
- Several requests in one test share its factory. Separate tests must not depend on execution order.

Test auth proves policy outcomes, not real JWT cryptography. Equal catalog JSON twice proves values, not actual cache reuse. Spy on underlying query count, inspect cached reference internally, or use controlled bypass/API writes to distinguish cached/live behavior.

Substitute real framework/version/project/solution names:

```powershell
dotnet new xunit -n YourApi.Tests --framework YOUR_TARGET_FRAMEWORK
dotnet add '.\YourApi.Tests\YourApi.Tests.csproj' reference '.\PATH\TO\YourApi.csproj'
dotnet add '.\YourApi.Tests\YourApi.Tests.csproj' package Microsoft.AspNetCore.Mvc.Testing --version YOUR_ASPNET_VERSION
dotnet sln '.\YourSolution.sln' add '.\YourApi.Tests\YourApi.Tests.csproj'
dotnet test '.\YourSolution.sln'
dotnet test '.\YourSolution.sln' --filter 'FullyQualifiedName~YourTestName'
```

Reuse supplied test projects; do not create duplicates. Use actual .slnx path if that is the solution format.

### HTTP status lookup

| Status | Typical meaning; actual contract wins |
|---|---|
| 200 | Successful read or operation returning content |
| 201 | Created resource, usually with Location |
| 204 | Success without response body |
| 400 | Invalid input/model validation |
| 401 | Missing/invalid authentication on protected route |
| 403 | Authenticated but permission denied |
| 404 | Resource missing / route unmatched |
| 409 | Duplicate/state/business conflict |
| 500 | Unexpected failure; do not disguise as business conflict |

<a id="troubleshooting"></a>
## ERROR LOOKUP

[Back to navigation](#navigation)

| Symptom | Inspect/fix |
|---|---|
| Target framework unsupported | SDK list/global.json/actual TargetFramework |
| Project not found / wrong project runs | Current directory and explicit real host path |
| Swagger 404 | UI installed/mapped? Development? actual port/RoutePrefix? |
| JSON works, Swagger absent | Document generator without UI |
| Missing startup config | Actual keys, environment, secrets, launch profile |
| Cannot resolve service | Interfaces, mapper dependencies, open-generic type order |
| Scoped dependency from singleton | Fix factory/UoW/service lifetime/context capture |
| Migration cannot create context | Correct projects/config; optional design-time context factory |
| Table already exists | EnsureCreated DB versus migration history; verify before reset |
| Missing column/table | Applied migration and actual DB path/working directory |
| LINQ cannot translate | Query shape/helper/provider version; inspect SQL |
| Update does nothing | Untracked row, Attach after mutation, missing save |
| All columns overwritten | Blanket Update/Modified; use tracked assignments |
| Creation timestamp changes | Mapping/blanket modification/missing save preservation |
| Wrong business error wins | Check order drift or new input annotation |
| Public route 401 | Class/base attribute or fallback policy |
| Wrong-scope test 401 | Test user not authenticated; wrong header/default scheme |
| Real bearer rejected | Issuer/audience/signature/lifetime/access-token/scope; placeholder settings |
| Location generation fails | CreatedAtAction name and required route values/target |
| Cache stale after parent change | Summary includes parent data; invalidate parent writes too |
| Cache removed after failed save | Invalidation before save/in finally; move after success |
| Memory DB disappears | Last SQLite connection closed; keep factory connection open |
| Tests affect each other | Shared DB/factory state or order dependency |
| Simultaneous booking/stock race | Atomic save alone is not a concurrency strategy |

<a id="practice"></a>
## PRACTICE PLAN - rehearse and transfer

[Back to navigation](#navigation)

### Round 1. Supplied Library API

Use a separate working copy of Example and [the practice walkthrough](LIBRARY_API_REFERENCE.md). After each change explain its purpose:

1. Map contracts and entity relationships.
2. Find premature materialization and loaded-collection counts.
3. Move one simple resource through repository -> service -> controller.
4. Add required factory/UoW; stage a multi-entity operation and save once.
5. Complete required cache/generic CRUD/security/audit/validation.
6. Verify the 13 practice scenarios and eight review answers.

### Round 2. Imagine an event-registration API

Before coding, answer:

| Question | What to identify |
|---|---|
| What replaces checkout? | Register/cancel workflow and actual changed rows |
| Which checks happen first? | New contract's event/user/eligibility/capacity check order |
| What fits generic CRUD? | Named simple reference resource, if requested |
| What is cached? | Named projection/fields/lifetime/invalidation |
| What are scopes? | New scope map, not books.write/loans.write |
| Which repos are specific? | Requested query-heavy entities, not automatic library equivalents |
| Which tests transfer? | Ordering/groups/atomic write/later read/auth/validation/cache purposes |

### If time is limited in the exam

Allocate according to its rubric. Get the project building early, preserve behavior, change coherent layers, and verify required outcomes. Keep a short missing-requirements list. Avoid unnecessary PR workflows or optional UI polishing while graded features remain incomplete.

<a id="submission"></a>
## SUBMISSION - final check on the actual repository

[Back to navigation](#navigation)

- [ ] Actual required repository/branch, not the practice names by assumption.
- [ ] Build/test correct solution and provider.
- [ ] Routes/DTOs/status/Location/errors/check order preserved.
- [ ] Controllers service-only where required; repos do not save/return IQueryable.
- [ ] Workflow stages all related changes and commits at designated boundary.
- [ ] Required query operations stay in EF; paging deterministic.
- [ ] Requested factory/generic CRUD/cache/security/audit/validation present.
- [ ] No extra provider/infrastructure/endpoints contrary to actual instructions.
- [ ] Migrations/snapshot committed when needed; DB/build output ignored.
- [ ] No secrets/tokens/credentials/sensitive logs committed.
- [ ] README: actual run/test/migrations/config/scopes and limitations.
- [ ] Review answers name your actual methods/classes, not copied library names.

```powershell
dotnet build '.\YourSolution.sln'
dotnet test '.\YourSolution.sln'
git diff --check
git status --short
# Inspect before staging; exclude secrets/runtime databases.
git add .
git diff --cached --stat
git commit -m 'Describe the actual completed change'
git push -u origin YOUR_REQUIRED_BRANCH
```

Replace placeholders first. Confirm remote is the authorized own/exam repository. Per-layer commits help; a PR per layer is only required if the actual instructions say so.

<a id="friends"></a>
## FRIENDS NOTES - use the right parts

| Source | Useful for | Adaptation warning |
|---|---|---|
| Sean's course mirror | Original labs/explanations | Includes broader topics outside practice scope |
| Abdull's theory/implementation | Reasons for layers and wiring alternatives | Some snippets/finished code conflict with strict practice requirements; see reference corrections |
| Abdull's hypothetical three-service guide | Different service boundaries/direct injection | Three services/no factory is an assumption, not a prediction of tomorrow's exam |
| Jainder's layer docs/solution | Concrete bottom-up workflow and Q&A | Domain/scopes/versions/test headers/nine PRs are not universal requirements |

**Transferable:** inspect -> freeze contract -> map relationships -> refactor layers -> preserve workflows -> verify.

**Must come from the new exam:** domain rules, entity names, service count, factory requirement, provider/version, cache policy, scopes, messages, tests, and submission branch.

Original practice routes/tests/answers and detailed comparison/corrections remain in [LIBRARY_API_REFERENCE.md](LIBRARY_API_REFERENCE.md).
