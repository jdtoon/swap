# SwapChat Demo

This project demonstrates the **Distributed Scalability** features of Swap.Htmx (Phase 4).

It implements a custom `ISseBackplane` using the file system (`FileSseBackplane`) to simulate a distributed message bus like Redis or NATS.

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

1. Open Browser A to `http://localhost:5001`.
2. Join the chat as "User1" in room "general".
3. Open Browser B to `http://localhost:5002`.
4. Join the chat as "User2" in room "general".
5. Send a message from Browser A.
6. **Observe:** The message appears in Browser B!

This works because:
1. Instance 1 receives the POST request.
2. Instance 1 publishes the message to `Data/bus.jsonl` via `FileSseBackplane`.
3. Instance 2's `FileSseBackplane` detects the file change.
4. Instance 2 reads the message and broadcasts it to its connected clients (Browser B).

This proves that `Swap.Htmx` is now architected for web farms and distributed systems.
