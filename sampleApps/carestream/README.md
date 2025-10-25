# Carestream EHR System - README

## Overview

Carestream is an Electronic Medical Records (EMR) system designed for the South African Military Health Service (SAMHS). It aims to streamline patient check-ins, medical assessments, treatment, and pharmacy operations within a secure environment, leveraging a modular monolithic architecture.

This application is built using:

*   **Backend/Frontend:** .NET 9 MVC (`carestream.web`)
*   **UI Interaction:** HTMX, Alpine.js
*   **Styling:** Tailwind CSS with DaisyUI
*   **Data Access:** Dapper
*   **Authentication:** Logto (Self-hosted via Docker)
*   **Database (Logto & Application):** PostgreSQL (via Docker for local dev)
*   **Database Migrations (Application):** DbUp
*   **Containerization:** Docker & Docker Compose
*   **Testing:** xUnit, Moq, Testcontainers
*   **DevOps Tooling:** Husky (for Git hooks)

## Local Development Setup

These instructions guide you through setting up the Carestream application and its dependencies (Logto, PostgreSQL) locally using Docker Compose.

**Prerequisites:**

1.  **Git:** To clone the repository.
2.  **.NET 9 SDK:** Required to build and run the .NET projects. ([Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0))
3.  **Docker Desktop:** Required to run the application and its dependencies (Logto, PostgreSQL) in containers. Ensure Docker Desktop is running and configured to use Linux containers. ([Download Docker Desktop](https://www.docker.com/products/docker-desktop/))
4.  **Node.js & npm:** Required for development tooling (Husky Git hooks) and potentially frontend build steps (e.g., Tailwind). ([Download Node.js - includes npm](https://nodejs.org/))

**Setup Steps:**

1.  **Clone the Repository:**
    ```bash
    git clone <repository-url>
    cd carestream # Navigate into the cloned directory (solution root)
    ```

2.  **Install Root Node Dependencies & Git Hooks:**
    Run this command in the **solution root directory** (where `carestream.sln` is). This installs development tools like Husky and automatically configures the `pre-commit` Git hook (which runs `dotnet test`).
    ```bash
    npm install
    ```
    *(Note: You may need to run `npm install` inside `src/web` separately if frontend dependencies are managed there).*

3.  **Prepare Docker Compose Files:**
    *   Ensure you have both `docker-compose.yml` (base configuration) and `docker-compose.override.yml` (local overrides like port mappings) in the root directory. The override file is automatically merged by Docker Compose. Verify the port mapping for `carestream-web` (e.g., `5237:3000`) and `logto` (`3001:3001`, `3002:3002`).

4.  **Build & Start Containers:**
    This command builds the necessary Docker images (including `carestream-web` and the custom `logto` image) and starts all services.
    ```bash
    docker compose up -d --build
    ```
    *   `-d`: Runs containers in detached mode.
    *   `--build`: Rebuilds images if needed. Use this the first time and after changing Dockerfiles or related scripts.
    *   The first run downloads base images, builds custom images, runs database migrations (DbUp for Carestream, Logto's internal process), and starts services. This may take several minutes.
    *   **Check Status:** Monitor using `docker compose ps`. Wait until `logto` and `postgres` show `(healthy)`.

5.  **One-Time Logto Admin User Creation:**
    *   Once `logto` is healthy, open your browser to the Logto Admin Console: **`http://localhost:3002`**
    *   Follow the on-screen instructions to create the **first admin user account** for your local Logto instance.

6.  **One-Time Logto Application Registration:**
    *   Log in to the Logto Admin Console (`http://localhost:3002`).
    *   Navigate to **Applications** -> **Create Application**.
    *   Choose **Traditional Web**.
    *   Enter a name (e.g., `Carestream Web Local`) and click **Create**.
    *   Go to the application's **Settings** tab:
        *   **Redirect URIs**: Add `http://localhost:5237/signin-oidc` (Replace `5237` with your host port for `carestream-web` if different).
        *   **Post sign-out redirect URIs**: Add `http://localhost:5237/` (Replace `5237` if needed).
    *   **Save Changes**.]
    *   **(ADDITION) Configure Roles in Logto:**
        *   Navigate to the **Roles** section in the Logto sidebar.
        *   Create roles that match the application's needs (e.g., `PatientAdmin`, `Nurse`, `Doctor`, `Pharmacist`, `SystemAdmin`). Click **Create Role**.
        *   Ensure the **API Resources** for the role include the default Logto API (usually named like `https://default.logto.app/api` or similar - this allows roles to be included in the ID token/userinfo). Add permissions if necessary later.
        *   You will need to assign these roles to your test users later via the **Users** section in Logto. The Carestream application uses these role names (e.g., `User.IsInRole("PatientAdmin")`) for authorization.

7.  **Retrieve Logto Application Credentials:**
    *   On the Logto application details page:
        *   Copy the **App ID** (this is your `ClientId`).
        *   Go to the **App Secrets** tab, generate/view a secret, and copy the **Client Secret** value.

8.  **Configure Carestream Web via User Secrets:**
    *   Keep Logto credentials secure and out of source control using .NET User Secrets.
    *   Open your terminal/command prompt **in the `src/web` directory**.
    *   Run these commands, replacing the placeholder values:
        ```bash
        # Set the Client ID
        dotnet user-secrets set "Logto:ClientId" "YOUR_COPIED_APP_ID"

        # Set the Client Secret
        dotnet user-secrets set "Logto:ClientSecret" "YOUR_COPIED_CLIENT_SECRET"
        ```
    *   *(Note: Ensure `Logto:Authority` is `http://logto:3001/oidc` in `docker-compose.yml` or `appsettings.Development.json` for internal container communication. `Logto:BaseURL` pointing to `http://localhost:3001/oidc` might be needed for the OIDC event workaround in `Program.cs`.)*
  
# Set the Client Secret
        dotnet user-secrets set "Logto:ClientSecret" "YOUR_COPIED_CLIENT_SECRET"
        ```
    *   **Note:** Ensure `Logto:Authority` is correctly set to `http://logto:3001/oidc` via the environment variable in `docker-compose.yml` or `appsettings.Development.json`. `Logto:BaseURL` pointing to `http://localhost:3001/oidc` might be needed for the OIDC event workaround in `Program.cs`.

    *   **(ADDITION) Initial User Linking:** When a user logs into Carestream via Logto for the first time, their Logto identity (`sub` claim) needs to be linked to their corresponding user record in the Carestream database (likely matched via `force_number`). This linking process is typically performed by a user with administrative privileges within the Carestream application itself. This functionality will be developed later. For initial testing, ensure your seeded users in `Script0003_SeedLocalDevData.sql` have matching details (like `force_number`) to the users you create/assign roles to in Logto.
    *   
9.  **Restart Carestream Web Service:**
    *   Restart the web app container to pick up the User Secrets:
        ```bash
        docker compose restart carestream-web
        ```

10. **Access Carestream Application:**
    *   Open your browser to: **`http://localhost:5237`** (or your mapped port).
    *   You should be able to log in via Logto.

## Running Tests

*   **Pre-Commit:** Unit tests (`dotnet test`) are automatically run via a Husky Git hook before each local commit. If tests fail, the commit will be aborted.
*   **Manually:** You can run all tests in the solution using:
    ```bash
    dotnet test carestream.sln
    ```

## Troubleshooting

*   **Check Container Status:** `docker compose ps`. Ensure `carestream-web`, `logto`, `postgres` are `Up` and `healthy`.
*   **Logto Logs:** `docker compose logs logto`. Look for DB connection errors or entrypoint script issues.
*   **Carestream Web Logs:** `docker compose logs carestream-web`. Check for OIDC config errors, User Secrets issues, DbUp migration errors, or application exceptions.
*   **DbUp Errors:** Ensure SQL migration scripts in `src/Persistence/Migrations` have **Build Action** set to **Embedded resource**. Check connection strings and permissions.
*   **Discovery Document:** Check `http://localhost:3001/oidc/.well-known/openid-configuration` in browser. Ensure endpoints use `localhost:3001`. If not, verify `ENDPOINT` env var in `docker-compose.yml` for `logto` service and run `docker compose down && docker compose up -d`.
*   **OIDC Redirect Issues:** Verify `Redirect URIs` in Logto app settings match `http://localhost:YOUR_PORT/signin-oidc`. Clear browser cache/cookies for `localhost`. Check `Program.cs` OIDC events/configuration. Restart `carestream-web` container after config changes.
*   **Port Conflicts:** Ensure ports `5237`, `3001`, `3002`, `5433` (or your Postgres port) are free on your host.
*   **Restart Everything:** `docker compose down`, wait, then `docker compose up -d`.

## Next Steps

Refer to the Functional Specification document (`carestream_specification_v1.pdf` or similar) for user stories and features to implement.