# SwapChat Demo

This project demonstrates the **Real-time** and **Security** features of Swap.Htmx.

It implements:
1.  **Real-time Chat:** Uses Server-Sent Events (SSE) with the default `InMemorySseBackplane`.
2.  **Security & Authorization:**
    -   **Authentication:** Cookie-based authentication.
    -   **Room Access Control:** `CanJoinRoom` validator prevents unauthorized access to specific rooms (e.g., "admin").
    -   **Targeted Messaging:** `ToUser(username)` allows sending private messages to specific users.

> **Note:** This demo runs on a single server. For a distributed example (multi-server scaling), see [SwapRedisDemo](../SwapRedisDemo/README.md).

## How to Run

Open a terminal and run:
```powershell
cd src
dotnet run --urls "http://localhost:5001"
```

## How to Test

### Chat & Rooms
1. Open Browser A to `http://localhost:5001`. Login as `User1` (Role: User).
2. Open Browser B (incognito) to `http://localhost:5001`. Login as `User2` (Role: User).
3. Send a message from Browser A.
4. **Observe:** The message appears in Browser B.

### Security Features
1. **Private Messaging:**
   - In Browser A (User1), use the "Private Message" form to send a message to `User2`.
   - **Observe:** Only Browser B receives the message.

2. **Room Access Control:**
   - Try to access `http://localhost:5001/swap/sse?room=admin` directly in the browser while logged in as a normal User.
   - **Observe:** The connection will be rejected.

3. **Admin Broadcast:**
   - Login as `AdminUser` (Role: Admin).
   - Use the "Admin Broadcast" form (only visible to admins).

## Documentation

For more details on the realtime capabilities used in this demo, see the [Realtime & WebSockets Guide](../../lib/Swap.Htmx/docs/WebSockets.md).
   - **Observe:** The message is sent to ALL users on ALL servers.

This proves that `Swap.Htmx` is now architected for secure, distributed systems.
