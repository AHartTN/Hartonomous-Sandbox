# 01 - Solution and Project Setup

This guide provides the explicit CLI commands to create the solution file and all necessary projects for the Hartonomous rewrite. Execute these commands from the root directory where you want the new solution to be created (e.g., a new `Hartonomous-Rewrite` folder).

## 1. Create the Solution

First, create the solution file that will contain all the projects.

```bash
dotnet new sln -n Hartonomous
```

## 2. Create Source Code Projects

These are the primary application projects, organized into a `src` directory.

```bash
# Create the main source directory
mkdir src

# --- Core Project (Interfaces, DTOs, Domain Models) ---
dotnet new classlib -n Hartonomous.Core -o src/Hartonomous.Core

# --- Database Project (The Engine: T-SQL, Stored Procs, etc.) ---
# Note: `dotnet new sqlproj` requires the SQL Server Data Tools build tools.
# If this command fails, create this project manually from Visual Studio.
dotnet new sqlproj -n Hartonomous.Database -o src/Hartonomous.Database

# --- SQL CLR Project (High-performance C# for the Engine) ---
# This will be a standard classlib, but its csproj will be configured for CLR compilation.
dotnet new classlib -n Hartonomous.SqlClr -o src/Hartonomous.SqlClr

# --- Infrastructure Project (Data Access Layer) ---
dotnet new classlib -n Hartonomous.Infrastructure -o src/Hartonomous.Infrastructure

# --- Ingestion Worker Project (Background Service) ---
dotnet new worker -n Hartonomous.Workers.Ingestion -o src/Hartonomous.Workers.Ingestion

# --- API Project (Web API) ---
dotnet new webapi -n Hartonomous.Api -o src/Hartonomous.Api
```

## 3. Create Test Projects

These projects will contain all unit, integration, and end-to-end tests, organized into a `tests` directory.

```bash
# Create the main tests directory
mkdir tests

# --- Database Test Project ---
dotnet new xunit -n Hartonomous.Database.Tests -o tests/Hartonomous.Database.Tests

# --- Core Test Project ---
dotnet new xunit -n Hartonomous.Core.Tests -o tests/Hartonomous.Core.Tests

# --- Infrastructure Test Project ---
dotnet new xunit -n Hartonomous.Infrastructure.Tests -o tests/Hartonomous.Infrastructure.Tests

# --- Integration Test Project ---
dotnet new xunit -n Hartonomous.Integration.Tests -o tests/Hartonomous.Integration.Tests
```

## 4. Add Projects to the Solution

Finally, add all the newly created projects to the solution file.

```bash
# Add Source Projects
dotnet sln Hartonomous.sln add src/Hartonomous.Core/Hartonomous.Core.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.Database/Hartonomous.Database.sqlproj
dotnet sln Hartonomous.sln add src/Hartonomous.SqlClr/Hartonomous.SqlClr.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.Infrastructure/Hartonomous.Infrastructure.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.Workers.Ingestion/Hartonomous.Workers.Ingestion.csproj
dotnet sln Hartonomous.sln add src/Hartonomous.Api/Hartonomous.Api.csproj

# Add Test Projects
dotnet sln Hartonomous.sln add tests/Hartonomous.Database.Tests/Hartonomous.Database.Tests.csproj
dotnet sln Hartonomous.sln add tests/Hartonomous.Core.Tests/Hartonomous.Core.Tests.csproj
dotnet sln Hartonomous.sln add tests/Hartonomous.Infrastructure.Tests/Hartonomous.Infrastructure.Tests.csproj
dotnet sln Hartonomous.sln add tests/Hartonomous.Integration.Tests/Hartonomous.Integration.Tests.csproj
```

## 5. Centralize Dependency Management

To ensure consistency and prevent version conflicts across the solution's many projects, it is a modern best practice to centralize NuGet package management. This is achieved by adding two files to the root of the repository.

### `Directory.Packages.props`
This file is used to define the version for each NuGet package used in the solution in a single place. This is known as Central Package Management (CPM).

**Example `Directory.Packages.props`:**
```xml
<!-- In Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Define all package versions here -->
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageVersion Include="Serilog" Version="3.1.1" />
    <PackageVersion Include="xunit" Version="2.5.3" />
  </ItemGroup>
</Project>
```

### `Directory.Build.props`
This file is used to define common properties for all projects, such as `TargetFramework`, `LangVersion`, and company information. It automatically applies these settings to every project in the solution.

**Example `Directory.Build.props`:**
```xml
<!-- In Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <Company>Hartonomous</Company>
  </PropertyGroup>
</Project>
```

### How to Use in a Project File
Once these files are in place, your individual `.csproj` files become much simpler. You no longer specify the `Version` for each package.

**Example `.csproj` file:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- No TargetFramework needed, it's inherited -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Version is inherited from Directory.Packages.props -->
    <PackageReference Include="Newtonsoft.Json" />
    <PackageReference Include="Serilog" />
  </ItemGroup>

</Project>
```

Adopting this approach is a critical step in maintaining a large, multi-project solution and preventing the "dependency hell" that can arise from version mismatches.
