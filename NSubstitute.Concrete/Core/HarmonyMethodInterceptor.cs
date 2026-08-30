using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Core;

/// <summary>
/// Harmony-based method interceptor that patches methods at runtime
/// </summary>
public class HarmonyMethodInterceptor : ConcreteMethodInterceptor
{
    // One Harmony instance for every instance interceptor, with a reference count per method.
    //
    // A patch is global to the method, not to the instance: two substitutes of the same type asking
    // for the same method must not install the prefix twice, or the prefix — and therefore the call
    // record — would run twice for a single call. The count is what lets the last one out unpatch.
    private static readonly Harmony SharedHarmony = new Harmony("NSubstitute.Concrete.Instance");
    private static readonly Dictionary<MethodBase, int> PatchCounts = new Dictionary<MethodBase, int>();
    private static readonly object PatchGate = new object();

    private readonly Type _targetType;
    private readonly List<MethodBase> _patchedMethods = new List<MethodBase>();

    private static readonly Dictionary<object, HarmonyMethodInterceptor> _instanceInterceptors =
        new Dictionary<object, HarmonyMethodInterceptor>(ReferenceComparer.Instance);

    public object ProxyInstance { get; private set; }

    /// <summary>The type whose members this interceptor patches.</summary>
    public Type TargetType => _targetType;

    /// <summary>The methods this interceptor currently holds a patch on.</summary>
    public IReadOnlyList<MethodBase> PatchedMethods
    {
        get { lock (PatchGate) return _patchedMethods.ToList().AsReadOnly(); }
    }

    public HarmonyMethodInterceptor(Type targetType)
    {
        _targetType = targetType;
    }

    public void Initialize(object instance)
    {
        ProxyInstance = instance;
        SetProxy(instance);
        lock (_instanceInterceptors)
        {
            _instanceInterceptors[instance] = this;
        }
    }

    public void PatchMethod(MethodInfo method)
    {
        if (method == null) return;

        lock (PatchGate)
        {
            if (_patchedMethods.Contains(method))
                return;

            if (!PatchCounts.TryGetValue(method, out var count) || count == 0)
            {
                // Choose the right prefix based on whether method returns void
                string prefixName = method.ReturnType == typeof(void)
                    ? nameof(VoidPrefixInterceptor)
                    : nameof(PrefixInterceptor);

                var prefix = typeof(HarmonyMethodInterceptor).GetMethod(
                    prefixName,
                    BindingFlags.Static | BindingFlags.NonPublic);

                SharedHarmony.Patch(method, prefix: new HarmonyMethod(prefix));
            }

            PatchCounts[method] = count + 1;
            _patchedMethods.Add(method);
        }
    }

    public void PatchProperty(PropertyInfo property)
    {
        if (property.CanRead && property.GetGetMethod(nonPublic: true) is MethodInfo getter)
        {
            PatchMethod(getter);
        }

        if (property.CanWrite && property.GetSetMethod(nonPublic: true) is MethodInfo setter)
        {
            PatchMethod(setter);
        }
    }

    /// <summary>
    /// Patches every member of the target type that can be intercepted: public and protected
    /// instance methods and property accessors, declared on the type or inherited from a base other
    /// than <see cref="object"/>.
    /// <para>
    /// Patching a method only when a setup names it is enough to answer that setup, but it leaves
    /// every other call invisible. Anything that reasons about calls nobody configured — a strict
    /// behaviour, a default-value provider, a "no other calls" assertion — needs the whole surface
    /// patched up front, which is what this does.
    /// </para>
    /// </summary>
    public void PatchAll()
    {
        foreach (var method in InterceptableMethods(_targetType))
        {
            try
            {
                PatchMethod(method);
            }
            catch (Exception)
            {
                // A method Harmony cannot patch (inlined, extern, or otherwise unavailable) simply
                // stays unpatched; the rest of the type is still worth intercepting.
            }
        }
    }

    /// <summary>
    /// The methods of <paramref name="type"/> that are worth patching: instance methods that are
    /// public or protected, have a body, and are not <see cref="object"/>'s own members.
    /// </summary>
    public static IEnumerable<MethodInfo> InterceptableMethods(Type type)
    {
        if (type == null) yield break;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var seen = new HashSet<string>();

        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            foreach (var method in current.GetMethods(flags))
            {
                if (method.IsAbstract) continue;
                if (method.IsPrivate && !method.IsFamily) continue;
                if (!method.IsPublic && !method.IsFamily && !method.IsFamilyOrAssembly) continue;
                if (method.IsGenericMethodDefinition) continue;
                if (method.DeclaringType == typeof(object)) continue;
                if (IsObjectMember(method)) continue;
                if ((method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0) continue;
                if (method.GetMethodBody() == null) continue;
                if (method.GetParameters().Any(p => p.ParameterType.IsByRef || p.ParameterType.IsPointer)) continue;

                if (!seen.Add(MethodKeys.For(method))) continue;
                yield return method;
            }
        }
    }

    /// <summary>
    /// <see cref="object"/>'s own members, overridden or not. Patching them would put the
    /// interceptor in the middle of equality, hashing and formatting — including the ones this
    /// library's own diagnostics use.
    /// </summary>
    private static bool IsObjectMember(MethodInfo method)
    {
        var parameters = method.GetParameters();
        switch (method.Name)
        {
            case "ToString":
            case "GetHashCode":
                return parameters.Length == 0;
            case "Equals":
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(object);
            case "Finalize":
                return parameters.Length == 0;
            default:
                return false;
        }
    }

    /// <summary>
    /// Harmony prefix that intercepts method calls with return values
    /// </summary>
    private static bool PrefixInterceptor(
        object __instance,
        MethodBase __originalMethod,
        object[] __args,
        ref object __result)
    {
        var interceptor = Find(__instance);
        if (interceptor == null)
        {
            // No interceptor configured, run original method
            return true;
        }

        var outcome = interceptor.InterceptCall(__originalMethod, __args);
        if (ReferenceEquals(outcome, Interception.RunOriginal)) return true;

        __result = outcome;
        return false; // Skip original method
    }

    /// <summary>
    /// Harmony prefix for void methods (no return value)
    /// </summary>
    private static bool VoidPrefixInterceptor(
        object __instance,
        MethodBase __originalMethod,
        object[] __args)
    {
        var interceptor = Find(__instance);
        if (interceptor == null)
        {
            // No interceptor configured, run original method
            return true;
        }

        var outcome = interceptor.InterceptCall(__originalMethod, __args);
        return ReferenceEquals(outcome, Interception.RunOriginal);
    }

    private static HarmonyMethodInterceptor Find(object instance)
    {
        if (instance == null) return null;
        lock (_instanceInterceptors)
        {
            return _instanceInterceptors.TryGetValue(instance, out var interceptor) ? interceptor : null;
        }
    }

    /// <summary>
    /// Record a method call for verification purposes
    /// </summary>
    public void RecordCall(MethodBase method, object[] arguments)
    {
        _receivedCalls.Add(new MethodCall
        {
            Method = method as MethodInfo,
            Arguments = arguments,
            Target = ProxyInstance,
            CalledAt = DateTime.UtcNow,
            Ordinal = Interception.NextOrdinal(),
        });
    }

    public override IReadOnlyList<MethodCall> GetReceivedCalls()
    {
        return _receivedCalls.AsReadOnly();
    }

    public override int GetCallCount(MethodInfo method, object[] arguments)
    {
        var key = MethodKeys.For(method);
        return _receivedCalls.Count(c => MethodKeys.For(c.Method) == key && ArgumentsMatch(arguments, c.Arguments));
    }

    public void Unpatch()
    {
        lock (PatchGate)
        {
            foreach (var method in _patchedMethods)
            {
                if (!PatchCounts.TryGetValue(method, out var count) || count <= 0) continue;

                if (count == 1)
                {
                    PatchCounts.Remove(method);
                    string prefixName = (method as MethodInfo)?.ReturnType == typeof(void)
                        ? nameof(VoidPrefixInterceptor)
                        : nameof(PrefixInterceptor);

                    var prefix = typeof(HarmonyMethodInterceptor).GetMethod(
                        prefixName,
                        BindingFlags.Static | BindingFlags.NonPublic);

                    SharedHarmony.Unpatch(method, prefix);
                }
                else
                {
                    PatchCounts[method] = count - 1;
                }
            }

            _patchedMethods.Clear();
        }

        var instance = ProxyInstance;
        if (instance != null)
        {
            lock (_instanceInterceptors)
            {
                _instanceInterceptors.Remove(instance);
            }
        }

        // The registry key was the only thing keeping the substituted instance alive from here.
        ProxyInstance = null;
    }

    public override void Cleanup()
    {
        Unpatch();
        base.Cleanup();
    }

    /// <summary>The number of instance methods currently patched across every interceptor.</summary>
    public static int TotalPatchedMethodCount
    {
        get { lock (PatchGate) return PatchCounts.Count; }
    }
}
