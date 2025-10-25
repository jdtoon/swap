const CACHE_NAME = "app-cache-v0.0.1";

// Base static resources that you know will always exist
const baseResources = [
  "/",
  "/favicon.ico",
  "/manifest.json",
  "/lib/htmx/htmx.min.js",
  "/lib/qrcodejs/qrcode.min.js",
];

// Directories to cache
const directoriesToCache = ["/css/", "/js/", "/icons/"];

// File extensions to include
const fileExtensions = ["css", "js", "svg", "png", "ico", "json"];

async function getDirectoryContents(directory) {
  try {
    const response = await fetch(
      `/api/service-worker/directory-contents?path=${directory}`
    );
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
    const files = await response.json();
    return files;
  } catch (error) {
    console.error(`Failed to get contents of ${directory}:`, error);
    return [];
  }
}

self.addEventListener("install", (event) => {
  console.log("New Service Worker installing.");

  // Prevent the service worker from automatically activating after installation
  event.waitUntil(
    (async () => {
      const cache = await caches.open(CACHE_NAME);

      // Add timeout to cache operations
      const timeoutPromise = new Promise((_, reject) =>
        setTimeout(() => reject(new Error("Cache operation timeout")), 30000)
      );

      // Cache base resources with timeout
      await Promise.race([
        Promise.all(
          baseResources.map((resource) =>
            cache.add(resource).catch((error) => {
              console.error(`Failed to cache: ${resource}`, error);
              return Promise.resolve(); // Continue despite error
            })
          )
        ),
        timeoutPromise,
      ]);

      // First cache the base resources
      console.log("Caching base resources...");
      for (const resource of baseResources) {
        try {
          await cache.add(resource);
          console.log(`Successfully cached: ${resource}`);
        } catch (error) {
          console.error(`Failed to cache: ${resource}`, error);
        }
      }

      // Then cache directory contents
      for (const directory of directoriesToCache) {
        try {
          const files = await getDirectoryContents(directory);
          for (const file of files) {
            const fileExtension = file.split(".").pop().toLowerCase();
            if (fileExtensions.includes(fileExtension)) {
              try {
                await cache.add(file);
                console.log(`Successfully cached: ${file}`);
              } catch (error) {
                console.error(`Failed to cache: ${file}`, error);
              }
            }
          }
        } catch (error) {
          console.error(`Failed to process directory ${directory}:`, error);
          throw error;
        }
      }
    })()
  );
});

// Activate event
self.addEventListener("activate", (event) => {
  console.log("New Service Worker activating.");
  event.waitUntil(
    Promise.all([
      // Delete old caches
      caches.keys().then((cacheNames) => {
        return Promise.all(
          cacheNames
            .filter((name) => name !== CACHE_NAME)
            .map((name) => {
              console.log("Deleting old cache:", name);
              return caches.delete(name);
            })
        );
      }),
      // Take control of all clients
      clients.claim(),
    ])
  );
});

// Message event
self.addEventListener("message", (event) => {
  if (event.data.type === "SKIP_WAITING") {
    self.skipWaiting();
  } else if (event.data.type === "GET_CACHE_NAME") {
    event.source.postMessage({
      type: "CACHE_NAME",
      cacheName: CACHE_NAME,
    });
  }
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);

  // Only cache GET requests
  if (event.request.method !== "GET") return;

  // Check if the request is for a static resource
  const fileExtension = url.pathname.split(".").pop().toLowerCase();
  const isStaticResource = fileExtensions.includes(fileExtension);

  if (isStaticResource) {
    event.respondWith(
      caches.match(event.request).then((cachedResponse) => {
        if (cachedResponse) {
          return cachedResponse;
        }
        return fetch(event.request).then((response) => {
          if (!response || response.status !== 200) {
            return response;
          }
          return caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, response.clone());
            return response;
          });
        });
      })
    );
  }
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  event.waitUntil(
    clients.matchAll({ type: "window" }).then((clientList) => {
      const url = event.notification.data.url || "/";

      // If a window is already open, focus it
      for (const client of clientList) {
        if (client.url === url && "focus" in client) {
          return client.focus();
        }
      }

      // If no window is open, open a new one
      return clients.openWindow(url);
    })
  );
});
