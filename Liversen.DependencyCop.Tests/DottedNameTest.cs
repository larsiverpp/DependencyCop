using System;
using System.Collections.Immutable;
using System.Linq;
using Shouldly;
using Xunit;

namespace Liversen.DependencyCop
{
    public static class DottedNameTest
    {
        static readonly DottedName SomeValue = new("Foo.Bar.Zoo");
        static readonly ImmutableArray<string> SomeParts = ["Foo", "Bar", "Zoo"];

        [Fact]
        public static void GivenEmptyString_WhenNewing_ThenArgumentError() =>
            Should.Throw<ArgumentException>(() => new DottedName(string.Empty));

        [Fact]
        public static void GivenDotString_WhenNewing_ThenArgumentError() =>
            Should.Throw<ArgumentException>(() => new DottedName("."));

        [Fact]
        public static void GivenDottedName_WhenGettingParts_ThenParts() =>
            SomeValue.Parts.ShouldBe(SomeParts);

        [Fact]
        public static void GivenParts_WhenCreating_ThenDottedName() =>
            DottedName.Create(SomeParts).ShouldBe(SomeValue);

        [Fact]
        public static void GivenDottedName_WhenSkippingSome_ThenDottedNameWithSkippedParts() =>
            SomeValue.Skip(1).ShouldBe(DottedName.Create(SomeParts.Skip(1)));

        [Fact]
        public static void GivenDottedName_WhenTakingSome_ThenDottedNameWithTakenParts() =>
            SomeValue.Take(2).ShouldBe(DottedName.Create(SomeParts.Take(2)));

        [Fact]
        public static void GivenIdenticalDottedNames_WhenSkippingCommonPrefix_ThenNull() =>
            SomeValue.SkipCommonPrefix(SomeValue).ShouldBeNull();

        [Fact]
        public static void GivenDottedNamesWithSomeCommonPrefix_WhenSkippingCommonPrefix_ThenCommonPrefixSkipped() =>
            new DottedName("Foo.Bar.Zoo.Tab").SkipCommonPrefix(new("Foo.Bar.Zoo2.Tab")).ShouldBe(new("Zoo.Tab"));

        [Fact]
        public static void GivenDottedNamesWithNoCommonPrefix_WhenSkippingCommonPrefix_ThenNothingSkipped() =>
            SomeValue.SkipCommonPrefix(new($"A{SomeValue.Value}")).ShouldBe(SomeValue);
    }
}
