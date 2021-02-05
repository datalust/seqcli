# SeqCli.Templates

A simple JSON templating system, designed for importing sets of
related entities into a Seq server.

## Template syntax

The templating language is a JSON superset which, when evaluated,
produces JSON values.

The syntax extends JSON with JavaScript-style function calls:

```
{
    "Title": "Web",
    "SignalId": ref("signal-HTTP Requests.json")
}
```

The supported functions are:

 * `ref(filename)` - resolves to the id of the entity produced
   by importing the template _filename_ into the target Seq instance.
   
