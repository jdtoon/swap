importScripts(
    "https://www.gstatic.com/firebasejs/11.1.0/firebase-app-compat.js"
);
importScripts(
    "https://www.gstatic.com/firebasejs/11.1.0/firebase-messaging-compat.js"
);

// Register event handlers first, before any async operations
self.addEventListener("push", function (event) {
    console.log("Push event received:", event);
});

self.addEventListener("pushsubscriptionchange", function (event) {
    console.log("Push subscription change event:", event);
});

self.addEventListener("notificationclick", (event) => {
    console.log("Notification clicked:", event);
    event.notification.close();

    event.waitUntil(
        clients
            .matchAll({
                type: "window",
            })
            .then((clientList) => {
                for (const client of clientList) {
                    if (client.url === "/" && "focus" in client) return client.focus();
                }
                if (clients.openWindow) return clients.openWindow("/");
            })
    );
});

let messaging;

// Get config from the registration scope
self.addEventListener("message", (event) => {
    if (event.data && event.data.type === "FIREBASE_CONFIG") {
        console.log("Service Worker received FIREBASE_CONFIG message");
        try {
            firebase.initializeApp(event.data.config);
            console.log("Firebase initialized in service worker");

            messaging = firebase.messaging();
            console.log("Messaging initialized in service worker");

            // Set up background message handler after initialization
            messaging.onBackgroundMessage((payload) => {
                const notificationTitle = payload.notification.title;
                const notificationOptions = {
                    body: payload.notification.body,
                    icon: "/icons/icon-512.png",
                    badge: "/icons/icon-512.png",
                    data: payload.data,
                };

                return self.registration.showNotification(
                    notificationTitle,
                    notificationOptions
                );
            });
        } catch (error) {
            console.error("Error initializing Firebase in service worker:", error);
        }
    }
});