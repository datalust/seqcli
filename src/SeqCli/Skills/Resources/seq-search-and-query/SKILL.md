---
name: seq-search-and-query
description: Search and query logs and spans in Seq. Use when interacting with Seq.
license: Apache-2.0
metadata:
  author: Datalust and Contributors
---

Seq is a database containing log and trace telemetry. Search Seq to retrieve matching log events and spans. Query Seq to
compute tabular, aggregate results from the same data.

## Data Model

All events stored in Seq use the same data model. Spans are only distinguished from log events by the presence of the
`@Start` property. The following built-in properties are supported. The type column uses `?` to indicate properties that
may be undefined for some events.

| Built in property name | Type      | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|------------------------|-----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `@Arrived`             | `number`  | An integer indicating the order in which the event arrived at the Seq server relative to other events in the same batch.                                                                                                                                                                                                                                                                                                                                               |
| `@Data`                | `object`  | A compact internal representation of the event as a single structured object.                                                                                                                                                                                                                                                                                                                                                                                          |
| `@Elapsed`             | `number?` | The elapsed duration of a span, expressed in 100 nanosecond ticks. This is in the same domain as Seq's duration literals such as `1s`, `23ms`, or `3d`. Only present on spans, not present on log events.                                                                                                                                                                                                                                                              |
| `@EventType`           | `number`  | A numeric hash of the message template that was used to generate the event. The message template itself is in the `@MessageTemplate` property.                                                                                                                                                                                                                                                                                                                         |
| `@Exception`           | `string?` | The exception associated with the event if any, as a string. This normally incorporates the exception type, message, and stack trace.                                                                                                                                                                                                                                                                                                                                  |
| `@Id`                  | `string`  | The event's unique id in Seq.                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `@Level`               | `string`  | The severity of a log event, or completion status of a span. Values are source-dependent, so for example `'Error'`, `'error'`, and `'err'` would all be typical values.                                                                                                                                                                                                                                                                                                |
| `@Message`             | `string`  | Human-readable text associated with the event. This is often the result of substituting `@Properties` values into `@MessageTemplate`. For spans, this property carries the span name.                                                                                                                                                                                                                                                                                  |
| `@MessageTemplate`     | `string`  | A message template, following the `messagetemplates.org` syntax. Message templates collectively identify events generated from the same line of logging/tracing code.                                                                                                                                                                                                                                                                                                  |
| `@ParentId`            | `string?` | The `@SpanId` of the parent of a given span, if any. The parent span will always belong to the same trace, that is, share a `@TraceId` value. Only present on spans, not log events.                                                                                                                                                                                                                                                                                   |
| `@Properties`          | `object?` | An object containing the user-defined properties of a log event or span. Properties with names that are valid C-style identifiers can be accessed implicitly, so `RequestPath` is syntactically equivalent to `@Properties['RequestPath']`. Properties generally conform to naming conventions used throughout the Seq server - sometimes simple PascalCase names, and at other times using the OpenTelemetry semantic conventions. See also `@Resource` and `@Scope`. |
| `@Resource`            | `object?` | For an OpenTelemetry log event or span, the properties associated with the OpenTelemetry resource. These may follow the OTel semantic conventions, but may also be domain-specific or user-defined.                                                                                                                                                                                                                                                                    |
| `@Scope`               | `object?` | For an OpenTelemetry log event or span, the properties associated with the OpenTelemetry scope. These may match definitions in the OTel semantic conventions, but may also be domain-specific or user-defined.                                                                                                                                                                                                                                                         |
| `@SpanId`              | `string?` | The W3C span id that uniquely identifies a span within a trace. Log events recorded during the span carry the same `@SpanId` value as the span itself.                                                                                                                                                                                                                                                                                                                 |
| `@SpanKind`            | `string?` | The OpenTelemetry span kind. Only present on spans, not log events.                                                                                                                                                                                                                                                                                                                                                                                                    |
| `@Start`               | `number?` | The time at which the span started. The difference between the start time and `@Timestamp` is the `@Elapsed` time of the span. In the same units as Seq's duration literal syntax.                                                                                                                                                                                                                                                                                     |
| `@Timestamp`           | `number`  | The time at which an event was recorded (completion time, for spans). Carried on all log events and spans. In the same units as Seq's duration literal syntax.                                                                                                                                                                                                                                                                                                         |
| `@TraceId`             | `string?` | The W3C trace id that uniquely identifies a trace. All spans and log events within a trace carry the same trace id value.                                                                                                                                                                                                                                                                                                                                              |

## Type System

Stored data and intermediate values in expression evaluation are typed dynamically. Values are one of the following types.

| Type name  | Description                                                            | Example literals                                                             |
|------------|------------------------------------------------------------------------|------------------------------------------------------------------------------|
| **null**   | The atom `null`. Null is a value in Seq's type system.                 | `null`                                                                       |
| **bool**   | The atoms `true` and `false`.                                          | `true`, `false`                                                              |
| **number** | Decimal numbers with the range and precision of .NET's `decimal` type. | `0`, `12.34`, `56ms`, `DateTime('2026-05-29T10:56:01.43278Z')`, `0xa1b234ff` |
| **array**  | An ordered array of values.                                            | `[]`, `[17, null, {a: 'test'}]`                                              |
| **object** | An unordered set of name/value pairs.                                  | `{}`, `{a: 'test', 'b c': 17, d: []}`                                        |

In expression evaluation, Seq does not perform any type coercion. Functions and operators that receive invalid arguments
evaluate to _undefined_, which is the absence of a value (_undefined_ has roughly the same semantics as `NULL` in standard SQL).

## Scalar Functions

These built-in functions and operators work with individual values. See Aggregate Functions for information on functions like count() and distinct() that work with sets of values.

| Function signature | Description | Result type |
| --- | --- | --- |
| `Arrived(eventId)` | Evaluates to the arrival order encoded in eventId. The arrival order is a hint that preserves the order of events from the same source that have the same timestamp. If any argument is null, the result is undefined. | `number` |

Bucket(number, err)

Reduce precision by computing the midpoint of the closest logarithmic bucket. If any argument is non-numeric, the result is undefined.

Result type: number

Example

Bucket(3.141592, 0.001)

Coalesce(arg0, arg1, ...)

Evaluates to the first defined, non-null argument. If no argument meets this requirement, Coalesce returns the value of its final argument.

Result type: any
Concat(str0, str1, ...)

Concatenate all string arguments. No type coercion is performed: the result is undefined if any argument is not a string. If any argument is null, the result is undefined.

Result type: any
Contains(text, substring)

Evaluates to true if text contains substring. Accepts a /regular expression/ in place of substring. If any argument is null, the result is undefined.

Result type: bool
Supports the ci modifier?: Yes
DatePart(datetime, part, offset)

Compute the value of part for the date/time datetime at time zone offset offset. Both datetime and offset are 100-nanosecond tick values. If part is not a string, or not a recognized part name, the result is undefined. See the documentation section on date and time handling for more information. If any argument is null, the result is undefined.

Result type: number

Example

DatePart(Now(), 'weekday', OffsetIn('Australia/Brisbane', Now()))

DateTime(str)

Attempt to parse the date/time value encoded in the string str. If the value cannot be parsed as a date/time, the result is undefined. If any argument is null, the result is undefined.

Result type: number
ElementAt(collection, index)

Access the element of the array or object collection at the index or key index. If any argument is null, the result is undefined.

Result type: any
Supports the ci modifier?: Yes
EndsWith(text, substring)

Evaluates to true if text ends with substring. Accepts a /regular expression/ in place of substring. If any argument is null, the result is undefined.

Result type: bool
Supports the ci modifier?: Yes
Every(collection, predicate)

Evaluates to true if the function predicate evaluates to true for all elements of the array or object collection. If any argument is null, the result is undefined.

Result type: bool

Example

Every(['0.1', '0.1-pre'], |tag| StartsWith(tag, '0.'))

FromJson(json)

Parse the JSON-encoded string json. If json is not a string, or is not valid JSON, the result is undefined. This function has a high runtime cost and should be avoided when possible. If any argument is null, the result is undefined.

Result type: any
Has(arg)

Evaluates to true if arg is defined. Otherwise, if arg is undefined, the result is false.

Result type: bool
IndexOf(text, substring)

Return the zero-based index of the first occurrence of substring in text. Accepts a /regular expression/ in place of substring. If substring is not present in text, the result is -1. If any argument is null, the result is undefined.

Result type: number
Supports the ci modifier?: Yes
Keys(obj)

Evaluates to an array containing the keys of the object obj. The result is undefined if obj is not an object. If any argument is null, the result is undefined.

Result type: array
LastIndexOf(text, substring)

Return the zero-based index of the last occurrence of substring in text. Accepts a /regular expression/ in place of substring. If substring is not present in text, the result is -1. If any argument is null, the result is undefined.

Result type: number
Supports the ci modifier?: Yes
Length(arg)

Evaluates to the length of the string or array arg. If any argument is null, the result is undefined.

Result type: number
Now()

Evaluates to the current time, as 100-nanosecond ticks since 00:00:00 on 0001-01-01. If any argument is null, the result is undefined.

Result type: number
OffsetIn(timezone, instant)

Determine the offset from UTC in time zone timezone at instant instant. The time zone name must be an IANA time zone name. The instant value is specified in 100-nanosecond ticks. See the documentation section on date and time handling for more information. If any argument is null, the result is undefined.

Result type: number

Example

OffsetIn('Australia/Brisbane', Now())

Replace(text, substring, replacement)

Replace all occurrences of substring in text with replacement. Accepts a /regular expression/ in place of substring, in which case replacement may use $0 to refer to the match, $1 the first capturing group, and so on. Regular expression replacements use $$ to escape a single dollar sign. If any argument is null, the result is undefined.

Result type: number
Supports the ci modifier?: Yes
Round(value, places)

Round value to specified number of decimal places. Midpoint values (0.5) are rounded up. If any argument is non-numeric, the result is undefined.

Result type: number

Example

// Evaluates to 123.5
Round(123.456, 1)

Some(collection, predicate)

Evaluates to true if the function predicate evaluates to true for any element of the array or object collection. If any argument is null, the result is undefined.

Result type: bool

Example

Some(['0.1', '0.1-pre'], |tag| EndsWith(tag, '-pre'))

StartsWith(text, substring)

Evaluates to true if text starts with substring. Accepts a /regular expression/ in place of substring. If any argument is null, the result is undefined.

Result type: bool
Supports the ci modifier?: Yes
Substring(str, start, length)

Evaluates to the substring of string str from the zero-based index start, of length characters. If length is not specified, or exceeds the number of characters remaining after start, the result is the remainder of the string. The result is undefined if start is out of bounds, or if either start or length is negative. If any argument is undefined, the result is undefined.

Result type: any
TimeOfDay(datetime, offsetHours)

Compute the time of day of the date/time datetime in the time zone offset offsetHours. If any argument is non-numeric, the result is undefined.

Result type: number

Example

TimeOfDay(Now(), -7)

TimeSpan(str)

Attempt to parse the d.HH:mm:ss.f formatted time value encoded in the string str. If the value cannot be parsed as a time, the result is undefined. If any argument is null, the result is undefined.

Result type: number

Example

TimeSpan('1.1:59:59.123')

ToEventType(str)

Compute the event type that Seq automatically assigns to @EventType from the message template str. If any argument is null, the result is undefined.

Result type: any
ToHexString(num)

Format num as a hexadecimal string, including leading 0x. Decimal digits are discarded. If any argument is non-numeric, the result is undefined.

Result type: string
ToIsoString(datetime, offset)

Format datetime as an ISO-8601 string, with an optional time zone offset. Both the datetime and offset arguments are interpreted as 100-nanosecond ticks since the epoch. If any argument is non-numeric, the result is undefined. The offset argument defaults to 0, or UTC. Output is given with the UTC or full time zone designator, i.e. Z or ±hh:mm. If any argument is non-numeric, the result is undefined.

Result type: string

Example

ToIsoString(DateTime('2023-12-20'), 10h)

ToJson(arg)

Convert the value arg to JSON. If the argument is undefined, the result is undefined. Can be used to convert a value (e.g. number) to a string. If any argument is undefined, the result is undefined.

Result type: string
ToLower(str)

Convert string str to lowercase. To compare strings in a case-insensitive manner, use the equality operator and ci modifier instead. The result is undefined if str is not a string. If any argument is null, the result is undefined.

Result type: string
ToNumber(str)

Parse string str as a number. If any argument is null, the result is undefined.

Result type: number
TotalMilliseconds(timespan)

Evaluates to the total number of milliseconds represented by the time span timespan. If timespan is a number, it will be interpreted as containing 100-nanosecond ticks. If timespan is a string, it will be parsed in the same manner as performed by TimeSpan(). If any argument is null, the result is undefined.

Result type: number

Example

TotalMilliseconds(1s)

ToTimeString(timespan)

Format timespan as an d.HH:mm:ss.f string. The timespan argument is interpreted as 100-nanosecond ticks. The inverse of TimeSpan. If any argument is non-numeric, the result is undefined.

Result type: string

Example

ToTimeString(1h)

ToUpper(str)

Convert string str to uppercase. To compare strings in a case-insensitive manner, use the equality operator and ci modifier instead. The result is undefined if str is not a string. If any argument is null, the result is undefined.

Result type: string
TypeOf(arg)

Returns the type of value, either 'object', 'array', 'string', 'number', 'bool', 'null', or 'undefined'.

Result type: string
Values(obj)

Evaluates to an array containing the values of the members of object obj. The result is undefined if obj is not an object. If any argument is null, the result is undefined.

Result type: array
Operator -

Subtract one number from another. If any argument is non-numeric, the result is undefined.

Result type: number
Operator - (prefix)

Negate a number. If any argument is non-numeric, the result is undefined.

Result type: number
Operator *

Multiply two numbers. If any argument is non-numeric, the result is undefined.

Result type: number
Operator /

Divide one number by another. If the right-hand operand is zero, the result is undefined. If any argument is non-numeric, the result is undefined.

Result type: number
Operator %

Compute the remainder after dividing the left-hand operand by the right. If the right-hand operand is zero, the result is undefined. If any argument is non-numeric, the result is undefined.

Result type: number
Operator ^

Raise a number to the specified power. If any argument is non-numeric, the result is undefined.

Result type: number
Operator +

Add two numbers. If any argument is non-numeric, the result is undefined.

Result type: number
Operator <

Compare two numbers and return true if the left-hand operand is less than the right-hand operand. If any argument is non-numeric, the result is undefined.

Result type: bool
Operator <=

Compare two numbers and return true if the left-hand operand is less than or equal to the right-hand operand. If any argument is non-numeric, the result is undefined.

Result type: bool
Operator <>

Compare two values, returning true if the values are unequal, and false otherwise. Structural comparison is supported, so the values may be of any type including objects and arrays. If the right-hand operand is a /regular expression/, the result is true if the left-hand operand is a string that is an exact match for the regular expression. If any argument is undefined, the result is undefined.

Result type: any
Supports the ci modifier?: Yes
Operator =

Compare two values, returning true if the values are equal, and false otherwise. Structural comparison is supported, so the values may be of any type including objects and arrays. If any argument is undefined, the result is undefined.

Result type: any
Supports the ci modifier?: Yes
Operator >

Compare two numbers and return true if the left-hand operand is greater than the right-hand operand. If any argument is non-numeric, the result is undefined.

Result type: bool
Operator >=

Compare two numbers and return true if the left-hand operand is greater than or equal to the right-hand operand. If any argument is non-numeric, the result is undefined.

Result type: bool
Operator and

The logical AND operator. The result of a and b is true if and only if both a and b are true; the result is otherwise false. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.

Result type: bool
Operator not (prefix)

Logical NOT. Evaluates to true only if the operand is false, or if the operand is undefined. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.

Result type: bool
Operator or

The logical OR operator. The result of a or b is true if either a or b is true; otherwise the result is false. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.

Result type: bool

## Aggregate Functions (Queries Only)

## Cheat Sheet


## Grammar

### Expressions

### Queries


## Gotchas

 - Seq expression literals are not JSON, take care to use the Seq expression syntax when formatting literal values.
 - Seq queries are not SQL. Don't expect standard SQL syntax, operators, or semantics to apply, always use the grammar
   and built-ins described above.
 - Seq searches work backwards through the event stream and always return results in reverse-chronological order, from
   **most recent** to least recent.
 - Data in Seq servers don't always use OpenTelemetry semantic conventions. When searching or querying, only use property
   names from the built-ins described above, that appear on search results, or that are returned from the search result
   schema tool.
 - Bare identifiers like `SomeName` are synonymous with `@Properties['SomeName']`. The latter form allows irregular names
   to be used.
 - The only escape sequence allowed and required in Seq strings is a doubled single quote - `''` - which evaluates to an 
   embedded literal single quote. Backslash escaping is not recognized.
 - `@Timestamp`, `@Start`, and `@Elapsed` are internally represented as .NET `DateTime` ticks (`ulong` with 100 ns 
   resolution) in order to support consistent timestamp/duration math. Comparing these properties with strings will 
   fail: use duration literals for durations, and the `DateTime` function
   to convert from ISO-8601 strings.
 - Although Seq's types resemble those from JavaScript, Seq does not support JavaScript operators and does not use 
   JavaScript's system of comparisons.
 - The expression `null = null` is `true` in Seq's type system; `null` is just a regular value.
 