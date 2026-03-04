using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace FlawsFightNight.IO.Handlers
{
    public class SafeSerializationBinder : ISerializationBinder
    {
        // Cache validated types for performance
        private static readonly ConcurrentDictionary<string, Type?> _typeCache = new();
        
        // Whitelist for system types that can't be decorated with attributes
        private static readonly HashSet<string> _systemTypeWhitelist = new(StringComparer.Ordinal)
        {
            "System.Collections.Generic.List`1",
            "System.Collections.Generic.Dictionary`2",
            "System.String",
            "System.Int32",
            "System.Int64",
            "System.DateTime",
            "System.Guid"
        };

        public Type BindToType(string? assemblyName, string typeName)
        {
            string fullTypeName = string.IsNullOrEmpty(assemblyName)
                ? typeName
                : $"{typeName}, {assemblyName.Split(',')[0]}";

            // Check cache first
            if (_typeCache.TryGetValue(fullTypeName, out var cachedType))
            {
                return cachedType ?? throw new InvalidOperationException(
                    $"Type '{fullTypeName}' was previously rejected for deserialization.");
            }

            // Try to resolve the type
            Type? resolvedType = Type.GetType(fullTypeName, throwOnError: false)
                              ?? Type.GetType(typeName, throwOnError: false);

            if (resolvedType == null)
            {
                _typeCache[fullTypeName] = null;
                throw new InvalidOperationException($"Type '{fullTypeName}' could not be resolved.");
            }

            // Check if it's a system type
            if (_systemTypeWhitelist.Contains(typeName) || resolvedType.Namespace?.StartsWith("System") == true)
            {
                _typeCache[fullTypeName] = resolvedType;
                return resolvedType;
            }

            // Accept types decorated with an attribute named "SafeForSerializationAttribute"
            // This handles the attribute living in Core or Data (different assemblies/namespaces).
            var hasSafeAttr = resolvedType.GetCustomAttributes(inherit: true)
                                          .Any(a => string.Equals(a.GetType().Name, "SafeForSerializationAttribute", StringComparison.Ordinal));
            if (hasSafeAttr)
            {
                _typeCache[fullTypeName] = resolvedType;
                return resolvedType;
            }

            // REJECT: Type is not whitelisted
            _typeCache[fullTypeName] = null;
            throw new InvalidOperationException(
                $"Type '{fullTypeName}' is not marked with [SafeForSerialization] and is not whitelisted. " +
                $"Add [SafeForSerialization] attribute to the type definition if it's safe to deserialize.");
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }

        /// <summary>
        /// Clears the type cache (useful for testing or dynamic assembly scenarios).
        /// </summary>
        public static void ClearCache() => _typeCache.Clear();
    }
}
