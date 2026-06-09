using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace SeqCli.Tests.Skills;

public class SkillComplexityTests
{
    public static IEnumerable<object[]> FindPackagedSkills()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Skills");

        foreach (var skillSourceDirectory in Directory.EnumerateDirectories(sourcePath))
        {
            var skillName = Path.GetFileName(skillSourceDirectory);
            yield return [skillName, Path.Combine(skillSourceDirectory, "SKILL.md")];
        }        
    }
    
    [Theory]
    [MemberData(nameof(FindPackagedSkills))]
    public void SkillIsWithinRecommendedSizeLimits(string skillName, string path)
    {
        const int recommendedMaxLines = 500, recommendedMaxTokens = 5000;
        
        // Still some work to do, here.
        const int allowedMaxTokens = recommendedMaxTokens + 4000;
        
        var lines = File.ReadAllLines(path);
        Assert.True(lines.Length < recommendedMaxLines, $"`{skillName}/SKILL.md` line count {lines.Length} exceeds the recommended {recommendedMaxLines}-line limit");

        const string benchmarkModelName = "gpt-5";
        var tokenizer = TiktokenTokenizer.CreateForModel(benchmarkModelName);
        var tokenCount = tokenizer.CountTokens(File.ReadAllText(path));
        Assert.True(tokenCount < allowedMaxTokens, $"`{skillName}/SKILL.md` token count {tokenCount} ({benchmarkModelName}) exceeds {allowedMaxTokens} ({recommendedMaxTokens} is the recommended limit)");
    }
}