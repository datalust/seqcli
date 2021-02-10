# Roastery sample

This project simulates a simple web application for ordering
coffee beans.

Customers place orders through the website, and staff fulfill orders
by shipping them and updating the site.

The simulation produces events from an MVC-like web stack and a
SQL database.

At various points, failures are randomly injected so that diagnostic
data can be produced.

### See also...

This is the data set relied upon by the entities created with
`seqcli sample setup` - if the log event structure is changed, the
templates will likely need updating, too.
