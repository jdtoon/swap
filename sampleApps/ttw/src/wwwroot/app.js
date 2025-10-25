// Create and manage the update overlay
const createUpdateOverlay = (message) => {
    const overlay = document.createElement('div');
    overlay.className = 'update-overlay';
    overlay.id = 'update-overlay';
    overlay.innerHTML = `
        <div id="update-overlay" class="update-content">
            <div class="update-spinner"></div>
            <h2 class="text-xl font-bold mb-2">Updating App</h2>
            <p>${message}</p>
        </div>
    `;
    document.body.appendChild(overlay);
    return overlay;
};

const removeUpdateOverlay = () => {
    const overlay = document.getElementById('update-overlay');
    if (overlay) overlay.remove();
};

const setUpdateTimeout = () => {
    return setTimeout(() => {
        removeUpdateOverlay();
        console.log('Update timed out - removing overlay');
    }, 15000); // 15 seconds timeout
};

const showCompletionDialog = () => {
    return new Promise((resolve) => {
        const dialog = document.createElement('div');
        dialog.className = 'update-overlay';
        dialog.innerHTML = `
            <div class="update-content">
                <h2 class="text-xl font-bold mb-4">Update Complete</h2>
                <p class="mb-4">The application has been updated to the latest version.</p>
                <button class="btn btn-primary">Continue</button>
            </div>
        `;

        const button = dialog.querySelector('button');
        button.onclick = () => {
            dialog.remove();
            resolve();
            window.location.reload();
        };

        document.body.appendChild(dialog);
    });
};

// In app.js
async function initializeUpdateCheck() {
    if ("serviceWorker" in navigator) {
        try {
            let registration = await navigator.serviceWorker.getRegistration();
            if (!registration) {
                registration = await navigator.serviceWorker.register('/service-worker.js', {
                    scope: '/'
                });
                console.log('ServiceWorker registration successful');
            }

            registration.addEventListener("updatefound", () => {
                const newWorker = registration.installing;
                registration.update()
                const updateOverlay = createUpdateOverlay("Installing new version...");
                const updateTimeout = setUpdateTimeout();

                newWorker.addEventListener("statechange", async () => {
                    console.log('ServiceWorker state:', newWorker.state);

                    try {
                        if (newWorker.state === "installed" && navigator.serviceWorker.controller) {
                            clearTimeout(updateTimeout);

                            // Get the new cache name from the service worker
                            const newCacheName = await Promise.race([
                                new Promise(resolve => {
                                    navigator.serviceWorker.addEventListener("message", function handler(event) {
                                        if (event.data.type === "CACHE_NAME") {
                                            navigator.serviceWorker.removeEventListener("message", handler);
                                            resolve(event.data.cacheName);
                                        }
                                    });
                                    newWorker.postMessage({ type: "GET_CACHE_NAME" });
                                }),
                                new Promise((_, reject) =>
                                    setTimeout(() => reject(new Error('Cache name request timeout')), 5000)
                                )
                            ]);

                            // Cache cleanup
                            try {
                                const keys = await window.caches.keys();
                                await Promise.all(
                                    keys
                                        .filter(key => key !== newCacheName)
                                        .map(key => window.caches.delete(key))
                                );
                            } catch (error) {
                                console.error('Cache cleanup failed:', error);
                            }

                            removeUpdateOverlay();

                            try {
                                await showCompletionDialog();
                            } catch (error) {
                                console.error('Show completion dialog failed:', error);
                            }

                            // Skip waiting and reload
                            navigator.serviceWorker.controller.postMessage({ type: "SKIP_WAITING" });

                            let reloadTriggered = false;
                            navigator.serviceWorker.addEventListener("controllerchange", () => {
                                if (!reloadTriggered) {
                                    reloadTriggered = true;
                                    window.location.reload();
                                }
                            });
                        } else if (newWorker.state === "redundant") {
                            // Worker became redundant - something went wrong
                            clearTimeout(updateTimeout);
                            removeUpdateOverlay();
                            console.error('Service Worker became redundant');
                        }
                    } catch (error) {
                        clearTimeout(updateTimeout);
                        removeUpdateOverlay();
                        console.error('Update process failed:', error);
                    }
                });
            });
        } catch (error) {
            console.error("ServiceWorker registration failed:", error);
            removeUpdateOverlay();
        }
    }
}

// Initialize
initializeUpdateCheck();