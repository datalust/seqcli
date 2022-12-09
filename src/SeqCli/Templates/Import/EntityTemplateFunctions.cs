using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Evaluator;

#nullable enable

namespace SeqCli.Templates.Import
{
    class EntityTemplateFunctions
    {
        readonly TemplateImportState _state;
        readonly IReadOnlyDictionary<string, JsonTemplate> _templateArgs;

        public EntityTemplateFunctions(TemplateImportState state, IReadOnlyDictionary<string, JsonTemplate> templateArgs)
        {
            _state = state;
            _templateArgs = templateArgs;
            
            Exports = new Dictionary<string, JsonTemplateFunction>
            {
                ["ref"] = Ref,
                ["arg"] = Arg
            };
        }

        public IReadOnlyDictionary<string, JsonTemplateFunction> Exports { get; }

        bool Ref(JsonTemplate[] args, [NotNullWhen(true)] out JsonTemplate? result, [NotNullWhen(false)] out string? err)
        {
            if (args.Length != 1 || args[0] is not JsonTemplateString { Value: { } filename })
            {
                result = null;
                err = "The `ref()` function accepts a single string argument corresponding to the referenced template filename.";
                return false;
            }

            if (!_state.TryGetCreatedEntityId(filename, out var referencedId))
            {
                result = null;
                err = $"The referenced template file `{filename}` does not exist or has not been evaluated.";
                return false;
            }

            result = new JsonTemplateString(referencedId);
            err = null;
            return true;
        }

        bool Arg(JsonTemplate[] args, [MaybeNullWhen(false)] out JsonTemplate result, [MaybeNullWhen(true)] out string err)
        {
            if (args.Length != 1 || args[0] is not JsonTemplateString { Value: { } templateArgName })
            {
                result = null;
                err = "The `arg()` function accepts a single string argument corresponding to the template argument name.";
                return false;
            }

            if (!_templateArgs.TryGetValue(templateArgName, out result))
            {
                err = $"The argument `{templateArgName}` is not defined.";
                return false;
            }

            err = null;
            return true;
        }
    }
}