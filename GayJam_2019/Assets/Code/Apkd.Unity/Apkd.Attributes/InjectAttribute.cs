using static System.AttributeTargets;

namespace Apkd
{
    [System.AttributeUsage(Field | Property, Inherited = true, AllowMultiple = false)]
    public class Inject : System.Attribute
    {
        public bool Optional { get; set; }

        public sealed class FromChildren : Inject
        {
            public bool IncludeInactive { get; set; } = false;
        }

        public sealed class FromParents : Inject
        {
            public bool IncludeInactive { get; set; } = false;
        }

        public sealed class Singleton : Inject { }
    }
}