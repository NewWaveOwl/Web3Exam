# Repository review report

Folder: `K:\CLASSES 26 FALL\WEB_3\Finaltest\Example`

20 source/project files • 1 projects • 32 type declarations • 32 route/verb attributes • 17 review prompts
No repository files were changed. Source patterns need human review.

Heuristic source inspection only. A finding is not a proven bug; absence is not proof of compliance. Comments are masked, but full C# semantic/control-flow analysis is not performed. No repository code was executed.

## Findings

| Level | Review | Source | Why / next step |
|---|---|---|---|
| Review | Controller mentions DbContext | LibraryApi\Controllers\BooksController.cs:11 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly (3 matches) | LibraryApi\Controllers\BooksController.cs:102 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Check | Paging: verify stable ordering | LibraryApi\Controllers\BooksController.cs:37 | Confirm OrderBy plus a unique tie-breaker occurs on the same query before Skip/Take. Verify bounds from the actual exam sheet. |
| Review | Controller mentions DbContext | LibraryApi\Controllers\CategoriesController.cs:11 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly (3 matches) | LibraryApi\Controllers\CategoriesController.cs:49 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Review | Controller mentions DbContext | LibraryApi\Controllers\CopiesController.cs:11 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly (2 matches) | LibraryApi\Controllers\CopiesController.cs:49 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Review | Controller mentions DbContext | LibraryApi\Controllers\FinesController.cs:10 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly | LibraryApi\Controllers\FinesController.cs:71 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Review | Materialization followed by LINQ nearby (2 matches) | LibraryApi\Controllers\FinesController.cs:18 | Check whether the same result is filtered/grouped/summed in memory. Nearby methods or change-tracker lists can be legitimate; this heuristic has no data-flow analysis. |
| Review | Controller mentions DbContext | LibraryApi\Controllers\LoansController.cs:11 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly (2 matches) | LibraryApi\Controllers\LoansController.cs:114 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Review | Materialization followed by LINQ nearby (2 matches) | LibraryApi\Controllers\LoansController.cs:30 | Check whether the same result is filtered/grouped/summed in memory. Nearby methods or change-tracker lists can be legitimate; this heuristic has no data-flow analysis. |
| Review | Controller mentions DbContext | LibraryApi\Controllers\PatronsController.cs:11 | If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage. |
| Review | Controller commits directly (2 matches) | LibraryApi\Controllers\PatronsController.cs:73 | For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior. |
| Review | Materialization followed by LINQ nearby | LibraryApi\Controllers\PatronsController.cs:31 | Check whether the same result is filtered/grouped/summed in memory. Nearby methods or change-tracker lists can be legitimate; this heuristic has no data-flow analysis. |
| Review | EnsureCreated used | LibraryApi\Program.cs:27 | If migrations are required, use committed migrations/Migrate. Do not reset a real database blindly. |

## Inventory

| Kind | Declaration | Source |
|---|---|---|
| Type | CategoryResponse | LibraryApi\Contracts\CatalogContracts.cs:3 |
| Type | UpsertCategoryRequest | LibraryApi\Contracts\CatalogContracts.cs:5 |
| Type | BookListItemResponse | LibraryApi\Contracts\CatalogContracts.cs:10 |
| Type | BookGroupResponse | LibraryApi\Contracts\CatalogContracts.cs:19 |
| Type | BookRequest | LibraryApi\Contracts\CatalogContracts.cs:21 |
| Type | CopyResponse | LibraryApi\Contracts\CatalogContracts.cs:29 |
| Type | CreateCopyRequest | LibraryApi\Contracts\CatalogContracts.cs:31 |
| Type | PatronResponse | LibraryApi\Contracts\CirculationContracts.cs:3 |
| Type | UpsertPatronRequest | LibraryApi\Contracts\CirculationContracts.cs:12 |
| Type | LoanResponse | LibraryApi\Contracts\CirculationContracts.cs:20 |
| Type | CheckoutRequest | LibraryApi\Contracts\CirculationContracts.cs:33 |
| Type | ReturnLoanResponse | LibraryApi\Contracts\CirculationContracts.cs:39 |
| Type | FineResponse | LibraryApi\Contracts\CirculationContracts.cs:41 |
| Type | FineTotalResponse | LibraryApi\Contracts\CirculationContracts.cs:50 |
| Type | BooksController | LibraryApi\Controllers\BooksController.cs:11 |
| Route | [Route("api/books")] | LibraryApi\Controllers\BooksController.cs:10 |
| Route | [HttpGet] | LibraryApi\Controllers\BooksController.cs:13 |
| Route | [HttpGet("by-category")] | LibraryApi\Controllers\BooksController.cs:52 |
| Route | [HttpGet("{id:int}")] | LibraryApi\Controllers\BooksController.cs:66 |
| Route | [HttpPost] | LibraryApi\Controllers\BooksController.cs:88 |
| Route | [HttpPut("{id:int}")] | LibraryApi\Controllers\BooksController.cs:119 |
| Route | [HttpDelete("{id:int}")] | LibraryApi\Controllers\BooksController.cs:137 |
| Type | CategoriesController | LibraryApi\Controllers\CategoriesController.cs:11 |
| Route | [Route("api/categories")] | LibraryApi\Controllers\CategoriesController.cs:10 |
| Route | [HttpGet] | LibraryApi\Controllers\CategoriesController.cs:13 |
| Route | [HttpGet("{id:int}")] | LibraryApi\Controllers\CategoriesController.cs:24 |
| Route | [HttpPost] | LibraryApi\Controllers\CategoriesController.cs:37 |
| Route | [HttpPut("{id:int}")] | LibraryApi\Controllers\CategoriesController.cs:57 |
| Route | [HttpDelete("{id:int}")] | LibraryApi\Controllers\CategoriesController.cs:79 |
| Type | CopiesController | LibraryApi\Controllers\CopiesController.cs:11 |
| Route | [Route("api/books/{bookId:int}/copies")] | LibraryApi\Controllers\CopiesController.cs:10 |
| Route | [HttpGet] | LibraryApi\Controllers\CopiesController.cs:13 |
| Route | [HttpPost] | LibraryApi\Controllers\CopiesController.cs:28 |
| Route | [HttpDelete("{copyId:int}")] | LibraryApi\Controllers\CopiesController.cs:55 |
| Type | FinesController | LibraryApi\Controllers\FinesController.cs:10 |
| Route | [Route("api/fines")] | LibraryApi\Controllers\FinesController.cs:9 |
| Route | [HttpGet] | LibraryApi\Controllers\FinesController.cs:12 |
| Route | [HttpGet("totals")] | LibraryApi\Controllers\FinesController.cs:39 |
| Route | [HttpPost("{id:int}/pay")] | LibraryApi\Controllers\FinesController.cs:60 |
| Type | LoansController | LibraryApi\Controllers\LoansController.cs:11 |
| Route | [Route("api/loans")] | LibraryApi\Controllers\LoansController.cs:10 |
| Route | [HttpGet] | LibraryApi\Controllers\LoansController.cs:13 |
| Route | [HttpGet("overdue-summary")] | LibraryApi\Controllers\LoansController.cs:35 |
| Route | [HttpGet("{id:int}")] | LibraryApi\Controllers\LoansController.cs:68 |
| Route | [HttpPost] | LibraryApi\Controllers\LoansController.cs:78 |
| Route | [HttpPost("{id:int}/return")] | LibraryApi\Controllers\LoansController.cs:120 |
| Type | PatronsController | LibraryApi\Controllers\PatronsController.cs:11 |
| Route | [Route("api/patrons")] | LibraryApi\Controllers\PatronsController.cs:10 |
| Route | [HttpGet] | LibraryApi\Controllers\PatronsController.cs:13 |
| Route | [HttpGet("{id:int}")] | LibraryApi\Controllers\PatronsController.cs:36 |
| Route | [HttpPost] | LibraryApi\Controllers\PatronsController.cs:59 |
| Route | [HttpPut("{id:int}")] | LibraryApi\Controllers\PatronsController.cs:86 |
| Type | LibraryDbContext | LibraryApi\Data\LibraryDbContext.cs:6 |
| Type | LibrarySeed | LibraryApi\Data\LibrarySeed.cs:6 |
| Project | LibraryApi \| net10.0 \| Microsoft.NET.Sdk.Web | LibraryApi\LibraryApi.csproj:1 |
| Package | Microsoft.AspNetCore.OpenApi 10.0.12 | LibraryApi\LibraryApi.csproj:1 |
| Package | Microsoft.EntityFrameworkCore.Sqlite 10.0.12 | LibraryApi\LibraryApi.csproj:1 |
| Type | Book | LibraryApi\Models\Book.cs:3 |
| Type | Category | LibraryApi\Models\Category.cs:3 |
| Type | Copy | LibraryApi\Models\Copy.cs:3 |
| Type | CopyStatus | LibraryApi\Models\CopyStatus.cs:3 |
| Type | Fine | LibraryApi\Models\Fine.cs:3 |
| Type | Loan | LibraryApi\Models\Loan.cs:3 |
| Type | Patron | LibraryApi\Models\Patron.cs:3 |
| Type | Program | LibraryApi\Program.cs:33 |
| Type | RequestRules | LibraryApi\RequestRules.cs:5 |
| Type | ApiProblems | LibraryApi\RequestRules.cs:22 |
