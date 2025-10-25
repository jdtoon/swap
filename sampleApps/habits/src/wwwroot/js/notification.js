window.NotificationService =
    window.NotificationService ||
    class {
        constructor() {
            this.emailToggle = document.getElementById("emailNotificationToggle");
            //this.pushToggle = document.getElementById("pushNotificationToggle");
            
            if (!this.emailToggle) return;
            //if (!this.pushToggle) return;

            // Initialize email toggle state
            this.initializeEmailToggle();
            
            // Initialize push notification state
            //this.initializePushNotifications();
        }

        async initializeEmailToggle() {
            try {
                const response = await fetch("/api/notifications/status");
                if (response.ok) {
                    const { emailNotifications } = await response.json();
                    this.emailToggle.checked = emailNotifications;
                }
            } catch (error) {
                console.error("Error checking email notification status:", error);
            }
        }

        async initializePushNotifications() {
            // Check if browser supports notifications
            if (!("Notification" in window)) {
                this.pushToggle.disabled = true;
                return;
            }

            // Check if permission is already granted
            if (Notification.permission === "granted") {
                this.pushToggle.disabled = false;
                await this.checkPushNotificationStatus();
            } else if (Notification.permission === "denied") {
                this.pushToggle.disabled = true;
            } else {
                // Add click handler to request permission
                this.pushToggle.addEventListener("click", async () => {
                    const permission = await Notification.requestPermission();
                    if (permission === "granted") {
                        this.pushToggle.disabled = false;
                        await this.initializePushNotifications();
                    } else {
                        this.pushToggle.disabled = true;
                        this.pushToggle.checked = false;
                    }
                });
            }

            // Add change handler for the toggle
            this.pushToggle.addEventListener("change", async (e) => {
                if (e.target.checked) {
                    await this.enablePushNotifications();
                } else {
                    await this.disablePushNotifications();
                }
            });
        }

        async checkPushNotificationStatus() {
            try {
                const response = await fetch("/api/notifications/push/status");
                if (response.ok) {
                    const { receivePush } = await response.json();
                    this.pushToggle.checked = receivePush;
                }
            } catch (error) {
                console.error("Error checking push notification status:", error);
            }
        }

        async enablePushNotifications() {
            // Your existing FCM token logic here
            const token = await this.getFcmToken();
            if (token) {
                await this.saveFcmToken(token);
            }
        }

        async disablePushNotifications() {
            await fetch("/api/notifications/push", {
                method: "DELETE"
            });
        }

        async getFcmToken() {
            try {
                console.log("Starting FCM token retrieval process");

                // Register the service worker
                const registration = await navigator.serviceWorker.register(
                    "/firebase-messaging-sw.js",
                    {
                        scope: "/firebase-push/",
                    }
                );
                console.log("Firebase service worker registered successfully");

                // Send config to service worker
                registration.active.postMessage({
                    type: "FIREBASE_CONFIG",
                    config: window.firebaseConfig,
                });

                // Get registration token
                console.log("Attempting to get FCM token");
                const checkRegistration = await navigator.serviceWorker.ready;
                const token = await window.firebase.getToken(
                    window.firebase.messaging,
                    {
                        vapidKey: window.firebaseConfig.vapidKey,
                        serviceWorkerRegistration: checkRegistration,
                    }
                );

                if (token) {
                    console.log("FCM token retrieved successfully");
                    return token;
                } else {
                    console.warn("No FCM token was generated");
                    return null;
                }
            } catch (error) {
                console.error("Detailed FCM token error:", {
                    message: error.message,
                    stack: error.stack,
                    error,
                });
                return null;
            }
        }

        async saveFcmToken(token) {
            try {
                const response = await fetch("/api/notifications/token", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ token }),
                });

                if (!response.ok) {
                    throw new Error("Failed to save FCM token");
                }
            } catch (error) {
                console.error("Error saving FCM token:", error);
                throw error;
            }
        }
    };

// Initialize the service
window.notificationService =
    window.notificationService || new NotificationService();