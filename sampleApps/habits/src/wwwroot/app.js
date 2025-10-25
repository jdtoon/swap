async function initializeServiceWorker() {
    if ("serviceWorker" in navigator) {
        try {
            await navigator.serviceWorker.register("/service-worker.js", {
                scope: "/",
                updateViaCache: "none"
            });
        } catch (error) {
            console.error("Service worker registration failed:", error);
        }
    }
}

// Initialize immediately
initializeServiceWorker();