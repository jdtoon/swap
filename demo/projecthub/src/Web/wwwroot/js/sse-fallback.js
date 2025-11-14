/*!
 * Swap.Htmx SSE Fallback Extension
 * Provides automatic polling fallback when Server-Sent Events fail
 * Part of the Swap framework for ASP.NET Core + HTMX
 */

(function () {
  "use strict";

  // Extension state management
  const SseFallback = {
    connections: new Map(),
    defaultConfig: {
      pollInterval: 5000, // 5 seconds
      maxRetries: 3,
      retryBackoff: 2, // Exponential backoff multiplier
      timeout: 30000, // 30 seconds
      enableFallback: true,
      fallbackAfterErrors: 2, // Switch to polling after 2 SSE errors
      debug: false,
    },
  };

  // Connection state tracking
  class SseConnection {
    constructor(element, config) {
      this.element = element;
      this.config = { ...SseFallback.defaultConfig, ...config };
      this.retryCount = 0;
      this.errorCount = 0;
      this.usingFallback = false;
      this.eventSource = null;
      this.pollTimer = null;
      this.lastEventId = null;
      this.isActive = false;
    }

    log(message, ...args) {
      if (this.config.debug) {
        console.log(`[SSE-Fallback] ${message}`, ...args);
      }
    }

    start() {
      if (this.isActive) return;
      this.isActive = true;
      this.log("Starting SSE connection", this.config);
      this.connectSSE();
    }

    stop() {
      this.isActive = false;
      this.cleanup();
      this.log("SSE connection stopped");
    }

    cleanup() {
      if (this.eventSource) {
        this.eventSource.close();
        this.eventSource = null;
      }
      if (this.pollTimer) {
        clearInterval(this.pollTimer);
        this.pollTimer = null;
      }
    }

    connectSSE() {
      if (!this.isActive) return;

      try {
        const url =
          this.element.getAttribute("hx-sse")?.replace("connect:", "") ||
          this.element.getAttribute("sse-connect");
        if (!url) {
          this.log("No SSE URL found, skipping SSE connection");
          if (this.config.enableFallback) {
            this.startPolling();
          }
          return;
        }

        this.log("Connecting to SSE:", url);
        this.eventSource = new EventSource(url);

        this.eventSource.onopen = () => {
          this.log("SSE connection opened");
          this.retryCount = 0;
          this.errorCount = 0;
          this.triggerEvent("sse:connected", { elementId: this.element.id });
        };

        this.eventSource.onerror = (error) => {
          this.log("SSE error occurred:", error);
          this.errorCount++;
          this.triggerEvent("sse:error", {
            error,
            errorCount: this.errorCount,
            elementId: this.element.id,
          });

          if (
            this.config.enableFallback &&
            this.errorCount >= this.config.fallbackAfterErrors
          ) {
            this.log(
              "Switching to polling fallback after",
              this.errorCount,
              "errors"
            );
            this.switchToPolling();
          } else {
            this.scheduleRetry();
          }
        };

        this.eventSource.onmessage = (event) => {
          this.handleMessage(event.type || "message", event.data);
        };

        // Handle custom events
        const swapAttribute = this.element.getAttribute("hx-sse-swap");
        if (swapAttribute) {
          const events = swapAttribute.split(",").map((e) => e.trim());
          events.forEach((eventName) => {
            this.eventSource.addEventListener(eventName, (event) => {
              this.handleMessage(eventName, event.data);
            });
          });
        }
      } catch (error) {
        this.log("Failed to create SSE connection:", error);
        if (this.config.enableFallback) {
          this.startPolling();
        }
      }
    }

    scheduleRetry() {
      if (!this.isActive || this.retryCount >= this.config.maxRetries) {
        if (this.config.enableFallback) {
          this.log("Max retries reached, switching to polling");
          this.startPolling();
        }
        return;
      }

      const delay = Math.min(
        1000 * Math.pow(this.config.retryBackoff, this.retryCount),
        30000
      );
      this.retryCount++;

      this.log(
        `Retrying SSE connection in ${delay}ms (attempt ${this.retryCount}/${this.config.maxRetries})`
      );

      setTimeout(() => {
        if (this.isActive) {
          this.cleanup();
          this.connectSSE();
        }
      }, delay);
    }

    switchToPolling() {
      this.usingFallback = true;
      this.cleanup();
      this.triggerEvent("sse:fallback", { elementId: this.element.id });
      this.startPolling();
    }

    startPolling() {
      if (!this.config.enableFallback || this.pollTimer) return;

      const pollUrl =
        this.element.getAttribute("hx-sse-fallback-url") ||
        this.element.getAttribute("hx-get") ||
        this.element.getAttribute("hx-sse")?.replace("connect:", "") + "/poll";

      if (!pollUrl) {
        this.log("No polling URL configured, fallback disabled");
        return;
      }

      const pollInterval =
        parseInt(this.element.getAttribute("hx-sse-fallback-interval")) ||
        this.config.pollInterval;
      const pollType =
        this.element.getAttribute("hx-sse-fallback-type") || "html";

      this.log(
        "Starting polling fallback to:",
        pollUrl,
        "every",
        pollInterval,
        "ms",
        "type:",
        pollType
      );
      this.triggerEvent("sse:polling-start", { elementId: this.element.id });

      this.pollTimer = setInterval(() => {
        if (!this.isActive) return;

        const headers = {
          "HX-Request": "true",
          "HX-Current-URL": window.location.href,
        };

        if (this.lastEventId) {
          headers["Last-Event-ID"] = this.lastEventId;
        }

        if (pollType === "json") {
          headers["Accept"] = "application/json";
        }

        fetch(pollUrl, { headers })
          .then((response) => {
            if (response.status === 204) return null; // No content
            if (!response.ok) throw new Error(`HTTP ${response.status}`);

            // Update last event ID if provided
            const eventId = response.headers.get("X-Event-ID");
            if (eventId) {
              this.lastEventId = eventId;
            }

            return pollType === "json" ? response.json() : response.text();
          })
          .then((data) => {
            if (data) {
              if (pollType === "json") {
                this.triggerEvent("sse:data", {
                  data,
                  elementId: this.element.id,
                  usingFallback: true,
                });
              } else {
                this.handleMessage("poll-update", data);
              }
            }
          })
          .catch((error) => {
            this.log("Polling error:", error);
            this.triggerEvent("sse:poll-error", {
              error,
              elementId: this.element.id,
            });
          });
      }, pollInterval);
    }

    handleMessage(eventType, data) {
      this.log("Received message:", eventType, data?.substring?.(0, 100));

      if (!this.lastEventId) {
        this.lastEventId = Date.now().toString(); // Simple event ID for polling
      }

      // Trigger HTMX swap if this is the target element
      const swapAttribute = this.element.getAttribute("hx-sse-swap");
      if (swapAttribute && swapAttribute.includes(eventType)) {
        this.swapContent(data);
      }

      // Trigger custom events
      this.triggerEvent(`sse:${eventType}`, {
        data,
        usingFallback: this.usingFallback,
        elementId: this.element.id,
      });
    }

    swapContent(html) {
      const swapStrategy = this.element.getAttribute("hx-swap") || "innerHTML";

      switch (swapStrategy) {
        case "innerHTML":
          this.element.innerHTML = html;
          break;
        case "outerHTML":
          this.element.outerHTML = html;
          break;
        case "beforebegin":
          this.element.insertAdjacentHTML("beforebegin", html);
          break;
        case "afterbegin":
          this.element.insertAdjacentHTML("afterbegin", html);
          break;
        case "beforeend":
          this.element.insertAdjacentHTML("beforeend", html);
          break;
        case "afterend":
          this.element.insertAdjacentHTML("afterend", html);
          break;
        default:
          this.element.innerHTML = html;
      }

      // Process any new HTMX elements
      if (window.htmx) {
        htmx.process(this.element);
      }

      this.triggerEvent("sse:swapped", {
        html,
        strategy: swapStrategy,
        elementId: this.element.id,
      });
    }

    triggerEvent(eventName, detail = {}) {
      const event = new CustomEvent(eventName, {
        detail: { ...detail, connection: this },
        bubbles: true,
        cancelable: true,
      });
      this.element.dispatchEvent(event);
      document.dispatchEvent(event); // Also trigger on document for global listeners
    }
  }

  // HTMX Integration
  if (window.htmx) {
    // Define the extension
    htmx.defineExtension("sse-fallback", {
      onEvent: function (name, evt) {
        if (name === "htmx:load") {
          initializeSseFallback(evt.target);
        } else if (name === "htmx:beforeRequest") {
          // Add SSE fallback headers to HTMX requests if needed
          const element = evt.target;
          if (element.hasAttribute("hx-sse-fallback-url")) {
            evt.detail.requestConfig.headers["X-SSE-Fallback"] = "true";
          }
        }
      },
    });

    // Auto-initialize on document ready
    document.addEventListener("DOMContentLoaded", () => {
      initializeSseFallback(document.body);
    });
  }

  // Initialize SSE fallback for elements
  function initializeSseFallback(container) {
    const elements = container.querySelectorAll(
      '[hx-sse], [hx-ext*="sse-fallback"]'
    );

    elements.forEach((element) => {
      if (SseFallback.connections.has(element)) return;

      // Parse configuration from attributes
      const config = {
        pollInterval:
          parseInt(element.getAttribute("hx-sse-fallback-interval")) ||
          undefined,
        maxRetries:
          parseInt(element.getAttribute("hx-sse-max-retries")) || undefined,
        timeout: parseInt(element.getAttribute("hx-sse-timeout")) || undefined,
        enableFallback: element.getAttribute("hx-sse-fallback-url")
          ? true
          : undefined,
        fallbackAfterErrors:
          parseInt(element.getAttribute("hx-sse-fallback-errors")) || undefined,
        debug: element.hasAttribute("hx-sse-debug"),
      };

      // Remove undefined values
      Object.keys(config).forEach((key) => {
        if (config[key] === undefined) delete config[key];
      });

      const connection = new SseConnection(element, config);
      SseFallback.connections.set(element, connection);

      // Auto-start if SSE URL is specified
      if (element.getAttribute("hx-sse")) {
        connection.start();
      }

      // Cleanup on element removal
      const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
          mutation.removedNodes.forEach((node) => {
            if (node === element) {
              connection.stop();
              SseFallback.connections.delete(element);
              observer.disconnect();
            }
          });
        });
      });

      if (element.parentNode) {
        observer.observe(element.parentNode, { childList: true });
      }
    });
  }

  // Global API
  window.SseFallback = {
    connections: SseFallback.connections,
    config: SseFallback.defaultConfig,

    // Manually initialize an element
    init: initializeSseFallback,

    // Get connection for an element
    getConnection: (element) => SseFallback.connections.get(element),

    // Global configuration
    configure: (options) => {
      Object.assign(SseFallback.defaultConfig, options);
    },
  };
})();
