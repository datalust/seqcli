namespace SeqCli.Cli
{
    static class Requirement
    {
        public static bool IsNonEmpty(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static string NonEmptyDescription(string optionName)
        {
            return $"a {optionName} is required";
        }
    }
}
