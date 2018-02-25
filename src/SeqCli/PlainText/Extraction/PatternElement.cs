using System.Collections.Generic;
using Superpower;
using Superpower.Model;

namespace SeqCli.PlainText.Extraction
{
    abstract class PatternElement
    {
        readonly string _name;
        
        bool IsIgnored => _name == null;

        protected PatternElement(string name)
        {
            _name = name;
        }

        public abstract TextParser<Unit> Match { get; }

        public abstract bool TryExtract(
            TextSpan input,
            Dictionary<string, object> result,
            out TextSpan remainder);

        protected void CollectResult(Dictionary<string, object> result, object value)
        {
            if (!IsIgnored)
                result.Add(_name, value);                
        }
    }
}
