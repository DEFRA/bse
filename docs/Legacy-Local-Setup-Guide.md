# Running the BSE Legacy Application Locally

**Applies to:** `C:\Workspace\bsenet-v2-2025-10\BSESystem.sln`  
**Stack:** ASP.NET WebForms · VB.NET · .NET Framework 4.8 · SQL Server · IIS Express · Windows Authentication

---

## Prerequisites

Install the following if not already present:

| Software | Version / Notes |
|---|---|
| **Visual Studio 2019 or 2022** | With workloads: *ASP.NET and web development*, *Data storage and processing* (for SSDT) |
| **.NET Framework 4.8 Developer Pack** | Download from Microsoft if not bundled with VS |
| **SQL Server** (any edition) | SQL Server 2017+ or SQL Server Express (free). Use instance name `localhost` or `.\SQLEXPRESS` |
| **SQL Server Management Studio (SSMS)** | Optional but recommended for running seed scripts |
| **IIS Express** | Installed automatically with Visual Studio |

> The solution also contains `BSEIntegrationServices` (SSIS `.dtproj`). You do **not** need SQL Server Integration Services or SSIS to run the web application — that project can be skipped.

---

## Step 1 — Clone / Open the Solution

1. Open **Visual Studio**.
2. Open `C:\Workspace\bsenet-v2-2025-10\BSESystem.sln`.
3. Visual Studio may prompt to upgrade projects — click **OK / Continue** to allow the upgrade in-place (targets remain at .NET 4.8, only tooling metadata changes).

---

## Step 2 — Restore NuGet Packages

The solution has one NuGet dependency: **AjaxControlToolkit 17.1.1.0**.

1. In Visual Studio: **Tools → NuGet Package Manager → Manage NuGet Packages for Solution**.
2. Click **Restore** if packages are marked as missing, or right-click the solution in Solution Explorer → **Restore NuGet Packages**.
3. Alternatively, run from the Developer Command Prompt:
   ```
   nuget restore "C:\Workspace\bsenet-v2-2025-10\BSESystem.sln"
   ```

---

## Step 3 — Set Up the Local SQL Server Database

### 3a. Create the BSE Database

Open SSMS (or run via `sqlcmd`) and create an empty database:

```sql
USE master;
GO
CREATE DATABASE BSE;
GO
```

### 3b. Deploy the Database Schema using SSDT

The `BSEDatabase` project (`BSEDatabase\BSEDatabase.sqlproj`) contains all table and stored procedure definitions. Deploy it to your local SQL Server:

1. In Visual Studio Solution Explorer, right-click **BSEDatabase** → **Publish…**
2. Set **Target database connection** to your local server:
   - Server: `localhost` (or `.\SQLEXPRESS` for Express edition)
   - Database: `BSE`
   - Authentication: Windows Authentication
3. Click **Publish** — this creates all tables, views, stored procedures, and indexes.

> If SSDT prompts about the target server, click **Edit…** and enter your local SQL Server instance name.

### 3c. Create the SQL Login and Database User

The web application connects using a SQL login (`BSESystemUser`). Run the following in SSMS connected to your local server:

```sql
USE master;
GO

-- Create the SQL Server login
CREATE LOGIN BSESystemUser WITH PASSWORD = 'll3nEltt1l';
GO

-- Grant access to the BSE database
USE BSE;
GO
CREATE USER BSESystemUser FOR LOGIN BSESystemUser;
GO

-- Grant the minimum permissions needed
ALTER ROLE db_datareader ADD MEMBER BSESystemUser;
ALTER ROLE db_datawriter ADD MEMBER BSESystemUser;
-- Stored procedures need EXECUTE permission
GRANT EXECUTE TO BSESystemUser;
GO
```

> You can change the password — just make sure it matches the `DBConnectionString` in Web.config (Step 4).

### 3d. Seed the `luUserGroup` Table

The group lookup table must be populated. Run this in SSMS:

```sql
USE BSE;
GO
INSERT INTO [dbo].[luUserGroup] ([ID], [Name]) VALUES
	(1, 'VLA Maintenance'),
	(2, 'DEFRA Data Entry'),
	(3, 'DEFRA Viewer'),
	(4, 'DEFRA Maintenance'),
	(5, 'VLA Data Entry'),
	(6, 'DEFRA AI Wales Scotland'),
	(7, 'DEFRA AHO User');
GO
```

> The group **ID** and **Name** values must exactly match what the code checks in `Session(SessionVars.SV_HeaderGroupName)`. The names above are taken directly from the page code-behind files.

### 3e. Add Your Windows Account to the `[User]` Table

The application looks up your Windows username (after stripping the domain prefix) in the `[User]` table. If it is not found, you are redirected to `unauthorized.htm`.

Find your Windows login name — it is the part **after** the backslash:

```powershell
# Run this in PowerShell to find your username
$env:USERNAME
```

Then insert your account with the desired group (use `1` for VLA Maintenance = full access during development):

```sql
USE BSE;
GO
INSERT INTO [dbo].[User] ([NTLogin], [Name], [Email], [UserGroup], [IsActive])
VALUES (
	'YOUR_WINDOWS_USERNAME',   -- e.g. 'john.smith'  (no domain prefix)
	'Your Full Name',
	'your.email@example.com',
	1,                         -- 1 = VLA Maintenance (full access)
	1                          -- IsActive = true
);
GO
```

> **Group IDs** from Step 3d:  
> `1` = VLA Maintenance (full access) · `2` = DEFRA Data Entry · `3` = DEFRA Viewer · `4` = DEFRA Maintenance · `5` = VLA Data Entry

---

## Step 4 — Update `Web.config`

Open `BSESystem\Web.config` and update the `<appSettings>` section.

### 4a. Database Connection String (required)

Find this line:
```xml
<add key="DBConnectionString" value="Data Source=VLA88;Initial Catalog=BSE;User Id=BSESystemUser;Password=ll3nEltt1l;" />
```

Change `Data Source=VLA88` to your local SQL Server instance:

| Your SQL Server instance | New `Data Source` value |
|---|---|
| Default instance on local machine | `localhost` or `.` |
| Named instance (e.g. Express) | `localhost\SQLEXPRESS` or `.\SQLEXPRESS` |

**Example after change:**
```xml
<add key="DBConnectionString" value="Data Source=localhost\SQLEXPRESS;Initial Catalog=BSE;User Id=BSESystemUser;Password=ll3nEltt1l;" />
```

### 4b. SMTP / ADNS Settings (optional — only needed if testing ADNS export)

These settings point to government mail servers that are not accessible locally. They can be left as-is if you are not testing the ADNS Export feature. If you are, change `SMTPHost` to a local mail relay or MailHog:

```xml
<add key="SMTPHost" value="localhost" />
<add key="SMTPPort" value="25" />
```

### 4c. System Version Label (cosmetic — optional)

```xml
<add key="SystemVersion" value="LOCAL DEV" />
```

---

## Step 5 — Enable Windows Authentication in IIS Express

The application uses **Windows Authentication** (`<authentication mode="Windows"/>` in Web.config). IIS Express disables Windows Auth by default and must be manually configured.

### 5a. Locate the IIS Express `applicationhost.config`

The per-solution IIS Express config is stored at:
```
C:\Workspace\bsenet-v2-2025-10\.vs\BSESystem\config\applicationhost.config
```
If the `.vs` folder does not exist yet, **build and run the project once** (it will fail to authenticate, but the file will be created), then stop and apply the changes below.

### 5b. Enable Windows Authentication and Disable Anonymous Authentication

Open `applicationhost.config` in a text editor and find the `<site>` entry for **BSESystem** (search for `BSESystem`). Under its `<location path="BSESystem">` section, update the authentication settings:

```xml
<location path="BSESystem">
	<system.webServer>
		<security>
			<authentication>
				<anonymousAuthentication enabled="false" />
				<windowsAuthentication enabled="true" />
			</authentication>
		</security>
	</system.webServer>
</location>
```

> If the `<location>` block does not exist yet, find the global `<authentication>` section and ensure `windowsAuthentication enabled="true"` is set there.

### 5c. Alternative — Use the Global IIS Express Config

If a solution-level config was not created, edit the **global** IIS Express config at:
```
C:\Users\<YourUsername>\Documents\IISExpress\config\applicationhost.config
```
Find the `<windowsAuthentication>` element and set:
```xml
<windowsAuthentication enabled="true">
```
And find `<anonymousAuthentication>` and set:
```xml
<anonymousAuthentication enabled="false" />
```

---

## Step 6 — Build the Solution

1. In Visual Studio: **Build → Build Solution** (`Ctrl+Shift+B`).
2. Ensure all four projects build successfully:
   - `DataAccessLib` (libDataAccess)
   - `BSELib`
   - `BSESystem`
   - `BSEDatabase` (schema project — may show warnings, not errors)

> If you see `Could not find file '...\AjaxControlToolkit.dll'` — re-run NuGet restore (Step 2).

---

## Step 7 — Run the Application

1. In Solution Explorer, right-click **BSESystem** → **Set as Startup Project**.
2. Press **F5** (Debug) or **Ctrl+F5** (Run without debugging).
3. IIS Express will start and open a browser at `http://localhost:4208/`.
4. The application will redirect you to `Home.aspx`.
5. `VLAHeader.ascx` reads your current Windows login (`HttpContext.Current.User.Identity.Name`), strips the domain prefix, and looks it up in the `[User]` table.
6. If found → you are logged in and see the home page with menu items for your group.
7. If not found → you are redirected to `unauthorized.htm` (go back and re-check Step 3e).

---

## Step 8 — Verify the Setup

Open a browser and navigate to:

| URL | Expected result |
|---|---|
| `http://localhost:4208/` | Redirects to `Home.aspx` |
| `http://localhost:4208/Home.aspx` | Home page loads; header shows your name and group |
| `http://localhost:4208/SearchMenu.aspx` | Search menu loads |
| `http://localhost:4208/unauthorized.htm` | Plain HTML "unauthorized" page (if auth fails) |

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| Browser shows `401 Unauthorized` | IIS Express anonymous auth is still enabled, or Windows auth is not set | Redo Step 5; ensure `anonymousAuthentication enabled="false"` |
| Page redirects to `unauthorized.htm` | Your Windows username is not in `[User]` table | Run Step 3e with the exact value of `$env:USERNAME` |
| `SqlException: Cannot open database "BSE"` | Connection string still points to `VLA88` | Update `DBConnectionString` in Web.config (Step 4a) |
| `Login failed for user 'BSESystemUser'` | SQL login not created or wrong password | Re-run Step 3c; verify password matches Web.config |
| `AjaxControlToolkit.dll not found` | NuGet packages not restored | Run **Restore NuGet Packages** (Step 2) |
| Home page loads but all menu items are hidden | Your `[User].UserGroup` is `3` (DEFRA Viewer) | Update `[User].UserGroup = 1` for VLA Maintenance access |
| `SessionError.aspx` appears when navigating | Session expired (20 min timeout) or skipped a required page flow | Re-navigate from `Home.aspx`; session timeout is 20 min |
| SMTP errors during ADNS Export | Government SMTP relay not reachable locally | Set `SMTPHost` to `localhost` and install a local mail trap (Step 4b) |

---

## Summary of Files Changed

| File | What to Change |
|---|---|
| `BSESystem\Web.config` | `DBConnectionString` → point to `localhost` or `.\SQLEXPRESS` |
| `.vs\BSESystem\config\applicationhost.config` | Enable `windowsAuthentication`, disable `anonymousAuthentication` |
| SQL Server `[BSE].[dbo].[User]` table | Insert your Windows username with appropriate `UserGroup` |
