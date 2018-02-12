namespace SeqCli.Csv
{
    enum CsvToken
    {
        None,
        Newline,
        DoubleQuote,
        Comma,
        Number,
        Boolean,
        Null,
        Text,
        EscapedDoubleQuote
    }
}
