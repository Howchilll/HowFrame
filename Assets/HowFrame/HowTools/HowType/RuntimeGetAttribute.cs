using System;

namespace HowFrame
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RuntimeGetAttribute : Attribute
    {
        public string Key { get; }

        public RuntimeGetAttribute(string key = null)
        {
            Key = key;
        }
    }

}