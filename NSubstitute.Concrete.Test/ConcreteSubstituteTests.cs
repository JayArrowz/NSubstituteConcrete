using FluentAssertions;
using NSubstitute.Concrete.Cleanup;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Test.Fixtures;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Test;

public partial class ConcreteSubstituteTests
{
    #region Basic Setup and Returns
    [Fact]
    public void Setup_ReturnsConfiguredValue()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5)).Returns(10);
        var result = concrete.IncrementAndReturn(5);
        result.Should().Be(10);
        concrete.Cleanup();
    }

    [Fact]
    public void Setup_WithDifferentArgument_ReturnsBaseBehavior()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5)).Returns(10);
        var result = concrete.IncrementAndReturn(3);
        result.Should().Be(4); // 1 (ID) + 3
    }

    [Fact]
    public void Setup_ReturnsInOrder_ReturnsSequence()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(0)).ReturnsInOrder(10, 20, 30);

        concrete.IncrementAndReturn(0).Should().Be(10);
        concrete.IncrementAndReturn(0).Should().Be(20);
        concrete.IncrementAndReturn(0).Should().Be(30);
        concrete.IncrementAndReturn(0).Should().Be(30); // Last value repeats
        concrete.Cleanup();
    }
    #endregion

    #region Property Setup
    [Fact]
    public void SetupProperty_ReturnsConfiguredValue()
    {
        // Arrange
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupProperty(x => x.Name).Returns("Test");
        var result = concrete.Name;
        result.Should().Be("Test");
    }

    [Fact]
    public void SetProperty_DirectlySetsValue()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetProperty(x => x.Name, "DirectSet");
        concrete.Name.Should().Be("DirectSet");
        concrete.Cleanup();
    }
    #endregion

    #region Async Methods
    [Fact]
    public async Task SetupAsync_ReturnsConfiguredValue()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.GetDataAsync(1)).Returns(Task.FromResult(2));
        var result = await concrete.GetDataAsync(1);
        result.Should().Be(2);
        concrete.Cleanup();
    }

    [Fact]
    public async Task SetupAsync_WithTaskReturn_ReturnsConfiguredTask()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        var task = Task.FromResult(100);
        concrete.SetupAsync(x => x.GetDataAsync(10)).Returns(task);
        var result = await concrete.GetDataAsync(10);
        result.Should().Be(100);
        concrete.Cleanup();
    }

    [Fact]
    public async Task SetupAsync_ThrowsException()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.GetDataAsync(2)).Throws<InvalidOperationException>();
        Func<Task> act = () => concrete.GetDataAsync(2);
        await act.Should().ThrowAsync<InvalidOperationException>();
        concrete.Cleanup();
    }
    #endregion

    #region Callbacks
    [Fact]
    public void Callback_ExecutesAction()
    {
        int counter = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.DoSomething(0, ""))
            .Callback(() => counter++);
        concrete.DoSomething(0, "");
        counter.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void Callback_WithParameters_ReceivesArguments()
    {
        int receivedA = 0;
        string receivedB = "";
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.DoSomething(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((a, b) =>
            {
                receivedA = a;
                receivedB = b;
            });
        concrete.DoSomething(5, "test");
        receivedA.Should().Be(5);
        receivedB.Should().Be("test");
        concrete.Cleanup();
    }

    [Fact]
    public async Task CallbackAsync_ExecutesAsyncAction()
    {
        // Arrange
        int counter = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.DoSomethingAsync())
            .CallbackAsync(async () =>
            {
                await Task.Delay(10);
                counter++;
            });
        await concrete.DoSomethingAsync();
        counter.Should().Be(1);
        concrete.Cleanup();
    }
    #endregion

    #region Void Methods
    [Fact]
    public void SetupVoidMethod_WithCallback_Works()
    {
        int counter = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.Abc(It.IsAny<int>()))
            .Callback(() => counter++);
        concrete.Abc(1);
        counter.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void VoidMethod_ThrowsException()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.Abc(99))
            .Throws<InvalidOperationException>();
        Action act = () => concrete.Abc(99);
        act.Should().Throw<InvalidOperationException>();
        concrete.Cleanup();
    }
    #endregion

    #region Verification
    [Fact]
    public void Verify_CallCount_Works()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(t => t.IncrementAndReturn(1));
        concrete.IncrementAndReturn(1);
        concrete.IncrementAndReturn(1);
        concrete.IncrementAndReturn(2);
        concrete.Verify(x => x.IncrementAndReturn(1), 2);
        concrete.Cleanup();
    }

    [Fact]
    public void Verify_WrongCallCount_Throws()
    {
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.IncrementAndReturn(1);
        Assert.Throws<Exception>(() =>
            concrete.Verify(x => x.IncrementAndReturn(1), 2));
        concrete.Cleanup();
    }
    #endregion

    #region Cleanup and Diagnostics
    [Fact]
    public void Cleanup_RemovesInterceptor()
    {

        var prevDiagnostics = ConcreteCleanupExtensions.GetDiagnostics();
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        concrete.Cleanup();
        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        diagnostics.ActiveSubstituteCount.Should().Be(prevDiagnostics.ActiveSubstituteCount);
    }

    [Fact]
    public void ClearAll_ResetsEverything()
    {
        var concrete1 = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        var concrete2 = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(2);

        ConcreteCleanupExtensions.ClearAll();
        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        diagnostics.ActiveSubstituteCount.Should().Be(0);
        diagnostics.CachedProxyTypeCount.Should().Be(0);
        concrete1.Cleanup();
        concrete2.Cleanup();
    }

    [Fact]
    public void GetDiagnostics_ShowsCorrectCounts()
    {
        ConcreteCleanupExtensions.ClearAll();
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        diagnostics.ActiveSubstituteCount.Should().Be(1);
        diagnostics.CachedProxyTypeCount.Should().BeGreaterThan(0);
        concrete.Cleanup();
    }
    #endregion

    #region Advanced Scenarios
    [Fact]
    public void ReturnsAndCallback_CombinesBehavior()
    {
        int callbackCount = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5))
            .ReturnsAndCallback(10, () => callbackCount++);

        var result = concrete.IncrementAndReturn(5);

        result.Should().Be(10);
        callbackCount.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public async Task DelayAndCallback_WorksAsExpected()
    {
        int callbackCount = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.DoSomethingAsync())
            .DelayAndCallback(TimeSpan.FromMilliseconds(100), () => callbackCount++);

        var start = DateTime.UtcNow;
        await concrete.DoSomethingAsync();
        var duration = DateTime.UtcNow - start;

        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
        callbackCount.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void Setup_WithValueFactory_Works()
    {
        int callCount = 0;
        var concrete = NSubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(It.IsAny<int>()))
            .Returns(() => ++callCount * 10);
        concrete.IncrementAndReturn(1).Should().Be(10);
        concrete.IncrementAndReturn(1).Should().Be(20);
        concrete.Cleanup();
    }

    #endregion
}