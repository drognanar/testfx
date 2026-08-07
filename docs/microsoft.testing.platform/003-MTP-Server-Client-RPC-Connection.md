# MTP server-mode client RPC connection abstraction

## Status

Design only. This document does not introduce `IMtpRpcConnection` or change the server-mode client
implementation.

## Motivation

`MtpServerClient` currently depends directly on `MtpJsonRpcConnection`. That implementation is the correct
default for the source-only package: it is dependency-free, trim/AOT friendly, and uses the platform's existing
Jsonite or System.Text.Json formatter. Some source-package consumers already use StreamJsonRpc and need to
integrate the MTP protocol into their existing RPC lifetime, diagnostics, and debugger pipeline.

The client should depend on a small connection contract while retaining the current implementation as its
default. The abstraction must preserve more than method signatures: the current ordered read loop guarantees
that notification handlers run before a later response completes its request task. Consumers rely on that
barrier to collect all test-node updates by awaiting `DiscoverTestsAsync` or `RunTestsAsync`.

## Proposed interface

The interface remains internal because the source-only package compiles it into the consumer assembly.

```csharp
internal interface IMtpRpcConnection : IDisposable
{
    event Action<NotificationMessage>? NotificationReceived;

    Func<RequestMessage, CancellationToken, Task<object?>>? ServerRequestHandler { get; set; }

    void Start();

    Task<ResponseMessage> SendRequestAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken);

    Task SendNotificationAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken);
}
```

The members deliberately match the surface `MtpServerClient` uses today:

- `Start` begins inbound processing after the client has installed its notification and server-request handlers.
  It is idempotent and throws `ObjectDisposedException` after disposal.
- `SendRequestAsync` allocates/correlates the JSON-RPC id and returns the decoded response envelope.
- `SendNotificationAsync` sends a framed notification without awaiting a response.
- `NotificationReceived` provides ordered server notifications.
- `ServerRequestHandler` handles server-to-client requests and returns their raw result.
- `Dispose` terminates inbound processing and releases transport resources.

`NotificationMessage`, `RequestMessage`, and `ResponseMessage` remain the source package's internal protocol
envelopes. An adapter maps its backend's native messages to these types at its boundary.

## Client and process construction

`MtpServerClient` changes its field and existing-connection constructor from `MtpJsonRpcConnection` to
`IMtpRpcConnection`:

```csharp
internal MtpServerClient(
    IMtpRpcConnection connection,
    MtpServerClientOptions? options = null)
```

The constructor keeps its current ownership behavior: a client created with an externally supplied connection
owns and disposes that connection, but does not own a process. The launch factories remain unchanged for callers.
`MtpServerProcess.StartAsync` creates the TCP message handler and the default `MtpJsonRpcConnection`, exposes it
as `IMtpRpcConnection`, and continues to own the launched process and socket.

No connection factory is added to `MtpServerClientOptions`. A consumer that needs StreamJsonRpc constructs its
adapter over an already-connected transport and passes it to the existing-connection constructor. This avoids
mixing transport creation with test-application process launch.

## Default hand-rolled implementation

`MtpJsonRpcConnection` implements `IMtpRpcConnection` without behavioral changes:

- `TcpMessageHandler` retains LSP-style `Content-Length` framing.
- Jsonite remains the down-level formatter; System.Text.Json remains the modern .NET formatter.
- The write semaphore prevents interleaved frames.
- The single read loop dispatches each inbound envelope in wire order.
- Response dispatch completes the corresponding pending request only when the read loop reaches that response.
- Caller cancellation completes the request as canceled and sends one best-effort `$/cancelRequest`.
- Closing the connection fails every pending request.

This remains the only implementation shipped by testfx. The source package does not reference StreamJsonRpc or
any dependency needed by an alternative backend.

## StreamJsonRpc adapter

A consumer may compile an internal `StreamJsonRpcMtpConnection` in its own assembly. It implements
`IMtpRpcConnection` and references StreamJsonRpc through the consumer's existing dependencies. The adapter:

1. Creates a StreamJsonRpc `JsonRpc` instance over the supplied duplex stream.
2. Registers local targets for MTP notifications and server requests.
3. Maps request/notification parameter objects to the source package's raw dictionaries and maps results back to
   `ResponseMessage`.
4. Starts listening only when `Start` is called.
5. Owns the `JsonRpc` instance and any stream ownership explicitly transferred to it.

StreamJsonRpc must not expose its native request task directly. StreamJsonRpc can observe a response while an
earlier notification callback is still running, which would violate the MTP client contract.

### Ordered dispatch contract

Every `IMtpRpcConnection` implementation must provide this guarantee:

> Inbound notifications, server requests, and response-completion markers are processed in wire order on one
> non-concurrent dispatch pipeline. A notification handler is invoked synchronously within that pipeline. A
> request task completes only when its response marker reaches the pipeline, after all earlier notification
> handlers have returned.

Consequently, when a server writes zero or more `testing/testUpdates/tests` notifications followed by the
terminal discover/run response, awaiting the client operation proves that every preceding `TestNodesUpdated`
handler has already run. Implementations must not schedule notification handlers as detached work.

The StreamJsonRpc adapter satisfies the contract with both:

- a `NonConcurrentSynchronizationContext` used by StreamJsonRpc target invocation, and
- a single FIFO ordered dispatch queue shared by notifications, server requests, and response markers.

For each outbound request, the adapter awaits the native StreamJsonRpc request internally. When the native
response arrives, it enqueues a response marker rather than completing the public task immediately. Processing
that marker completes the adapter-owned `TaskCompletionSource<ResponseMessage>`. Notification queue entries call
`NotificationReceived` inline and do not complete until all handlers return. The non-concurrent synchronization
context prevents StreamJsonRpc from concurrently entering adapter targets; the FIFO queue provides the explicit
barrier between target callbacks and response completion.

Handlers must retain the existing restriction: they must not block for a long time or synchronously issue and
wait for another client request, because that would stall the ordered pipeline.

## Cancellation and `$/cancelRequest`

Cancellation is part of the interface contract rather than a backend-specific convenience:

- If the token is already canceled before a request is sent, `SendRequestAsync` throws without writing a request
  or a cancellation notification.
- After a request id has been allocated and its frame has started, caller cancellation completes the public task
  with that token and sends exactly one best-effort `$/cancelRequest` containing the same request id.
- Cancellation must not interrupt a partially written frame. Implementations finish the current frame before
  writing `$/cancelRequest`, preserving framing integrity.
- Failure to send `$/cancelRequest` does not replace the caller's cancellation result.
- A late response to a canceled request is drained and ignored.
- Connection shutdown fails all non-canceled pending requests with the terminal connection/disposal exception.
- The token supplied to `ServerRequestHandler` represents connection/read-loop lifetime. The current protocol has
  no server-to-client equivalent of `$/cancelRequest`; if one is added later, the interface contract can be
  extended to link per-request cancellation.

For StreamJsonRpc, the adapter owns JSON-RPC request ids or uses an API that exposes the id so the cancellation
notification contains the exact correlated id. If the selected StreamJsonRpc API cannot expose or control that
id, the adapter cannot satisfy this interface and must use a lower-level message layer rather than approximating
cancellation.

## Verification required with the future refactor

The implementation PR should run the existing connection and client suites unchanged against
`MtpJsonRpcConnection`, then add a reusable connection contract suite covering:

- idempotent start and disposal,
- ordered notification delivery,
- the discover/run response barrier,
- server-request response handling,
- pre-send and in-flight cancellation,
- one correctly correlated `$/cancelRequest`,
- late-response draining, and
- pending-request failure on transport shutdown.

A consumer-side StreamJsonRpc adapter should run the same contract suite. The interface refactor is not complete
until both backends demonstrate identical ordering and cancellation behavior.
