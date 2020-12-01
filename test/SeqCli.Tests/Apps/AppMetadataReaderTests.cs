using System.ComponentModel;
using SeqCli.Apps.Definitions;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace SeqCli.Tests.Apps
{
    public class AppMetadataReaderTests
    {
        enum Test
        {
            First,
            Second = First,
            [Description("The third")]
            Third
        }
        
        [Fact]
        public void TheSettingTypeForAnEnumIsText()
        {
            var settingType = AppMetadataReader.GetSettingType(typeof(Test));
            Assert.Equal(AppSettingType.Text, settingType);
        }

        [Fact]
        public void TheAllowedValuesForANonEnumTypeAreUndefined()
        {
            var allowed = AppMetadataReader.TryGetAllowedValues(typeof(string));
            Assert.Null(allowed);
        }

        [Fact]
        public void TheAllowedValuesForAnEnumTypeAreAllNames()
        {
            var allowed = AppMetadataReader.TryGetAllowedValues(typeof(Test));
            Assert.Equal(3, allowed.Length);

            static void AssertSettingValueMembers(
                AppSettingValue value,
                object expectedValue,
                string expectedDescription)
            {
                Assert.Equal(expectedValue, value.Value);
                Assert.Equal(expectedDescription, value.Description);
            }
            
            AssertSettingValueMembers(allowed[0], "First", null);
            AssertSettingValueMembers(allowed[1], "Second", null);
            AssertSettingValueMembers(allowed[2], "Third", "The third");
        }
    }
}
