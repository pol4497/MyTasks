# MyTasks

A task management application built with ASP.NET Core, Entity Framework Core, and SQLite.

Prerequisites: 
1. Visual Studio 2022
2. .NET SDK 9.0.304 (x64) from Visual Studio

Getting Started:
1. Clone the repository:

2. Open the solution in Visual Studio 2022.

3. Restore NuGet packages (if not restored automatically):

   Command Line Interface: dotnet restore

5. Apply the Entity Framework Core migrations to create the SQLite database:

   Command Line Interface: dotnet ef database update

   This will create the MyTasks.db file automatically.

5. Run the application.
6. If you want to run the CRUD operations with OpenAPI, create a {fileName}.http file in project's directory to run the requests. 


Technologies:
1. ASP.NET Web API
2. Entity Framework Core
3. SQLite
