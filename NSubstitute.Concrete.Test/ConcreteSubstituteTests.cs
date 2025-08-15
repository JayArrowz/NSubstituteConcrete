using FluentAssertions;

namespace NSubstitute.Concrete.Test;

public partial class ConcreteSubstituteTests
{
    #region Basic Setup and Returns
    [Fact]
    public void Setup_ReturnsConfiguredValue()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5)).Returns(10);

        // Act
        var result = concrete.IncrementAndReturn(5);

        // Assert
        result.Should().Be(10);
        concrete.Cleanup();
    }

    [Fact]
    public void Setup_WithDifferentArgument_ReturnsBaseBehavior()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5)).Returns(10);

        // Act
        var result = concrete.IncrementAndReturn(3);

        // Assert
        result.Should().Be(4); // 1 (ID) + 3
    }

    [Fact]
    public void Setup_ReturnsInOrder_ReturnsSequence()
    {
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
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
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupProperty(x => x.Name).Returns("Test");

        // Act
        var result = concrete.Name;

        // Assert
        result.Should().Be("Test");
    }

    [Fact]
    public void SetProperty_DirectlySetsValue()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        // Act
        concrete.SetProperty(x => x.Name, "DirectSet");

        // Assert
        concrete.Name.Should().Be("DirectSet");
        concrete.Cleanup();
    }
    #endregion

    #region Async Methods
    [Fact]
    public async Task SetupAsync_ReturnsConfiguredValue()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.GetDataAsync(1)).Returns(Task.FromResult(2));

        // Act
        var result = await concrete.GetDataAsync(1);

        // Assert
        result.Should().Be(2);
        concrete.Cleanup();
    }

    [Fact]
    public async Task SetupAsync_WithTaskReturn_ReturnsConfiguredTask()
    {
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        var task = Task.FromResult(100);

        concrete.SetupAsync(x => x.GetDataAsync(10)).Returns(task);

        var result = await concrete.GetDataAsync(10);

        result.Should().Be(100);
        concrete.Cleanup();
    }

    [Fact]
    public async Task SetupAsync_ThrowsException()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.GetDataAsync(2)).Throws<InvalidOperationException>();

        // Act
        Func<Task> act = () => concrete.GetDataAsync(2);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        concrete.Cleanup();
    }
    #endregion

    #region Callbacks
    [Fact]
    public void Callback_ExecutesAction()
    {
        // Arrange
        int counter = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.DoSomething(0, ""))
            .Callback(() => counter++);

        // Act
        concrete.DoSomething(0, "");

        // Assert
        counter.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void Callback_WithParameters_ReceivesArguments()
    {
        // Arrange
        int receivedA = 0;
        string receivedB = "";
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.DoSomething(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((a, b) =>
            {
                receivedA = a;
                receivedB = b;
            });

        // Act
        concrete.DoSomething(5, "test");

        // Assert
        receivedA.Should().Be(5);
        receivedB.Should().Be("test");
        concrete.Cleanup();
    }

    [Fact]
    public async Task CallbackAsync_ExecutesAsyncAction()
    {
        // Arrange
        int counter = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.DoSomethingAsync())
            .CallbackAsync(async () =>
            {
                await Task.Delay(10);
                counter++;
            });

        // Act
        await concrete.DoSomethingAsync();

        // Assert
        counter.Should().Be(1);
        concrete.Cleanup();
    }
    #endregion

    #region Void Methods
    [Fact]
    public void SetupVoidMethod_WithCallback_Works()
    {
        // Arrange
        int counter = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.Abc(It.IsAny<int>()))
            .Callback(() => counter++);

        // Act
        concrete.Abc(1);

        // Assert
        counter.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void VoidMethod_ThrowsException()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.Abc(99))
            .Throws<InvalidOperationException>();

        // Act
        Action act = () => concrete.Abc(99);

        // Assert
        act.Should().Throw<InvalidOperationException>();
        concrete.Cleanup();
    }
    #endregion

    #region Verification
    [Fact]
    public void Verify_CallCount_Works()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        concrete.Setup(t => t.IncrementAndReturn(1));

        // Act
        concrete.IncrementAndReturn(1);
        concrete.IncrementAndReturn(1);
        concrete.IncrementAndReturn(2);

        // Assert
        concrete.Verify(x => x.IncrementAndReturn(1), 2);
        concrete.Cleanup();
    }

    [Fact]
    public void Verify_WrongCallCount_Throws()
    {
        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.IncrementAndReturn(1);
        
        // Act & Assert
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

        // Arrange
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        // Act
        concrete.Cleanup();
        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        // Assert
        diagnostics.ActiveSubstituteCount.Should().Be(prevDiagnostics.ActiveSubstituteCount);
    }

    [Fact]
    public void ClearAll_ResetsEverything()
    {
        // Arrange
        var concrete1 = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        var concrete2 = SubstituteExtensions.ForConcrete<SampleConcreteClass>(2);

        // Act
        ConcreteCleanupExtensions.ClearAll();
        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        // Assert
        diagnostics.ActiveSubstituteCount.Should().Be(0);
        diagnostics.CachedProxyTypeCount.Should().Be(0);
        concrete1.Cleanup();
        concrete2.Cleanup();
    }

    [Fact]
    public void GetDiagnostics_ShowsCorrectCounts()
    {
        // Arrange
        ConcreteCleanupExtensions.ClearAll();
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);

        // Act
        var diagnostics = ConcreteCleanupExtensions.GetDiagnostics();

        // Assert
        diagnostics.ActiveSubstituteCount.Should().Be(1);
        diagnostics.CachedProxyTypeCount.Should().BeGreaterThan(0);
        concrete.Cleanup();
    }
    #endregion

    #region Advanced Scenarios
    [Fact]
    public void ReturnsAndCallback_CombinesBehavior()
    {
        // Arrange
        int callbackCount = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(5))
            .ReturnsAndCallback(10, () => callbackCount++);

        // Act
        var result = concrete.IncrementAndReturn(5);

        // Assert
        result.Should().Be(10);
        callbackCount.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public async Task DelayAndCallback_WorksAsExpected()
    {
        // Arrange
        int callbackCount = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.SetupAsync(x => x.DoSomethingAsync())
            .DelayAndCallback(TimeSpan.FromMilliseconds(100), () => callbackCount++);

        // Act
        var start = DateTime.UtcNow;
        await concrete.DoSomethingAsync();
        var duration = DateTime.UtcNow - start;

        // Assert
        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
        callbackCount.Should().Be(1);
        concrete.Cleanup();
    }

    [Fact]
    public void Setup_WithValueFactory_Works()
    {
        // Arrange
        int callCount = 0;
        var concrete = SubstituteExtensions.ForConcrete<SampleConcreteClass>(1);
        concrete.Setup(x => x.IncrementAndReturn(It.IsAny<int>()))
            .Returns(() => ++callCount * 10);

        // Act & Assert
        concrete.IncrementAndReturn(1).Should().Be(10);
        concrete.IncrementAndReturn(1).Should().Be(20);
        concrete.Cleanup();
    }

    #endregion

    #region Static services
    
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
        MyStaticService.Calculate(1,2).Should().Be(1000);
        MyStaticService.Calculate(3,2).Should().Be(5);
        Static.ClearAll();
    }
    #endregion
}