# Progress Web Platform — Phase 0 (Foundation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A deployable Blazor Server skeleton: role-based login (reusing the desktop's existing `Users` credentials and PBKDF2 hashes) over the existing `Neat_Academy` database, with a role-gated app shell — no module features and no writes.

**Architecture:** New solution under `web/` with `KingdomPrep.Web` (Blazor Server), `KingdomPrep.Web.Core` (auth logic, ported PBKDF2 + role parsing, DTOs), `KingdomPrep.Web.Data` (EF Core `AppDbContext` mapped to the legacy schema), and `KingdomPrep.Web.Tests` (xUnit). Dependency direction `Web → Core → Data`. Cookie auth; Accountant denied; Parent gets a placeholder.

**Tech Stack:** .NET 10 (installed LTS), ASP.NET Core Blazor Server (Interactive Server), EF Core + `Microsoft.EntityFrameworkCore.SqlServer`, xUnit. Dev DB = `(localdb)\MSSQLLocalDB / Neat_Academy`.

---

## Ground rules

- All web work lives under `web/` at the repo root. **Do not touch the desktop project** (`kingdom_Preparatory_School_Management_System.csproj` or its sources).
- Target framework `net10.0` for every project (the spec said .NET 8 LTS; only the .NET 10 SDK — also LTS — is installed, so we use net10.0).
- Run all `dotnet` commands from `web/` unless stated. Project root paths below are relative to `web/`.
- Commit after each task. Use explicit `git add <paths>`; never `git add -A`.
- Verification per task: `dotnet build` (0 errors) and, where tests exist, `dotnet test` (all pass).

---

## Task 1: Scaffold the web solution

**Files:** Create `web/KingdomPrep.Web.sln` and four projects.

- [ ] **Step 1: Create solution + projects**

Run from the repo root:
```bash
mkdir -p web && cd web
dotnet new sln -n KingdomPrep.Web
dotnet new blazor -n KingdomPrep.Web --interactivity Server --framework net10.0
dotnet new classlib -n KingdomPrep.Web.Core --framework net10.0
dotnet new classlib -n KingdomPrep.Web.Data --framework net10.0
dotnet new xunit -n KingdomPrep.Web.Tests --framework net10.0
dotnet sln add KingdomPrep.Web/KingdomPrep.Web.csproj KingdomPrep.Web.Core/KingdomPrep.Web.Core.csproj KingdomPrep.Web.Data/KingdomPrep.Web.Data.csproj KingdomPrep.Web.Tests/KingdomPrep.Web.Tests.csproj
```

- [ ] **Step 2: Wire references + EF Core package**
```bash
cd web
dotnet add KingdomPrep.Web.Data reference KingdomPrep.Web.Core
dotnet add KingdomPrep.Web reference KingdomPrep.Web.Core KingdomPrep.Web.Data
dotnet add KingdomPrep.Web.Tests reference KingdomPrep.Web.Core KingdomPrep.Web.Data
dotnet add KingdomPrep.Web.Data package Microsoft.EntityFrameworkCore.SqlServer
dotnet add KingdomPrep.Web.Data package Microsoft.EntityFrameworkCore
```
Delete the placeholder `Class1.cs` from `KingdomPrep.Web.Core` and `KingdomPrep.Web.Data`.

- [ ] **Step 3: Build + test (baseline)**

Run: `cd web && dotnet build` → `Build succeeded. 0 Error(s)`
Run: `cd web && dotnet test` → the template's sample test passes.

- [ ] **Step 4: Add a `.gitignore` for build output** at `web/.gitignore`:
```
bin/
obj/
```

- [ ] **Step 5: Commit**
```bash
git add web/.gitignore web/KingdomPrep.Web.sln web/KingdomPrep.Web web/KingdomPrep.Web.Core web/KingdomPrep.Web.Data web/KingdomPrep.Web.Tests
git commit -m "feat(web): scaffold KingdomPrep.Web Blazor Server solution"
```

---

## Task 2: Port `UserRole` + `RoleParser` to Core (TDD)

**Files:** Create `web/KingdomPrep.Web.Core/Auth/UserRole.cs`, `RoleParser.cs`; `web/KingdomPrep.Web.Tests/RoleParserTests.cs`.

Reference: the desktop enum is `Services/AuthService.cs:16` (`Director, Administrator, Headmaster, Teacher, Accountant, Parent, Unknown`) and `ParseRole` lives in the same file — open it and port its exact string→enum logic.

- [ ] **Step 1: Write the failing test** `RoleParserTests.cs`:
```csharp
using KingdomPrep.Web.Core.Auth;
using Xunit;

public class RoleParserTests
{
    [Theory]
    [InlineData("Director", UserRole.Director)]
    [InlineData("Administrator", UserRole.Administrator)]
    [InlineData("Headmaster", UserRole.Headmaster)]
    [InlineData("Teacher", UserRole.Teacher)]
    [InlineData("Accountant", UserRole.Accountant)]
    [InlineData("Parent", UserRole.Parent)]
    [InlineData("something-unknown", UserRole.Unknown)]
    [InlineData(null, UserRole.Unknown)]
    public void Parse_MapsUserTypeStrings(string userType, UserRole expected)
        => Assert.Equal(expected, RoleParser.Parse(userType));
}
```

- [ ] **Step 2: Run test → fails to compile (types missing).** `cd web && dotnet test` → FAIL.

- [ ] **Step 3: Implement** `UserRole.cs`:
```csharp
namespace KingdomPrep.Web.Core.Auth;

public enum UserRole { Director, Administrator, Headmaster, Teacher, Accountant, Parent, Unknown }
```
`RoleParser.cs` — port the desktop `ParseRole` logic (case-insensitive match of `User_Type` to the enum; anything unrecognized or null → `Unknown`):
```csharp
using System;

namespace KingdomPrep.Web.Core.Auth;

public static class RoleParser
{
    public static UserRole Parse(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType)) return UserRole.Unknown;
        return Enum.TryParse<UserRole>(userType.Trim(), ignoreCase: true, out var r)
            ? r
            : UserRole.Unknown;
    }
}
```
> If the desktop `ParseRole` maps any non-enum aliases (e.g. "Admin" → Administrator), replicate those `switch` cases here so existing accounts map identically. Read `Services/AuthService.cs` `ParseRole` and match it exactly.

- [ ] **Step 4: Run test → PASS.** `cd web && dotnet test`

- [ ] **Step 5: Commit**
```bash
git add web/KingdomPrep.Web.Core/Auth/UserRole.cs web/KingdomPrep.Web.Core/Auth/RoleParser.cs web/KingdomPrep.Web.Tests/RoleParserTests.cs
git commit -m "feat(web): port UserRole + RoleParser to Core"
```

---

## Task 3: Port `PasswordHasher` to Core (TDD — desktop compatibility)

**Files:** Create `web/KingdomPrep.Web.Core/Auth/PasswordHasher.cs`; `web/KingdomPrep.Web.Tests/PasswordHasherTests.cs`.

Reference (desktop `Services/AuthService.cs`): format `P2${iterations}${base64Salt}${base64Hash}`, `Iterations=100000`, `SaltSize=8`, `HashSize=16`, `DeriveHash` uses `new Rfc2898DeriveBytes(password, salt, iterations)` = **PBKDF2/HMAC-SHA1**. The web must pass `HashAlgorithmName.SHA1` explicitly.

- [ ] **Step 1: Write the failing test** `PasswordHasherTests.cs`:
```csharp
using KingdomPrep.Web.Core.Auth;
using Xunit;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_RoundTrip_True()
    {
        string hash = PasswordHasher.Hash("Secret123!");
        Assert.StartsWith("P2$", hash);
        Assert.True(PasswordHasher.Verify("Secret123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_False()
    {
        string hash = PasswordHasher.Hash("Secret123!");
        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_KnownDesktopVector_True()
    {
        // PBKDF2-HMAC-SHA1, 100000 iters, salt=8 bytes, hash=16 bytes.
        // password "password", salt = 0x0102030405060708.
        const string stored = "P2$100000$AQIDBAUGBwg=$Vt0xJ3oR4q3l1m7kqg0n2A==";
        Assert.True(PasswordHasher.Verify("password", stored));
    }

    [Fact]
    public void Verify_LegacyPlaintext_FallsBack()
    {
        // Desktop pre-hash accounts stored plaintext; VerifyPassword compares directly.
        Assert.True(PasswordHasher.Verify("plainpw", "plainpw"));
        Assert.False(PasswordHasher.Verify("plainpw", "different"));
    }
}
```
> NOTE: the literal in `Verify_KnownDesktopVector_True` is illustrative. In Step 3, BEFORE writing the test as final, generate the real vector with the snippet below and paste the actual `stored` value so the test is a true known-answer test:
> ```bash
> cd web && dotnet run --project KingdomPrep.Web.Core --help 2>/dev/null # n/a; use a scratch:
> ```
> Use a scratch fsi/console to compute: `Convert.ToBase64String(new Rfc2898DeriveBytes("password", new byte[]{1,2,3,4,5,6,7,8}, 100000, HashAlgorithmName.SHA1).GetBytes(16))`, then build the `P2$100000$AQIDBAUGBwg=$<thatBase64>` string. Replace the literal with the computed value.

- [ ] **Step 2: Run test → FAIL (type missing).** `cd web && dotnet test`

- [ ] **Step 3: Implement** `PasswordHasher.cs`:
```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace KingdomPrep.Web.Core.Auth;

/// <summary>
/// Verifies/creates passwords compatible with the desktop AuthService:
/// PBKDF2/HMAC-SHA1, 100000 iterations, 8-byte salt, 16-byte hash,
/// stored as "P2$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;".
/// Non-"P2$" stored values are treated as legacy plaintext.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100000;
    private const int SaltSize = 8;
    private const int HashSize = 16;
    private const string Prefix = "P2";

    public static string Hash(string password)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
        byte[] hash = Derive(password, salt, Iterations, HashSize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
            return stored == password; // legacy plaintext

        string[] parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;
        byte[] salt, expected;
        try { salt = Convert.FromBase64String(parts[2]); expected = Convert.FromBase64String(parts[3]); }
        catch { return false; }

        byte[] actual = Derive(password, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA1);
        return pbkdf2.GetBytes(length);
    }
}
```

- [ ] **Step 4: Run test → PASS.** `cd web && dotnet test`

- [ ] **Step 5: Commit**
```bash
git add web/KingdomPrep.Web.Core/Auth/PasswordHasher.cs web/KingdomPrep.Web.Tests/PasswordHasherTests.cs
git commit -m "feat(web): port desktop-compatible PasswordHasher (PBKDF2-SHA1)"
```

---

## Task 4: EF Core data layer mapped to the legacy schema

**Files:** Create `web/KingdomPrep.Web.Data/Entities/UserEntity.cs`, `StudentEntity.cs`; `AppDbContext.cs`; `IUserRepository.cs`/`UserRepository.cs`; `IStudentStats.cs`/`StudentStatsRepository.cs`.

Schema facts (from desktop): `Users(Username, Password, User_Type, EmploymentID)`; `Students(StudentID, ClassID, …)`.

- [ ] **Step 1: Entities**

`UserEntity.cs`:
```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Users")]
public class UserEntity
{
    [Column("Username")] public string Username { get; set; } = "";
    [Column("Password")] public string Password { get; set; } = "";
    [Column("User_Type")] public string? UserType { get; set; }
    [Column("EmploymentID")] public int? EmploymentID { get; set; }
}
```
`StudentEntity.cs`:
```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace KingdomPrep.Web.Data.Entities;

[Table("Students")]
public class StudentEntity
{
    [Column("StudentID")] public string StudentID { get; set; } = "";
    [Column("ClassID")] public string? ClassID { get; set; }
}
```

- [ ] **Step 2: DbContext** `AppDbContext.cs`:
```csharp
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<StudentEntity> Students => Set<StudentEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserEntity>().HasKey(u => u.Username);   // Username is the login key
        b.Entity<StudentEntity>().HasKey(s => s.StudentID);
    }
}
```
> EF will NOT create or migrate these tables; the context only reads the existing schema. Do not run `dotnet ef migrations`.

- [ ] **Step 3: Repositories** `IUserRepository.cs`:
```csharp
using KingdomPrep.Web.Data.Entities;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public interface IUserRepository { Task<UserEntity?> FindByUsernameAsync(string username); }
```
`UserRepository.cs`:
```csharp
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<UserEntity?> FindByUsernameAsync(string username) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
}
```
`IStudentStats.cs` + `StudentStatsRepository.cs` (for a real dashboard value):
```csharp
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public interface IStudentStats { Task<int> CountAsync(); }

public class StudentStatsRepository(AppDbContext db) : IStudentStats
{
    public Task<int> CountAsync() => db.Students.CountAsync();
}
```

- [ ] **Step 4: Build** `cd web && dotnet build` → 0 errors.

- [ ] **Step 5: Commit**
```bash
git add web/KingdomPrep.Web.Data
git commit -m "feat(web): EF Core context + repositories mapped to legacy schema"
```

---

## Task 5: `AuthService` in Core (TDD)

**Files:** Create `web/KingdomPrep.Web.Core/Auth/AuthUser.cs`, `IAuthService.cs`, `AuthService.cs`; `web/KingdomPrep.Web.Tests/AuthServiceTests.cs`.

Core must not depend on EF; define a thin user-lookup abstraction in Core that Data implements (adapter).

- [ ] **Step 1: Contracts** `AuthUser.cs`:
```csharp
namespace KingdomPrep.Web.Core.Auth;

public record AuthUser(string Username, UserRole Role, int? EmploymentId);
```
`IAuthService.cs`:
```csharp
using System.Threading.Tasks;

namespace KingdomPrep.Web.Core.Auth;

public interface IAuthService
{
    /// <summary>Returns the authenticated user, or null on bad credentials.</summary>
    Task<AuthUser?> AuthenticateAsync(string username, string password);
}

// Implemented by the Data layer (adapter over UserRepository).
public interface IUserLookup
{
    Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string username);
}
```

- [ ] **Step 2: Failing test** `AuthServiceTests.cs`:
```csharp
using KingdomPrep.Web.Core.Auth;
using System.Threading.Tasks;
using Xunit;

public class AuthServiceTests
{
    private sealed class FakeLookup : IUserLookup
    {
        private readonly (string, string?, int?)? _row;
        public FakeLookup((string, string?, int?)? row) => _row = row;
        public Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string u)
            => Task.FromResult(_row);
    }

    [Fact]
    public async Task Authenticate_ValidTeacher_ReturnsUser()
    {
        string hash = PasswordHasher.Hash("pw");
        var svc = new AuthService(new FakeLookup((hash, "Teacher", 5)));
        var user = await svc.AuthenticateAsync("jane", "pw");
        Assert.NotNull(user);
        Assert.Equal(UserRole.Teacher, user!.Role);
        Assert.Equal(5, user.EmploymentId);
    }

    [Fact]
    public async Task Authenticate_BadPassword_ReturnsNull()
    {
        string hash = PasswordHasher.Hash("pw");
        var svc = new AuthService(new FakeLookup((hash, "Teacher", 5)));
        Assert.Null(await svc.AuthenticateAsync("jane", "wrong"));
    }

    [Fact]
    public async Task Authenticate_UnknownUser_ReturnsNull()
    {
        var svc = new AuthService(new FakeLookup(null));
        Assert.Null(await svc.AuthenticateAsync("ghost", "pw"));
    }
}
```

- [ ] **Step 3: Run test → FAIL.** `cd web && dotnet test`

- [ ] **Step 4: Implement** `AuthService.cs`:
```csharp
using System.Threading.Tasks;

namespace KingdomPrep.Web.Core.Auth;

public class AuthService(IUserLookup users) : IAuthService
{
    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;
        var row = await users.FindAsync(username.Trim());
        if (row is null) return null;
        if (!PasswordHasher.Verify(password, row.Value.Password)) return null;
        return new AuthUser(username.Trim(), RoleParser.Parse(row.Value.UserType), row.Value.EmploymentId);
    }
}
```

- [ ] **Step 5: Run test → PASS.** `cd web && dotnet test`

- [ ] **Step 6: Data adapter** — create `web/KingdomPrep.Web.Data/UserLookupAdapter.cs`:
```csharp
using KingdomPrep.Web.Core.Auth;
using System.Threading.Tasks;

namespace KingdomPrep.Web.Data;

public class UserLookupAdapter(IUserRepository repo) : IUserLookup
{
    public async Task<(string Password, string? UserType, int? EmploymentId)?> FindAsync(string username)
    {
        var u = await repo.FindByUsernameAsync(username);
        return u is null ? null : (u.Password, u.UserType, u.EmploymentID);
    }
}
```
Build → 0 errors.

- [ ] **Step 7: Commit**
```bash
git add web/KingdomPrep.Web.Core/Auth web/KingdomPrep.Web.Data/UserLookupAdapter.cs web/KingdomPrep.Web.Tests/AuthServiceTests.cs
git commit -m "feat(web): AuthService with desktop-compatible verification"
```

---

## Task 6: Cookie auth + login page

**Files:** Modify `web/KingdomPrep.Web/Program.cs`, `web/KingdomPrep.Web/appsettings.json`; create `web/KingdomPrep.Web/Components/Account/Login.razor` and a `LoginModel`; modify `Components/App.razor`/`Routes.razor` as needed.

- [ ] **Step 1: Connection string** in `appsettings.json` (dev → LocalDB):
```json
"ConnectionStrings": {
  "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
}
```

- [ ] **Step 2: DI + auth in `Program.cs`** — after `builder.Services.AddRazorComponents()...`:
```csharp
using KingdomPrep.Web.Core.Auth;
using KingdomPrep.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentStats, StudentStatsRepository>();
builder.Services.AddScoped<IUserLookup, UserLookupAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/denied";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
```
And in the pipeline (after `app.UseHttpsRedirection()` / before `app.MapRazorComponents`):
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 3: Login endpoint + page.** Because Blazor Server can't write the auth cookie from an interactive circuit, use a plain POST handler. Add minimal API endpoints in `Program.cs`:
```csharp
app.MapPost("/login", async (HttpContext ctx, IAuthService auth, string username, string password) =>
{
    var user = await auth.AuthenticateAsync(username, password);
    if (user is null || user.Role == UserRole.Unknown)
        return Results.Redirect("/login?error=1");
    if (user.Role == UserRole.Accountant)
        return Results.Redirect("/denied?accountant=1");

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, user.Username),
        new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
    };
    var id = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(id));
    return Results.Redirect("/");
});

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
```
(Add `using Microsoft.AspNetCore.Authentication;` for `SignInAsync`/`SignOutAsync`.)

- [ ] **Step 4: `Login.razor`** (static SSR form posting to `/login`) at `Components/Pages/Login.razor`:
```razor
@page "/login"
@inject NavigationManager Nav
<PageTitle>Sign in - Kingdom Preparatory</PageTitle>
<div class="login-card">
    <h1>Kingdom Preparatory School</h1>
    <h2>Sign in</h2>
    @if (Error) { <p class="error">Invalid username or password.</p> }
    <form method="post" action="/login">
        <input name="username" placeholder="Username" required />
        <input name="password" type="password" placeholder="Password" required />
        <button type="submit">Sign in</button>
    </form>
</div>
@code {
    [SupplyParameterFromQuery(Name = "error")] public string? ErrorFlag { get; set; }
    bool Error => ErrorFlag == "1";
}
```
> The form posts form-encoded `username`/`password`; bind them in the `/login` handler via `[FromForm]` (change the handler signature to `async (HttpContext ctx, IAuthService auth, [FromForm] string username, [FromForm] string password)` and add `using Microsoft.AspNetCore.Mvc;`). Ensure the login page renders with static SSR (no `@rendermode InteractiveServer`) so the browser does a real POST.

- [ ] **Step 5: Build + run smoke**

Run: `cd web && dotnet build` → 0 errors.
Run: `cd web/KingdomPrep.Web && dotnet run` (LocalDB must be running). Browse the printed URL → `/login` renders. (Full login tested in Task 8.)

- [ ] **Step 6: Commit**
```bash
git add web/KingdomPrep.Web/Program.cs web/KingdomPrep.Web/appsettings.json web/KingdomPrep.Web/Components/Pages/Login.razor
git commit -m "feat(web): cookie auth + login endpoint and page"
```

---

## Task 7: Role-gated shell, accountant block, landing pages

**Files:** Create `Components/Pages/Home.razor`, `Components/Pages/Denied.razor`; modify `Components/Layout/MainLayout.razor` and `NavMenu.razor`; modify `Components/Routes.razor` to require auth.

- [ ] **Step 1: Require auth globally.** In `Components/Routes.razor`, wrap routing in `AuthorizeRouteView` and redirect unauthenticated to `/login`. Replace the `<RouteView .../>` with:
```razor
<AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
    <NotAuthorized>
        @{ Nav.NavigateTo("/login", forceLoad: true); }
    </NotAuthorized>
</AuthorizeRouteView>
```
(Inject `NavigationManager Nav` in `Routes.razor`; keep `Login` and `Denied` pages reachable by giving them `@attribute [AllowAnonymous]`.)

- [ ] **Step 2: `Denied.razor`** at `Components/Pages/Denied.razor`:
```razor
@page "/denied"
@attribute [Microsoft.AspNetCore.Authorization.AllowAnonymous]
<PageTitle>Access denied</PageTitle>
@if (Accountant == "1")
{
    <h2>Accountants use the desktop app</h2>
    <p>The web portal is for academic progress. Please use the desktop application for accounts/fees.</p>
}
else { <h2>Access denied</h2> }
<a href="/login">Back to sign in</a>
@code { [SupplyParameterFromQuery(Name="accountant")] public string? Accountant { get; set; } }
```

- [ ] **Step 3: `Home.razor`** (role-based landing) at `Components/Pages/Home.razor`:
```razor
@page "/"
@inject KingdomPrep.Web.Data.IStudentStats Stats
@inject AuthenticationStateProvider AuthState
@using Microsoft.AspNetCore.Components.Authorization
<PageTitle>Dashboard</PageTitle>
<AuthorizeView>
    <Authorized>
        <h1>Welcome, @context.User.Identity?.Name</h1>
        <p>Signed in as <strong>@RoleName(context)</strong></p>
        @if (IsParent(context))
        {
            <p>Your child's progress will appear here soon.</p>
        }
        else
        {
            <div class="cards">
                <div class="card">Students: @_studentCount</div>
                <div class="card disabled">Academic progress (coming soon)</div>
                <div class="card disabled">Attendance (coming soon)</div>
                <div class="card disabled">HR &amp; Leave (coming soon)</div>
            </div>
        }
        <form method="post" action="/logout"><button type="submit">Sign out</button></form>
    </Authorized>
</AuthorizeView>
@code {
    int _studentCount;
    protected override async Task OnInitializedAsync() => _studentCount = await Stats.CountAsync();
    static string RoleName(AuthorizeViewContext c) => c.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
    static bool IsParent(AuthorizeViewContext c) => RoleName(c) == "Parent";
}
```

- [ ] **Step 4: Nav/layout** — in `NavMenu.razor`, show the school name and disabled "coming soon" links for Students/Academic/Attendance/HR. Keep it minimal; no functional nav targets yet.

- [ ] **Step 5: Build** `cd web && dotnet build` → 0 errors.

- [ ] **Step 6: Commit**
```bash
git add web/KingdomPrep.Web/Components
git commit -m "feat(web): role-gated shell, accountant block, role landing"
```

---

## Task 8: End-to-end verification + docs

**Files:** Create `web/README.md`.

- [ ] **Step 1: Full build + tests**

Run: `cd web && dotnet build` → 0 errors.
Run: `cd web && dotnet test` → all pass (RoleParser, PasswordHasher incl. known vector, AuthService).

- [ ] **Step 2: Manual smoke (LocalDB running)**

Run: `cd web/KingdomPrep.Web && dotnet run`. In a browser:
1. Visit `/` → redirected to `/login`.
2. Log in with an existing **staff** account (same username/password as the desktop) → lands on Home showing the real student count + "coming soon" cards.
3. Log in as a **Parent** account → Home shows the parent placeholder.
4. Log in as an **Accountant** account → redirected to `/denied` (accountant notice).
5. Wrong password → `/login?error=1` shows the error.
6. **Sign out** → back to `/login`.

Record results. If staff login fails with a valid desktop account, the password vector/parameters are wrong — revisit Task 3.

- [ ] **Step 3: `web/README.md`** — document: prerequisites (.NET 10 SDK, LocalDB), `dotnet run` from `web/KingdomPrep.Web`, the dev connection string, and the prod path (Azure SQL + App Service; set `ConnectionStrings:Default` as an App Service setting; migrate `Neat_Academy` via BACPAC; repoint the desktop app's connection string at cutover).

- [ ] **Step 4: Commit**
```bash
git add web/README.md
git commit -m "docs(web): Phase 0 foundation readme + run/deploy notes"
```

---

## Self-Review

- **Spec coverage:** Blazor Server + 3-project structure + tests (T1); UserRole/RoleParser (T2); PBKDF2-SHA1 PasswordHasher with known-vector test (T3); EF Core legacy-schema mapping read-only (T4); AuthService desktop-compatible (T5); cookie auth + login (T6); role policies, **accountant denied**, parent placeholder, role landing, shell (T7); security cookie flags (T6 Step 2); manual role verification + deploy/migration notes (T8). Azure SQL + EF Core decisions honored (config + README). ✓
- **Placeholder scan:** The only deferred literal is the known-answer hash vector in T3, which has an explicit generation step to replace it before finalizing — not a TODO in shipped code. RoleParser notes to match any desktop aliases by reading the source. No "add error handling"-style hand-waves. ✓
- **Type consistency:** `IUserLookup.FindAsync` tuple `(string Password, string? UserType, int? EmploymentId)` is produced by `UserLookupAdapter` and consumed by `AuthService`; `AuthUser(Username, Role, EmploymentId)` used in T5/T6; `IStudentStats.CountAsync` defined in T4, consumed in T7; DI registrations in T6 match the interfaces from T4/T5. ✓
- **Runtime note:** net10.0 chosen over the spec's net8.0 because only the .NET 10 SDK (also LTS) is installed — documented in Ground rules.
