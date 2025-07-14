# IAsyncLifetime Implementation Guide

## Overview

The `IntegrationTestBase` class now implements xUnit's `IAsyncLifetime` interface, providing proper async initialization and cleanup for integration tests.

## Benefits

1. **Async Initialization**: Database connections and services can be initialized asynchronously before tests run
2. **Proper Cleanup**: Connections are automatically closed and disposed after tests complete
3. **Resource Tracking**: Active connections are tracked and cleaned up automatically
4. **Extensibility**: Derived classes can override `OnInitializeAsync` and `OnDisposeAsync` for custom behavior

## Usage

### Basic Usage

```csharp
public class MyIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public MyIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MyTest()
    {
        // CheckSkipIntegrationTest() to respect RUN_INTEGRATION_TESTS flag
        CheckSkipIntegrationTest();
        
        // CreateSqlServerConnection() automatically tracks the connection
        using var connection = CreateSqlServerConnection();
        await connection.OpenAsync();
        
        // Your test logic here
    }
}
```

### Advanced Usage with Custom Initialization

```csharp
public class AdvancedIntegrationTests : IntegrationTestBase
{
    private TestData _testData;

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        
        // Custom async initialization
        _testData = await LoadTestDataAsync();
    }

    protected override async Task OnDisposeAsync()
    {
        // Custom async cleanup
        await CleanupTestDataAsync();
        
        await base.OnDisposeAsync();
    }
}
```

## Key Features

### Automatic Connection Tracking

All connections created via `CreateSqlServerConnection()` are automatically tracked and closed during disposal:

```csharp
// This connection is automatically tracked
var connection = CreateSqlServerConnection();
// No need to manually close - handled by DisposeAsync
```

### Lifecycle Methods

1. **InitializeAsync**: Called before any test method runs
   - Loads environment variables
   - Sets up dependency injection
   - Initializes services

2. **OnInitializeAsync**: Virtual method for custom initialization
   - Override in derived classes
   - Called after base initialization

3. **DisposeAsync**: Called after all test methods complete
   - Closes all tracked connections
   - Disposes services

4. **OnDisposeAsync**: Virtual method for custom cleanup
   - Override in derived classes
   - Called before base cleanup

## Best Practices

1. **Always call base methods** when overriding:
   ```csharp
   protected override async Task OnInitializeAsync()
   {
       await base.OnInitializeAsync();
       // Your custom initialization
   }
   ```

2. **Use CheckSkipIntegrationTest()** to respect test settings:
   ```csharp
   [Fact]
   public async Task MyIntegrationTest()
   {
       CheckSkipIntegrationTest();
       // Test code
   }
   ```

3. **Let the base class handle connection lifecycle**:
   - Don't manually track connections
   - Use `CreateSqlServerConnection()` method
   - Connections are automatically cleaned up

4. **Handle async operations properly**:
   ```csharp
   protected override async Task OnInitializeAsync()
   {
       await base.OnInitializeAsync();
       
       // Use await for async operations
       await PrepareTestDataAsync();
       
       // Don't use .Result or .Wait()
   }
   ```

## Migration Guide

If you have existing tests that inherit from `IntegrationTestBase`:

1. **Remove manual connection tracking** - the base class now handles this
2. **Move initialization logic** from constructors to `OnInitializeAsync`
3. **Move cleanup logic** from `Dispose` to `OnDisposeAsync`
4. **Update connection creation** to use the tracked method

### Before:
```csharp
public MyTests()
{
    _connection = new SqlServerConnection(connectionString);
}

public void Dispose()
{
    _connection?.Dispose();
}
```

### After:
```csharp
protected override async Task OnInitializeAsync()
{
    await base.OnInitializeAsync();
    // Initialization logic here
}

[Fact]
public async Task MyTest()
{
    var connection = CreateSqlServerConnection(); // Automatically tracked
    // Test logic
}
```

## Testing the Implementation

See `AsyncLifetimeTests.cs` for examples of testing the IAsyncLifetime implementation.