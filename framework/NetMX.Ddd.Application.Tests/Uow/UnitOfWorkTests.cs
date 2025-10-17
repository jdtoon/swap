using Moq;
using NetMX.Ddd.Application.Uow;
using Xunit;

namespace NetMX.Ddd.Application.Tests.Uow;

/// <summary>
/// Unit tests for Unit of Work implementation
/// Testing the TDD way - tests first, then implementation!
/// </summary>
public class UnitOfWorkTests
{
    [Fact]
    public void Should_BeginUnitOfWork_WithoutTransaction()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();

        // Act
        var uow = (UnitOfWork)uowManager.Begin();

        // Assert
        Assert.NotNull(uow);
        Assert.False(uow.IsCompleted);
        Assert.False(uow.IsDisposed);
    }

    [Fact]
    public async Task Should_CompleteUnitOfWork_Successfully()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        var uow = (UnitOfWork)uowManager.Begin();
        
        var saveChangesCalled = false;
        uow.OnSaveChanges = () =>
        {
            saveChangesCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await uow.CompleteAsync();

        // Assert
        Assert.True(saveChangesCalled);
        Assert.True(uow.IsCompleted);
    }

    [Fact]
    public void Should_RollbackChanges_WhenNotCompleted()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        var uow = (UnitOfWork)uowManager.Begin();
        
        var saveChangesCalled = false;
        uow.OnSaveChanges = () =>
        {
            saveChangesCalled = true;
            return Task.CompletedTask;
        };

        // Act
        uow.Dispose();

        // Assert - Dispose without Complete should NOT save
        Assert.False(saveChangesCalled);
        Assert.True(uow.IsDisposed);
    }

    [Fact]
    public async Task Should_ThrowException_WhenCompletingTwice()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        var uow = (UnitOfWork)uowManager.Begin();
        
        uow.OnSaveChanges = () => Task.CompletedTask;
        await uow.CompleteAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CompleteAsync());
    }

    [Fact]
    public async Task Should_SupportNestedUnitOfWork()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        
        var outerSaveCalled = false;
        var innerSaveCalled = false;

        // Act
        using (var outerUow = (UnitOfWork)uowManager.Begin(requiresNew: true))
        {
            outerUow.OnSaveChanges = () =>
            {
                outerSaveCalled = true;
                return Task.CompletedTask;
            };

            using (var innerUow = (UnitOfWork)uowManager.Begin(requiresNew: true))
            {
                innerUow.OnSaveChanges = () =>
                {
                    innerSaveCalled = true;
                    return Task.CompletedTask;
                };

                await innerUow.CompleteAsync();
            }

            await outerUow.CompleteAsync();
        }

        // Assert
        Assert.True(innerSaveCalled); // inner UoW should save when completed
        Assert.True(outerSaveCalled); // outer UoW should save when completed
    }

    [Fact]
    public async Task Should_RollbackBothUoW_WhenInnerFails()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        
        var outerSaveCalled = false;
        var innerSaveCalled = false;

        // Act
        Exception? caughtException = null;
        try
        {
            using (var outerUow = (UnitOfWork)uowManager.Begin(requiresNew: true))
            {
                outerUow.OnSaveChanges = () =>
                {
                    outerSaveCalled = true;
                    return Task.CompletedTask;
                };

                using (var innerUow = (UnitOfWork)uowManager.Begin(requiresNew: true))
                {
                    innerUow.OnSaveChanges = () =>
                    {
                        innerSaveCalled = true;
                        throw new InvalidOperationException("Simulated error");
                    };

                    await innerUow.CompleteAsync(); // This will throw
                }

                await outerUow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.IsType<InvalidOperationException>(caughtException);
        Assert.True(innerSaveCalled); // inner save was attempted
        Assert.False(outerSaveCalled); // outer save should not be called due to inner exception
    }

    [Fact]
    public async Task Should_GetCurrentUnitOfWork()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();

        // Act & Assert - No current UoW
        Assert.Null(uowManager.Current);

        using (var uow = uowManager.Begin())
        {
            // Current should be set
            Assert.NotNull(uowManager.Current);
            Assert.Same(uow, uowManager.Current);
        }

        // After dispose, current should be null again
        Assert.Null(uowManager.Current);
    }

    [Fact]
    public async Task Should_ExecuteOnCompleted_Callbacks()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        var uow = (UnitOfWork)uowManager.Begin();
        
        var callbackExecuted = false;
        uow.OnCompleted(() =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        });

        uow.OnSaveChanges = () => Task.CompletedTask;

        // Act
        await uow.CompleteAsync();

        // Assert
        Assert.True(callbackExecuted);
    }

    [Fact]
    public void Should_NotExecuteOnCompleted_WhenRolledBack()
    {
        // Arrange
        var uowManager = new UnitOfWorkManager();
        var uow = (UnitOfWork)uowManager.Begin();
        
        var callbackExecuted = false;
        uow.OnCompleted(() =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        });

        // Act - Dispose without completing
        uow.Dispose();

        // Assert
        Assert.False(callbackExecuted);
    }
}
