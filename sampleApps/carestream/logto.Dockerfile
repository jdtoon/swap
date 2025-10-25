# Use the same Logto base image you specified in docker-compose
# FROM ghcr.io/logto-io/logto:latest
FROM ghcr.io/logto-io/logto:latest

# Switch to root user to install packages
USER root

# Install the PostgreSQL client tools using apk (for Alpine Linux)
RUN apk update && \
    apk add --no-cache postgresql-client

# --- START CHANGE: Fix permissions for alteration script ---
# Grant the 'node' user ownership of the directory needed by the alteration script.
# The alteration process needs to write into the 'alteration-scripts' directory.
# We grant ownership to the parent 'cli' directory to be safe.
# Ensure the parent directory exists first.
RUN mkdir -p /etc/logto/packages/cli/alteration-scripts && \
    chown -R node:node /etc/logto/packages/cli
# --- END CHANGE ---

# Copy your custom entrypoint script into the image
COPY logto-entrypoint-wrapper.sh /usr/local/bin/logto-entrypoint-wrapper.sh

# Ensure the script is executable
RUN chmod +x /usr/local/bin/logto-entrypoint-wrapper.sh

# Switch back to the non-root user 'node'
USER node

# Set the entrypoint to your custom script
ENTRYPOINT ["/usr/local/bin/logto-entrypoint-wrapper.sh"]