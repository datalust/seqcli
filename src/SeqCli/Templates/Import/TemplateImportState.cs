using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace SeqCli.Templates.Import
{
    class TemplateImportState
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global, MemberCanBePrivate.Global
        // Exposed just for serialization's sake.
        public Dictionary<string, string> Created { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        
        public static async Task<TemplateImportState> LoadAsync(string stateFile)
        {
            await using var file = File.OpenRead(stateFile);
            var options = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
            return await JsonSerializer.DeserializeAsync<TemplateImportState>(file, options)
                   ?? throw new InvalidOperationException("File does not contain a valid import state.");
        }

        public static async Task SaveAsync(string stateFile, TemplateImportState state)
        {
            var tmp = stateFile + ".tmp";
            await using (var file = File.Create(tmp))
            {
                var options = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true};
                await JsonSerializer.SerializeAsync(file, state, options);
            }
            if (File.Exists(stateFile))
                File.Replace(tmp, stateFile, destinationBackupFileName: null);
            else
                File.Move(tmp, stateFile);
        }

        public bool TryGetCreatedEntityId(string templateName, [MaybeNullWhen(false)] out string entityId) =>
            Created.TryGetValue(templateName, out entityId);

        public void AddOrUpdateCreatedEntityId(string templateName, string entityId) =>
            Created[templateName] = entityId;
    }
}
