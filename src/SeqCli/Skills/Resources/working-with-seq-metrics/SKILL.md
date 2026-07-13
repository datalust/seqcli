---
name: seq-search-and-query
description: Query metrics stored in Seq. Use this when monitoring data or diagnostic metric information is required.
license: Apache-2.0
metadata:
 author: Datalust and Contributors
---

Seq stores OpenTelemetry metric samples in the `series` data source, which is a columnar data store.

Metrics are accessed **strictly** using the following workflow:

 1. Search for metric definitions
 2. Note the _kind_ of any relevant metrics
 3. List the dimensions that apply to the metric
 4. Optionally, list the values present in dimensions of interest
 5. Run aggregate queries on metric samples using metric names, kinds, and dimensions discovered in the previous steps

**NEVER** query the `series` data source for metric definitions directly. **ALWAYS** use the dedicated definition search tools instead.

**NEVER** query the `series` data source directly for available dimensions. **ALWAYS** use the dedicated dimension listing tools instead.

**NEVER** list dimension values using the `distinct` aggregation. **ALWAYS** use the dedicated dimension value listing tools instead.

**NEVER** evaluate projection queries against the `series` data source. This risks impacting the performance of Seq for other
users and workloads.

## Searching for Metric Definitions

Metric samples carry an `@Definitions` object where metric names are keys into the object. Under each metric name key,
a `description` property is available.

Search for metrics with `cpu` in their **name** or **description** (case-insensitive):

```
Keys(@Definitions)[?] like '%cpu%' ci or @Definitions[?].description like '%cpu%' ci
```

Search for metrics reported by a specific service:

```
@Resource.service.name = 'accounting'
```

If metrics searches report syntax errors, you MUST load the `seq-search-and-query` skill.

## Listing Dimensions

Determine the dimensions that apply to a metric using the dimensions MCP tool or the `seqcli metrics dimensions` CLI
command.

## Listing Unique Dimension Values

DO NOT use `distinct` to query for the values of a metric dimension like `@Resource.service.name`. Always use the
dimension values MCP tool or `seqcli metrics dimensionvalues` CLI command.

## Querying Metric Samples

Metrics returned from definition searches are tagged with a kind:

* `Sum` - counter with delta aggregation temporality
* `Gauge` - gauge or counter with cumulative aggregation temporality
* `Fixed` - histogram with fixed buckets
* `Exponential` - histogram with exponential buckets

Correctly querying metric sample data from `series` relies on knowing the metric kind.

### `Sum` Query Example

```
select sum(app_product_review_counter) as reviews_submitted
from series
where Has(app_product_review_counter)
  and @Timestamp >= now() - 15m
group by time(1m)
```

### `Gauge` Query Examples

```
select max(dotnet.gc.last_collection.heap.size) as heap_size_bytes
from series 
where (Has(dotnet.gc.last_collection.heap.size))
  and @Resource.service.name = 'accounting'
  and @Timestamp >= now() - 15m
group by time(1m), @Resource.service.instance.id
having heap_size_bytes > 5000000
limit 100
```

### `Fixed` Query Examples

HTTP request timing percentiles:

```
select
  phist(coalesce((bucket.from + bucket.to) / 2, bucket.from, bucket.to), bucket.count, 95) as p95,
  phist(coalesce((bucket.from + bucket.to) / 2, bucket.from, bucket.to), bucket.count, 99) as p99
from series
  lateral unnest(http.server.request.duration.buckets) as bucket 
where Has(http.server.request.duration.buckets)
  and @Resource.service.name = 'cart'
  and @Scope.name = 'Microsoft.AspNetCore.Hosting'
  and @Timestamp >= now() - 15m
group by time(1m)
limit 100
```

HTTP request througput (counts):

```
select sum(http.server.request.duration.count) as request_throughput
from series
where Has(http.server.request.duration.buckets)
  and @Resource.service.name = 'cart'
  and @Scope.name = 'Microsoft.AspNetCore.Hosting'
  and @Timestamp >= now() - 15m
group by time(1m)
limit 100
```

Maximium HTTP request body size:

```
select max(http.server.request.body.size.max) as max_body_size
from series 
where Has(http.server.request.body.size.buckets)
  and @Resource.service.name = 'shipping'
  and @Scope.name = 'opentelemetry-instrumentation-actix-web'
  and @Timestamp >= now() - 15m
group by time(1m)
limit 100
```

### `Exponential` Query Examples

Commit timing percentiles:

```
select
  phist(bucket.midpoint, bucket.count, 95) as p95,
  phist(bucket.midpoint, bucket.count, 99) as p99
from series
  lateral unnest(commit_duration.buckets) as bucket 
where Has(commit_duration.buckets)
  and @Timestamp >= now() - 15m
group by time(1m)
limit 100
```

## Gotchas

- If metrics that should be _counters_ show up as _gauges_, aggregation temporality is likely to be misconfigured: danger! This can produce wildly inaccurate results if not detected.
- When writing queries that aggregate metric samples, `where Has(<metric name>)` must always be included to avoid row count skew.
- Do NOT use `select disctint(<dimension>)` with metric dimensions: aways use the optimized dimension value searches exposed by `seqcli` or the MCP tools.
- Group keys are automatically included in result rowsets and **must not** be explicitly included in the `select` list.
- To order by group keys, apply an alias with `group by <expr> as <alias>` and use `order by <alias>`. Never add the group key to the `select` list, this will fail.
