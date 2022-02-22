# Easily perform **temporal queries** with **EF Core**

Available on NuGet: ![latest version](https://img.shields.io/nuget/v/EfCoreTemporalTable)

| :exclamation::exclamation::exclamation: IMPORTANT - EF Core 6 users :exclamation::exclamation::exclamation: |
| ----------- |
| If your project is targeting EF Core 6+ you may not need this extension, please read [the Microsoft blog post announcement](https://devblogs.microsoft.com/dotnet/prime-your-flux-capacitor-sql-server-temporal-tables-in-ef-core-6-0/) |
| If you want help how to use it with EF Core 6, you can find [a sample](https://github.com/glautrou/EfCoreTemporalTablePart3/blob/master/README.md) and [associated tutorial](https://blog.webnet.fr/les-tables-temporelles-partie-3-utilisation-avec-entity-framework-core-6/) |

## Give a Star! :star:

If you like or are using this NuGet extension, please give it a star. Thanks!

## Table of Contents

[1. Overview](#1-overview)

[2. Dependencies](#2-dependencies)

[3. Installation](#3-installation)

[4. Usage](#4-usage)

### 1. Overview
There is no way querying temporal tables with Entity Framework Core except writing boring SQL code and executing raw queries.
This package allows you to easily query your historic data and mix it with Entity Framework Core in an intuitive way.

All temporal criterias are supported and it works with all databases supported by EF Core and all operating systems supported by .NET Core (Windows/MacOS/Linux).

### 2. Dependencies
- NETStandard 2.0
- Microsoft.EntityFrameworkCore >= 5.0.2
- Microsoft.EntityFrameworkCore.Relational >= 5.0.2

(EF Core [2.x](https://github.com/glautrou/EfCoreTemporalTable/tree/gilles/ef-core-2-support) or [3.x](https://github.com/glautrou/EfCoreTemporalTable/tree/gilles/ef-core-3-support) support)

### 3. Installation
There are two ways to install the package:
- via Visual Studio : Right Click on project > Manage NuGet packages > Search for "EfCoreTemporalTable" > Install
- via command line: `dotnet add package EfCoreTemporalTable`

### 4. Usage
You can use it with your existing EF Core DbContext/DbSet.
On top of your file, add `using EfCoreTemporalTable;`

On your DbSet properties you now get the following extension methods:
- AsTemporalAll()
- AsTemporalAsOf(date)
- AsTemporalFrom(startDate, endDate)
- AsTemporalBetween(startDate, endDate)
- AsTemporalContained(startDate, endDate)

Those methods return an `IQueryable<T>`, meaning the execution is deferred and you can mix it with your usual EF Core and cutom methods.

For example, if you want to get all employees named "Lautrou" at the time of yesterday, and their company at that time but with up-to-date information:

```csharp
var result = myDbContext.Employees
    .AsTemporalOf(DateTime.UtcNow.AddDays(-1))
    .Include(i=> i.Company)
    .FirstOrDefault(i => i.Name == "Lautrou");
```  

The generated SQL query will be:

```sql
exec sp_executesql N'SELECT TOP(1) [e].[Id], [e].[CompanyId], [e].[Lastname], [e].[Firstname], [c].[Id], [c].[Name]
    FROM (
        SELECT * FROM [dbo].[Employee] FOR SYSTEM_TIME AS OF @p0
    ) AS [e]
    INNER JOIN [Company] AS [c] ON [e].[CompanyId] = [c].[Id]
    WHERE [e].[Lastname] = N''Lautrou''',N'@p0 datetime2(7)',@p0='2019-11-27 17:26:10.1256588'
```   

As you can see the SQL query is clean and the temporal parameter is a `DbParameter` (and not inlined).

You can of course join temporal tables, and write your C# in the way you want:

```csharp
var employees = from employee in db.Employees.AsTemporalOf(date)
                join company in db.Entreprise.AsTemporalOf(date) on employee.CompanyId equals company.Id
                select new
                {
                    Employee = employee,
                    Company = company
                };
```
