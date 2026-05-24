# Build and Setup Guide for Kingdom Preparatory School Management System

This guide addresses common issues encountered when setting up, building, and running the modernized Kingdom Preparatory School Management System.

## 🛠 Prerequisites

1.  **Visual Studio 2022** (recommended) or 2019 with the following workloads:
    *   .NET Desktop Development
2.  **.NET Framework 4.7.2 SDK** (included with Visual Studio)
3.  **Microsoft SQL Server LocalDB** (usually installed with Visual Studio)
4.  **Microsoft OLE DB Driver for SQL Server (MSOLEDBSQL)**
    *   [Download from Microsoft](https://learn.microsoft.com/en-us/sql/connect/oledb/download-oledb-driver)

## 🏗 Building the Project

### 1. Missing Crystal Reports References (MSB3245)
If you see warnings about `CrystalDecisions.CrystalReports.Engine` or `FlashControlV71` not being found:
1.  Download and install the **SAP Crystal Reports runtime engine for .NET Framework** (Version 13.0.35 or newer).
    *   [Crystal Reports Downloads](https://www.sap.com/products/technology-platform/crystal-reports.html)
2.  If you do not need reports immediately, you can still build and run other parts of the application, but report-related forms will fail at runtime.

### 2. Fixing the MSB3822 Resource Error
If you encounter the error `MSB3822: Non-string resources require the System.Resources.Extensions assembly at runtime`, follow these steps:

1.  Open the solution in **Visual Studio**.
2.  Right-click on the project in Solution Explorer and select **Manage NuGet Packages**.
3.  Ensure **`System.Resources.Extensions`** (version 6.0.0) is installed.
4.  **Crucial:** If you are using the command line to build, run:
    `dotnet restore`
    followed by:
    `MSBuild kingdom_Preparatory_School_Management_System.sln`

### 3. RuntimeIdentifier Error ('win')
If you see an error about 'win' not being listed as a "RuntimeIdentifier":
*   I have updated the `.csproj` file to include `win`.
*   If Visual Studio still complains, try **Clean Solution** and then **Rebuild Solution**.

### 4. Dependency Resolution
If third-party controls (Bunifu, Guna UI) are missing:
*   Ensure the DLLs in the `lib/` folder are referenced correctly.
*   If you see "type not found" errors for `Guna.UI2` or `Bunifu`, you may need to re-add the references from the `lib/` folder manually.

## 🗄 Database Setup

The application is configured to use **Microsoft SQL Server LocalDB** with a database named `Neat_Academy`.

### 1. Attaching the Database
1.  Locate the database file at `C:\Users\DELL\Downloads\database\database\Neat_Academy.mdf`.
2.  Open **SQL Server Management Studio (SSMS)**.
3.  Connect to `(localdb)\MSSQLLocalDB`.
4.  Right-click **Databases** > **Attach...**
5.  Click **Add** and select the `Neat_Academy.mdf` file.
6.  Ensure the database name in SSMS matches `Neat_Academy`.

### 2. Connection String Configuration
If your database is in a different location or you are using a different SQL instance, update the connection string in `App.config`:

```xml
<connectionStrings>
    <add name="kingdom_Preparatory_School_Management_System.Properties.Settings.ConnectionString"
         connectionString="Provider=MSOLEDBSQL;Data Source=(localdb)\MSSQLLocalDB;Integrated Security=SSPI;Initial Catalog=Neat_Academy;Encrypt=False"
         providerName="System.Data.OleDb" />
</connectionStrings>
```

## 🔐 Initial Login

*   **Default Administrator Account:**
    *   **Username:** `admin` (if already registered)
*   **Creating a New Account:**
    1.  Run the application.
    2.  On the login screen, click **"Create a new account"**.
    3.  Follow the registration process. The system will automatically hash your password and set up the necessary tables.

## 📝 Recent Improvements

*   **Robust Authentication:** Password hashing is now implemented using PBKDF2 with automatic upgrade from plain text on login.
*   **Database Resilience:** The system now automatically creates the `Users` table and required columns if they are missing.
*   **Modern UI:** All major forms (Admissions, Employees, Fees, Attendance, Exams, Dashboard) have been refactored to use a consistent, modern theme and `async/await` for better performance.

---
**Kingdom Preparatory School Management System** | Modernization v2.0
