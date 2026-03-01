using System;

namespace FlawsFightNight.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class SafeForSerializationAttribute : Attribute
    {
        /// <summary>
        /// Optional reason why this type is whitelisted (for documentation).
        /// </summary>
        public string? Reason { get; init; }

        public SafeForSerializationAttribute() { }

        public SafeForSerializationAttribute(string reason)
        {
            Reason = reason;
        }
    }
}
