using System.Reflection;
using FluentAssertions;
using NSubstitute.Concrete.Cleanup;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Statics;
using NSubstitute.Concrete.Test.Fixtures;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Test;

/// <summary>
/// Regression tests for the interceptor defects fixed for host libraries that drive the patching
/// themselves rather than through <c>Setup</c>: per-method call counts, per-signature configuration,
/// call ordering, scoped static clearing, whole-surface patching, identity-keyed registries, the
/// "run the original" signal and the fallback hook.
/// </summary>
[Collection("Static Tests")]
public class InterceptorFixTests : IDisposable
{
    public void Dispose()
    {
        ConcreteCleanupExtensions.ClearAll();
    }

    // F1 — Verify counts calls to the method it names, not every call with matching arguments.
    [Fact]
    public void GetCallCount_only_counts_the_method_it_was_given()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = new HarmonyMethodInterceptor(typeof(OverloadedService));

        var a = typeof(OverloadedService).GetMethod(nameof(OverloadedService.A))!;
        var b = typeof(OverloadedService).GetMethod(nameof(OverloadedService.B))!;

        service.Setup(x => x.A(1)).Returns("mocked");
        service.Setup(x => x.B(1)).Returns("mocked");

        service.A(1);
        service.A(1);
        service.B(1);

        // Reached through the base type, which is how ConcreteExtensions.Verify reaches it.
        ConcreteMethodInterceptor asBase = NSubstituteExtensions.GetHarmonyInterceptor(service)!;
        asBase.GetCallCount(a, new object[] { 1 }).Should().Be(2);
        asBase.GetCallCount(b, new object[] { 1 }).Should().Be(1);

        GC.KeepAlive(interceptor);
    }

    [Fact]
    public void Verify_does_not_count_a_different_method_with_the_same_arguments()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();

        service.Setup(x => x.A(1)).Returns("mocked");
        service.Setup(x => x.B(1)).Returns("mocked");

        service.B(1);

        var act = () => service.Verify(x => x.A(1));
        act.Should().Throw<Exception>();
    }

    // F2 — configuration is keyed on the full signature, so overloads do not share a bucket.
    [Fact]
    public void Overloads_are_configured_independently()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();

        service.Setup(x => x.Describe(1)).Returns("mocked int");

        service.Describe(1).Should().Be("mocked int");
        service.Describe("1").Should().Be("string:1");
    }

    [Fact]
    public void Static_overloads_are_configured_independently()
    {
        Static.Setup(() => OverloadedStatics.Describe(1)).Returns("mocked int");

        OverloadedStatics.Describe(1).Should().Be("mocked int");
        OverloadedStatics.Describe("1").Should().Be("string:1");
    }

    // F3 — recorded calls carry an ordering that survives calls made in the same tick.
    [Fact]
    public void Recorded_calls_carry_a_strictly_increasing_ordinal()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;

        interceptor.PatchAll();

        service.A(1);
        service.B(2);
        service.A(3);

        var calls = interceptor.GetReceivedCalls();
        calls.Should().HaveCount(3);
        calls.Select(c => c.Ordinal).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        calls.Select(c => c.Method!.Name).Should().Equal("A", "B", "A");
    }

    [Fact]
    public void Recorded_static_calls_carry_an_ordinal()
    {
        StaticMethodInterceptor.Instance.PatchAll(typeof(ScopedStaticsOne));

        ScopedStaticsOne.Read();
        ScopedStaticsOne.Read();

        var calls = StaticMethodInterceptor.Instance.GetAllCalls();
        calls.Should().HaveCount(2);
        calls[0].Ordinal.Should().BeLessThan(calls[1].Ordinal);
    }

    // F4 — statics can be cleared for one type without disturbing another.
    [Fact]
    public void ClearFor_a_type_leaves_other_patched_statics_alone()
    {
        Static.Setup(() => ScopedStaticsOne.Read()).Returns("mocked one");
        Static.Setup(() => ScopedStaticsTwo.Read()).Returns("mocked two");

        ScopedStaticsOne.Read().Should().Be("mocked one");
        ScopedStaticsTwo.Read().Should().Be("mocked two");

        StaticMethodInterceptor.Instance.ClearFor(typeof(ScopedStaticsOne));

        ScopedStaticsOne.Read().Should().Be("real one");
        ScopedStaticsTwo.Read().Should().Be("mocked two");
    }

    [Fact]
    public void PatchAll_covers_every_static_of_the_type()
    {
        StaticMethodInterceptor.Instance.PatchAll(typeof(AnotherStaticService));

        StaticMethodInterceptor.Instance.PatchedMethods
            .Where(m => m.DeclaringType == typeof(AnotherStaticService))
            .Select(m => m.Name)
            .Should().BeEquivalentTo("GetValue", "Multiply");
    }

    // F5 — a type's whole instance surface can be patched up front.
    [Fact]
    public void PatchAll_covers_public_and_protected_instance_members()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;

        interceptor.PatchAll();

        interceptor.PatchedMethods.Select(m => m.Name)
            .Should().Contain(new[] { "Describe", "A", "B", "Protected", "CallProtected" });
    }

    [Fact]
    public void PatchAll_records_calls_that_were_never_set_up()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;

        interceptor.PatchAll();

        service.A(7).Should().Be("A:7", "an unconfigured call still runs the real method");
        interceptor.GetReceivedCalls().Should().ContainSingle(c => c.Method!.Name == "A");
    }

    [Fact]
    public void PatchAll_does_not_patch_object_members()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;

        interceptor.PatchAll();

        interceptor.PatchedMethods.Select(m => m.Name)
            .Should().NotContain(new[] { "ToString", "GetHashCode", "Equals" });
    }

    // F6 — the registries key on identity, so a value-equal type does not collide.
    [Fact]
    public void Two_substitutes_that_compare_equal_keep_separate_configurations()
    {
        var first = NSubstituteExtensions.ForConcrete<ValueEqualService>("k");
        var second = NSubstituteExtensions.ForConcrete<ValueEqualService>("k");

        first.Equals(second).Should().BeTrue("the fixture compares by value");

        first.Setup(x => x.Read()).Returns("mocked first");

        first.Read().Should().Be("mocked first");
        second.Read().Should().Be("real:k");
    }

    [Fact]
    public void Unpatching_releases_the_substitute_and_the_patch()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;
        interceptor.PatchAll();

        interceptor.Unpatch();

        interceptor.ProxyInstance.Should().BeNull();
        interceptor.PatchedMethods.Should().BeEmpty();
        service.A(1).Should().Be("A:1");
    }

    [Fact]
    public void A_second_substitute_of_the_same_type_still_intercepts_after_the_first_unpatches()
    {
        var first = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var second = NSubstituteExtensions.ForConcrete<OverloadedService>();

        first.Setup(x => x.A(1)).Returns("first");
        second.Setup(x => x.A(1)).Returns("second");

        NSubstituteExtensions.GetHarmonyInterceptor(first)!.Unpatch();

        first.A(1).Should().Be("A:1", "the first substitute is no longer intercepted");
        second.A(1).Should().Be("second", "the second substitute still holds its patch");
    }

    [Fact]
    public void A_single_call_is_recorded_once_even_when_two_substitutes_patch_the_same_method()
    {
        var first = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var second = NSubstituteExtensions.ForConcrete<OverloadedService>();

        var firstInterceptor = NSubstituteExtensions.GetHarmonyInterceptor(first)!;
        firstInterceptor.PatchAll();
        NSubstituteExtensions.GetHarmonyInterceptor(second)!.PatchAll();

        first.A(1);

        firstInterceptor.GetReceivedCalls().Count(c => c.Method!.Name == "A").Should().Be(1);
    }

    // F7 — nothing configured means the real method runs, rather than a reflected base call.
    [Fact]
    public void An_unconfigured_call_runs_the_real_method_on_a_fully_patched_substitute()
    {
        var service = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(5);
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;
        interceptor.PatchAll();

        service.IncrementAndReturn(3).Should().Be(8);
        service.Id.Should().Be(5);
    }

    // F8 — a fallback answers, or defers, for anything the interceptor itself cannot.
    [Fact]
    public void The_fallback_answers_calls_nothing_else_configured()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;
        interceptor.PatchAll();

        var seen = new List<string>();
        interceptor.SetFallback((method, instance, args) =>
        {
            seen.Add(method.Name);
            instance.Should().BeSameAs(service);
            return method.Name == "A" ? "from fallback" : Interception.RunOriginal;
        });

        service.A(1).Should().Be("from fallback");
        service.B(1).Should().Be("B:1", "the fallback deferred, so the real method ran");
        seen.Should().Equal("A", "B");
    }

    [Fact]
    public void A_configured_result_takes_precedence_over_the_fallback()
    {
        var service = NSubstituteExtensions.ForConcrete<OverloadedService>();
        var interceptor = NSubstituteExtensions.GetHarmonyInterceptor(service)!;
        interceptor.PatchAll();
        interceptor.SetFallback((_, _, _) => "from fallback");

        service.Setup(x => x.A(1)).Returns("configured");

        service.A(1).Should().Be("configured");
    }

    [Fact]
    public void The_static_fallback_answers_calls_nothing_else_configured()
    {
        StaticMethodInterceptor.Instance.PatchAll(typeof(ScopedStaticsOne));
        StaticMethodInterceptor.Instance.SetFallback((method, _, _) =>
            method.Name == "Read" ? "from fallback" : Interception.RunOriginal);

        ScopedStaticsOne.Read().Should().Be("from fallback");
    }

    [Fact]
    public void The_static_fallback_can_defer_to_the_real_method()
    {
        StaticMethodInterceptor.Instance.PatchAll(typeof(ScopedStaticsTwo));
        StaticMethodInterceptor.Instance.SetFallback((_, _, _) => Interception.RunOriginal);

        ScopedStaticsTwo.Read().Should().Be("real two");
    }

    // The shared key helper, which both sides now use.
    [Fact]
    public void MethodKeys_distinguishes_overloads()
    {
        var byInt = typeof(OverloadedService).GetMethod(nameof(OverloadedService.Describe), new[] { typeof(int) })!;
        var byString = typeof(OverloadedService).GetMethod(nameof(OverloadedService.Describe), new[] { typeof(string) })!;

        MethodKeys.For(byInt).Should().NotBe(MethodKeys.For(byString));
    }

    [Fact]
    public void MethodKeys_of_a_null_method_is_empty()
    {
        MethodKeys.For((MethodBase?)null).Should().BeEmpty();
    }
}
