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

static class OutputTheme
{
    static readonly Dictionary<TemplateThemeStyle, string> SeqCliThemeStyles = new()
    {
        // Based on `TemplateTheme.Code`
        [TemplateThemeStyle.Text] = "\u001B[38;5;0253m",
        [TemplateThemeStyle.SecondaryText] = "\u001B[38;5;0246m",
        [TemplateThemeStyle.TertiaryText] = "\u001B[38;5;0242m",
        [TemplateThemeStyle.Invalid] = "\u001B[33;1m",
        [TemplateThemeStyle.Null] = "\u001B[38;5;0038m",
        [TemplateThemeStyle.Name] = "\u001B[38;5;0215m",
        [TemplateThemeStyle.String] = "\u001B[38;5;0217m",
        [TemplateThemeStyle.Number] = "\u001B[38;5;0200m",
        [TemplateThemeStyle.Boolean] = "\u001B[38;5;0039m",
        [TemplateThemeStyle.Scalar] = "\u001B[38;5;0079m",
        [TemplateThemeStyle.LevelVerbose] = "\u001B[37m",
        [TemplateThemeStyle.LevelDebug] = "\u001B[37m",
        [TemplateThemeStyle.LevelInformation] = "\u001B[37;1m",
        [TemplateThemeStyle.LevelWarning] = "\u001B[38;5;0229m",
        [TemplateThemeStyle.LevelError] = "\u001B[38;5;0197m\u001B[48;5;0238m",
        [TemplateThemeStyle.LevelFatal] = "\u001B[38;5;0197m\u001B[48;5;0238m"
    };

    public static readonly TemplateTheme SeqCli = new(SeqCliThemeStyles);
    
    // `CsvWriter` implements its own theming behavior because the required APIs are not public in Serilog.Expressions.
    // The best way forward for this is likely to be porting theming to Seq.Syntax, and exposing the required APIs there.
    
    const string AnsiStyleResetSequence = "\u001B[0m";
    
    // The passed-in theme is ignored because SerilogExpressions themes are opaque. All formatting uses the SeqCli theme.
    // ReSharper disable once UnusedParameter.Global
    extension(TemplateTheme theme)
    {
        public void Set(TextWriter output, TemplateThemeStyle style)
        {
            if (SeqCliThemeStyles.TryGetValue(style, out var styleSequence))
                output.Write(styleSequence);
        }

        public void Reset(TextWriter output)
        {
            output.Write(AnsiStyleResetSequence);
        }
    }
}