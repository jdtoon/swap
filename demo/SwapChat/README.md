# SwapChat Demo

This project demonstrates the **Distributed Scalability** and **Security** features of Swap.Htmx (Phase 4).

It implements:
1.  **Distributed SSE:** A custom `ISseBackplane` using the file system (`FileSseBackplane`) to simulate a distributed message bus like Redis or NATS.
2.  **Security & Authorization:**
    -   **Authentication:** Cookie-based authentication.
    -   **Room Access Control:** `CanJoinRoom` validator prevents unauthorized access to specific rooms (e.g., "admin").
    -   **Targeted Messaging:** `ToUser(username)` allows sending private messages to specific users across the cluster.

## How to Run

To see the distributed nature in action, you need to run two separate instances of the application on different ports.

### Instance 1
Open a terminal and run:
```powershell
cd src
dotnet run --urls "http://localhost:5001"
```

### Instance 2
Open a **second** terminal and run:
```powershell
cd src
dotnet run --urls "http://localhost:5002"
```

## How to Test

### Distributed Chat
1. Open Browser A to `http://localhost:5001`. Login as `User1` (Role: User).
2. Open Browser B to `http://localhost:5002`. Login as `User2` (Role: User).
3. Send a message from Browser A.
4. **Observe:** The message appears in Browser B! (Cross-server communication).

### Security Features
1. **Private Messaging:**
   - In Browser A (User1), use the "Private Message" form to send a message to `User2`.
   - **Observe:** Only Browser B receives the message.

2. **Room Access Control:**
   - Try to access `http://localhost:5001/swap/sse?room=admin` directly in the browser while logged in as a normal User.
   - **Observe:** The connection will be rejected (or receive no events, depending on implementation details).
   - *Note: The UI currently defaults to "general" room, but the backend enforces the rule.*

3. **Admin Broadcast:**
   - Login as `AdminUser` (Role: Admin).
   - Use the "Admin Broadcast" form (only visible to admins).
   - **Observe:** The message is sent to ALL users on ALL servers.

This proves that `Swap.Htmx` is now architected for secure, distributed systems.
