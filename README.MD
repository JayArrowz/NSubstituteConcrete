# NSubstitute.Concrete

A .NET library that extends NSubstitute to support mocking concrete classes and static methods using Harmony runtime patching.

[![Build Status](https://github.com/jayarrowz/NSubstituteConcrete/workflows/CI/badge.svg)](https://github.com/jayarrowz/NSubstituteConcrete/actions)
[![NuGet](https://img.shields.io/nuget/v/NSubstituteConcrete.svg)](https://www.nuget.org/packages/NSubstituteConcrete)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Concrete class mocking**: Mock non-virtual methods in concrete classes
- **Static method mocking**: Mock static methods with familiar NSubstitute syntax
- **Harmony-powered**: Uses Harmony library for runtime IL patching
- **NSubstitute syntax**: Familiar API for existing NSubstitute users
- **Zero configuration**: Works with existing concrete classes without modification

## Installation

```bash
dotnet add package NSubstitute.Concrete
```

## Usage

### Concrete Class Mocking

```csharp
using NSubstitute.Concrete;

public class UserService
{
    public string GetUserName(int id) => $"User{id}";
    public void SaveUser(User user) { /* implementation */ }
}

// Create substitute
var userService = NSubstituteExtensions.ForConcrete<UserService>();

// Setup methods
userService.Setup(x => x.GetUserName(1)).Returns("John Doe");
userService.Setup(x => x.SaveUser(It.IsAny<User>()))
           .Callback(() => Console.WriteLine("User saved"));

// Verify calls
userService.Verify(x => x.GetUserName(1), times: 1);

// Cleanup when done
userService.Cleanup();
```

### Static Method Mocking

```csharp
using NSubstitute.Concrete;

public static class FileHelper
{
    public static string ReadFile(string path) => File.ReadAllText(path);
    public static void DeleteFile(string path) => File.Delete(path);
}

// Setup static methods
Static.Setup(() => FileHelper.ReadFile("test.txt")).Returns("mock content");
Static.Setup(() => FileHelper.DeleteFile(It.IsAny<string>()))
      .Callback(() => Console.WriteLine("File deleted"));

// Use normally
string content = FileHelper.ReadFile("test.txt"); // Returns "mock content"

// Verify calls
Static.Verify(() => FileHelper.ReadFile("test.txt"), times: 1);

// Cleanup
Static.ClearAll();
```

### Async Methods

```csharp
var service = NSubstituteExtensions.ForConcrete<AsyncService>();

// Async methods with return values
service.SetupAsync(x => x.GetDataAsync(1)).Returns("result");

// Async void methods  
service.SetupAsync(x => x.ProcessAsync())
       .Callback(() => Console.WriteLine("Processing"));

// Static async methods
Static.SetupAsync(() => HttpClient.GetAsync("url")).Returns("response");

// Cleanup
service.Cleanup();
```

### Properties

```csharp
var service = NSubstituteExtensions.ForConcrete<ConfigService>();

// Setup property getters
service.SetupProperty(x => x.ConnectionString).Returns("test-connection");

// Set property values directly
service.SetProperty(x => x.Timeout, TimeSpan.FromSeconds(30));

// Cleanup
service.Cleanup();
```

## Comparison with Other Testing Libraries

| Feature | NSubstitute.Concrete | Standard NSubstitute | Moq | TypeMock Isolator | Microsoft Fakes |
|---------|:-------------------:|:-------------------:|:---:|:----------------:|:---------------:|
| **Concrete classes** | ✅ All methods | ❌ Virtual only | ❌ Virtual only | ✅ All methods | ✅ All methods |
| **Static methods** | ✅ Full support | ❌ Not supported | ❌ Not supported | ✅ Full support | ✅ Full support |
| **Non-virtual methods** | ✅ Supported | ❌ Not supported | ❌ Not supported | ✅ Supported | ✅ Supported |
| **Sealed classes** | ✅ Supported | ❌ Not supported | ❌ Not supported | ✅ Supported | ✅ Supported |
| **Cost** | 🆓 **FREE** | 🆓 Free | 🆓 Free | 💰 ~$1,000+/license | 💰 VS Enterprise required |
| **Performance** | ⚡ Fast | ⚡ Fast | ⚡ Fast | ⚠️ Slower (profiler) | ⚠️ Slower (shims) |
| **Setup complexity** | ✅ Simple | ✅ Simple | ✅ Simple | ⚠️ Complex setup | ⚠️ Complex setup |
| **Syntax familiarity** | ✅ NSubstitute | ✅ NSubstitute | ❌ Different | ❌ Different | ❌ Different |

## Cleanup

### Test Cleanup
```csharp
public class MyTests : IDisposable
{
    public void Dispose()
    {
        // Clean up all substitutes and static patches
        ConcreteCleanupExtensions.ClearAll();
    }
}
```

### Individual Cleanup
```csharp
// Clean up specific substitute
substitute.Cleanup();

// Clean up only static method patches
Static.ClearAll();
```

## Limitations

- **Method inlining**: Aggressively optimized methods may be inlined and cannot be patched
- **Generic methods**: Generic method patches may affect all type instantiations
- **Native methods**: P/Invoke and external methods cannot be patched
- **Static constructors**: May run at unexpected times during patching

## Requirements

- .NET 6.0 or later
- Compatible with .NET Framework 4.8+ via .NET Standard 2.0

## Dependencies

- [NSubstitute](https://github.com/nsubstitute/NSubstitute) - Base mocking library
- [Lib.Harmony](https://github.com/pardeike/Harmony) - Runtime IL patching

## Building

```bash
git clone https://github.com/yourname/NSubstitute.Concrete.git
cd NSubstitute.Concrete
dotnet restore
dotnet build
dotnet test
```

## Project Structure

```
NSubstitute.Concrete/
├── Core/                    # Core infrastructure
├── Static/                  # Static method support  
├── Setup/                   # Method setup classes
├── Utilities/               # Helper classes
└── Cleanup/                 # Cleanup and diagnostics
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure tests pass and add tests for new functionality.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [NSubstitute](https://nsubstitute.github.io/) - Inspiration and base API design
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching capabilities

## Related Projects

- [NSubstitute](https://github.com/nsubstitute/NSubstitute) - Original mocking library
- [Moq](https://github.com/moq/moq4) - Alternative mocking framework
- [Pose](https://github.com/tonerdo/pose) - Alternative method replacement library
