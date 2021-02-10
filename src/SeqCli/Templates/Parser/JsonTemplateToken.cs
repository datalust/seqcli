// Copyright Datalust Pty Ltd and Contributors
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

using Superpower.Display;

namespace SeqCli.Templates.Parser
{
    enum JsonTemplateToken
    {
        [Token(Example = "{")]
        LBracket,

        [Token(Example = "}")]
        RBracket,
        
        [Token(Example = "[")]
        LSquareBracket,
        
        [Token(Example = "]")]
        RSquareBracket,
        
        [Token(Example = ":")]
        Colon,
        
        [Token(Example = ",")]
        Comma,
        
        String,
        
        Number,
        
        [Token(Example = "(")]
        LParen,

        [Token(Example = ")")]
        RParen,
        
        Identifier,
    }
}
