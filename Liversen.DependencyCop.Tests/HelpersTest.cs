using Shouldly;
using Xunit;

namespace Liversen.DependencyCop
{
    public static class HelpersTest
    {
        [Fact]
        public static void GivenUnrelatedNamespaces_WhenRemovingCommonNamespacePrefix_ThenSame() =>
            Helpers.RemoveCommonNamespacePrefix("Foo", "Bar")
                .ShouldBe(("Foo", "Bar"));

        [Fact]
        public static void GivenNamespacesWithSameRoot_WhenRemovingCommonNamespacePrefix_ThenReduced() =>
            Helpers.RemoveCommonNamespacePrefix("DC.Foo", "DC.Bar")
                .ShouldBe(("DC.Foo", "DC.Bar"));

        [Fact]
        public static void GivenDeepNamespacesWithCommonRoot_WhenRemovingCommonNamespacePrefix_ThenReduced() =>
            Helpers.RemoveCommonNamespacePrefix("DC.Foo.Qwerty", "DC.Bar.Qwerty")
                .ShouldBe(("DC.Foo", "DC.Bar"));

        [Fact]
        public static void GivenOneNamespaceChildOfAnotherNamespace_WhenRemovingCommonNamespacePrefix_ThenEmptyNamespaces() =>
            Helpers.RemoveCommonNamespacePrefix("DC.Foo.Bar", "DC.Foo")
                .ShouldBe((string.Empty, string.Empty));
    }
}
