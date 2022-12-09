// Copyright 2018 Datalust Pty Ltd
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace SeqCli.Syntax
{
    class CombinedFilterBuilder
    {
        readonly List<string> _elements = new();

        public CombinedFilterBuilder Intersect(string? filter)
        {
            if (filter == null)
                return this;

            if (string.IsNullOrWhiteSpace(filter))
                throw new ArgumentNullException(nameof(filter));

            _elements.Add(filter.Trim());
            return this;
        }

        public string? Build()
        {
            if (!_elements.Any())
                return null;            

            if (_elements.Count == 1)
                return _elements.Single();

            return "(" + string.Join(")and(", _elements) + ")";
        }
    }
}
