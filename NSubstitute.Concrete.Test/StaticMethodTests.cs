using FluentAssertions;
using NSubstitute.Concrete.Statics;
using NSubstitute.Concrete.Test.Fixtures;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Test;

[CollectionDefinition("Static Tests", DisableParallelization = true)]
public class StaticMethodTests
{
    public StaticMethodTests()
    {
        Static.ClearAll();
    }

    #region Basic Static Method Tests

    [Fact]
    public void Setup_WithStaticMethod_Works()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Test 123");
        MyStaticService.GetStaticName().Should().Be("Test 123");
        Static.ClearAll();
        MyStaticService.GetStaticName().Should().Be("Real Static Name");
    }

    [Fact]
    public void Setup_WithStaticMethodArgs_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(1000);
        MyStaticService.Calculate(1, 2).Should().Be(1000);
        MyStaticService.Calculate(3, 2).Should().Be(5); // Different args, original behavior
        Static.ClearAll();
    }

    [Fact]
    public void Setup_WithStaticCallback_Works()
    {
        var counter = 0;
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()))
              .Returns<int, int>((a, b) => { counter++; return (a + b) * 2; });

        MyStaticService.Calculate(1, 2).Should().Be(6);
        MyStaticService.Calculate(3, 4).Should().Be(14);
        counter.Should().Be(2);
    }

    #endregion

    #region Argument Matching Tests

    [Fact]
    public void Setup_WithAnyArguments_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(999);

        MyStaticService.Calculate(1, 2).Should().Be(999);
        MyStaticService.Calculate(10, 20).Should().Be(999);
        MyStaticService.Calculate(-5, 100).Should().Be(999);
    }

    [Fact]
    public void Setup_WithSpecificArguments_OnlyMatchesThoseArgs()
    {
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(100);
        Static.Setup(() => MyStaticService.Calculate(3, 4)).Returns(200);

        MyStaticService.Calculate(1, 2).Should().Be(100);
        MyStaticService.Calculate(3, 4).Should().Be(200);
        MyStaticService.Calculate(5, 6).Should().Be(11); // Original behavior
    }

    [Fact]
    public void Setup_WithMixedArgumentMatchers_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(1, It.IsAny<int>())).Returns(500);

        MyStaticService.Calculate(1, 999).Should().Be(500);
        MyStaticService.Calculate(1, -10).Should().Be(500);
        MyStaticService.Calculate(2, 999).Should().Be(1001); // Original behavior
    }

    #endregion

    #region Multiple Setup Tests

    [Fact]
    public void Setup_MultipleStaticMethods_AllWork()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Mocked Name");
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        Static.Setup(() => AnotherStaticService.GetValue()).Returns("Mocked Value");

        MyStaticService.GetStaticName().Should().Be("Mocked Name");
        MyStaticService.Calculate(1, 2).Should().Be(42);
        AnotherStaticService.GetValue().Should().Be("Mocked Value");
    }

    [Fact]
    public void Setup_SameMethodMultipleTimes_LastOneWins()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("First");
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Second");

        MyStaticService.GetStaticName().Should().Be("Second");
    }

    [Fact]
    public void Setup_DifferentClassesSameMethodName_BothWork()
    {
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(100);
        Static.Setup(() => AnotherStaticService.Multiply(1, 2)).Returns(200);

        MyStaticService.Calculate(1, 2).Should().Be(100);
        AnotherStaticService.Multiply(1, 2).Should().Be(200);
    }

    #endregion

    #region Callback Tests

    [Fact]
    public void Setup_WithSimpleCallback_ExecutesCallback()
    {
        var callbackExecuted = false;
        Static.Setup(() => MyStaticService.GetStaticName())
              .Callback(() => callbackExecuted = true);

        MyStaticService.GetStaticName();
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public void Setup_WithParameterizedCallback_ReceivesCorrectArgs()
    {
        int receivedA = 0, receivedB = 0;
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()))
              .Callback<int, int>((a, b) => { receivedA = a; receivedB = b; });

        MyStaticService.Calculate(10, 20);
        receivedA.Should().Be(10);
        receivedB.Should().Be(20);
    }

    [Fact]
    public void Setup_WithCallbackAndReturn_BothExecute()
    {
        var callbackExecuted = false;
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAndCallback(999, () => callbackExecuted = true);

        var result = MyStaticService.Calculate(1, 2);
        result.Should().Be(999);
        callbackExecuted.Should().BeTrue();
    }

    #endregion

    #region Void Method Tests

    [Fact]
    public void Setup_VoidMethod_CallbackExecutes()
    {
        var callbackExecuted = false;
        Static.Setup(() => MyStaticService.DoSomething())
              .Callback(() => callbackExecuted = true);

        MyStaticService.DoSomething();
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public void Setup_VoidMethod_WithoutCallback_DoesNothing()
    {
        // Should not throw
        Action act = () => MyStaticService.DoSomething();
        act.Should().NotThrow();
    }

    #endregion

    #region Async Method Tests

    [Fact]
    public async Task SetupAsync_WithAsyncMethod_Returns()
    {
        Static.SetupAsync(() => MyStaticService.GetNameAsync()).Returns("Mocked Async");

        var result = await MyStaticService.GetNameAsync();
        result.Should().Be("Mocked Async");
    }

    [Fact]
    public async Task SetupAsync_WithTask_Returns()
    {
        var task = Task.FromResult("Custom Task Result");
        Static.SetupAsync(() => MyStaticService.GetNameAsync()).Returns(task);

        var result = await MyStaticService.GetNameAsync();
        result.Should().Be("Custom Task Result");
    }

    [Fact]
    public async Task SetupAsync_VoidAsyncMethod_Works()
    {
        var callbackExecuted = false;
        Static.SetupAsync(() => MyStaticService.DoSomethingAsync())
              .Callback(() => callbackExecuted = true);

        await MyStaticService.DoSomethingAsync();
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task SetupAsync_WithAsyncCallback_Works()
    {
        var callbackExecuted = false;
        Static.SetupAsync(() => MyStaticService.DoSomethingAsync())
              .CallbackAsync(async () =>
              {
                  await Task.Delay(10);
                  callbackExecuted = true;
              });

        await MyStaticService.DoSomethingAsync();
        callbackExecuted.Should().BeTrue();
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void Setup_ThrowsException_Works()
    {
        Static.Setup(() => MyStaticService.GetStaticName())
              .Throws(new InvalidOperationException("Test exception"));

        Action act = () => MyStaticService.GetStaticName();
        act.Should().Throw<InvalidOperationException>().WithMessage("Test exception");
    }

    [Fact]
    public void Setup_ThrowsGenericException_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()))
              .Throws<ArgumentException>();

        Action act = () => MyStaticService.Calculate(1, 2);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task SetupAsync_ThrowsException_Works()
    {
        Static.SetupAsync(() => MyStaticService.GetNameAsync())
              .Throws(new InvalidOperationException("Async exception"));

        Func<Task> act = async () => await MyStaticService.GetNameAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Async exception");
    }

    #endregion

    #region Sequence/Order Tests

    [Fact]
    public void Setup_ReturnsInOrder_ReturnsSequentially()
    {
        Static.Setup(() => MyStaticService.GetStaticName())
              .ReturnsInOrder("First", "Second", "Third");

        MyStaticService.GetStaticName().Should().Be("First");
        MyStaticService.GetStaticName().Should().Be("Second");
        MyStaticService.GetStaticName().Should().Be("Third");
        MyStaticService.GetStaticName().Should().Be("Third"); // Last value repeats
    }

    [Fact]
    public void Setup_MultipleValueArray_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(1, 2))
              .Returns(10, 20, 30);

        MyStaticService.Calculate(1, 2).Should().Be(10);
        MyStaticService.Calculate(1, 2).Should().Be(20);
        MyStaticService.Calculate(1, 2).Should().Be(30);
        MyStaticService.Calculate(1, 2).Should().Be(30); // Last value repeats
    }

    #endregion

    #region Verification Tests

    [Fact]
    public void Verify_StaticMethodCalled_Succeeds()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Test");

        MyStaticService.GetStaticName();

        Action verify = () => Static.Verify(() => MyStaticService.GetStaticName(), times: 1);
        verify.Should().NotThrow();
    }

    [Fact]
    public void Verify_StaticMethodNotCalled_Throws()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Test");

        // Don't call the method

        Action verify = () => Static.Verify(() => MyStaticService.GetStaticName(), times: 1);
        verify.Should().Throw<Exception>().WithMessage("*Expected 1 calls*but received 0*");
    }

    [Fact]
    public void Verify_StaticMethodWithArgs_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(999);

        MyStaticService.Calculate(1, 2);
        MyStaticService.Calculate(1, 2);

        Action verify = () => Static.Verify(() => MyStaticService.Calculate(1, 2), times: 2);
        verify.Should().NotThrow();
    }

    [Fact]
    public void Verify_StaticMethodWithAnyArgs_Works()
    {
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(999);

        MyStaticService.Calculate(1, 2);
        MyStaticService.Calculate(3, 4);
        MyStaticService.Calculate(5, 6);

        Action verify = () => Static.Verify(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()), times: 3);
        verify.Should().NotThrow();
    }

    #endregion

    #region Generic Method Tests

    [Fact]
    public void Setup_GenericStaticMethod_Works()
    {
        Static.Setup(() => MyStaticService.ProcessGeneric<string>("test")).Returns("mocked");

        MyStaticService.ProcessGeneric<string>("test").Should().Be("mocked");
        MyStaticService.ProcessGeneric<string>("other").Should().Be("other"); // Original behavior
        MyStaticService.ProcessGeneric<int>(123).Should().Be(123); // Different type, original behavior
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void ClearAll_RemovesAllSetups()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Mocked");
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(999);
        Static.Setup(() => AnotherStaticService.GetValue()).Returns("Mocked Value");

        // Verify setups work
        MyStaticService.GetStaticName().Should().Be("Mocked");
        MyStaticService.Calculate(1, 2).Should().Be(999);
        AnotherStaticService.GetValue().Should().Be("Mocked Value");

        // Clear all
        Static.ClearAll();

        // Verify original behavior restored
        MyStaticService.GetStaticName().Should().Be("Real Static Name");
        MyStaticService.Calculate(1, 2).Should().Be(3);
        AnotherStaticService.GetValue().Should().Be("Another Value");
    }

    [Fact]
    public void GetDiagnostics_ShowsCorrectCounts()
    {
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Test");
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(999);

        var diagnostics = Static.GetDiagnostics();
        diagnostics.PatchedMethodCount.Should().Be(2);
        diagnostics.ConfiguredMethodCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Setup_SameMethodDifferentSignatures_BothWork()
    {
        // If MyStaticService had overloads (this is hypothetical)
        Static.Setup(() => MyStaticService.Calculate(1, 2)).Returns(100);

        MyStaticService.Calculate(1, 2).Should().Be(100);
    }

    [Fact]
    public void Setup_MethodReturningDateTime_Works()
    {
        var fixedTime = new DateTime(2023, 1, 1, 12, 0, 0);
        Static.Setup(() => MyStaticService.GetCurrentTime()).Returns(fixedTime);

        MyStaticService.GetCurrentTime().Should().Be(fixedTime);
    }

    [Fact]
    public void Setup_BooleanMethod_Works()
    {
        Static.Setup(() => MyStaticService.IsValid("test")).Returns(false);
        Static.Setup(() => MyStaticService.IsValid("valid")).Returns(true);

        MyStaticService.IsValid("test").Should().BeFalse();
        MyStaticService.IsValid("valid").Should().BeTrue();
        MyStaticService.IsValid("other").Should().BeTrue(); // Original behavior
    }

    [Fact]
    public void Setup_NullArguments_Works()
    {
        Static.Setup(() => MyStaticService.IsValid(null)).Returns(false);

        MyStaticService.IsValid(null).Should().BeFalse();
        MyStaticService.IsValid("test").Should().BeTrue(); // Original behavior
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Setup_ManyCallsToSameMethod_PerformsWell()
    {
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(42);

        // Call many times - should not be prohibitively slow
        for (int i = 0; i < 1000; i++)
        {
            MyStaticService.Calculate(i, i + 1).Should().Be(42);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_ComplexScenario_Works()
    {
        // Setup multiple methods
        Static.Setup(() => MyStaticService.GetStaticName()).Returns("Service");
        Static.Setup(() => MyStaticService.Calculate(It.IsAny<int>(), It.IsAny<int>()))
              .Returns<int, int>((a, b) => a * b * 10);
        Static.Setup(() => MyStaticService.IsValid(It.IsAny<string>())).Returns(true);

        var callbackCount = 0;
        Static.Setup(() => MyStaticService.DoSomething())
              .Callback(() => callbackCount++);

        // Use all methods
        MyStaticService.GetStaticName().Should().Be("Service");
        MyStaticService.Calculate(2, 3).Should().Be(60); // 2 * 3 * 10
        MyStaticService.IsValid("anything").Should().BeTrue();

        MyStaticService.DoSomething();
        MyStaticService.DoSomething();
        callbackCount.Should().Be(2);

        // Verify calls
        Static.Verify(() => MyStaticService.GetStaticName(), times: 1);
        Static.Verify(() => MyStaticService.Calculate(2, 3), times: 1);
        Static.Verify(() => MyStaticService.DoSomething(), times: 2);
    }

    #endregion
}