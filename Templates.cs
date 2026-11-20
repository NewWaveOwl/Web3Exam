using System.Text.RegularExpressions;
namespace ExamHelper;

public sealed record CodeTemplate(string Title, string Help, Func<string, string, string> Generate);
public static class Templates
{
    private static readonly HashSet<string> Keywords = new("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while record required file scoped var dynamic".Split(' '));
    public static bool ValidIdentifier(string value) => Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*$") && !Keywords.Contains(value);
    public static bool ValidInputs(string ns, string entity) => ValidIdentifier(entity) && ns.Split('.').All(ValidIdentifier);
    public static IReadOnlyList<CodeTemplate> All { get; } =
    [
        new("01 • Request / response DTOs", "Replace Name and its example 120-character limit with the real contract. Keep server-owned fields out of requests. Responses keep starter JSON names.", (n,e) => $$"""
using System.ComponentModel.DataAnnotations;
namespace {{n}}.Contracts;

public sealed record Upsert{{e}}Request
{
    [Required, StringLength(120)] // EXAMPLE limit: use your exam's value
    public string Name { get; init; } = "";
}

public sealed record {{e}}Response(int Id, string Name);
// Optional audit response properties only if requested:
// DateTime CreatedAtUtc, DateTime? UpdatedAtUtc
// Do not add audit properties to the request.
"""),
        new("02 • Generic repository", "AppDbContext is a placeholder. Lists/projections are nontracking; Get is tracked for edits. No saves or IQueryable returns. All snippets need actual namespaces/model imports.", (n,e) => $$"""
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using {{n}}.Data;
namespace {{n}}.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<R>> QueryAsync<R>(
        Func<IQueryable<T>, IQueryable<R>> shape, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    protected DbSet<T> Set => db.Set<T>();
    public async Task<T?> GetAsync(int id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<R>> QueryAsync<R>(
        Func<IQueryable<T>, IQueryable<R>> shape, CancellationToken ct = default) =>
        await shape(Set.AsNoTracking()).ToListAsync(ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default) =>
        Set.AnyAsync(filter, ct);
    public void Add(T entity) => Set.Add(entity);
    public void Update(T entity)
    {
        // Contract: load tracked BEFORE mutating. Do not attach an already-mutated
        // detached entity and expect EF to reconstruct its original values.
        if (db.Entry(entity).State == EntityState.Detached)
            throw new InvalidOperationException("Load a tracked entity before updating.");
        // Tracked assignments are detected at SaveChanges; no blanket Set.Update.
    }
    public void Remove(T entity) => Set.Remove(entity);
}
"""),
        new("03 • Service + explicit outcomes", "Example Name-based CRUD slice. Requires repository + UoW templates, actual model with Id/Name, and DTOs. Add exact service rules/check order. Get response is a nontracking projection; update uses tracked Get.", (n,e) => $$"""
using {{n}}.Contracts;
using {{n}}.Models;
using {{n}}.Repositories;
namespace {{n}}.Services;

public enum Outcome { Success, NotFound, Conflict }
public sealed record ServiceResult<T>(Outcome Outcome, T? Value, string? Error = null);

public interface I{{e}}Service
{
    Task<{{e}}Response?> GetAsync(int id, CancellationToken ct);
    Task<{{e}}Response> CreateAsync(Upsert{{e}}Request request, CancellationToken ct);
    Task<ServiceResult<bool>> UpdateAsync(int id, Upsert{{e}}Request request, CancellationToken ct);
}

public sealed class {{e}}Service(IUnitOfWork uow) : I{{e}}Service
{
    private IRepository<{{e}}> Repository => uow.Repository<{{e}}>();
    public async Task<{{e}}Response?> GetAsync(int id, CancellationToken ct) =>
        (await Repository.QueryAsync(q => q.Where(x => x.Id == id)
            .Select(x => new {{e}}Response(x.Id, x.Name)), ct)).SingleOrDefault();

    public async Task<{{e}}Response> CreateAsync(Upsert{{e}}Request request, CancellationToken ct)
    {
        // Add required FK/duplicate/workflow checks IN CONTRACT ORDER.
        var entity = new {{e}} { Name = request.Name.Trim() };
        Repository.Add(entity);
        await uow.SaveChangesAsync(ct);
        return new(entity.Id, entity.Name);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, Upsert{{e}}Request request, CancellationToken ct)
    {
        var entity = await Repository.GetAsync(id, ct);
        if (entity is null) return new(Outcome.NotFound, false, "{{e}} not found.");
        // Add actual duplicate/FK checks and exact error messages here.
        entity.Name = request.Name.Trim();
        await uow.SaveChangesAsync(ct);
        return new(Outcome.Success, true);
    }
}
// If this domain changes several rows, stage every row BEFORE the one save.
"""),
        new("04 • Thin controller", "Requires the service + DTO templates. Route naming is illustrative; preserve the starter's real routes, status codes and error details. Add action policies using the real scope map.", (n,e) => $$"""
using Microsoft.AspNetCore.Mvc;
using {{n}}.Contracts;
using {{n}}.Services;
namespace {{n}}.Controllers;

[ApiController]
[Route("api/{{e.ToLowerInvariant()}}s")] // replace with exact contract route
public sealed class {{e}}sController(I{{e}}Service service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<{{e}}Response>> Get(int id, CancellationToken ct)
    {
        var response = await service.GetAsync(id, ct);
        return response is null
            ? Problem(statusCode: 404, detail: "{{e}} not found.") : Ok(response);
    }

    [HttpPost]
    // [Authorize(Policy = "ACTUAL_SCOPE")]
    public async Task<ActionResult> Create(Upsert{{e}}Request request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    // [Authorize(Policy = "ACTUAL_SCOPE")]
    public async Task<ActionResult> Update(int id, Upsert{{e}}Request request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.Outcome switch
        {
            Outcome.Success => NoContent(),
            Outcome.NotFound => Problem(statusCode: 404, detail: result.Error),
            Outcome.Conflict => Problem(statusCode: 409, detail: result.Error),
            _ => throw new InvalidOperationException("Unmapped service outcome.")
        };
    }
}
"""),
        new("05 • Dictionary factory", "Only add a factory if required. AppDbContext is a placeholder. Uncomment a specialized branch only after that repository implements IRepository<TEntity>. Register scoped.", (n,e) => $$"""
using {{n}}.Data;
using {{n}}.Models;
namespace {{n}}.Repositories;

public interface IRepositoryFactory
{
    IRepository<T> Get<T>() where T : class;
}
public sealed class RepositoryFactory(AppDbContext db) : IRepositoryFactory
{
    private readonly Dictionary<Type, object> repositories = [];
    public IRepository<T> Get<T>() where T : class
    {
        var key = typeof(T);
        if (!repositories.TryGetValue(key, out var repository))
        {
            // Specialized example, once implemented:
            // repository = key == typeof({{e}})
            //     ? (object)new {{e}}Repository(db) : new Repository<T>(db);
            repository = new Repository<T>(db);
            repositories[key] = repository;
        }
        return (IRepository<T>)repository;
    }
}
// Program.cs:
// builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
"""),
        new("06 • Unit of Work + DI", "Factory-based variant matching the templates. AppDbContext is a placeholder. Add cache invalidation AFTER successful save if required; no repository saves. Direct-injection variants can have commit-only UoW.", (n,e) => $$"""
using {{n}}.Data;
namespace {{n}}.Repositories;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
public sealed class UnitOfWork(AppDbContext db, IRepositoryFactory factory) : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class => factory.Get<T>();
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var written = await db.SaveChangesAsync(ct);
        // cache.Remove(SummaryCache.Key); // inject cache only if required
        // A failure above must retain the old cached entry.
        return written;
    }
}

// Program.cs (with the actual using directives):
// builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
// Keep the ACTUAL provider from the starter.
// builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// builder.Services.AddScoped<I{{e}}Service, {{e}}Service>();
"""),
        new("07 • EF entity configuration", "Adapt fields, max lengths, unique indexes and FK/delete rules. Name max length below is illustrative. Keep one configuration per required entity and commit migrations.", (n,e) => $$"""
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {{n}}.Models;
namespace {{n}}.Data.Configurations;

public sealed class {{e}}Configuration : IEntityTypeConfiguration<{{e}}>
{
    public void Configure(EntityTypeBuilder<{{e}}> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired(); // example
        // b.HasIndex(x => x.ACTUAL_UNIQUE_FIELD).IsUnique();
        // b.HasOne(x => x.Parent).WithMany(p => p.Children)
        //   .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
// Inside your DbContext.OnModelCreating, preserve other required setup:
// modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
"""),
        new("08 • Audit + save validation", "Put the interface in Models; override in the actual DbContext. Add fields to required entities and migrate. Entities implement IValidatableObject with actual invariants. This only covers this overload.", (n,e) => """
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
}

// Inside DbContext; imports: EF Core, System.ComponentModel.DataAnnotations
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var changed = ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
    var now = DateTime.UtcNow;
    foreach (var entry in changed)
    {
        if (entry.Entity is not IAuditable row) continue;
        if (entry.State == EntityState.Added)
        { row.CreatedAtUtc = now; row.UpdatedAtUtc = null; }
        else
        {
            row.UpdatedAtUtc = now;
            entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
        }
    }
    foreach (var entry in changed)
        Validator.ValidateObject(entry.Entity,
            new ValidationContext(entry.Entity), validateAllProperties: true);
    return await base.SaveChangesAsync(ct);
}
// Raw SQL / ExecuteUpdate / other unguarded save paths can bypass this.
"""),
        new("09 • JWT + scope policies", "Program.cs fragments; install compatible JwtBearer first. Replace config keys/scope from the real sheet. Placeholder settings cannot validate real tokens. Add policies only on the appropriate actions.", (n,e) => """
using Microsoft.AspNetCore.Authentication.JwtBearer;

var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("Configure Authority and Audience.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    { o.Authority = authority; o.Audience = audience; o.MapInboundClaims = false; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("resources.write", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(c => c.User.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains("resources.write", StringComparer.Ordinal)));
});
// After build, BEFORE endpoints:
// app.UseAuthentication();
// app.UseAuthorization();
// [Authorize(Policy = "resources.write")] on protected actions.
// Public reads stay public; no token -> 401; valid wrong scope -> 403.
"""),
        new("10 • Cache summaries", "Adapt the example fields/TTL/key to actual requirements. Query remains in repository; service caches scalar records. Inject IMemoryCache and invalidate centrally after commits, including parent changes.", (n,e) => $$"""
public sealed record {{e}}Summary(int Id, string Name);
public static class SummaryCache
{
    public const string Key = "{{e.ToLowerInvariant()}}:summary";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2); // exam value!
}

// Service fragment, using Microsoft.Extensions.Caching.Memory:
public async Task<IReadOnlyList<{{e}}Summary>> SummaryAsync(CancellationToken ct)
{
    var rows = await cache.GetOrCreateAsync(SummaryCache.Key, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = SummaryCache.Lifetime;
        return await uow.Repository<{{e}}>().QueryAsync(q => q
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new {{e}}Summary(x.Id, x.Name)), ct);
    });
    return rows!;
}
// builder.Services.AddMemoryCache();
// Keep filtered/live search separate unless the contract asks otherwise.
"""),
        new("11 • Query recipes", "Repository-side expressions; substitute actual fields and DTOs. IQueryable operators must execute before materialization. SQL support depends on provider/version.", (n,e) => $$"""
// Search + stable paging. Clamp bounds using the actual contract.
IQueryable<{{e}}> q = db.Set<{{e}}>().AsNoTracking();
if (!string.IsNullOrWhiteSpace(search))
{
    var term = search.Trim();
    q = q.Where(x => x.Name.Contains(term));
}
var rows = await q.OrderBy(x => x.Name).ThenBy(x => x.Id)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(x => new {{e}}Response(x.Id, x.Name)).ToListAsync(ct);

// Existence without loading rows:
bool exists = await db.Set<{{e}}>().AnyAsync(x => x.Id == id, ct);

// Example grouped aggregate shape (adapt actual property names):
// db.Orders.Where(o => !o.IsPaid)
//   .GroupBy(o => new { o.CustomerId, o.Customer.Name })
//   .OrderBy(g => g.Key.Name)
//   .Select(g => new TotalResponse(g.Key.CustomerId, g.Sum(o => o.Amount)))
//   .ToListAsync(ct);
// Do not ToList() first and then group/sum in C#.
// To inspect actual SQL before execution: query.ToQueryString().
"""),
        new("12 • Integration-test recipe", "Checklist and test shape, not a complete factory. Reuse a compatible fixture with isolated provider database and a test-only authentication scheme. Do not hard-code library IDs in a different domain.", (n,e) => $$"""
// Tests reference the host + compatible Microsoft.AspNetCore.Mvc.Testing.
// Host exposes public partial class Program;
// WebApplicationFactory configures isolated DB, migrations/seed, test auth.
// Keep the same factory for POST then later GET; fresh factory per isolated case.

[Fact]
public async Task Created_resource_is_visible_in_a_later_request()
{
    using var factory = new YourTestFactory(); // implement this fixture
    using var client = factory.CreateClient();
    // Authenticate according to YOUR fixture if POST is protected.
    var created = await client.PostAsJsonAsync("/api/{{e.ToLowerInvariant()}}s",
        new { name = "Test item" }); // adapt actual request/route
    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    Assert.NotNull(created.Headers.Location);
    var later = await client.GetFromJsonAsync<{{e}}Response>(created.Headers.Location);
    Assert.NotNull(later);
    Assert.Equal("Test item", later.Name);
}
// Also cover 401 / 403 / allowed, validation, conflicts, ordered pages,
// entity-save guard, audit preservation, and real cache reuse/invalidation.
"""),
        new("13 • Build / test / Swagger commands", "Copy and replace placeholders first. These commands do not execute from the helper. Inspect project/SDK/provider and keep versions matching the starter.", (n,e) => """
# PowerShell, from repository root:
dotnet --list-sdks
dotnet restore '.\PATH\TO\YourApi.csproj'
dotnet build '.\YourSolution.sln'
dotnet test '.\YourSolution.sln'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project '.\PATH\TO\YourApi.csproj' --no-launch-profile --urls http://localhost:5090
# --no-launch-profile bypasses profile-specific settings too.
# Follow the repo's supplied profile when it provides required config.
# Open http://localhost:5090/swagger ONLY if Swagger UI is configured.
# Built-in OpenAPI document commonly: /openapi/v1.json
# Ctrl+C stops API.

# Migration, only when schema changes; replace names/paths:
dotnet ef migrations add DescribeYourChange --project '.\PATH\TO\YourApi.csproj' --startup-project '.\PATH\TO\YourApi.csproj'
dotnet ef database update --project '.\PATH\TO\YourApi.csproj' --startup-project '.\PATH\TO\YourApi.csproj'
git diff --check
git status --short
"""),
        new("14 • Generic CRUD design", "Use when the exam requires a generic controller/service. This is the design checklist; do not force multi-entity workflows into CRUD. Full worked generic classes are in the local Jainder reference repository.", (n,e) => $$"""
// Keep generic parameter order consistent across interface/service/controller.
// ICrudService<TEntity,TResponse,TRequest>:
//   ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync
// CrudService<TEntity,TResponse,TRequest>:
//   repository through UoW, mapper/projection, ValidateAsync/CanDeleteAsync hooks
// abstract CrudController<TEntity,TResponse,TRequest>:
//   HTTP actions + outcome mapping; NO resource-specific [Route]
// {{e}}sController:
//   [ApiController], exact resource [Route], inherits generic base
// Specific service:
//   own interface, exact duplicate/delete checks, required projected counts

// Program:
// builder.Services.AddScoped(typeof(ICrudService<,,>), typeof(CrudService<,,>));
// Register every required mapper and specific service against its own interface.
// Mixed public/protected actions need action-level policies, including inherited ones.
// Keep checkout/place/cancel/register workflows in specific services/controllers.
""")
    ];
}
