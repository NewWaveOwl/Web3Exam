# Web 3 Practical Exam Guide - Library API

**Purpose:** an open-note reference for transforming the supplied controller-based API. Simple English, exact rules, code patterns, and answers. Based on the practical-test PDF, your actual `Example` starter, and the Abdull, Jainder, and Sean folders.

**Authority:** follow tomorrow's actual exam sheet first. This guide answers the supplied Library API practice test; it does not claim tomorrow's domain or questions will be identical.

<a id="swagger-start"></a>
## RUN SWAGGER - start here

**Swagger UI is the interactive page where you expand a route, click Try it out, enter values, and Execute.** Keep the API running while using it. Stop it with Ctrl+C in the terminal.

### A. Run Jainder's finished example (Swagger is already configured)

Open a PowerShell terminal and run:

```powershell
Set-Location -LiteralPath 'K:\CLASSES 26 FALL\WEB_3\Finaltest\Notes\Jainder\library-api-test-review-main'
dotnet restore
# Force Development so the Swagger/OpenAPI endpoints are enabled.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project LibraryApi --no-launch-profile --urls http://localhost:5090
```

Wait for **Now listening on: http://localhost:5090**, then open:

- **Swagger UI:** <http://localhost:5090/swagger>
- **OpenAPI JSON:** <http://localhost:5090/openapi/v1.json>
- **Public book list:** <http://localhost:5090/api/books>

The example has Development auth placeholders. They satisfy its startup checks but do not validate real access tokens. If Authority/Audience is reported missing, configure the placeholder values using the commands in [section 11](#commands), or use the real settings supplied in the exam.

### B. Add Swagger to your own starter / exam solution

**The untouched `Example` starter has the OpenAPI JSON endpoint but no Swagger UI.** Opening `/swagger` there initially returns 404. To add the UI, run this from your own solution folder containing `LibraryApi/`:

```powershell
dotnet add LibraryApi package Swashbuckle.AspNetCore.SwaggerUI --version 11.0.1
```

In `Program.cs`, keep this registration **before** `builder.Build()`:

```csharp
builder.Services.AddOpenApi();
```

Replace the existing Development-only OpenAPI block with:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Library API v1"));
}
```

Then start your solution, from its solution folder:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project LibraryApi --no-launch-profile --urls http://localhost:5090
```

Open <http://localhost:5090/swagger>. This adds an interface over the built-in OpenAPI document; do not replace `AddOpenApi` with a different document generator unnecessarily. If you already have Swagger configured, do not add duplicate registrations or middleware.

### C. Use Swagger and authorize protected calls

1. Expand **GET /api/books**, click **Try it out**, set `pageSize` to `2`, then **Execute**. Expect 200 and Clean Code followed by Refactoring.
2. For protected routes, use the **Authorize** button and a real access token with the required scope. In Jainder's HTTP bearer UI, paste the token itself; the UI adds the Bearer scheme.
3. If the Authorize button is missing in your starter, copy Jainder's `Security/BearerSecuritySchemeTransformer.cs` with its namespace, then replace `AddOpenApi()` with `AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>())`. Add the matching namespace import. This documents bearer authentication in OpenAPI; the actual JWT/policy configuration in [section 7](#security) must also exist.
4. Test **POST /api/loans** with `{ "patronId": 1, "bookId": 2 }` and a `loans.write` access token. Expect 201 for the unchanged seed when HB-001 is available.
5. No valid token -> **401**; a valid token with only `loans.read` -> **403**. Placeholder settings and test-only headers do not grant access to the real running API.

If port 5090 is occupied, use `--urls http://localhost:5100` and open that port's `/swagger`. If Swagger returns 404, confirm the UI package/block and Development environment. Swagger is useful for testing; it is not separately required by the supplied PDF.

<a id="answers"></a>
## PDF REVIEW QUESTIONS - all eight answers

These answers can be adapted into the required `TEST-REVIEW.md` in your eventual exam solution. Describe what your actual implementation does; do not claim a method/class you did not implement.

### REVIEW ANSWER 1 - Which starter reads do work in memory, and why move it to IQueryable?

`FinesController.List` loads fine entities with `ToListAsync` before filtering unpaid fines, sorting, and projecting. `FinesController.Totals` loads fines before grouping by patron and summing. `CategoriesController.Get` loads books to count them; `BooksController.Get` loads copies to count available copies; `PatronsController.Get` loads loans/fines and counts/sums them in memory. I moved these filters, counts, grouping, sums, and projections into EF queries executed inside repositories. SQLite now returns only the required rows and values, reducing memory and transfer while meeting the translated-query requirement. Starter book paging, book category grouping, and overdue joins were already composed in EF and should be preserved.

### REVIEW ANSWER 2 - Why one SaveChangesAsync for checkout?

The new Loan and the Copy's OnLoan status are one business operation. The service stages both in the same scoped DbContext and calls UnitOfWork.SaveChangesAsync once. EF Core saves those changes in one transaction, so failure cannot persist only one side. Return similarly commits the loan, copy status, and optional fine together. This guarantees atomic persistence; it does not by itself prevent two simultaneous requests selecting the same copy.

### REVIEW ANSWER 3 - Why cache summary records rather than Book entities?

The catalog needs only id, title, isbn, and categoryName. Immutable scalar summary records are small and safe to share across requests without navigation graphs or mutable tracked state. Caching a Book can share stale/mutable entity data beyond the request that loaded it and can retain unwanted graphs or lazy-loading dependencies. A normal POCO does not automatically hold a DbContext reference; the central problem is sharing entity state and lifecycle assumptions. The repository projects summaries with AsNoTracking and the cache expires after at most two minutes.

### REVIEW ANSWER 4 - Why is a normal dictionary acceptable in the factory?

The RepositoryFactory is scoped to the current request/Unit of Work and its calls are sequential. Dictionary<Type, object> can cache one repository per entity type without concurrent access; the object is cast back to IRepository<T>, or its specific interface. Sharing the factory across requests or accessing it concurrently would require a concurrency-safe design. Merely changing to ConcurrentDictionary would not make the captured DbContext thread-safe. Keep the factory scoped and use separate contexts/scopes for parallel database operations.

### REVIEW ANSWER 5 - Why generic CategoriesController but specific LoansController?

Categories follow normal list/get/create/update/delete operations. Category-specific book counts, unique-name checks, and the delete-with-books conflict fit projection and service hooks in generic CRUD. Loans represent checkout and return workflows, not ordinary CRUD: checkout has ordered patron/fine/book/copy checks, and return may create a fine while changing a copy. Specific loan actions keep those business operations explicit and avoid inventing loan PUT/DELETE routes.

### REVIEW ANSWER 6 - Why anonymous checkout 401, but loans.read checkout 403?

Checkout requires the loans.write policy. An anonymous caller has no authenticated identity, so authorization challenges with 401. A valid token carrying only loans.read authenticates successfully but lacks loans.write, so authorization returns 403. A valid loans.write token can reach the action; library rules can still reject its request with 404 or 409. The public book list remains 200 without a token.

### REVIEW ANSWER 7 - Why can the client not set createdAtUtc, and why preserve it on update?

Request DTOs do not define audit timestamp properties and services never map client JSON into them. Under the starter's default JSON settings an extra createdAtUtc member is ignored; it cannot set the entity timestamp. DbContext stamps CreatedAtUtc from the server clock on insert and leaves UpdatedAtUtc null. On update it stamps UpdatedAtUtc and marks CreatedAtUtc as not modified so the original creation time remains unchanged in the database. A client can physically send extra JSON; it simply does not control the stored audit value.

### REVIEW ANSWER 8 - Serilog log vs timestamp vs tamper-proof audit trail?

A Serilog request log records operational details such as method, path, status, and duration, plus explicit ID-based events; console output can be lost or changed. CreatedAtUtc/UpdatedAtUtc store creation and latest modification time on a row, without a full actor/old/new history, and database users can alter them. A tamper-resistant audit trail requires separately protected append-only history and integrity/access controls. This API provides logs and row timestamps, not an immutable or tamper-proof audit trail.

## Navigation

| Need | Jump here | Search keyword |
|---|---|---|
| Run Swagger / add it to the starter | [Swagger quick start](#swagger-start) | `RUN SWAGGER` |
| What to build / what goes where | [1. Quick map](#quick-map) | `QUICK MAP` |
| Steps from the starter | [2. Implementation order](#implementation) | `STEP 0` |
| Exact routes and scopes | [3. Route contract](#routes) | `ROUTE CONTRACT` |
| Checkout, return, fine, duplicate rules | [4. Business rules](#rules) | `CHECKOUT ORDER` |
| LINQ examples to adapt | [5. Query recipes](#queries) | `QUERY RECIPES` |
| Database, audit, entity validation | [6. Save pipeline](#save-pipeline) | `STAMP VALIDATE SAVE` |
| JWT and scope code | [7. Security](#security) | `JWT SETUP` |
| Testing / all 13 expected results | [8. Tests](#tests) | `TEST MATRIX` |
| Answers to the eight official questions | [PDF review answers - at the top](#answers) | `REVIEW ANSWER` |
| Extra questions from teammates' notes | [10. Extra Q&A](#faq) | `EXTRA QUESTIONS` |
| Commands and common errors | [11. Commands / troubleshooting](#commands) | `TROUBLESHOOTING` |
| Which friend's notes to trust for what | [12. Comparison and corrections](#comparison) | `FRIENDS NOTES` |
| Before submission | [13. Final checklist](#checklist) | `FINAL CHECKLIST` |
| Original local references | [14. Sources](#sources) | `SOURCE FILES` |

**Use in the exam:** open this file in VS Code and press **Ctrl+Shift+V** for Markdown preview. Use the links above or **Ctrl+F** for the keyword, route, class, or error message. Code snippets are patterns: add the namespace/usings and adapt method names to the interfaces you actually implement. Do not mix different teammates' generic type-parameter orders.

<a id="quick-map"></a>
## 1. QUICK MAP - understand the design

**One sentence to remember:** the controller handles HTTP, the service makes decisions, repositories query and stage changes, and one Unit of Work commits through EF Core.

| Part | Owns | Must not do |
|---|---|---|
| Controller | Routes, query/body binding, status codes, `Location`, authorization attributes | Inject DbContext, repositories, or UoW; return entities |
| Request DTO | Allowed client input and data annotations | Accept client audit timestamps |
| Response DTO | Exact JSON property names from starter | Expose entity navigation graphs |
| Service | Business rules, check order, response/result creation | Return MVC `IActionResult`; perform independent saves per repository |
| Repository | EF queries, SQL-side filtering/grouping/paging, stage changes | Return `IQueryable`; call `SaveChanges` |
| Repository factory | Resolve and cache one repository per entity type within scope | Be singleton; confuse this cache with catalog data cache |
| Unit of Work | Repository resolution and one commit method; invalidate catalog after success | Save checkout's loan and copy separately |
| DbContext | Tracking, configurations; stamp, validate, then save | Be used concurrently by several operations |
| Entity | Persistence properties and entity invariants | Become the HTTP contract |
| IMemoryCache | Shared, immutable catalog summaries for at most 2 minutes | Cache tracked Books or serve filtered search from catalog cache |

### Folder / file map

| Folder/file | Contents |
|---|---|
| `Models/` | Category, Book, Copy, Patron, Loan, Fine; IEntity, IAuditable |
| `Contracts/` | Request/response records, dynamic PublishedYear attribute |
| `Data/Configurations/` | Six `IEntityTypeConfiguration<T>` classes |
| `Data/LibraryDbContext.cs` | DbSets, apply configurations, audit/validation save override |
| `Data/Migrations/` | Generated migration and model snapshot, committed to Git |
| `Repositories/` | Generic + Book + Loan repositories, factory, UoW |
| `Services/` | Six resource services, generic CRUD service, mappings/results |
| `Controllers/` | Thin resource controllers; abstract generic category CRUD base |
| `Security/` or `Auth/` | Scope policies and optional OpenAPI bearer transformer |
| `Caching/` | Catalog key and lifetime, optional cache decorator |
| `Program.cs` | DI, JWT, Serilog, middleware, migrate/seed, Run |
| `LibraryApi.Tests/` | xUnit, isolated SQLite factory, test auth, integration tests |

### Lifetimes

| Registration | Lifetime | Why |
|---|---|---|
| DbContext, repositories, factory, UoW, application services | Scoped | Share one request's context and commit |
| IMemoryCache | Singleton | Reuse catalog across requests |
| Stateless mapper, TimeProvider.System | Singleton is appropriate | No scoped context or mutable request state |
| Transient | New object per resolution | Not needed for this architecture |

Scoped means per **DI scope**; ASP.NET creates one scope per HTTP request. It does not guarantee one OS thread. Sequential awaited operations are safe; parallel operations on the same context are not.

<a id="implementation"></a>
## 2. IMPLEMENTATION - do this in order

### STEP 0 - baseline and exam contract

1. Read the actual sheet. Mark every required route, response field, exact error string, scope, and test.
2. Work in your own copy/repository. Keep `Example` and teammates' notes as references.
3. Open the solution and run the unchanged starter once. It uses .NET 10 / SQLite and port 5090.
4. Commit the baseline, then use the required branch `feature/test-review-library-api`.
5. Add `.gitignore` entries before staging: `**/bin/`, `**/obj/`, `.vs/`, `*.db`, `*.db-wal`, `*.db-shm`, `*.db-journal`, `*.user`.

From the supplied example's solution directory:

```powershell
Set-Location -LiteralPath 'K:\CLASSES 26 FALL\WEB_3\Finaltest\Example'
dotnet restore
dotnet build
dotnet run --project LibraryApi
# Ctrl+C stops it. Copy the starter to your own working repo before changing it.
```

Do not assume the exact shell commands in a friend's Bash guide work in Windows PowerShell. This guide uses PowerShell-compatible commands on separate lines.

### STEP 1 - database model and migrations

**Edit:** all six entities, six configuration files, DbContext, Program.cs, migration.

1. Add `IEntity` with `int Id { get; set; }` if your generic implementation needs it.
2. Add `IAuditable` to all six entities: `DateTime CreatedAtUtc`, `DateTime? UpdatedAtUtc`.
3. Move each entity's current fluent rules from DbContext into its own configuration class. Preserve relationships, uniqueness, lengths, and precision.
4. In `OnModelCreating`, call `ApplyConfigurationsFromAssembly`.
5. Add the save override from [section 6](#save-pipeline), plus entity invariants.
6. Add Design 10.0.12. Replace startup `EnsureCreatedAsync()` with `MigrateAsync()`; keep seeding after migration.
7. Generate the migration **after** adding audit properties so they are included. If you add audit fields later, generate another migration.

```csharp
using LibraryApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Isbn).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Isbn).IsUnique();
        b.HasOne(x => x.Category).WithMany(x => x.Books)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Inside LibraryDbContext:
protected override void OnModelCreating(ModelBuilder modelBuilder) =>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
```

| Principal | Dependent and FK | Delete rule |
|---|---|---|
| Category | Book.CategoryId | Restrict |
| Book | Copy.BookId | Restrict |
| Copy | Loan.CopyId | Restrict |
| Patron | Loan.PatronId | Restrict |
| Loan | Fine.LoanId | Restrict |

| Field | Maximum length / other rule |
|---|---|
| Category.Name | 80, unique |
| Book.Title / Isbn | 200 / 20; ISBN unique |
| Copy.Barcode / Status | 32 / 20; barcode unique |
| Patron.StudentNumber / FullName / Email | 16 / 120 / 200; student number and email unique |
| Fine.Reason / Amount | 200 / `HasPrecision(10, 2)` |

**SQLite caution:** keep the required EF model configuration, but do not claim SQLite enforces string lengths or a fixed decimal precision merely because you set `HasMaxLength` / `HasPrecision`. Validate input and entities too.

```powershell
dotnet add LibraryApi package Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet tool install --global dotnet-ef --version 10.0.12
# If already installed, use: dotnet tool update --global dotnet-ef --version 10.0.12
dotnet ef migrations add InitialCreate --project LibraryApi --startup-project LibraryApi --output-dir Data/Migrations
dotnet ef database update --project LibraryApi --startup-project LibraryApi
```

An existing database made by `EnsureCreated` has no matching migration history. For a disposable practice DB only, stop the app, back up or remove the **specific** old DB, then apply migrations. Do not delete real data or unrelated files. See [commands](#commands).

**Check:** six tables, five restricted FKs, five unique indexes, audit columns, and `__EFMigrationsHistory`. Commit migration and snapshot, not `.db`.

### STEP 2 - DTO validation

**Edit:** `Contracts/CatalogContracts.cs`, `CirculationContracts.cs`.

Keep starter response names/properties. Add timestamps to responses only if wanted; requests never contain them. Add `[Required]` and `[StringLength]` to name/title/ISBN/barcode/patron fields using the lengths above.

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class PublishedYearAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is int year && year >= 1450 && year <= DateTime.UtcNow.Year + 1;
}

public sealed record BookRequest
{
    [Required, StringLength(200)]
    public string Title { get; init; } = "";
    [Required, StringLength(20)]
    public string Isbn { get; init; } = "";
    [PublishedYear(ErrorMessage = "Published year is out of range.")]
    public int PublishedYear { get; init; }
    public int CategoryId { get; init; }
}
```

For patron request email, use the sheet's email rule (the supplied invariant is nonblank and contains `@`). `[EmailAddress]` can enforce a stricter input rule; do not invent a different email contract if the exam specifies exact acceptance. `IsActive` defaults to `true`.

**Important:** do not add `[Range(1, ...)]` on checkout IDs if it changes a missing patron/book from the required **404** to **400**. Preserve check order.

`[ApiController]` turns annotation failures into `400 ValidationProblemDetails` with an `errors` object before the action. Remove the old handwritten required/length/year checks once attributes replace them. Missing rows and business conflicts remain service rules.

**Check:** authenticated book POST with blank title returns 400 and validation errors. No migration for DTO-only edits.

### STEP 3 - generic repository

**Edit:** `Repositories/IRepository.cs`, `Repository.cs`.

The code below uses Jainder-style names. `QueryAsync` accepts a query description but executes it **inside** the repository; it does not return an unexecuted `IQueryable`.

```csharp
using System.Linq.Expressions;
using LibraryApi.Data;
using Microsoft.EntityFrameworkCore;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(int id, CancellationToken token = default);
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? filter = null, CancellationToken token = default);
    Task<IReadOnlyList<R>> QueryAsync<R>(
        Func<IQueryable<T>, IQueryable<R>> shape, CancellationToken token = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken token = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public class Repository<T>(LibraryDbContext db) : IRepository<T> where T : class
{
    protected LibraryDbContext Db { get; } = db;
    protected DbSet<T> Set { get; } = db.Set<T>();
    public async Task<T?> GetAsync(int id, CancellationToken token = default) =>
        await Set.FindAsync([id], token);
    public async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? filter = null, CancellationToken token = default)
    {
        IQueryable<T> q = Set.AsNoTracking();
        if (filter is not null) q = q.Where(filter);
        return await q.ToListAsync(token);
    }
    public async Task<IReadOnlyList<R>> QueryAsync<R>(
        Func<IQueryable<T>, IQueryable<R>> shape, CancellationToken token = default) =>
        await shape(Set.AsNoTracking()).ToListAsync(token);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken token = default) =>
        Set.AnyAsync(filter, token);
    public void Add(T entity) => Set.Add(entity);
    public void Update(T entity)
    {
        // This pattern expects an entity loaded/tracked BEFORE assigning fields.
        // Do not use it to save an already-mutated detached entity: Attach alone
        // does not tell EF which properties changed.
        if (Db.Entry(entity).State == EntityState.Detached) Set.Attach(entity);
    }
    public void Remove(T entity) => Set.Remove(entity);
}
```

**Update workflow:** tracked `GetAsync` -> assign allowed fields -> one UoW save. No `DbSet.Update`, `EntityState.Modified`, or `IsModified = true` for every field. Public GET responses use DTO projections / nontracking reads, not this tracked getter.

### STEP 4 - specific repositories, factory, UoW

**Book repository:** inherit `Repository<Book>`, implement `IBookRepository : IRepository<Book>`. Add paged search, category grouping, details projection, catalog projection.

**Loan repository:** inherit `Repository<Loan>`, implement `ILoanRepository : IRepository<Loan>`. Add tracked available-copy lookup, tracked loan-with-copy lookup for return, loan response projection, overdue summary join.

**Other entities:** use `Repository<Category/Copy/Patron/Fine>` and its query-shaping method. A new FineRepository is not needed for this PDF.

Factory pattern (names are your choice, behavior is required):

```csharp
public interface IRepositoryFactory
{
    IRepository<T> Get<T>() where T : class;
}

public sealed class RepositoryFactory(LibraryDbContext db) : IRepositoryFactory
{
    private readonly Dictionary<Type, object> cache = [];
    public IRepository<T> Get<T>() where T : class
    {
        var type = typeof(T);
        if (!cache.TryGetValue(type, out var value))
        {
            value = type == typeof(Book) ? (object)new BookRepository(db)
                : type == typeof(Loan) ? new LoanRepository(db)
                : new Repository<T>(db);
            cache[type] = value;
        }
        return (IRepository<T>)value;
    }
}
```

If your specific repository constructor has extra dependencies, inject/resolve those scoped repositories in the factory instead of using the one-argument `new` shown above. Always return the same instance for repeated resolution within the scope.

```csharp
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    IBookRepository Books { get; }
    ILoanRepository Loans { get; }
    Task<int> SaveChangesAsync(CancellationToken token = default);
}

public static class CatalogCache
{
    public const string Key = "books:catalog";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
}

public sealed class UnitOfWork(
    LibraryDbContext db, IRepositoryFactory factory, IMemoryCache cache) : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class => factory.Get<T>();
    public IBookRepository Books => (IBookRepository)factory.Get<Book>();
    public ILoanRepository Loans => (ILoanRepository)factory.Get<Loan>();
    public async Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        var written = await db.SaveChangesAsync(token);
        cache.Remove(CatalogCache.Key); // only reached after a successful save
        return written;
    }
}
```

Use `using Microsoft.Extensions.Caching.Memory;` for cache code. The factory's dictionary caches **repository instances per scope**. IMemoryCache caches **catalog data across scopes**. They solve different problems.

**Check:** `Get<Book>()` returns a BookRepository, `Get<Loan>()` a LoanRepository, `Get<Fine>()` a generic repository. Repeating the same call returns the same object. No repository saves or returns `IQueryable`.

### STEP 5 - services preserve every rule

**Edit:** CategoryService, BookService, CopyService, PatronService, LoanService, FineService; generic CRUD service; mapper/result types.

For each starter action:

1. Move EF query execution into a repository.
2. Move checks and state changes into a service, in the original order.
3. Return a response DTO or a result with success/error information.
4. Call `uow.SaveChangesAsync` exactly once for each successful write operation.
5. After successful book create, checkout, return, log a named ID property.

Two valid error designs: **Jainder:** `ServiceResult` mapped in controller; **Abdull:** exceptions mapped centrally. Choose one. If using exceptions, explicitly handle them as 404/409; `UseExceptionHandler()` alone does not automatically know which exception means which business status. Never map every `InvalidOperationException` to 409 indiscriminately: it can also indicate programming/EF errors.

For this guide, prefer Jainder's result style. A result can contain a domain outcome enum, optional DTO, and error message; HTTP mapping stays in the controller. There is no need to make the service return MVC types.

**Checkout service algorithm:** see [exact order](#rules). Available copy must be tracked. Add a new loan and mutate that copy, then commit once. **Do not forget `uow.Repository<Loan>().Add(loan)`**; constructing the loan object alone does not insert it.

**Return service algorithm:** tracked loan with Copy -> missing/already returned checks -> set ReturnedAt and copy Available -> optionally stage one Fine -> commit once -> read response DTO -> return `{ loan, fine }`.

### STEP 6 - generic category CRUD + thin controllers

Required generic chain:

| Type | Purpose |
|---|---|
| `ICrudService<TEntity,TResponse,TRequest>` | List, get, create, update, delete contract |
| `CrudService<TEntity,TResponse,TRequest>` | Shared CRUD over UoW; mapper creates/applies/projects |
| `CategoryService` | Override unique-name check and delete-with-books guard; project BookCount |
| Abstract `CrudController<TEntity,TResponse,TRequest>` | HTTP action templates, service calls, result mapping; **no Route attribute** |
| `CategoriesController` | Concrete route `api/categories`; inherits generic controller |

Use the same generic parameter order everywhere. Abdull's and Jainder's type argument orders differ; swapping request/response silently causes confusing compile errors.

```csharp
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService service)
    : CrudController<Category, CategoryResponse, UpsertCategoryRequest>(service);
```

The snippet assumes you implemented a compatible generic base constructor and `ICategoryService` extends its closed generic service interface. The full worked base is in [Jainder's CrudController](Notes/Jainder/library-api-test-review-main/LibraryApi/Controllers/CrudController.cs) and [CrudService](Notes/Jainder/library-api-test-review-main/LibraryApi/Services/CrudService.cs).

All other controllers keep specific actions. They take only services. Normal create response:

```csharp
return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
```

Copy create has no single-copy GET in the required contract; preserve its starter `Location` URI with `Created(...)` rather than inventing a GET endpoint. Return-loan is **200**, not 204.

Place `books.write` authorization on write action methods in the generic base (for this category implementation), or on properly overridden concrete actions. A plain `[Authorize]` on the concrete class would also protect public GETs. Check inherited actions through HTTP tests.

**Check:** no DbContext/repository/UoW in controllers; DTOs only; same route templates and codes; 201 has Location.

### STEP 7 - catalog cache

Repository returns four-field summaries, ordered Title then Id:

```csharp
public sealed record BookSummary(int Id, string Title, string Isbn, string CategoryName);

// BookRepository.GetCatalogAsync:
return await Db.Books.AsNoTracking()
    .OrderBy(b => b.Title).ThenBy(b => b.Id)
    .Select(b => new BookSummary(b.Id, b.Title, b.Isbn, b.Category.Name))
    .ToListAsync(token);
```

Service caches only that result:

```csharp
public async Task<IReadOnlyList<BookSummary>> CatalogAsync(CancellationToken token)
{
    var rows = await cache.GetOrCreateAsync(CatalogCache.Key, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = CatalogCache.Lifetime;
        return await uow.Books.GetCatalogAsync(token);
    });
    return rows!;
}
```

`cache` is injected `IMemoryCache`; `uow` is injected `IUnitOfWork`. A repository decorator is also possible, but you do not need both approaches.

**Required:** two-minute **absolute** maximum lifetime; live filtered `/api/books`; invalidate after **any successful commit**, including category rename; failed save retains old cache. Do not clear in `finally` or before save. Immutable records contain only scalar fields; do not mutate the shared collection after caching it.

The simple memory cache meets the exercise's sequential tests. It does not provide distributed consistency or prevent a concurrent cache-fill/write race. Do not claim it is a full concurrency solution.

### STEP 8 - JWT, audit, Serilog, test project

Use [security](#security), [save pipeline](#save-pipeline), and [tests](#tests). Complete them; they are mandatory in this PDF.

In `Program.cs`, register:

```csharp
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<LibraryDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(ICrudService<,,>), typeof(CrudService<,,>));
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICopyService, CopyService>();
builder.Services.AddScoped<IPatronService, PatronService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IFineService, FineService>();
// Register your mapper(s) and any extra constructor dependencies too.
```

Logging setup before `builder.Build()`:

```csharp
builder.Host.UseSerilog((context, log) => log
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());
```

After build, typical pipeline:

```csharp
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.MigrateAsync();
    await LibrarySeed.ApplyAsync(db);
}
app.Run();
public partial class Program;
```

This is assembly guidance, not a complete Program.cs: include JWT/policy configuration from section 7 before build. Startup migration/seeding runs once before `Run`, not inside each HTTP request.

**Swagger UI:** useful extra in Jainder's notes, not an explicit PDF requirement. `/openapi/v1.json` is a document, not an interactive page. Add UI package/config only if useful and time permits; it does not replace required work.

### STEP 9 - verify and submit

Run build and tests, finish README, write all eight answers, check no secrets/generated DB files are staged, and push the required branch. The PDF requires one named branch; nine PRs are Jainder's organizational choice, not a requirement in this PDF.

<a id="routes"></a>
## 3. ROUTE CONTRACT - all 27 routes

`Public` means no token. All bodies/responses retain starter property names. All creates need **201 + Location**.

| Method | Route | Success | Policy / key behavior |
|---|---|---|---|
| GET | `/api/categories` | 200 | Public; bookCount per item |
| GET | `/api/categories/{id}` | 200 | Public; missing 404 |
| POST | `/api/categories` | 201 | books.write; body name |
| PUT | `/api/categories/{id}` | 204 | books.write |
| DELETE | `/api/categories/{id}` | 204 | books.write; books exist -> 409 |
| GET | `/api/books` | 200 | Public; title/categoryId/page/pageSize; live query |
| GET | `/api/books/by-category` | 200 | Public; group and count |
| GET | `/api/books/catalog` | 200 | Public; new cached route |
| GET | `/api/books/{id}` | 200 | Public; availableCopies; missing 404 |
| POST | `/api/books` | 201 | books.write; title/isbn/publishedYear/categoryId |
| PUT | `/api/books/{id}` | 204 | books.write |
| DELETE | `/api/books/{id}` | 204 | books.write; copies exist -> 409 |
| GET | `/api/books/{bookId}/copies` | 200 | Public; barcode order; missing book 404 |
| POST | `/api/books/{bookId}/copies` | 201 | books.write; barcode; initial status Available |
| DELETE | `/api/books/{bookId}/copies/{copyId}` | 204 | books.write; wrong book/copy 404; loans exist 409 |
| GET | `/api/patrons` | 200 | loans.read; activeOnly; openLoanCount/unpaidFineTotal |
| GET | `/api/patrons/{id}` | 200 | loans.read; missing 404 |
| POST | `/api/patrons` | 201 | loans.write; isActive defaults true |
| PUT | `/api/patrons/{id}` | 204 | loans.write |
| GET | `/api/loans` | 200 | loans.read; patronId/overdueOnly |
| GET | `/api/loans/overdue-summary` | 200 | loans.read; open overdue only; due then id order |
| GET | `/api/loans/{id}` | 200 | loans.read; missing 404 |
| POST | `/api/loans` | 201 | loans.write; patronId/bookId; checkout |
| POST | `/api/loans/{id}/return` | 200 | loans.write; `{ loan, fine }`; fine may be null |
| GET | `/api/fines` | 200 | loans.read; unpaidOnly |
| GET | `/api/fines/totals` | 200 | loans.read; unpaid sum per patron, patron-name order |
| POST | `/api/fines/{id}/pay` | 204 | loans.write; mark paid, do not delete |

### Status code lookup

| Code | Meaning in this API |
|---|---|
| 200 | Successful read or loan return |
| 201 | Created; response and Location |
| 204 | Successful update/delete/payment; no body |
| 400 | Invalid request annotations; ValidationProblemDetails |
| 401 | Protected route without valid authentication |
| 403 | Authenticated caller missing required scope |
| 404 | Missing requested/related row, including mismatched nested copy |
| 409 | Duplicate, forbidden delete, circulation conflict |

<a id="rules"></a>
## 4. BUSINESS RULES - exact behavior

### CHECKOUT ORDER - never reorder these checks

| Order | Check | Failure |
|---|---|---|
| 1 | Patron exists? | 404 `Patron not found.` |
| 2 | Patron active? | 409 `Patron is not active.` |
| 3 | Patron has unpaid fine? | 409 `Patron has unpaid fines.` |
| 4 | Book exists? | 404 `Book not found.` |
| 5 | Available copy exists? | 409 `No available copy.` |

Then pick available copy with **lowest barcode**, set it **OnLoan**, create loan with one captured UTC `now`, `DueAt = now.AddDays(14)`, stage loan, save once, return DTO with 201 + Location. An open overdue loan alone does **not** block checkout.

### Return and fine formula

1. Loan missing -> 404 `Loan not found.`
2. Already returned -> 409 `Loan is already returned.`
3. Set ReturnedAt to captured UTC now and copy status to Available.
4. Only if `returnedAt > DueAt`, add exactly one unpaid fine with reason `Overdue`.
5. Save loan + copy + optional fine together once. Return 200 with loan and fine/null.

```csharp
if (returnedAt > loan.DueAt)
{
    var days = Math.Max(1, (int)Math.Ceiling((returnedAt - loan.DueAt).TotalDays));
    var fine = new Fine
    {
        Loan = loan,
        Amount = days * 0.50m,
        IsPaid = false,
        Reason = "Overdue"
    };
    uow.Repository<Fine>().Add(fine);
}
// Save once after staging ALL changes.
```

| Return time | Fine |
|---|---|
| Before or exactly at DueAt | None |
| 1 second late | 0.50 |
| Exactly 24 hours late | 0.50 |
| 24 hours + 1 second late | 1.00 |
| Exactly 48 hours late | 1.00 |

Grace's seeded 1.00 fine follows two started days late; do not special-case Grace. Paying sets `IsPaid = true`; repeat payment -> 409 `Fine is already paid.`; missing -> 404 `Fine not found.`

### Delete and duplicate messages

| Situation | 409 detail |
|---|---|
| Category has books | `Category still has books.` |
| Book has copies | `Book has copies.` |
| Copy has loans, even returned ones | `Copy has loans.` |
| Duplicate category | `Category already exists.` |
| Duplicate ISBN | `ISBN already exists.` |
| Duplicate barcode | `Barcode already exists.` |
| Duplicate student number | `Student number already exists.` |
| Duplicate email | `Email already exists.` |

Missing category/book/copy/patron uses `Category not found.`, `Book not found.`, `Copy not found.`, `Patron not found.` with 404 as applicable. Update checks duplicate fields while excluding the same row (`x.Id != id`). Trim input as the starter does. Do not silently change comparison/case rules.

**Atomicity is not full concurrency protection:** one save prevents partial writes. Two simultaneous checkout requests can still select the same copy unless additional concurrency control exists. The PDF's one-save rule does not by itself prove double-checkout prevention under concurrency.

<a id="queries"></a>
## 5. QUERY RECIPES - SQL until execution

**Rule:** compose `Where`, order, page, group/join, and aggregates against EF's `IQueryable`; execute at the end. `ToListAsync()` then `Where/GroupBy/Sum` is wrong for this test even if JSON matches.

| Operator | Simple meaning | SQL-side use |
|---|---|---|
| Where | Keep matching rows | WHERE |
| OrderBy / ThenBy | Sort; break ties | ORDER BY |
| Skip / Take | Offset / page size | LIMIT / OFFSET |
| Select | Choose output columns / DTO | SELECT |
| GroupBy + aggregate | One result per group | GROUP BY, COUNT, SUM |
| Join | Combine related tables | JOIN |
| AnyAsync | Does one exist? | EXISTS |
| CountAsync | Count rows | COUNT |
| Include / ThenInclude | Load related entities | Related entity loading; usually unnecessary for scalar DTOs |
| AsNoTracking | Do not track returned entities | EF tracking behavior, not a SQL clause |
| ToListAsync | Execute and materialize | Boundary after which later LINQ uses memory |

All `db`/`Db` examples below belong inside repositories, not controllers. For generic entities, query shaping is supplied to `QueryAsync` and materialized inside the repository.

### A. Book search: filters + stable paging + availability count

```csharp
page = page < 1 ? 1 : page;
pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);
IQueryable<Book> query = db.Books.AsNoTracking();
if (!string.IsNullOrWhiteSpace(title))
{
    var term = title.Trim();
    query = query.Where(b => b.Title.Contains(term));
}
if (categoryId is int c) query = query.Where(b => b.CategoryId == c);

var books = await query.OrderBy(b => b.Title).ThenBy(b => b.Id)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(b => new BookListItemResponse(
        b.Id, b.Title, b.Isbn, b.PublishedYear, b.CategoryId, b.Category.Name,
        b.Copies.Count(c => c.Status == CopyStatus.Available)))
    .ToListAsync(token);
```

This uses the starter's seven-argument response record. If you add audit response fields, adjust constructors consistently. Page clamps may live in the service; all actual database filtering/sorting/paging stays in the query.

### B. Books by category: preserve grouping

```csharp
var groups = await (
    from book in db.Books.AsNoTracking()
    join category in db.Categories on book.CategoryId equals category.Id
    group book by category.Name into g
    orderby g.Key
    select new BookGroupResponse(g.Key, g.Count()))
    .ToListAsync(token);
```

Grouping books means a category with no books is absent from this route, as in the starter. Do not replace it with a different zero-count-category contract.

### C. Available copy: tracked, because checkout changes it

```csharp
// LoanRepository: use Db.Copies, NOT the inherited Set (which is DbSet<Loan>).
return await Db.Copies
    .Where(c => c.BookId == bookId && c.Status == CopyStatus.Available)
    .OrderBy(c => c.Barcode)
    .FirstOrDefaultAsync(token);
```

### D. Overdue summary: explicit four-table join

```csharp
var rows = await (
    from loan in db.Loans.AsNoTracking()
    join copy in db.Copies on loan.CopyId equals copy.Id
    join book in db.Books on copy.BookId equals book.Id
    join patron in db.Patrons on loan.PatronId equals patron.Id
    where loan.ReturnedAt == null && loan.DueAt < now
    orderby loan.DueAt, loan.Id
    select new LoanResponse(
        loan.Id, patron.Id, patron.FullName, copy.Id, copy.Barcode,
        book.Id, book.Title,
        new DateTimeOffset(loan.CheckedOutAt, TimeSpan.Zero),
        new DateTimeOffset(loan.DueAt, TimeSpan.Zero),
        null, true))
    .ToListAsync(token);
```

Navigation projections can also generate SQL joins. This explicit form most directly demonstrates the PDF's requested join across loan/copy/book/patron. Filter and ordering must remain in SQL. UTC `DateTimeOffset` construction is response formatting at the final projection.

### E. Fine list: filter before ToListAsync

```csharp
var rows = await uow.Repository<Fine>().QueryAsync(q => q
    .Where(f => !unpaidOnly || !f.IsPaid)
    .OrderBy(f => f.Loan.Patron.FullName).ThenBy(f => f.Id)
    .Select(f => new FineResponse(f.Id, f.LoanId, f.Loan.PatronId,
        f.Loan.Patron.FullName, f.Amount, f.IsPaid, f.Reason)), token);
```

### F. Fine totals: group and decimal sum in SQL

```csharp
var totals = await uow.Repository<Fine>().QueryAsync(q => q
    .Where(f => !f.IsPaid)
    .GroupBy(f => new { f.Loan.PatronId, f.Loan.Patron.FullName })
    .OrderBy(g => g.Key.FullName)
    .Select(g => new FineTotalResponse(
        g.Key.PatronId, g.Key.FullName, g.Sum(f => f.Amount))), token);
```

**Correct for EF Core 10.0.12:** SQLite provider supports decimal Sum through `ef_sum` (added in EF Core 9). Do not copy the old workaround in Abdull's fixed FineRepository that materializes then groups/sums. Do not change monetary decimals to double to avoid this. Official reference: [SQLite function mappings](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/functions).

### G. Patrons: open-loan count and unpaid sum in query

```csharp
var rows = await uow.Repository<Patron>().QueryAsync(q => q
    .Where(p => !activeOnly || p.IsActive)
    .OrderBy(p => p.FullName).ThenBy(p => p.Id)
    .Select(p => new PatronResponse(
        p.Id, p.StudentNumber, p.FullName, p.Email, p.IsActive,
        p.Loans.Count(l => l.ReturnedAt == null),
        p.Loans.SelectMany(l => l.Fines).Where(f => !f.IsPaid)
            .Sum(f => (decimal?)f.Amount) ?? 0m)), token);
```

Use nullable Sum + `?? 0m` to represent no unpaid fines as zero. `activeOnly` is a bool matching the starter query parameter. Project get-by-ID the same way rather than loading every related loan/fine into memory.

### H. Category book count, details, and existence

```csharp
// Category count is in SQL, no Include needed.
var categories = await uow.Repository<Category>().QueryAsync(q => q
    .OrderBy(c => c.Name)
    .Select(c => new CategoryResponse(c.Id, c.Name, c.Books.Count)), token);

// Duplicate check excluding the entity being updated.
bool duplicate = await uow.Repository<Book>().AnyAsync(
    b => b.Isbn == isbn && b.Id != id, token);
```

For a book detail response, reuse the projected available-copy count from A with `Where(b => b.Id == id)` and a single-result execution. Loading copies with Include then counting them in C# does not satisfy the PDF's blanket query requirement.

### I. Update only allowed fields

```csharp
var book = await uow.Repository<Book>().GetAsync(id, token);
// If null: return your service's NotFound result before continuing.
// Check category existence / duplicate ISBN in the service first.
book!.Title = request.Title.Trim();
book.Isbn = request.Isbn.Trim();
book.PublishedYear = request.PublishedYear;
book.CategoryId = request.CategoryId;
await uow.SaveChangesAsync(token);
```

No need to call Update for an already tracked object. Never copy audit columns from requests. To inspect SQL during practice, call `query.ToQueryString()` **inside repository/debug code before materializing**. Correct response values alone do not prove SQL translation.

<a id="save-pipeline"></a>
## 6. STAMP VALIDATE SAVE - audit and invariants

```csharp
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}
```

Every entity implements this. Creation timestamp is server-owned. New rows: Created now, Updated null. Changed rows: Created stays, Updated now.

Inside DbContext, with `System.ComponentModel.DataAnnotations` and EF Core imports:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken token = default)
{
    var now = DateTime.UtcNow;
    var entries = ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified)
        .ToList(); // local tracker inspection, NOT a database query materialization

    foreach (var entry in entries)
    {
        if (entry.Entity is not IAuditable row) continue;
        if (entry.State == EntityState.Added)
        {
            row.CreatedAtUtc = now;
            row.UpdatedAtUtc = null;
        }
        else
        {
            row.UpdatedAtUtc = now;
            entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
        }
    }
    foreach (var entry in entries)
        Validator.ValidateObject(entry.Entity,
            new ValidationContext(entry.Entity), validateAllProperties: true);

    return await base.SaveChangesAsync(token);
}
```

The local `.ToList()` here is fine: it snapshots change-tracker entries, not data that should be grouped/filtered in SQL. Make sure all application commits use the overridden overload through UoW; synchronous SaveChanges or a different overload can bypass a narrowly implemented override.

### Required entity invariants

| Entity | Rules in `IValidatableObject.Validate` and `IsValid` |
|---|---|
| Book | Nonblank Title/Isbn; year 1450 through next calendar year |
| Patron | Nonblank StudentNumber/FullName/Email; Email contains `@` |
| Loan | DueAt > CheckedOutAt; ReturnedAt null or >= CheckedOutAt |
| Fine | Amount > 0; nonblank Reason |

Example members to put inside `Loan : IValidatableObject` (alongside existing properties):

```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext context)
{
    if (DueAt <= CheckedOutAt)
        yield return new ValidationResult("DueAt must be after CheckedOutAt.", [nameof(DueAt)]);
    if (ReturnedAt is DateTime returned && returned < CheckedOutAt)
        yield return new ValidationResult("ReturnedAt cannot be before CheckedOutAt.", [nameof(ReturnedAt)]);
}
public bool IsValid(out List<string> errors)
{
    errors = Validate(new ValidationContext(this))
        .Select(r => r.ErrorMessage ?? "Invalid loan.").ToList();
    return errors.Count == 0;
}
```

Repeat that shared-rule pattern for Book, Patron, Fine. Do not maintain two different rule sets for IsValid and Validate. EF Core does **not** automatically run these DataAnnotations before persistence; the explicit Validator call is required.

### Three validation locations in the PDF

| Location | Checks | Outcome |
|---|---|---|
| Request | Data annotations | Automatic 400 ValidationProblemDetails |
| Entity | IValidatableObject + IsValid | Reports invalid domain state, usable outside HTTP |
| Save boundary | Explicit Validator call after audit stamping | Invalid added/modified entity throws before base save; no row written |

Services additionally enforce existence/uniqueness/circulation rules (404/409). Some teammates call these service checks the middle of a three-layer explanation; remember the PDF specifically requires **both entity validation methods and execution at SaveChanges**.

Validation failure may leave modified objects in the current change tracker; it does not persist them. Verify the database in a fresh scope. Raw SQL and `ExecuteUpdate` bypass this validation/audit override. Unique indexes/FKs remain independent database safeguards.

### Structured logging

```csharp
logger.LogInformation("Book {BookId} created", book.Id);
logger.LogInformation("Loan {LoanId} checked out", loan.Id);
logger.LogInformation("Loan {LoanId} returned", loan.Id);
```

Log after successful commit. Do not use interpolated strings for structured event fields. Never log patron names, email, student numbers, bodies, tokens, or secrets. Request logging and timestamps are not immutable change history.

<a id="security"></a>
## 7. JWT SETUP - authentication and authorization

Authentication answers **who is calling?** Authorization answers **does this caller have the required permission?**

Before build, with JwtBearer import:

```csharp
var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("Authority and Audience must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = authority;
        o.Audience = audience;
        o.MapInboundClaims = false;
    });
builder.Services.AddAuthorization(o =>
{
    foreach (var scope in new[] { "books.write", "loans.read", "loans.write" })
    {
        o.AddPolicy(scope, p => p.RequireAuthenticatedUser()
            .RequireAssertion(c => c.User.Claims
                .Where(claim => claim.Type is "scope" or "scp")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(scope, StringComparer.Ordinal)));
    }
});
```

The PDF requires `scope`, both one space-separated claim and multiple claims. Supporting `scp` too is a useful extension in your teammates' implementations. Match full scope tokens, not substring search: `loans.write.extra` is not `loans.write`.

On write action:

```csharp
[HttpPost]
[Authorize(Policy = "loans.write")]
public async Task<ActionResult> Checkout(CheckoutRequest request, CancellationToken token)
{
    // Call service and map its result. No EF here.
}
```

This is an action-shape illustration; supply a real body/result. Do not put `[Authorize]` on all of BooksController or CategoriesController because their reads stay public.

| Setting | Meaning |
|---|---|
| Authority | Trusted identity provider; JWT metadata/signing keys and expected issuer |
| Audience | Identifier of this API; token aud must match |
| Scope | Permission carried by a token, e.g. loans.write |
| Role | User/group classification, e.g. Librarian; not a substitute for this scope map |

Use `UseAuthentication()` before `UseAuthorization()`. Production validates JWT signature, issuer, audience, lifetime. A test handler intentionally replaces this for integration tests; it does not prove real JWT cryptographic validation works.

**Placeholders** let startup configuration checks pass but cannot authenticate real tokens. A real provider and matching access token are needed for manual successful protected calls. Do not send an ID token when the API expects an access token.

<a id="tests"></a>
## 8. TEST MATRIX - all 13 required cases and expected answers

Use isolated SQLite per factory. A unique temp SQLite file (Jainder) or an open in-memory SQLite connection per factory (Abdull) both fit. Do not use production `library.db` or EF's nonrelational InMemory provider.

| # | Test input / action | Expected result |
|---|---|---|
| 1 | GET books?pageSize=2 | 200; Clean Code, then Refactoring |
| 2 | GET books/by-category | Programming 2; Fiction 1 |
| 3 | Checkout The Hobbit for Ada | 201; HB-001; separate later GET copies shows OnLoan |
| 4 | Checkout Grace; Alan; valid active/no-fine patron + missing book | 409 unpaid fines; 409 inactive; 404 missing book |
| 5 | Return Ada's overdue Clean Code loan, then return again | 200 with one overdue fine; later GET copy Available; second return 409 |
| 6 | GET loans/overdue-summary before return | Includes Ada's open overdue loan; excludes Grace's returned loan |
| 7 | GET fines/totals before pay | Grace unpaid total 1.00 |
| 8 | Delete Programming category with books.write | 409 Category still has books. |
| 9 | Anonymous books; anonymous checkout; loans.read checkout; loans.write allowed checkout | 200; 401; 403; action reached (201 for allowed input) |
| 10 | Authenticated book POST with blank title | 400 ValidationProblemDetails, Title errors |
| 11 | Add loan DueAt <= CheckedOutAt, save | ValidationException; fresh context sees no new row |
| 12 | Create book, then update | Created set / Updated null initially; Created unchanged / Updated set later |
| 13 | Catalog twice, then successful title update, then catalog again | Cached summaries reused before update; new title after commit |

**Do not make tests depend on test order.** Each fresh factory should start from the seed. If several requests are part of one test, keep the same factory so they use the same isolated DB. Find IDs from responses when working with a modified seed.

### Factory setup checklist

1. Add `public partial class Program;` after top-level Program statements so the test project can reference it.
2. Reference API from xUnit project; add Mvc.Testing 10.0.12; add test project to solution.
3. Set nonempty Authority/Audience test placeholders **before** host startup checks.
4. Replace DbContext options with isolated SQLite. Remove the production options registrations; avoid two database configurations pointing at different files.
5. Let migrations and seed run against the test DB.
6. Register a test-only authentication handler as default authenticate/challenge/forbid scheme.
7. Dispose host/connection before deleting a temp DB. Never register the test auth scheme in production.

Use the complete [Jainder factory](Notes/Jainder/library-api-test-review-main/LibraryApi.Tests/LibraryFactory.cs) with its matching [TestAuthHandler](Notes/Jainder/library-api-test-review-main/LibraryApi.Tests/TestAuthHandler.cs). Do not mix its auth header with Abdull's fixture.

| Test fixture | Header it understands |
|---|---|
| Jainder | `Authorization: Test loans.write` or its `WithScopes` helper |
| Abdull | `X-Test-Scopes: loans.write` |
| Real running API | `Authorization: Bearer <real-access-token>` |

### Example cross-request test

This assumes Jainder's factory/handler and unchanged seed IDs (Ada 1, Hobbit 2):

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[Fact]
public async Task Checkout_is_visible_to_a_later_request()
{
    using var factory = new LibraryFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "loans.write");
    var post = await client.PostAsJsonAsync("/api/loans", new { patronId = 1, bookId = 2 });
    Assert.Equal(HttpStatusCode.Created, post.StatusCode);
    Assert.NotNull(post.Headers.Location);
    using var created = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
    Assert.Equal("HB-001", created.RootElement.GetProperty("barcode").GetString());

    using var copies = JsonDocument.Parse(
        await client.GetStringAsync("/api/books/2/copies"));
    Assert.Contains(copies.RootElement.EnumerateArray(), c =>
        c.GetProperty("barcode").GetString() == "HB-001" &&
        c.GetProperty("status").GetString() == "OnLoan");
}
```

Put this method in a test class in the fixture's namespace. It proves persistence across requests, not just that the POST echoed the requested value.

### Cache test that actually proves reuse

Two HTTP JSON arrays being equal proves equal values, not cache reuse. Better: spy on the underlying catalog repository call count; or use Jainder's controlled direct-DB title edit that bypasses UoW, confirm cached old value, then update through the API and confirm fresh value. Within DI/service tests, reference equality can also show the same cached object.

Also verify failed save retains cache and category rename invalidates it: the 13-case minimum is not the entire behavior contract.

<a id="faq"></a>
## 10. EXTRA QUESTIONS - teammate Q&A consolidated and corrected

### Database and relationships

**What is a migration?** Generated, versioned C# operations that change schema, with Up/Down and a model snapshot. EF records applied versions in `__EFMigrationsHistory`.

**Why Migrate instead of EnsureCreated?** EnsureCreated creates a schema for a new database but does not incrementally evolve it using migration history. Migrate applies the committed changes. Do not mix them on the same existing practice DB without a deliberate reset/baseline strategy.

**Does database update seed the starter data?** It applies migrations. This starter's seed is a separate `LibrarySeed.ApplyAsync` startup call, so run the API to execute that seed. Migration-based seed data is a different mechanism.

**Where is the DB and how do I start it?** SQLite is a file, not a server to activate. Connection string is `ConnectionStrings:Default` in appsettings; normally `Data Source=library.db`. A relative path resolves from the process working directory, so confirm which file a tool/test uses. The starter launch/run arrangement uses the API project folder. Port is configured in `Properties/launchSettings.json`.

**Which side holds a foreign key?** In this model's one-to-many relations, the dependent/many side: Book has CategoryId, Copy has BookId, Loan has CopyId/PatronId, Fine has LoanId. Foreign keys can also be on the dependent of a one-to-one relation; “always the many side” only describes one-to-many.

**Why Restrict plus a service delete check?** Restrict protects the database if application checks are bypassed or race. Service checks return the required friendly 409 detail instead of exposing a database exception. Cascade would remove dependent/history rows, violating this test.

**Why a unique index if the service checks duplicates?** Simultaneous requests can both pass a precheck. The database unique index enforces the constraint. A robust application can translate recognized unique violations into 409 too; do not translate every DbUpdateException to a duplicate.

**What do length and precision mean?** They describe the EF relational model. This PDF requires the exact settings; SQLite does not itself enforce all SQL Server-style length/precision constraints. Keep application validation and decimal arithmetic.

**How do I model other relationship types if the exam changes domain?** One-to-one uses HasOne/WithOne and HasForeignKey<Dependent>; implicit many-to-many uses HasMany/WithMany; a relationship with payload needs an explicit join entity. Loan connects Copy and Patron with dates; its own Id allows repeated borrowing over time. Do not replace Loan with a composite CopyId/PatronId key that prevents repeat loans.

**When is a migration needed?** Persisted model/schema changes: columns, indexes, FKs, requiredness, etc. Ordinary DTO/controller/service/query changes do not require one. Editing this runtime seed does not create a migration automatically. A model configuration edit that has no schema effect may produce no migration operations; inspect generated output.

### LINQ and tracking

**IQueryable vs IEnumerable?** EF IQueryable builds a provider expression tree; supported operators translate to SQL. After materialization or AsEnumerable, subsequent operators use .NET iteration. Not every possible IQueryable expression translates; integration tests and SQL inspection confirm it.

**Deferred execution?** Query construction does not fetch rows. Enumeration or terminal operations such as ToListAsync/AnyAsync/CountAsync execute the query. Re-executing a query normally queries again.

**Method syntax vs query syntax?** Two C# syntaxes for LINQ. Both can translate to SQL. Query syntax can make join/group examples easier to read.

**Why projection instead of Include?** Projection asks for precisely the fields/aggregates needed by a response. Include loads related entities; use it for tracked return's Copy or other operations that need the entity graph. It is unnecessary for scalar DTO projection and must not lead to in-memory counting here.

**Does projection always mean no tracking?** A DTO projection containing only scalar values is not entity tracking. A projection that includes an entity instance can still track that instance. Use AsNoTracking on read-only queries and avoid entity fields in DTOs.

**Find vs FirstOrDefault vs SingleOrDefault?** Find uses primary key and checks tracked entities first. FirstOrDefault returns a first match or null; use ordering when which row matters. SingleOrDefault expects zero/one and throws on multiple matches. Neither query method magically makes an untracked update persist.

**Why Any instead of loading rows?** Any asks whether a match exists, usually translated as EXISTS. Count is for actual counts; loading a list just to test existence wastes data.

**Why Title then Id before paging?** SQL without ordering has no guaranteed sequence. Title can tie; unique Id makes the sequence deterministic. Use Skip((page-1)*size), Take(size) after the order and before execution.

**Can I sum decimal in SQLite?** Yes for this EF Core 10 provider: decimal Sum is translated through ef_sum, available since EF Core 9. The old workaround in one friend's solution is not appropriate for the given version/rubric.

**Why tracked reads for updates?** EF compares current values to original tracked values and writes changed properties. Load before changing fields. Attaching an object after you already changed it does not reconstruct original values. Calling DbSet.Update indiscriminately marks the entity broadly modified.

**What is the cancellation token for?** ASP.NET supplies request cancellation; pass it down so EF can cancel work when possible. Cancellation is cooperative, not a guarantee that an already committed write can be undone.

### Repository, factory, UoW, services

**Why repositories if EF already has DbSet?** This exercise asks for a boundary separating query/staging code from business rules. It centralizes queries and supports the required factory/caching architecture. EF already supplies much repository/UoW behavior; the extra types are the course's chosen design.

**Why not return IQueryable?** It lets callers run/change database queries outside the repository boundary, possibly after context disposal. Execute in the repository and return materialized results. QueryAsync accepts a description and materializes it internally.

**Why no repository SaveChanges?** One business operation may stage changes through several repositories. Independent saves break the single transaction boundary; UoW commits once.

**Does transient always create another DbContext?** No. A transient repository/factory resolved within one scope can still receive the same scoped DbContext. A note claiming transient automatically creates multiple contexts is inaccurate. Scoped is required here for consistent per-scope repository/factory identity and matches the rubric.

**Why not a singleton factory?** It would capture/request shared scoped dependencies and expose a non-thread-safe context across requests. ConcurrentDictionary would only address dictionary access, not context lifetime or thread safety.

**Why cast dictionary values?** It holds repositories for different generic entity types, so the value type is object. The lookup's entity Type key determines the expected IRepository<T> cast. Book and Loan also expose specific interfaces.

**Generic service vs generic controller?** The service reuses business-independent CRUD implementation. The controller reuses HTTP actions. Category hooks handle unique name/delete guards/counts; loan workflows remain specific.

**What is an open generic DI registration?** `AddScoped(typeof(ICrudService<,,>), typeof(CrudService<,,>))` registers a generic template. DI can close it for requested type arguments, provided all dependencies/constraints are satisfied. Register a specific service against its own interface to ensure its overrides are used.

**Where do new features go?** Define DTO/route contract, implement repository query, service rules/commit, thin action and policy, then tests. A change that alters persisted schema also needs a migration.

### HTTP, caching, security, logs, tests

**What does ApiController do here?** Attribute-route API conventions, binding-source inference, and automatic model-validation 400 responses. It does not validate every future entity save or automatically authorize actions.

**How does CreatedAtAction work?** It generates Location from the named action and route values and returns 201 with a body. If there is no matching action/route, URL generation can fail. Preserve the copy-create starter Location rather than inventing a copy GET.

**Why no Route on generic base?** Concrete controllers must own resource routes. A resource-specific base route can produce unintended inherited routes/conflicts. The base still defines verb/action suffixes.

**What is `{id:int}`?** A route constraint restricting matches to integer IDs. It keeps literal routes such as catalog/by-category separate from the ID template; it is not an entity existence check.

**ProblemDetails vs ValidationProblemDetails?** ProblemDetails gives standard error fields such as status/title/detail, with optional extensions. ValidationProblemDetails adds field-keyed errors. A traceId is commonly added by ASP.NET configuration; it is not universally required by the base standard.

**Why absolute rather than sliding cache expiration?** Reads must not extend the entry beyond two minutes from insertion. Sliding-only expiration could retain frequently accessed data indefinitely.

**Why invalidate for category rename?** Catalog stores CategoryName as well as book fields. Any successful commit must remove the entry per the PDF; invalidate after success, not before or in finally.

**Why records? Are they always immutable?** Positional records of scalar values give convenient init-only properties and value equality. Records can still contain mutable properties/collections; record syntax alone does not guarantee deep immutability. Keep cached summaries scalar and do not mutate the shared list.

**Why scope policies instead of class-wide Authorize?** Books/categories mix public reads and protected writes. Policies on individual actions preserve anonymous access and distinguish loans.read from loans.write.

**What does MapInboundClaims=false do?** It preserves original JWT claim names rather than mapping known names into framework claim-type names. That makes scope/scp lookup predictable.

**Why auth placeholders / fail at startup?** Missing Authority/Audience is a configuration error; fail early. Placeholders document required settings and support test-host checks, but do not configure a working identity provider.

**Why structured logs instead of interpolation?** Named templates such as `{LoanId}` retain a separate property for querying; interpolation produces only formatted text. Log the event after success and exclude sensitive data.

**Why timestamps in DbContext?** One common save path stamps consistently, including seed/service writes through that path. They still reflect a server clock and can be bypassed by raw SQL or another save path.

**Why entity validation if DTOs already validate?** Seed, internal jobs, and service code can create invalid entities without HTTP. The explicit save-boundary guard rejects invalid state before database writes.

**Unit vs integration test?** Unit isolates one component with fakes. WebApplicationFactory integration tests exercise routing/auth/model validation/real EF SQLite behavior together. They catch SQL translation and route/Location failures that in-memory mocks can miss.

**Why public partial Program?** Top-level statements generate Program. Declaring it public/partial lets a separate test assembly reference `WebApplicationFactory<Program>`.

**Why keep an in-memory SQLite connection open?** The database disappears when the last connection closes. A factory-owned open connection keeps it alive for several HTTP requests; isolate connections per factory and dispose afterward.

**What does test auth prove?** Correct policy outcomes for anonymous, wrong-scope, and allowed-scope identities. It does not test a real issuer's signature validation, metadata, or token issuance.

**Is Swagger mandatory?** Not in this practical PDF. Jainder adds an interactive UI over the OpenAPI document; useful for manual calls, but not worth replacing required tasks. The API can be tested through `.http` requests or HttpClient without UI.

<a id="commands"></a>
## 11. COMMANDS AND TROUBLESHOOTING

Run commands from the **solution folder containing LibraryApi/LibraryApi.csproj**, unless noted. Inspect the starter's csproj first: keep EF Core/JWT packages at the PDF's 10.0.12 rather than upgrading mid-exam.

### Packages / tests / tools

```powershell
dotnet --list-sdks
dotnet restore
dotnet build
dotnet add LibraryApi package Microsoft.EntityFrameworkCore.Design --version 10.0.12
dotnet add LibraryApi package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.12
# Versions used by the local worked example:
dotnet add LibraryApi package Serilog.AspNetCore --version 10.0.0
dotnet add LibraryApi package Serilog.Sinks.Console --version 6.1.1

dotnet new xunit -n LibraryApi.Tests -o LibraryApi.Tests --framework net10.0
dotnet add LibraryApi.Tests package Microsoft.AspNetCore.Mvc.Testing --version 10.0.12
dotnet add LibraryApi.Tests reference LibraryApi/LibraryApi.csproj
dotnet sln add LibraryApi.Tests/LibraryApi.Tests.csproj
dotnet test
dotnet test --filter 'FullyQualifiedName~Checkout'
```

Only create the test project once. Templates may generate `.slnx`; if your folder has more than one solution, pass the intended `.sln`/`.slnx` explicitly to `dotnet sln`, build, and test.

### Migration commands

```powershell
dotnet ef --version
dotnet ef migrations add InitialCreate --project LibraryApi --startup-project LibraryApi --output-dir Data/Migrations
dotnet ef database update --project LibraryApi --startup-project LibraryApi
dotnet ef migrations list --project LibraryApi --startup-project LibraryApi
dotnet ef migrations has-pending-model-changes --project LibraryApi --startup-project LibraryApi
# Later model change: use a new descriptive migration name, not InitialCreate again.
dotnet ef migrations add AddAuditColumns --project LibraryApi --startup-project LibraryApi --output-dir Data/Migrations
```

Do not run both example migration-add commands blindly; AddAuditColumns is needed only if those fields were added after InitialCreate. `migrations remove` is for removing the last unapplied migration you are correcting, not deleting migration history already in use.

If `dotnet ef` cannot create the context because Program needs auth config, supply Development user-secrets/env vars or implement `IDesignTimeDbContextFactory<LibraryDbContext>` that configures the same SQLite provider/connection. A design-time factory avoids launching the HTTP host; it is a useful helper, not separately required by the PDF.

### Local auth settings

```powershell
dotnet user-secrets init --project LibraryApi
dotnet user-secrets set 'Authentication:Authority' 'https://YOUR-IDENTITY-PROVIDER/' --project LibraryApi
dotnet user-secrets set 'Authentication:Audience' 'YOUR-API-AUDIENCE' --project LibraryApi
dotnet run --project LibraryApi
```

Replace placeholders with exam-provided settings for real token tests. To use environment variables in the current PowerShell session:

```powershell
$env:Authentication__Authority = 'https://YOUR-IDENTITY-PROVIDER/'
$env:Authentication__Audience = 'YOUR-API-AUDIENCE'
dotnet run --project LibraryApi
```

User-secrets are loaded by the default builder in Development. The testing factory must supply its own config before startup checks.

### Safe practice-DB reset

Only when the DB is disposable and you have confirmed this exact solution folder. Stop the API and any DB editing tool. These are exact file removals, not a recursive cleanup:

```powershell
Get-Item -LiteralPath '.\LibraryApi\library.db' -ErrorAction SilentlyContinue
# After verifying it is the old practice database you intend to discard:
Remove-Item -LiteralPath '.\LibraryApi\library.db' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath '.\LibraryApi\library.db-wal' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath '.\LibraryApi\library.db-shm' -ErrorAction SilentlyContinue
Remove-Item -LiteralPath '.\LibraryApi\library.db-journal' -ErrorAction SilentlyContinue
dotnet ef database update --project LibraryApi --startup-project LibraryApi
dotnet run --project LibraryApi
```

### Git workflow required by the practice PDF

In your own starter copy, if Git is not already initialized:

```powershell
git init
git add .
git commit -m 'Import unchanged Library API starter'
git switch -c feature/test-review-library-api
```

After completing work:

```powershell
dotnet build
dotnet test
git status --short
git diff --check
# Inspect staged content and ensure no DB, generated build files or secrets.
git add .
git diff --cached --stat
git commit -m 'Refactor Library API with repositories services security and tests'
git push -u origin feature/test-review-library-api
```

Set up your authorized own repository remote before push. Do not point it at the course notes repository. If a required branch already exists, switch to it instead of creating it again. Separate commits per layer are useful; a separate PR per layer is optional unless tomorrow's sheet says otherwise.

### TROUBLESHOOTING

| Symptom | Likely cause / next step |
|---|---|
| net10.0 unsupported | Check .NET 10 SDK with dotnet --list-sdks; use course machine setup |
| dotnet ef not found | Install matching tool; reopen terminal if its tool directory is not in PATH |
| Table already exists after migration | Old EnsureCreated DB; verify exact disposable file and reset deliberately |
| No such table / missing audit column | Wrong DB path or unapplied migration; inspect migration list and actual working directory |
| Migrate fails after audit added | Generate migration for the changed model; commit snapshot too |
| Unable to resolve service | Missing interface/mapper/open-generic registration or wrong type argument order |
| Scoped service captured by singleton | Factory/UoW/repository/service lifetime incorrect; keep them scoped |
| Book GET unexpectedly 401 | Class-level authorization, inherited write policy on reads, or restrictive fallback policy |
| Protected POST unexpectedly 400 instead of missing patron 404 | Added ID annotation changed the contract |
| Wrong-scope test returns 401 | Test handler did not authenticate; check fixture-specific header/default scheme |
| Placeholder auth cannot validate bearer | Expected: configure real authority/audience/token for manual protected calls |
| SQLite decimal sum advice says unsupported | Old note; EF Core 10 supports ef_sum. Check package/provider version and query shape |
| LINQ cannot be translated | Check custom helper calls, premature IEnumerable conversion, unsupported query shape; inspect SQL and simplify |
| Copy change not persisted | Lookup was AsNoTracking, detached mutation attached too late, or save omitted |
| Loan constructed but missing | Forgot repository.Add(loan) before UoW save |
| Duplicate fine on second return | Missing already-returned guard before changing/staging anything |
| CreatedAt changes on update | Request mapping or broad DbSet.Update; fix tracked workflow and CreatedAt.IsModified=false |
| Catalog stale after category rename | Invalidating only BookService writes; invalidate centrally after every successful commit |
| Catalog lost after failed save | Cleared before save or in finally; move after awaited save |
| Test DB disappears between requests | In-memory SQLite connection closed; keep one open for factory lifetime |
| database locked | Another writer/DB editor or an undisposed test connection; close the responsible handle |
| API port already in use | Stop the other process or run `dotnet run --project LibraryApi --urls http://localhost:5100` |
| Swagger 404 | UI is optional / Development-only; test actual route or /openapi/v1.json |
| No route matches supplied values | CreatedAtAction's Get name/route values do not match; preserve correct copy Location handling |

<a id="comparison"></a>
## 12. FRIENDS NOTES - differences and corrections

| Source | What it actually provides | Best use | Main caution |
|---|---|---|---|
| Sean / WebNotes | Offline mirror of the course website: lectures, labs, patterns, test-review material | Check original course explanations and lab solutions | It is reference material, not Sean's single finished exam answer; do not treat every later lecture as in scope |
| Abdull / notes.md | Broad theory and worked snippets | Understand why the layers and rules exist | Some snippets are inconsistent with their containing repository; check actual types |
| Abdull / EXAM-PLAN.md | Earlier planning document marked planning-only | Rubric checklist, routes, implementation stages | Some permissive choices conflict with strict PDF requirements (e.g. counting an aggregate in memory) |
| Abdull / Exam-implementation.md | Implementation account plus alternatives and factory/no-factory appendix | Follow the staged refactor and understand design alternatives | Claims about completed tests are the document's claims, not evidence every rubric condition is met |
| Abdull / fixed-exam | Concrete solution; cache decorator, exception-oriented services, in-memory SQLite tests | Alternative architecture and fixture reference | Actual code still has in-memory totals, broad Set.Update, and extra specialized FineRepository |
| Abdull / potential-exam-guide.md | Explicitly hypothetical three-service/no-factory variant | Adapt only if tomorrow's sheet changes scope | It removes required features from this practice PDF; not a complete solution for the given sheet |
| Jainder | Bottom-up implementation recipe, per-layer Q&A, worked solution | Closest main implementation reference for this PDF | Nine branches/PRs and Swagger UI are workflow/extras, not PDF requirements; some explanations need precision |

### Concrete implementation differences

| Topic | Abdull's fixed solution | Jainder's solution | Recommendation for this PDF |
|---|---|---|---|
| Expected errors | Exception-oriented service flow | ServiceResult + controller mapping | Either can work; use one consistently, preserve exact status/detail |
| Catalog caching | Repository decorator + invalidator | BookService + shared key; UoW invalidates | Both fit when expiration and commit rules hold |
| Factory lookup | Generic + CustomRepository lookup; Fine special case too | Generic lookup + Books/Loans typed UoW properties | Keep Book/Loan specific, others generic as explicitly requested |
| Fine totals | Materialize then group/sum | Generic query shaping, SQL GroupBy/Sum | Use SQL translation; no decimal workaround on EF 10 |
| Update | EfRepository.Update calls Set.Update | Tracked mutations; Update avoids broad Modified state | Use tracked load then allowed field assignments |
| Test database | Open in-memory SQLite per factory | Unique temporary SQLite file | Both valid when isolated, persistent during test, cleaned up |
| Test authentication | X-Test-Scopes header | Authorization: Test scopes | Copy matching factory AND handler; production still Bearer |
| Service count | Six in implemented version; three in hypothetical version | Six specific resources + generic category machinery | Follow six resource-specific services for this PDF |

### Do not copy these mistakes

1. **“SQLite cannot sum decimal.”** Outdated for this specified EF version. Fixed-exam FineRepository and its review answer materialize/group/sum in memory; the PDF explicitly disallows this. Modern provider uses ef_sum.
2. **`Set.Update(entity)` everywhere.** Abdull's generic implementation does this; it marks properties broadly modified. Use tracked load/assign/save instead.
3. **Wrong DbSet in snippets.** In notes.md, available-copy code under LoanRepository uses `Set` as if it were Copy; inherited Set is Loan. Use `Db.Copies`. Another HasCopies snippet treats Book Set as Copy; use `Db.Copies.AnyAsync(c => c.BookId == bookId)` or `Db.Books.AnyAsync(b => b.Id == bookId && b.Copies.Any())`.
4. **Checkout snippet that never adds the loan.** One teaching fragment constructs a Loan and saves without explicitly staging it. Add it through the loan repository.
5. **Assuming all unmodified in-memory count paths are acceptable.** The PDF says every route filter/sort/group/join/aggregate must stay in EF; project details counts too.
6. **“A normal POCO always holds a disposed context.”** Not generally true. Caching entities is still wrong here due to shared mutable state/graphs and potential lazy-loading dependencies.
7. **“Scoped means one fixed thread.”** It means lifetime scope. Safety comes from no concurrent access, not thread affinity.
8. **“Transient automatically creates multiple DbContexts.”** A scoped DbContext can still be shared within the same scope. Scoped factory/UoW remain the required choice.
9. **“HasPrecision makes SQLite enforce exact DECIMAL(10,2).”** Keep model settings, but SQLite type behavior differs. Do not overstate enforcement.
10. **“Passing tests proves the entire rubric.”** Route-value tests can pass with in-memory grouping and broad updates. Review architecture and SQL placement as well.

The guide does not change any of your friends' files or the starter. The comparison is based on the current local files, including contradictions between plans and actual implementations.

<a id="checklist"></a>
## 13. FINAL CHECKLIST - match the 100-point rubric

| Area | Points | Verify |
|---|---|---|
| Routes/statuses/circulation | 15 | All 27 routes; exact messages/check order; 201 Location; 14 days; started-day fines |
| Migrations/configurations/queries | 15 | Migrate; committed schema; six configs; Restrict/indexes; translated filters/groups/sums |
| Services/DI/DTO | 10 | Controllers service-only; correct lifetimes; records rather than entities |
| Repository/factory/UoW | 15 | Generic + Book/Loan specific; dictionary per scope; no IQueryable returned; one commit |
| Catalog | 8 | Four fields; stable order; immutable summaries; <=2 min; invalidate after success |
| Generic category controller | 7 | Generic service/controller; categories inherit; no base Route; others specific |
| JWT/scopes | 10 | Configured JWT; exact scope map; public 200; anonymous 401; wrong-scope 403 |
| Audit/logging/validation | 10 | Six auditable entities; stamp then validate then save; DTO annotations; four entity guards; ID logs |
| Tests | 10 | All 13 minimum scenarios; isolated SQLite; write then later read |

- [ ] `dotnet build` succeeds and `dotnet test` passes on the solution.
- [ ] CreatedAtUtc stays fixed; UpdatedAtUtc set on actual updates.
- [ ] No blanket Update/Modified overwriting all fields.
- [ ] No route-level filter/count/group/sum after ToList/AsEnumerable.
- [ ] Checkout and return stage every needed row before the single commit.
- [ ] Fine remains a generic repository entity for this PDF.
- [ ] No JSON persistence, SQL Server/provider switch, Lost endpoint, SendGrid, event bus, ESB, or Aspire.
- [ ] README: run/test/migrations, auth placeholders, scope map, limitations of validation/logs/timestamps.
- [ ] TEST-REVIEW.md: eight answers tied to classes/routes you actually built.
- [ ] Required branch pushed to your own repository; no secrets, DB, bin, obj staged.

**If the actual exam changes the domain:** first write a new entity/FK table and route/scope matrix; then apply the same layer order. Keep invariants and business workflows from that sheet, not library-specific assumptions.

<a id="sources"></a>
## 14. SOURCE FILES - offline references

Primary requirements: `K:\CLASSES 26 FALL\WEB_3\tmp\Practical Test_ Library API _ 420-5W6-Notes.pdf` (one long captured page; all sections reviewed). Matching offline practical page is in Abdull's Notes mirror.

| Reference | Local link |
|---|---|
| Actual untouched starter | [Example README](Example/README.md), [Controllers](Example/LibraryApi/Controllers/), [Contracts](Example/LibraryApi/Contracts/), [DbContext](Example/LibraryApi/Data/LibraryDbContext.cs) |
| Abdull theory | [notes.md](Notes/Abdull/Web3Exam/notes.md) |
| Abdull plan | [EXAM-PLAN.md](Notes/Abdull/Web3Exam/EXAM-PLAN.md) |
| Abdull implementation/alternatives | [Exam-implementation.md](Notes/Abdull/Web3Exam/Exam-implementation.md) |
| Abdull hypothetical three-service guide | [potential-exam-guide.md](Notes/Abdull/Web3Exam/potential-exam-guide.md) |
| Abdull fixed project | [README](Notes/Abdull/Web3Exam/fixed-exam/LibraryApi/README.md), [review answers](Notes/Abdull/Web3Exam/fixed-exam/LibraryApi/TEST-REVIEW.md) |
| Jainder implementation order | [EXAM-INSTRUCTIONS.md](Notes/Jainder/library-api-test-review-main/EXAM-INSTRUCTIONS.md) |
| Jainder layer explanations/Q&A | [Layer index](Notes/Jainder/library-api-test-review-main/docs/layers/README.md) |
| Jainder commands / LINQ | [Commands](Notes/Jainder/library-api-test-review-main/docs/MIGRATIONS-AND-COMMANDS.md), [LINQ](Notes/Jainder/library-api-test-review-main/docs/LINQ-CHEATSHEET.md) |
| Jainder Visual Studio setup | [Setup](Notes/Jainder/library-api-test-review-main/docs/SETUP-VISUAL-STUDIO.md) |
| Jainder DB/port explanation | [Database and localhost](Notes/Jainder/library-api-test-review-main/docs/DATABASE-AND-LOCALHOST.md) |
| Jainder review answers | [TEST-REVIEW.md](Notes/Jainder/library-api-test-review-main/TEST-REVIEW.md) |
| Sean course mirror | [Offline index](Notes/Sean/WebNotes/index.html), [Test review](Notes/Sean/WebNotes/gabriellarco-jac.github.io/420-5W6-website/docs/test-review/index.html) |
| Official decimal translation correction | [Microsoft SQLite function mappings](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/functions), [EF Core 9 decimal Sum support](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew) |

**Validation of this document:** requirements checked against the PDF and actual starter; referenced solution patterns inspected. This is a study guide, not a newly built solution, and it does not certify either teammate project as fully rubric-compliant.
