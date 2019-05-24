using static System.AttributeTargets;

namespace Apkd.Internal
{
    [System.AttributeUsage(Field | Property, Inherited = true, AllowMultiple = false)]
    public sealed class CustomObjectPickerAttribute : UnityEngine.PropertyAttribute
    {
        public System.Type InterfaceType { get; set; }
    }
}
