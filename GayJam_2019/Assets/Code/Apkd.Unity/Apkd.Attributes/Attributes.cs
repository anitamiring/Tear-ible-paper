using static System.AttributeTargets;

namespace Apkd
{
    [System.AttributeUsage(Property, Inherited = true, AllowMultiple = false)]
    public sealed class SAttribute : System.Attribute
    {
        public SAttribute(string oldName = null) { }
    }

    [System.AttributeUsage(Property, Inherited = true, AllowMultiple = false)]
    public sealed class HAttribute : System.Attribute { }

#if !ODIN_INSPECTOR
    [System.AttributeUsage(Field | Property, Inherited = true, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : UnityEngine.PropertyAttribute { }
#endif
}
