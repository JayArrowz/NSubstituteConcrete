using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NSubstitute.Concrete.Callbacks;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Statics;

/// <summary>
/// Global interceptor for static method substitution using Harmony
/// </summary>
public class StaticMethodInterceptor
{
    private static readonly StaticMethodInterceptor _instance = new StaticMethodInterceptor();
    public static StaticMethodInterceptor Instance => _instance;

    private readonly Harmony _harmony;
    internal readonly ConcurrentDictionary<string, object> _configuredReturns = new ConcurrentDictionary<string, object>();
    internal readonly ConcurrentDictionary<string, List<(object[] Arguments, object ReturnValue)>> _methodConfigurations = new ConcurrentDictionary<string, List<(object[], object)>>();
    private readonly ConcurrentDictionary<string, List<MethodCall>> _receivedCalls = new ConcurrentDictionary<string, List<MethodCall>>();
    internal readonly ConcurrentDictionary<MethodBase, bool> _patchedMethods = new ConcurrentDictionary<MethodBase, bool>();
    private InterceptionFallback _fallback;

    private StaticMethodInterceptor()
    {
        _harmony = new Harmony("NSubstitute.Concrete.Static");
    }

    /// <summary>
    /// Installs the hook consulted when nothing configured here answers a static call. See
    /// <see cref="InterceptionFallback"/>. Pass <c>null</c> to remove it.
    /// </summary>
    public void SetFallback(InterceptionFallback fallback)
    {
        _fallback = fallback;
    }

    /// <summary>Whether a fallback hook is installed.</summary>
    public bool HasFallback => _fallback != null;

    /// <summary>The static methods currently patched.</summary>
    public IReadOnlyList<MethodBase> PatchedMethods => _patchedMethods.Keys.ToList().AsReadOnly();

    /// <summary>The number of static methods currently patched.</summary>
    public int PatchedMethodCount => _patchedMethods.Count;

    /// <summary>The number of static methods that have at least one configured result.</summary>
    public int ConfiguredMethodCount => _configuredReturns.Count + _methodConfigurations.Count;

    /// <summary>
    /// Patch a static method for interception
    /// </summary>
    public void PatchMethod(MethodInfo method)
    {
        if (!method.IsStatic)
            throw new ArgumentException("Method must be static", nameof(method));

        if (_patchedMethods.ContainsKey(method))
            return;

        try
        {
            _harmony.Patch(method, prefix: new HarmonyMethod(PrefixFor(method)));
            _patchedMethods[method] = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to patch static method {method.DeclaringType?.Name}.{method.Name}", ex);
        }
    }

    /// <summary>
    /// Patches every static method and static property accessor of <paramref name="type"/>, so that
    /// calls nobody configured are still observed. Required by anything that reasons about
    /// unconfigured calls: strict behaviour, default-value providers, "no other calls" assertions.
    /// </summary>
    public void PatchAll(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        foreach (var method in InterceptableMethods(type))
        {
            try
            {
                PatchMethod(method);
            }
            catch (InvalidOperationException)
            {
                // A method Harmony cannot patch stays unpatched; the rest of the type is still
                // worth intercepting.
            }
        }
    }

    /// <summary>The static methods of <paramref name="type"/> that are worth patching.</summary>
    public static IEnumerable<MethodInfo> InterceptableMethods(Type type)
    {
        if (type == null) yield break;

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(flags))
        {
            if (method.IsPrivate && !method.IsFamily) continue;
            if (!method.IsPublic && !method.IsFamily && !method.IsFamilyOrAssembly) continue;
            if (method.IsGenericMethodDefinition) continue;
            if ((method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0) continue;
            if (method.GetMethodBody() == null) continue;
            if (method.GetParameters().Any(p => p.ParameterType.IsByRef || p.ParameterType.IsPointer)) continue;

            yield return method;
        }
    }

    /// <summary>
    /// Configure a static method return value
    /// </summary>
    public void ConfigureReturn(MethodInfo method, object[] arguments, object returnValue)
    {
        var methodKey = MethodKeys.For(method);

        if (arguments == null || arguments.Length == 0)
        {
            _configuredReturns[methodKey] = returnValue;
        }
        else
        {
            var configs = _methodConfigurations.GetOrAdd(methodKey, _ => new List<(object[], object)>());
            lock (configs)
            {
                configs.Add((arguments, returnValue));
            }
        }
    }

    /// <summary>
    /// Get call count for a static method
    /// </summary>
    public int GetCallCount(MethodInfo method, object[] arguments = null)
    {
        var methodKey = MethodKeys.For(method);
        if (!_receivedCalls.TryGetValue(methodKey, out var calls))
            return 0;

        lock (calls)
        {
            if (arguments == null)
                return calls.Count;

            return calls.Count(c => ArgumentsMatch(arguments, c.Arguments));
        }
    }

    /// <summary>
    /// Get all calls for a static method
    /// </summary>
    public IReadOnlyList<MethodCall> GetCalls(MethodInfo method)
    {
        var methodKey = MethodKeys.For(method);
        if (!_receivedCalls.TryGetValue(methodKey, out var calls))
            return new List<MethodCall>().AsReadOnly();

        lock (calls)
        {
            return calls.ToList().AsReadOnly();
        }
    }

    /// <summary>Every recorded static call, in the order it was received.</summary>
    public IReadOnlyList<MethodCall> GetAllCalls()
    {
        var all = new List<MethodCall>();
        foreach (var calls in _receivedCalls.Values)
        {
            lock (calls) all.AddRange(calls);
        }

        return all.OrderBy(c => c.Ordinal).ToList().AsReadOnly();
    }

    /// <summary>
    /// Clear all static method configurations and patches
    /// </summary>
    public void ClearAll()
    {
        // Remove all Harmony patches
        _harmony.UnpatchAll(_harmony.Id);

        // Clear all state
        _configuredReturns.Clear();
        _methodConfigurations.Clear();
        _receivedCalls.Clear();
        _patchedMethods.Clear();
        _fallback = null;
    }

    /// <summary>
    /// Unpatches every static method of <paramref name="type"/> and drops their configuration and
    /// recorded calls, leaving statics patched on other types alone. Without this a scope covering
    /// one type could only be torn down by tearing down every scope.
    /// </summary>
    public void ClearFor(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        foreach (var method in _patchedMethods.Keys.Where(m => m.DeclaringType == type).ToList())
        {
            ClearFor(method as MethodInfo);
        }
    }

    /// <summary>
    /// Unpatches one static method and drops its configuration and recorded calls.
    /// </summary>
    public void ClearFor(MethodInfo method)
    {
        if (method == null) return;

        if (_patchedMethods.TryRemove(method, out _))
        {
            try
            {
                _harmony.Unpatch(method, PrefixFor(method));
            }
            catch (Exception)
            {
                // Already unpatched, or Harmony no longer holds the patch. Either way the
                // configuration below is what makes the method behave normally again.
            }
        }

        var methodKey = MethodKeys.For(method);
        _configuredReturns.TryRemove(methodKey, out _);
        _methodConfigurations.TryRemove(methodKey, out _);
        _receivedCalls.TryRemove(methodKey, out _);
    }

    /// <summary>Drops recorded calls for every static method of <paramref name="type"/>.</summary>
    public void ClearCallsFor(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        foreach (var method in _patchedMethods.Keys.Where(m => m.DeclaringType == type).ToList())
        {
            _receivedCalls.TryRemove(MethodKeys.For(method), out _);
        }
    }

    /// <summary>Drops configured results for every static method of <paramref name="type"/>.</summary>
    public void ClearSetupsFor(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        foreach (var method in _patchedMethods.Keys.Where(m => m.DeclaringType == type).ToList())
        {
            var methodKey = MethodKeys.For(method);
            _configuredReturns.TryRemove(methodKey, out _);
            _methodConfigurations.TryRemove(methodKey, out _);
        }
    }

    private static MethodInfo PrefixFor(MethodInfo method)
    {
        var prefixName = method.ReturnType == typeof(void)
            ? nameof(VoidPrefixInterceptor)
            : nameof(PrefixInterceptor);

        return typeof(StaticMethodInterceptor).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
    }

    /// <summary>
    /// Harmony prefix interceptor for static methods with return values
    /// </summary>
    private static bool PrefixInterceptor(
        MethodBase __originalMethod,
        object[] __args,
        ref object __result)
    {
        var instance = Instance;

        // Always record the call
        instance.RecordCall(__originalMethod as MethodInfo, __args);

        var outcome = instance.Answer(__originalMethod, __args);
        if (ReferenceEquals(outcome, Interception.RunOriginal)) return true;

        __result = outcome;
        return false; // Skip original method
    }

    /// <summary>
    /// Harmony prefix interceptor for void static methods
    /// </summary>
    private static bool VoidPrefixInterceptor(
        MethodBase __originalMethod,
        object[] __args)
    {
        var instance = Instance;

        // Always record the call
        instance.RecordCall(__originalMethod as MethodInfo, __args);

        return ReferenceEquals(instance.Answer(__originalMethod, __args), Interception.RunOriginal);
    }

    private void RecordCall(MethodInfo method, object[] arguments)
    {
        if (method == null) return;

        var methodKey = MethodKeys.For(method);
        var calls = _receivedCalls.GetOrAdd(methodKey, _ => new List<MethodCall>());

        lock (calls)
        {
            calls.Add(new MethodCall
            {
                Method = method,
                Arguments = arguments,
                Target = null, // Static methods have no target
                CalledAt = DateTime.UtcNow,
                Ordinal = Interception.NextOrdinal(),
            });
        }
    }

    private bool HasConfiguration(string methodKey, object[] arguments)
    {
        // Check method configurations with arguments
        if (_methodConfigurations.TryGetValue(methodKey, out var configs))
        {
            lock (configs)
            {
                foreach (var config in configs)
                {
                    if (ArgumentsMatch(config.Arguments, arguments))
                    {
                        return true;
                    }
                }
            }
        }

        // Check simple method configurations
        return _configuredReturns.ContainsKey(methodKey);
    }

    /// <summary>Whether anything configured here can answer a call to <paramref name="method"/>.</summary>
    public bool HasConfiguration(MethodInfo method, object[] arguments)
        => HasConfiguration(MethodKeys.For(method), arguments);

    /// <summary>
    /// Answers a call from the configuration held here, deferring to the fallback and finally to
    /// <see cref="Interception.RunOriginal"/> when nothing matches.
    /// </summary>
    private object Answer(MethodBase method, object[] arguments)
    {
        var methodKey = MethodKeys.For(method);

        if (_methodConfigurations.TryGetValue(methodKey, out var configs))
        {
            lock (configs)
            {
                foreach (var config in configs)
                {
                    if (!ArgumentsMatch(config.Arguments, arguments)) continue;

                    return config.ReturnValue is ICallbackWrapper wrapper
                        ? wrapper.Execute(arguments)
                        : config.ReturnValue;
                }
            }
        }

        if (_configuredReturns.TryGetValue(methodKey, out var configuredReturn))
        {
            return configuredReturn is ICallbackWrapper wrapper
                ? wrapper.Execute(arguments)
                : configuredReturn;
        }

        var fallback = _fallback;
        return fallback == null ? Interception.RunOriginal : fallback(method, null, arguments);
    }

    /// <summary>
    /// Answers a call the way the Harmony prefix does, for callers driving the interceptor directly.
    /// </summary>
    public object InterceptCall(MethodInfo method, object[] arguments)
    {
        RecordCall(method, arguments);
        return Answer(method, arguments);
    }

    private bool ArgumentsMatch(object[] setupArgs, object[] callArgs)
    {
        if (setupArgs == null && callArgs == null) return true;
        if (setupArgs == null || callArgs == null) return false;
        if (setupArgs.Length != callArgs.Length) return false;

        for (int i = 0; i < setupArgs.Length; i++)
        {
            if (setupArgs[i] is NSubstitute.Core.Arguments.IArgumentMatcher matcher)
            {
                if (!matcher.IsSatisfiedBy(callArgs[i]))
                {
                    return false;
                }
            }
            else if (!Equals(setupArgs[i], callArgs[i]))
            {
                return false;
            }
        }
        return true;
    }
}
