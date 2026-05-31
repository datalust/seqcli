---
name: seq-search-and-query
description: Search and query logs and spans in Seq. Use when interacting with Seq.
license: Apache-2.0
metadata:
  author: Datalust and Contributors
---

Seq is a storage service for log and trace telemetry. Search Seq to retrieve matching log events and spans. Query Seq to
compute tabular, aggregate results from the same data.

> This skill does not currently cover interactions with metrics (the `series` storage object).

## Data Model

All events stored in Seq use the same data model. Spans are only distinguished from log events by the presence of the
`@Start` property. The following built-in properties are supported.

| Built in property name | Type      | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|------------------------|-----------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `@Arrived`             | `number`  | An integer indicating the order in which the event arrived at the Seq server relative to other events in the same batch.                                                                                                                                                                                                                                                                                                                                               |
| `@Data`                | `object`  | A compact internal representation of the event as a single structured object.                                                                                                                                                                                                                                                                                                                                                                                          |
| `@Definitions`         | `object?` | Metadata attached to metric samples, not present on log events or spans.                                                                                                                                                                                                                                                                                                                                                                                               |
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

Stored data and intermediate values in expression evaluation are typed dynamically. Values are always one of the following types.

| Type name   | Description                                                            | Example literals                                                             |
|-------------|------------------------------------------------------------------------|------------------------------------------------------------------------------|
| **null**    | The atom `null`. Null is a value in Seq's type system.                 | `null`                                                                       |
| **boolean** | The atoms `true` and `false`.                                          | `true`, `false`                                                              |
| **number**  | Decimal numbers with the range and precision of .NET's `decimal` type. | `0`, `12.34`, `56ms`, `DateTime('2026-05-29T10:56:01.43278Z')`, `0xa1b234ff` |
| **array**   | An ordered array of values.                                            | `[]`, `[17, null, {a: 'test'}]`                                              |
| **object**  | An unordered set of name/value pairs.                                  | `{}`, `{a: 'test', 'b c': 17, d: []}`                                        |

In expression evaluation, Seq does not perform any type coercion.

The results of functions and operators that receive invalid arguments are undefined, which is the absence of a value 
(_undefined_ has roughly the same "poison" semantics as `NULL` in standard SQL).

Type notation in this document column uses the suffix `?` on a type name to indicate values that may be undefined.
The synthetic type name `any` is used as an alias for `null | boolean | number | array | object`.

## Scalar Functions and Operators

These built-in functions and operators work with individual values. See Aggregate Functions for information on functions like `count()` and `distinct()` that work with sets of values.

| Function signature                                                      | Description                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|-------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Arrived(eventId: string): number?`                                     | Evaluates to the arrival order encoded in `eventId`. The arrival order is a hint that preserves the order of events from the same source that have the same timestamp.                                                                                                                                                                                                                                                                         |
| `Bucket(n: number, err: number?): number`                               | Reduce precision of `n` by computing the midpoint of the closest logarithmic bucket. The optional `err` parameter specifies the maximum permissible error fraction.                                                                                                                                                                                                                                                                            |
| `Coalesce(arg0: any?, arg1: any?, ...): any?`                           | Evaluates to the first defined, non-`null` argument. If no argument meets this requirement, `Coalesce` returns the value of its final argument.                                                                                                                                                                                                                                                                                                |
| `Concat(str0: string, str1: string, ...): string`                       | Concatenate all string arguments. No type coercion is performed.                                                                                                                                                                                                                                                                                                                                                                               |
| `Contains(text: string, substring: string): boolean`                    | Evaluates to `true` if text contains substring. Accepts a `/regular expression/` in place of `substring`. Supports the `ci` modifier.                                                                                                                                                                                                                                                                                                          |
| `DatePart(datetime: number, part: string, offset: number): number?`     | Compute the value of `part` for the date/time `datetime` at time zone offset `offset`. Both `datetime` and `offset` are 100-nanosecond tick values. If `part` is not a recognized part name, the result is undefined. See the documentation section on date and time handling for more information.                                                                                                                                            |
| `DateTime(str: string): number?`                                        | Attempt to parse the date/time value encoded in the string `str`. If the value cannot be parsed as a date/time, the result is undefined.                                                                                                                                                                                                                                                                                                       |
| `ElementAt(collection: array \| object, index: number \| string): any?` | Access the element of the array or object `collection` at the index or key `index`. Supports the `ci` modifier.                                                                                                                                                                                                                                                                                                                                |
| `EndsWith(text: string, substring: string): boolean`                    | Evaluates to `true` if `text` ends with `substring`. Accepts a `/regular expression/` in place of `substring`. Supports the `ci` modifier.                                                                                                                                                                                                                                                                                                     |
| `Every(collection: array \| object, predicate): boolean`                | Evaluates to `true` if the function predicate evaluates to `true` for all elements of the array or object `collection`.                                                                                                                                                                                                                                                                                                                        |
| `FromJson(json: string): any?`                                          | Parse the JSON-encoded string `json`. If `json` is not valid JSON, the result is undefined. This function has a high runtime cost and should be avoided when possible.                                                                                                                                                                                                                                                                         |
| `Has(arg: any?): boolean`                                               | Evaluates to `true` if `arg` is defined. Otherwise, if `arg` is undefined, the result is `false`.                                                                                                                                                                                                                                                                                                                                              |
| `IndexOf(text: string, substring: string): number`                      | Return the zero-based index of the first occurrence of `substring` in `text`. Accepts a `/regular expression/` in place of `substring`. If substring is not present in text, the result is `-1`. Supports the `ci` modifier.                                                                                                                                                                                                                   |
| `Keys(obj: object): array`                                              | Evaluates to an array containing the keys of the object `obj`.                                                                                                                                                                                                                                                                                                                                                                                 |
| `LastIndexOf(text: string, substring: string): number`                  | Return the zero-based index of the last occurrence of `substring` in `text`. Accepts a `/regular expression/` in place of substring. If substring is not present in text, the result is `-1`. Supports the `ci` modifier.                                                                                                                                                                                                                      |
| `Length(arg: string \| array): number`                                  | Evaluates to the length of the string or array `arg`.                                                                                                                                                                                                                                                                                                                                                                                          |
| `Now(): number`                                                         | Evaluates to the current time, as 100-nanosecond ticks since 00:00:00 on 0001-01-01.                                                                                                                                                                                                                                                                                                                                                           |
| `OffsetIn(timezone: string, instant: number): number?`                  | Determine the offset from UTC in time zone `timezone` at instant `instant`. The time zone name must be an IANA time zone name. The instant value is specified in 100-nanosecond ticks. See the documentation section on date and time handling for more information.                                                                                                                                                                           |
| `Replace(text: string, substring: string, replacement: string): string` | Replace all occurrences of `substring` in `text` with `replacement`. Accepts a `/regular expression/` in place of `substring`, in which case replacement may use `$0` to refer to the match, `$1` the first capturing group, and so on. Regular expression replacements use `$$` to escape a single dollar sign. Supports the `ci` modifier.                                                                                                   |
| `Round(value: number, places: number): number`                          | Round `value` to specified number of decimal places. Midpoint values (0.5) are rounded up.                                                                                                                                                                                                                                                                                                                                                     |
| `Some(collection: array \| object, predicate): boolean`                 | Evaluates to `true` if the function predicate evaluates to `true` for any element of the array or object `collection`.                                                                                                                                                                                                                                                                                                                         |
| `StartsWith(text: string, substring: string): boolean`                  | Evaluates to `true` if text starts with substring. Accepts a `/regular expression/` in place of `substring`. Supports the `ci` modifier.                                                                                                                                                                                                                                                                                                       |
| `Substring(str: string, start: number, length: number?): string?`       | Evaluates to the substring of string `str` from the zero-based index `start`, of `length` characters. If `length` is not specified, or exceeds the number of characters remaining after `start`, the result is the remainder of the string. The result is undefined if `start` is out of bounds, or if either `start` or `length` is negative.                                                                                                 |
| `TimeOfDay(datetime: number, offsetHours: number): number`              | Compute the time of day of the date/time `datetime` in the time zone offset `offsetHours`.                                                                                                                                                                                                                                                                                                                                                     |
| `TimeSpan(str: string): number?`                                        | Attempt to parse the `d.HH:mm:ss.f` formatted time value encoded in the string `str`. If the value cannot be parsed as a time, the result is undefined.                                                                                                                                                                                                                                                                                        |
| `ToEventType(str: string): any`                                         | Compute the event type that Seq automatically assigns to `@EventType` from the message template `str`.                                                                                                                                                                                                                                                                                                                                         |
| `ToHexString(num: number): string`                                      | Format `num` as a hexadecimal string, including leading `0x`. Decimal digits are discarded.                                                                                                                                                                                                                                                                                                                                                    |
| `ToIsoString(datetime: number, offset: number?): string`                | Format `datetime` as an ISO-8601 string, with an optional time zone offset. Both the datetime and offset arguments are interpreted as 100-nanosecond ticks since the epoch. The offset argument defaults to 0, or UTC. Output is given with the UTC or full time zone designator, i.e. Z or ±hh:mm.                                                                                                                                            |
| `ToJson(arg: any): string`                                              | Convert the value `arg` to JSON. Can be used to convert a value (e.g. number) to a string.                                                                                                                                                                                                                                                                                                                                                     |
| `ToLower(str: string): string`                                          | Convert string `str` to lowercase. To compare strings in a case-insensitive manner, use the equality operator and `ci` modifier instead.                                                                                                                                                                                                                                                                                                       |
| `ToNumber(str: string): number?`                                        | Parse string `str` as a number.                                                                                                                                                                                                                                                                                                                                                                                                                |
| `TotalMilliseconds(timespan: number \| string): number?`                | Evaluates to the total number of milliseconds represented by the time span `timespan`. If `timespan` is a number, it will be interpreted as containing 100-nanosecond ticks. If `timespan` is a string, it will be parsed in the same manner as performed by `TimeSpan()`.                                                                                                                                                                     |
| `ToTimeString(timespan: number): string`                                | Format `timespan` as an `d.HH:mm:ss.f` string. The `timespan` argument is interpreted as 100-nanosecond ticks. The inverse of TimeSpan.                                                                                                                                                                                                                                                                                                        |
| `ToUpper(str: string): string`                                          | Convert string `str` to uppercase. To compare strings in a case-insensitive manner, use the equality operator and `ci` modifier instead.                                                                                                                                                                                                                                                                                                       |
| `TypeOf(arg: any?): string`                                             | Returns the type of value, either `'object'`, `'array'`, `'string'`, `'number'`, `'boolean'`, `'null'`, or `'undefined'`.                                                                                                                                                                                                                                                                                                                      |
| `Values(obj: object): array`                                            | Evaluates to an array containing the values of the members of object `obj`.                                                                                                                                                                                                                                                                                                                                                                    |
| Operator `-`                                                            | Subtract one number from another. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                                                                                     |
| Operator `-` (prefix)                                                   | Negate a number. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                                                                                                      |
| Operator `*`                                                            | Multiply two numbers. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                                                                                                 |
| Operator `/`                                                            | Divide one number by another. If the right-hand operand is zero, the result is undefined. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                             |
| Operator `%`                                                            | Compute the remainder after dividing the left-hand operand by the right. If the right-hand operand is zero, the result is undefined. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                  |
| Operator `^`                                                            | Raise a number to the specified power. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                                                                                |
| Operator `+`                                                            | Add two numbers. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                                                                                                      |
| Operator `<`                                                            | Compare two numbers and return `true` if the left-hand operand is less than the right-hand operand. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                   |
| Operator `<=`                                                           | Compare two numbers and return `true` if the left-hand operand is less than or equal to the right-hand operand. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                       |
| Operator `<>`                                                           | Compare two values, returning `true` if the values are unequal, and `false` otherwise. Structural comparison is supported, so the values may be of any type including objects and arrays. If the right-hand operand is a /regular expression/, the result is `true` if the left-hand operand is a string that is an exact match for the regular expression. If any argument is undefined, the result is undefined. Supports the `ci` modifier. |
| Operator `=`                                                            | Compare two values, returning `true` if the values are equal, and `false` otherwise. Structural comparison is supported, so the values may be of any type including objects and arrays. If any argument is undefined, the result is undefined. Supports the `ci` modifier.                                                                                                                                                                     |
| Operator `>`                                                            | Compare two numbers and return `true` if the left-hand operand is greater than the right-hand operand. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                                |
| Operator `>=`                                                           | Compare two numbers and return `true` if the left-hand operand is greater than or equal to the right-hand operand. If any argument is non-numeric, the result is undefined.                                                                                                                                                                                                                                                                    |
| Operator `and`                                                          | The logical AND operator. The result of `a and b` is `true` if and only if both `a` and `b` are `true`; the result is otherwise `false`. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.                                                                                                                                                                                                              |
| Operator `not` (prefix)                                                 | Logical NOT. Evaluates to `true` if the operand is `false`, or if the operand is undefined. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.                                                                                                                                                                                                                                                           |
| Operator `or`                                                           | The logical OR operator. The result of `a or b` is `true` if either `a` or `b` is `true`; otherwise the result is `false`. Type coercion is not performed. If any argument is non-Boolean, the result is undefined.                                                                                                                                                                                                                            |
| Operator `like`                                                         | Determine if the left-hand operand is a string matching the right-hand pattern. The pattern can contain `%` and `?` wildcards for zero-or-many, or zero-or-one characters. `%` and `?` are escaped by doubling. The inverse `not like` is also supported.                                                                                                                                                                                      |
| Operator `is null`                                                      | Determine if the left-hand operand is `null` or undefined. The result is always a defined `boolean`. The inverse `is not null` is also supported.                                                                                                                                                                                                                                                                                              |
| Operator `in`                                                           | Determine if the left-hand operand is an element of the right-hand array.                                                                                                                                                                                                                                                                                                                                                                      |

## Aggregate Functions

`select` queries (see grammar below) have access to the following aggregate functions.

| Aggregate function signature                                   | Description                                                                                                                                                                                                                       | Example                               |
|----------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------|
| `all(expr: boolean): boolean`                                  | Return `true` if `expr` is `true` for all events in the stream.                                                                                                                                                                   | `all(@Level = 'Error')`               |
| `any(expr: boolean): boolean`                                  | Return `true` if `expr` is `true` for any event in the stream.                                                                                                                                                                    | `any(@Level = 'Error')`               |
| `bottom(expr: any, n: number): rowset`                         | Compute the last `n` values that appear for `expr`. The `bottom` function cannot appear with any other aggregate functions. The default ordering for `select` queries on `stream` is time-ascending. See also: `top()`, `first()`, `last()`. | `bottom(StatusCode, 5)`               |
| `count(property: any): number`                                 | Computes the number of events that have a non-`null` value for `property`. The special property name `*` can be used to count all events.                                                                                         | `count(*)`                            |
| `distinct(expr: any): rowset`                                  | Computes the set of distinct values for `expr`. The `distinct` function cannot appear with any other aggregate functions. `distinct()` and `count()` can be combined to count distinct values without returning them all.         | `distinct(ExceptionType)`             |
| `first(expr: any): any`                                        | Returns the value of `expr` applied to the first events in the target range.                                                                                                                                                      | `first(Elapsed)`                      |
| `last(expr: any): any`                                         | Returns the value of `expr` applied to the last events in the target range.                                                                                                                                                       | `last(Elapsed)`                       |
| `interval(): number`                                           | In a query that groups by time, the duration of each time slice.                                                                                                                                                                  | `count(*) / (interval() / 1d)`        |
| `min(expr: number): number`                                    | Computes the smallest value for `expr`.                                                                                                                                                                                           | `min(Elapsed / 1000)`                 |
| `max(expr: number): number`                                    | Computes the largest value for `expr`.                                                                                                                                                                                            | `max(Elapsed / 1000)`                 |
| `mean(expr: number): number`                                   | Computes the arithmetic mean (average) of `expr`, i.e. `sum(expr) / count(expr)`. Events where the expression is `null` or not numeric are ignored and do not contribute to the final result.                                     | `mean(ItemCount)`                     |
| `percentile(expr: number, p: number [, err: number?]): number` | Given a percentage `p`, calculates the value of `expr` at or below which `p` percent of the results fall. The optional `err` parameter specifies the maximum permissible error fraction. Higher error values reduce compute and memory resource consumption. | `percentile(ResponseTime, 95 , 0.01)` |
| `sum(expr: number): number`                                    | Calculates the sum of `expr`. Non-numeric values are ignored.                                                                                                                                                                     | `sum(ItemsOrdered)`                   |
| `top(expr: any, n: number): rowset`                            | Select the first `n` values of `expr`. The `top` function cannot appear with any other aggregate functions.                                                                                                                       | `top(StatusCode, 5)`                  |

## Grammar

### Base

```ebnf
identifier          = ( letter | '_' ) , { letter | digit | '_' } ;
built_in_identifier = '@' , ( letter | digit | '_' ) , { letter | digit | '_' } ;
variable            = '$' , ( letter | digit | '_' ) , { letter | digit | '_' } ;
letter = ? any Unicode letter ? ;
digit  = ? any Unicode digit ? ;
string_literal      = "'" , { string_char } , "'" ;
string_char         = "''" | ? any character except single quote ? ;
number              = natural , [ '.' , natural ] ;
hex_number          = '0x' , hex_digit , { hex_digit } ;
natural             = digit , { digit } ;
hex_digit           = digit | 'a' | 'b' | 'c' | 'd' | 'e' | 'f'
                             | 'A' | 'B' | 'C' | 'D' | 'E' | 'F' ;
duration            = { natural , time_unit }- ;
time_unit           = 'd' | 'h' | 'ms' | 'm' | 'us' | 'μs' | 'ns' | 's' ;
regular_expression  = '/' , { regex_char } , '/' ;
regex_char          = '\/' | ? any character except '/' ? ;
```

### Expression

```
Expr = Disjunction ;
Disjunction = Conjunction , { 'or' , Conjunction } ;
Conjunction = Comparison , { 'and' , Comparison } ;
Comparison = Comparand , { comparison_op , Comparand , [ 'ci' ] } ;
comparison_op = 'not' , 'like'
              | 'like'
              | 'not' , 'in'
              | 'in'
              | '<=' | '<>' | '<'
              | '>=' | '>'
              | '=' ;       
Comparand = Term , { ( '+' | '-' ) , Term }  ;
Term = InnerTerm , { ( '*' | '/' | '%' ) , InnerTerm } ;
InnerTerm = Operand , { '^' , Operand } ;

Operand = ( unary_op , Operand | Path ) , [ 'is' , null_test ] ;
unary_op  = '-' | 'not' ;
null_test = 'null' | 'not' , 'null' ;
Path = Factor , { path_step } ;
path_step = '.' , identifier
          | '[' , ( wildcard | Expr ) , ']' ;
wildcard = '?' | '*' ;
Factor = '(' , Expr , ')'
       | Item ;
Item = Property
     | Literal
     | Function
     | ArrayLiteral
     | ObjectLiteral
     | Conditional
     | Block
     | Lambda
     | Variable ;
Property = built_in_identifier
         | identifier ;                 (* when not followed by '(' *)
Literal = string_literal
        | number
        | hex_number
        | duration
        | regular_expression
        | 'true'
        | 'false'
        | 'null' ;
Function = function_name , '(' , arg_list , ')' , [ 'ci' ] ;
function_name = identifier
              | 'and' | 'not' | 'or' ;
arg_list = '*'                          (* only valid for count(*) *)
         | [ Expr , { ',' , Expr } ] ;
ArrayLiteral = '[' , [ Expr , { ',' , Expr } ] , ']' ;
ObjectLiteral = '{' , [ ObjectMember , { ',' , ObjectMember } ] , '}' ;
ObjectMember  = ( identifier | string_literal ) , ':' , Expr ;
Conditional = 'if' , Expr , 'then' , Expr , 'else' , Expr ;
Block        = 'let' , '|' , Binding , { ',' , Binding } , '|' , Expr ;
Binding      = identifier , ':' , Expr ;
Lambda       = '|' , [ identifier , { ',' , identifier } ] , '|' , Expr ;
Variable     = variable ;
```

**Disambiguation:** The `/` character introduces a regular expression when it appears at the
start of input, or when the preceding token is an operator or opening delimiter — specifically
one of: `and`, `or`, `not`, `(`, `[`, `,`, `=`, `<>`, `like`, `>`, `>=`, `<`, `<=`, `in`,
`is`, `&&`, `||`, `!=`, `==`, `!`, `if`, `then`, `else`, `:`. In all other positions, `/` is
the division operator.

### Queries

```ebnf
Query = [ ExplainClause ]
        SelectClause
        [ IntoClause ]
        [ FromClause ]
        [ WhereClause ]
        [ GroupByClause ]
        [ HavingClause ]
        [ OrderByClause ]
        [ LimitClause ]
        [ ForClause ] ;
ExplainClause = 'explain' , [ 'analyze' | 'lower' ] ;
SelectClause = 'select' , SelectColumn , { ',' , SelectColumn } ;
SelectColumn = '*'
             | Expr , [ 'as' , identifier ] ;
IntoClause = 'into' , variable ;
FromClause = 'from' , source , { LateralJoin } ;
source     = 'stream' | 'series' ;
LateralJoin = 'lateral' , Expr , 'as' , identifier ;
WhereClause = 'where' , Expr ;
GroupByClause = 'group' , 'by' , Grouping , { ',' , Grouping } ;
Grouping      = TimeGrouping
              | Expr , [ 'ci' ] , [ 'as' , identifier ] ;
TimeGrouping  = 'time' , '(' , duration , ')' ;
HavingClause = 'having' , Expr ;
OrderByClause = 'order' , 'by' , Ordering , { ',' , Ordering } ;
Ordering      = TimeOrdering
              | identifier , [ 'ci' ] , [ 'asc' | 'desc' ] ; (* identifier must be a selected column or group key alias *)
TimeOrdering  = 'time' ;
LimitClause = 'limit' , natural ;
ForClause = 'for' , ForOption , { ',' , ForOption } ;
ForOption = identifier , [ '(' , [ Expr , { ',' , Expr } ] , ')' ] ;
```

Keywords are case-insensitive. The `stream` source contains log events and spans. The `series` source contains
metric samples.

## Schema

Seq servers are compatible with a vast array of data sources. They may use a mix of OpenTelemetry and
framework/ecosystem-specific property names, and may do so inconsistently. When exploring, **always use the MCP schema
tool** to inspect the actual properties appearing on search results, cross-referencing with source code where necessary.

## Example Expressions

| Example                                                      | Purpose~~~~                                                                                      |
|--------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| `@Timestamp >= now() - 10m`                                  | Match events that occurred in the last ten minutes.                                          |
| `@TraceId = '0af7651916cd43dd8448eb211c80319c'`              | Match all events (both spans and log events) belonging to the given trace.                   |
| `Has(@Start)`                                                | Match all spans (excludes log events).                                                       |
| `@Message like '%overflow%' or @Exception like '%overflow%'` | Given a piece of text, find events with that text in their message or exception/stack trace. |
| `Items[?] = 'coffee'`                                        | Wildcard "any" - check if element appears in collection.                                     |
| `ToIsoString(@Timestamp)`                                    | Render a numeric timestamp as ISO-8601.                                                      |
| `ToTimeString(@Elapsed)`                                     | Render a numeric duration value as a human-readable time string.                             |
| `@Resource.service.name = 'unknown_service'`                 | Match events from a specific service (OpenTelemetry semantic convention)                     |
| `@TraceId = '...' and @SpanId = '...' and Has(@Start)`       | Retrieve a specific trace span using a search expression                                     |

## Example Queries

Grouped query with ordering:

```
select count(*)
from stream
group by @Resource.service.name as service
order by service
```

## Tracing Tactics

Reconstruct a trace in execution (start-time) order:

```
select
   @SpanId as span_id,
   @ParentId as parent_id,
   @Resource.service.name as service,
   @Message as span_name,
   @SpanKind as kind,
   @Start as start,                    -- raw ticks — order by THIS column
   ToIsoString(@Start) as start_iso,   -- readable copy, for display only
   TotalMilliseconds(@Elapsed) as ms
from stream
where @TraceId = '<paste-trace-id>' and Has(@Start)
order by start asc
limit 1000                             -- traces can be large; if the result looks truncated, raise this
```

This orders rows by start time; it does NOT build the hierarchy. The call tree is assembled from `parent_id` (each row's 
`@ParentId` = its parent's `@SpanId`; the root's is null).

Note that you'll need to use the "retrieve a specific trace span" search recipe to see more about a span appearing in
these results.

Rank services by span latency over a window:

```
select
   count(*) as spans,
   Round(TotalMilliseconds(percentile(@Elapsed, 95)), 2) as p95_ms,
   Round(TotalMilliseconds(max(@Elapsed)), 2) as max_ms
from stream
where @Timestamp >= now() - 30m and Has(@Start)
group by @Resource.service.name as service     -- group key: alias it, do NOT put it in the select list
having spans > 50                              -- filter groups by the select alias, NOT by count(*) directly
order by p95_ms desc                           -- order by a selected aggregate's alias
limit 100
```

## Gotchas

 - Group keys are automatically included in result rowsets and **must not** be explicitly included in the `select` list.
 - To order by group keys, apply an alias with `group by <expr> as <alias>` and use `order by <alias>`. Never add the
  group key to the `select` list, this will fail.
 - OpenTelemetry dotted property names correspond to property accessor paths in Seq, so `@Resource.service.name` and
  `http.response.status_code` are written exactly like this.
 - **Never** put a dotted OTel name inside `[...]`. `@Resource['a.b.c']` is a single literal key (almost always undefined);
  use `@Resource.a.b.c` for path navigation.
 - Seq expression literals are not JSON, take care to use the Seq expression syntax when formatting literal values.
 - Seq queries are not SQL. Don't expect standard SQL syntax, operators, or semantics to apply, always use the grammar
   and built-ins described above.
 - Seq searches work backwards through the event stream and always return results in reverse-chronological order, from
   **most recent** to least recent.
 - Data in Seq servers doesn't always use OpenTelemetry semantic conventions. When searching or querying, only use property
   names from the built-ins described above, that appear on search results, or that are returned from the schema tool.
 - Bare identifiers like `SomeName` are synonymous with `@Properties['SomeName']`. The latter form allows irregular names
   to be used.
 - The only escape sequence allowed and required in Seq strings is a doubled single quote - `''` - which evaluates to an 
   embedded literal single quote. Backslash escaping is not recognized.
 - `@Timestamp`, `@Start`, and `@Elapsed` are internally represented as .NET `DateTime` ticks (100 ns 
   resolution) in order to support consistent timestamp/duration math. Comparing these properties with strings will 
   fail: use duration literals for durations, and the `DateTime` function
   to convert from ISO-8601 strings.
 - Although Seq's types resemble those from JavaScript, Seq does not support JavaScript operators and does not use 
   JavaScript's system of comparisons.
 - The expression `null = null` is `true` in Seq's type system; `null` is just a regular value.
 - Timestamp bounds with inclusive starts and exclusive ends are the most efficient for Seq to work with.
 - Regular expression evaluation is extremely expensive, avoid these as much as possible.
 - Queries without `from stream` or `from series` are scalar (can't project out fields or compute aggregations).
 - Searches and queries should always constrain results using `@Timestamp`, `@TraceId`, or `@Id`.
 - `group by time(..)` requires an inclusive lower time bound on `@Timestamp`.
 - Queries impose a default limit of 1024 rows, which can be changed with the `limit` clause. Set smaller limits to
   conserve resources when speculatively exploring.
 - Use `ToIsoString()` and `ToTimeString()` to make timestamps or durations (even computed ones) readable. If you forget,
   you can convert individual values cheaply with a scalar query like `ToIsoString(12345)`.
 - When grouping by `time(..)`, the time ordering leaves of the interval - just `order by time`, the interval isn't
   re-specified.
