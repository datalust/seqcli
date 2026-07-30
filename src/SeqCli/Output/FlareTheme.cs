// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using Serilog.Templates.Themes;

namespace SeqCli.Output;

/// <summary>
/// <c>Flare</c> is Seq's embedded stream/columnar database. This theme is derived from one build originally
/// for the <c>flaretl</c> command-line tooling used there.
/// </summary>
static class FlareTheme
{
    static readonly Dictionary<TemplateThemeStyle, string> FlareThemeStyles = new()
    {
        [TemplateThemeStyle.Name] = "\e[38;5;0215m",
        [TemplateThemeStyle.Number] = "\e[38;5;0200m",
        [TemplateThemeStyle.Boolean] = "\e[38;5;0039m",
        [TemplateThemeStyle.Null] = "\e[38;5;0039m",
        [TemplateThemeStyle.String] = "\e[38;5;0217m",
        [TemplateThemeStyle.Scalar] = "\e[38;5;0217m",
        [TemplateThemeStyle.LevelError] = "\e[38;5;0197m",
        [TemplateThemeStyle.TertiaryText] = "\e[38;5;0244m",
        // Based on `TemplateTheme.Code`
        [TemplateThemeStyle.Text] = "\e[38;5;0253m",
        [TemplateThemeStyle.SecondaryText] = "\e[38;5;0246m",
        [TemplateThemeStyle.Invalid] = "\e[33;1m",
        [TemplateThemeStyle.LevelVerbose] = "\e[37m",
        [TemplateThemeStyle.LevelDebug] = "\e[37m",
        [TemplateThemeStyle.LevelInformation] = "\e[37;1m",
        [TemplateThemeStyle.LevelWarning] = "\e[38;5;0229m",
        [TemplateThemeStyle.LevelFatal] = "\e[38;5;0197m\e[48;5;0238m"
    };

    public static readonly TemplateTheme SeqCli = new(FlareThemeStyles);
    
    // `CsvWriter` implements its own theming behavior because the required APIs are not public in Serilog.Expressions.
    // The best way forward for this is likely to be porting theming to Seq.Syntax, and exposing the required APIs there.
    
    const string AnsiStyleResetSequence = "\e[0m";
    
    // The passed-in theme is ignored because SerilogExpressions themes are opaque. All formatting uses the SeqCli theme.
    // ReSharper disable once UnusedParameter.Global
    extension(TemplateTheme theme)
    {
        public void Set(TextWriter output, TemplateThemeStyle style)
        {
            if (FlareThemeStyles.TryGetValue(style, out var styleSequence))
                output.Write(styleSequence);
        }

        public void Reset(TextWriter output)
        {
            output.Write(AnsiStyleResetSequence);
        }
    }
}