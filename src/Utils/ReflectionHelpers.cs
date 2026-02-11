#nullable enable

using HarmonyLib;

namespace flawlesssvanaxfork;

internal static class ReflectionHelpers
{
    public static T GetField<T>(this object instance, string fieldName)
        => (T)AccessTools.Field(instance.GetType(), fieldName).GetValue(instance)!;

    public static void SetField<T>(this object instance, string fieldName, T value)
        => AccessTools.Field(instance.GetType(), fieldName).SetValue(instance, value);

    public static T CallMethod<T>(this object instance, string method, params object[] args)
        => (T)AccessTools.Method(instance.GetType(), method).Invoke(instance, args)!;

    public static void CallMethod(this object instance, string method, params object[] args)
        => AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
}