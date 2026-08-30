# NSubstituteConcrete

A .NET library that extends NSubstitute to support mocking concrete classes and static methods using Harmony runtime patching.

[![Build Status](https://github.com/jayarrowz/NSubstituteConcrete/workflows/CI/badge.svg)](https://github.com/jayarrowz/NSubstituteConcrete/actions)
[![NuGet](https://img.shields.io/nuget/v/NSubstituteConcrete.svg)](https://www.nuget.org/packages/NSubstituteConcrete)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-support-yellow.svg)](https://buymeacoffee.com/jayarrowz)

## Features

- **Concrete class mocking**: Mock non-virtual methods in concrete classes
- **Static method mocking**: Mock static methods with familiar NSubstitute syntax
- **Harmony-powered**: Uses Harmony library for runtime IL patching
- **NSubstitute syntax**: Familiar API for existing NSubstitute users
- **Zero configuration**: Works with existing concrete classes without modification

## Installation

```bash
dotnet add package NSubstituteConcrete
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

| Feature | NSubstituteConcrete | Standard NSubstitute | Moq | TypeMock Isolator | Microsoft Fakes |
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

## Hosting this library

A library layering its own setup and verification API over this one drives the interceptors directly
rather than through `Setup`. The seams for that are:

| Seam | What it is for |
| --- | --- |
| `NSubstituteExtensions.GetHarmonyInterceptor(substitute)` | Reaches the interceptor behind a concrete substitute. |
| `HarmonyMethodInterceptor.PatchAll()` / `StaticMethodInterceptor.PatchAll(type)` | Patches the whole surface, so calls nobody configured are still observed. |
| `SetFallback(InterceptionFallback)` on either interceptor | Answers a call nothing else configured. Return `Interception.RunOriginal` to let the real method run; anything else, including `null`, becomes the result. |
| `MethodCall.Ordinal` | Orders calls across every interceptor in the process. `CalledAt` has no useful resolution for this. |
| `StaticMethodInterceptor.ClearFor(type)` / `ClearFor(method)` | Unpatches one type's — or one method's — statics, leaving other patches alone. |
| `MethodKeys.For(method)` | The signature key configuration and call records are filed under. |

## Release notes

### Unreleased

- **`Verify` counted calls to the wrong method.** `HarmonyMethodInterceptor.GetCallCount` was declared
  `new` rather than `override`, so a call through the base type ran the base implementation — which
  ignored the method entirely and counted every call whose arguments matched. `GetCallCount` and
  `GetReceivedCalls` are now `virtual`/`override`, and both compare the method as well as the arguments.
- **Configuration was keyed on the method name.** Two overloads shared one bucket, and
  `InterceptCall(string, …)` could not tell them apart. Both interceptors now key on the full
  signature through the new `MethodKeys` helper, which the static side already did.
- **Recorded calls carried no usable ordering.** `MethodCall` gains `Ordinal`, stamped from a
  process-wide counter, and `CalledAt` is now set on the instance side too.
- **Statics could only be cleared globally.** `StaticMethodInterceptor` gains `ClearFor(Type)`,
  `ClearFor(MethodInfo)`, `ClearCallsFor`, `ClearSetupsFor`, `PatchAll(Type)` and read-only views of
  the patched set, so a scope covering one type can be torn down on its own.
- **No way to patch a type's whole surface.** `HarmonyMethodInterceptor.PatchAll()` patches public and
  protected instance methods and property accessors, excluding `object`'s own members.
- **The substitute registries used value equality and leaked.** They now key on identity, so a mocked
  class that overrides `Equals`/`GetHashCode` no longer collides with another instance of itself, and
  `Unpatch` drops the strong reference to the substitute.
- **Two substitutes of the same type patched the same method twice**, so a single call was intercepted
  and recorded twice. Instance patches are now reference-counted behind one shared Harmony instance.
- **The "no match after all" path called the wrong method.** It reflected onto `_proxy.GetType().BaseType`,
  which is right for a generated proxy and wrong for the real instance the Harmony path uses. The
  prefix now returns `true` and lets the original run.
- **No fallback hook.** `SetFallback` on both interceptors is the seam a host library needs for strict
  behaviour, default-value providers and its own setup engine.

## Limitations

- **Method inlining**: Aggressively optimized methods may be inlined and cannot be patched
- **Generic methods**: Generic method patches may affect all type instantiations
- **Native methods**: P/Invoke and external methods cannot be patched
- **Static constructors**: May run at unexpected times during patching
- **Patches are process-wide**: a patch applies to the method, not to one instance. The prefix filters
  by instance, but test collections touching the same static type must be serialised.

## Requirements

- .NET 6.0 or later
- Compatible with .NET Framework 4.8+ via .NET Standard 2.0

## Dependencies

- [NSubstitute](https://github.com/nsubstitute/NSubstitute) - Base mocking library
- [Lib.Harmony](https://github.com/pardeike/Harmony) - Runtime IL patching

## Building

```bash
git clone https://github.com/jayarrowz/NSubstituteConcrete.git
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

## ☕ Support

If you find NSubstituteConcrete useful and want to support its development:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-support-yellow.svg)](https://buymeacoffee.com/jayarrowz)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Authors

JayArrowz

## Acknowledgments

- [NSubstitute](https://nsubstitute.github.io/) - Inspiration and base API design
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching capabilities

## Related Projects

- [NSubstitute](https://github.com/nsubstitute/NSubstitute) - Original mocking library
- [Moq](https://github.com/moq/moq4) - Alternative mocking framework
- [Pose](https://github.com/tonerdo/pose) - Alternative method replacement library
