#!/bin/sh
# Exit immediately if a command exits with a non-zero status.
set -e

# --- Configuration (Read from environment variables) ---
# Use environment variables provided by Docker Compose
DB_USER="logto_user" # Default values are fallback
DB_PASSWORD="StrongPassword123!"
DB_HOST="postgres"  # Default to 'postgres' service name
DB_NAME="logto_db"

# !! IMPORTANT: Verify this table name !!
# Choose a table reliably created ONLY by the initial seed command.
# Check Logto's schema or run seed once and inspect the DB if unsure.
SEED_CHECK_TABLE="organizations"
SEED_CHECK_SCHEMA="public" # Usually 'public' unless Logto specifies another schema

# Check if required DB variables are set
if [ -z "${DB_PASSWORD}" ]; then
  echo "Error: POSTGRES_PASSWORD environment variable is not set."
  exit 1
fi

# Export password for psql (safer than command-line argument)
export PGPASSWORD="${DB_PASSWORD}"

# --- Idempotent Seeding ---
echo "Checking if database '${DB_NAME}' on host '${DB_HOST}' needs seeding..."

# Check if psql is available
if ! command -v psql > /dev/null; then
  echo "Error: psql command not found. Cannot check database seed status."
  echo "Please ensure postgresql-client is installed in the Logto image."
  exit 1
fi

# Use psql to check if the target table exists.
# -t: Tuples only (no headers)
# -A: Unaligned output
# -c: Command to execute
# Redirect stderr to /dev/null to hide connection messages/errors if DB isn't quite ready
# Check command exit status ($?) to ensure psql connected successfully BEFORE checking its output
set +e # Temporarily disable exit on error for the check command
table_exists_output=$(psql -h "${DB_HOST}" -U "${DB_USER}" -d "${DB_NAME}" -tAc \
    "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = '${SEED_CHECK_SCHEMA}' AND table_name = '${SEED_CHECK_TABLE}');" 2>/dev/null)
psql_exit_status=$?
set -e # Re-enable exit on error

# Default to needing seeding if check fails or table not found
NEEDS_SEEDING=1

if [ $psql_exit_status -ne 0 ]; then
    # If psql failed to connect/run query (DB might not be ready yet, or config wrong)
    # Depending on the desired behavior, you could retry here or exit.
    # Exiting is safer if the DB should already be healthy via depends_on.
    echo "Warning: Failed to execute psql check (Exit code: ${psql_exit_status}). Assuming seeding is required."
    echo "Ensure DB is running, accessible, and credentials are correct."
    # Consider exiting if this check *must* succeed before proceeding:
    # echo "Error: psql check failed. Exiting."
    # exit 1
    NEEDS_SEEDING=1
elif [ "$table_exists_output" = "t" ]; then
    # 't' means TRUE, the table exists
    echo "Database already seeded (table '${SEED_CHECK_TABLE}' found)."
    NEEDS_SEEDING=0
else
    # 'f' means FALSE (table not found), or output was unexpected
    echo "Database not seeded (table '${SEED_CHECK_TABLE}' not found)."
    NEEDS_SEEDING=1
fi

# Run seed command ONLY if needed
if [ "$NEEDS_SEEDING" -eq 1 ]; then
    echo "Running Logto database seed via CLI..."
    # Ensure the cli command uses the correct env vars (DB_URL or PG*)
    # Assuming it uses DB_URL based on Logto docs examples:
    if [ -z "${DB_URL}" ]; then
        echo "Warning: DB_URL environment variable not explicitly set for seed command, constructing from POSTGRES_* vars."
        TEMP_DB_URL="postgres://${DB_USER}:${DB_PASSWORD}@${DB_HOST}:5432/${DB_NAME}"
        # !!! CHANGE THIS LINE: Remove --no, add -y !!!
        npx -y @logto/cli@latest db seed --db-url "${TEMP_DB_URL}"
    else
        # !!! CHANGE THIS LINE: Remove --no, add -y !!!
        npx -y @logto/cli@latest db seed --db-url "${DB_URL}"
    fi
    echo "Database seeding completed."
else
    echo "Skipping database seed."
fi

# --- Run Migrations (Alterations) ---
# This command is typically idempotent itself (migration tools track applied migrations)
echo "Running Logto database alterations/migrations to latest version..."
# Ensure the alteration command uses the correct env vars (DB_URL or PG*)
# Assuming it also uses DB_URL:
if [ -z "${DB_URL}" ]; then
    echo "Warning: DB_URL environment variable not explicitly set for alteration command."
    # The `npm run alteration` might implicitly pick up DB_URL from the environment,
    # or you might need to pass it explicitly if the script requires it. Check Logto's alteration script.
fi
npm run alteration deploy -- latest
echo "Database alterations completed."

# --- Start Logto Core Service ---
# Now, execute the original command that the Logto container was supposed to run.

# Verify the path 'packages/core' exists relative to the WORKDIR defined in the Dockerfile
CORE_PATH="packages/core"
if [ ! -d "$CORE_PATH" ]; then
    echo "Error: Directory '$CORE_PATH' not found. Cannot start Logto core."
    # Maybe the WORKDIR is different? Let's try starting from root if it exists there.
    if [ -f "./packages/core/index.js" ] || [ -f "./packages/core/dist/index.js" ]; then
      echo "Found packages/core in current directory instead. Adjusting path."
      CORE_PATH="." # Assume we should run from the WORKDIR containing packages/core
    elif [ -f "/app/packages/core/index.js" ] || [ -f "/app/packages/core/dist/index.js" ]; then
      echo "Found packages/core in /app. Changing directory to /app/packages/core."
      CORE_PATH="/app/packages/core" # Common /app WORKDIR
    else
       echo "Cannot determine correct path to Logto core. Exiting."
       exit 1
    fi
fi

echo "Changing directory to '$CORE_PATH'..."
cd "$CORE_PATH" # Change directory first

echo "Starting Logto core service in production mode..."
# Use 'exec' to replace the shell process with the node process.
# This is important for signal handling (like SIGTERM for graceful shutdown).
exec env NODE_ENV=production node .