---
applyTo: "src/**/*.rs,apps/**/src/**/*.rs,services/**/src/**/*.rs"
scope: "Rust source that may own asynchronous service work"
---

# Rust asynchronous services

Apply these rules when the code owns long-running tasks, listeners, workers, or
request processing.

- Give every spawned task an owner, shutdown signal, and observed completion. Avoid
  detached `tokio::spawn` work whose error and lifetime are lost.
- Propagate cancellation from the service root and wait for child tasks during
  graceful shutdown with a bounded deadline.
- Use bounded channels, semaphores, or admission limits where producers can outpace
  consumers. Unbounded queues are not a backpressure strategy.
- Do not hold synchronous or asynchronous locks across `.await` unless the protected
  invariant explicitly requires it and contention is understood.
- Move blocking filesystem, process, database-driver, or CPU-heavy work off async
  executor threads with the appropriate blocking boundary. Keep `spawn_blocking` work
  bounded and terminating; use a dedicated owned thread for a long-lived blocking loop.
- Put timeouts at external I/O and protocol boundaries. Distinguish timeout,
  cancellation, peer shutdown, and permanent failure.
- Design `select!` branches so cancellation cannot leave partially committed state.
- Test shutdown, saturation, task failure, and timeout behavior; do not validate async
  behavior with arbitrary sleeps.
