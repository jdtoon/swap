const CACHE_NAME = "habits-cache-v0.09.10";

// Base static resources that you know will always exist
const baseResources = [
    "/",
    "/favicon.ico",
    "/manifest.json",
    "/lib/htmx/htmx.min.js",
    "/lib/qrcodejs/qrcode.min.js",
    "/lib/sortable/Sortable.min.js",
];

// Directories to cache
const directoriesToCache = ["/css/", "/js/", "/svg/", "/icons/"];

// File extensions to include
const fileExtensions = ["css", "js", "svg", "png", "ico", "json"];

async function getDirectoryContents(directories) {
    try {
        const queryString = directories
            .map(dir => `paths=${encodeURIComponent(dir)}`)
            .join('&');
            
        const response = await fetch(`/api/service-worker/directory-contents?${queryString}`);
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const files = await response.json();
        return files;
    } catch (error) {
        console.error(`Failed to get contents of directories:`, error);
        return [];
    }
}

//self.addEventListener("install", (event) => {
//    event.waitUntil(
//        (async () => {
//            const cache = await caches.open(CACHE_NAME);

//            // Cache base resources first
//            await Promise.all(
//                baseResources.map(resource => 
//                    cache.add(resource).catch(error => 
//                        console.warn(`Failed to cache ${resource}:`, error)
//                    )
//                )
//            );

//            // Get all files from all directories in a single request
//            try {
//                const files = await getDirectoryContents(directoriesToCache);
//                await Promise.all(
//                    files
//                        .filter(file => 
//                            fileExtensions.includes(file.split(".").pop().toLowerCase())
//                        )
//                        .map(file => 
//                            cache.add(file).catch(error => 
//                                console.warn(`Failed to cache ${file}:`, error)
//                            )
//                        )
//                );
//            } catch (error) {
//                console.error(`Failed to process directories:`, error);
//            }

//            self.skipWaiting();
//        })()
//    );
//});

//self.addEventListener("activate", (event) => {
//    event.waitUntil(
//        Promise.all([
//            // Delete old caches
//            caches.keys()
//                .then(cacheNames => 
//                    Promise.all(
//                        cacheNames
//                            .filter(name => name !== CACHE_NAME)
//                            .map(name => caches.delete(name))
//                    )
//                ),
//            // Take control of all clients immediately
//            clients.claim()
//        ])
//    );
//});

//// Message event
//self.addEventListener("message", (event) => {
//    if (event.data.type === "SKIP_WAITING") {
//        self.skipWaiting();
//    } else if (event.data.type === "GET_CACHE_NAME") {
//        event.source.postMessage({
//            type: "CACHE_NAME",
//            cacheName: CACHE_NAME,
//        });
//    }
//});

//self.addEventListener("fetch", (event) => {
//    if (event.request.headers.get("Accept")?.includes("text/html")) {
//        return;
//    }

//    event.respondWith(
//        caches
//            .match(event.request)
//            .then((cachedResponse) => cachedResponse || fetch(event.request))
//    );
//});

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