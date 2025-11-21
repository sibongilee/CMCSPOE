# CMCSPOE

Short description
A lightweight ASP.NET Core Razor/Views web application for CMCSPOE. This repo contains controllers, views, models and a simple data access helper for SQL Server LocalDB.

Key tech
- .NET 8 (C# 12)
- ASP.NET Core (Razor Views / MVC-style controllers)
- SQL Server LocalDB (connection string currently targeted to `(localdb)\MSSQLLocalDB`)

Prerequisites
- .NET 8 SDK
- Visual Studio 2022 (recommended) or VS Code
- Local SQL Server LocalDB (for development) or another SQL Server instance

Quick start

1. Clone
   git clone https://github.com/sibongilee/CMCSPOE.git

2. Open the solution
   - In Visual Studio: use __File > Open > Project/Solution__ and open the `.sln` file.
   - Or via CLI: `dotnet restore`

3. Configure the database connection
   - Add (or edit) your connection string in `appsettings.json`:
     ```json
     {
       "ConnectionStrings": {
         "CMCSPOE": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CMCSPOE;Integrated Security=True;"
       }
     }
     ```
   - Or set environment variable: `ConnectionStrings__CMCSPOE`.

4. (Optional) Install recommended DB client
   - In Visual Studio: __Tools > NuGet Package Manager > Manage NuGet Packages for Solution__ and install `Microsoft.Data.SqlClient`.
   - Or CLI: `dotnet add package Microsoft.Data.SqlClient`

5. Register services (if using the improved DI-friendly `DatabaseConnection`)
   - Add the registration to `Program.cs` where other services are configured: