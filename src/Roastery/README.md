# Roastery sample ☕

This project simulates a simple web application for ordering
coffee beans.

Customers place orders through the website, and staff fulfill orders
by shipping them and updating the site.

Back-end batch processes detect abandoned orders, and archive fulfilled 
orders so that the simulation doesn't exhaust memory :-).

The simulation produces events from an MVC-like web stack and a
SQL database.

At various points, failures are randomly injected so that diagnostic
data can be produced.

## How the simulation works

The simulation centers on a single instance of an in-memory data 
structure modelling the HTTP stack from client through to MVC-style
"controllers" on the server-side:

`HttpClient` &rarr;`Middleware` &rarr;`Router` &rarr; `Controller`

The middleware and routing layer is based on a single interface,
`HttpServer`, with a method accepting `HttpRequest` and returning
`HttpResponse`.

The subclasses of `Controller` interact with the in-memory `Database`
instance, which maintains a map of ids to "persistent" entities like
`Product` and `Order`.

The simulation is driven by a range of `Actors` that run concurrently,
each manipulating the simulation's data by sending requests through
the `HttpClient`.

## `HttpClient`

The `HttpClient` is a thin wrapper over the end of the middleware
pipeline; it acts primarily as a factory for `HttpRequest`s for each
HTTP method.

The `HttpClient` itself doesn't do any logging, as it's outside of
the view of the server and does double duty in the role of mock web
browser.

## Middleware and fault-injection

Each element in the HTTP pipeline is modeled by a subclass of:

```csharp
abstract class HttpServer
{
    public abstract Task<HttpResponse> InvokeAsync(HttpRequest request);
}
```

Between the `HttpClient` that consumes the outermost middleware layer,
and the `Router` that dispatches requests to controllers, we have:

 * `NetworkLatencyMiddleware` &mdash; injects a random delay between
   the client sending a request, and the server-side middleware receiving it
 * `RequestLoggingMiddleware` &mdash; emits an event with timing 
   information at the completion of each request, including exception
   details if the request failed
 * `FaultInjectionMiddleware` &mdash; randomly fails requests by throwing
   various exceptions
 * `SchedulingLatencyMiddleware` &mdash; slows request processing down,
   and more so when there are multiple requests executing concurrently

The `RequestLoggingMiddleware` pushes `RequestId` onto the log context;
this tags all events generated during a request with the same id. A user
can then follow the request id to the final request completion event to
examine full HTTP request and timing information in a single event.

## Routing and controllers

The `Router` middleware is the end of the middleware chain; it examines
the path of incoming requests, and invokes action methods it discovers
on `Controller` subclasses.

Route templates have a very simple substitution system: only `{id}` is
treated as a wildcard, and its value isn't (currently) extracted or
presented to the controller once a route has been selected.

The router pushes the `Controller` and `Action` properties onto the log
context to give some notion of the cause of events like database
operations deeper in the stack. We might in future emulate Serilog's
`IDiagnosticContext` and set these properties there instead.

Action methods are decorated with the `[Route]` attribute, are 
asynchronous, accept `HttpRequest`, and return `HttpResponse`.

Controllers tend to log domain-level events: _an order was placed_ and
so-on.

## The database

The simulated database is a thread-safe in-memory dictionary.

Operations act on the dictionary using simple operations and predicates,
with additional fake SQL generated and logged to illustrate what's
going on in the 

Entities are cloned when they're inserted and when they're retrieved,
so that some hygiene is maintained.

## `Actor`s

All of the activity in the simulation is triggered by subclasses of
`Actor` like `Customer` or `WarehouseStaff`.

Actors have _behaviors_ - methods with the signature:

```csharp
Task Behavior(CancellationToken cancellationToken);
```

The base `Actor` class periodically invokes the actor's behaviors,
and the behaviors execute a workflow like creating and placing a new
coffee order.

The actors that represent back-end batch processes log their
activities, generally including domain identifiers like `OrderId` for
some correlation. We might in future use the batch processes as a
way to illustrate tracing across application boundaries.

### See also...

This is the data set relied upon by the entities created with
`seqcli sample setup` - if the log event structure is changed, the
templates will likely need updating, too.
