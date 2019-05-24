using System.Linq;

namespace Apkd.Internal
{
    public static class ValidationHelpers
    {
        public static void ValidateSerializedInterface(ref UnityEngine.Object fieldValue, System.Type interfaceType)
        {
            if (!IsValidSerializedInterfaceValue(fieldValue, interfaceType))
                fieldValue = null;
        }

        public static void ValidateSerializedInterfaceArray(ref UnityEngine.Object[] fieldValue, System.Type interfaceType)
        {
            // array reference has not been initialized for whatever magical reason
            if (fieldValue == null)
                return;

            for (int i = 0; i < fieldValue.Length; ++i)
            {
                // workaround for unity complaining about array type mismatch here
                var temp = fieldValue[i];

                ValidateSerializedInterface(ref temp, interfaceType);

                if (object.ReferenceEquals(temp, null))
                    fieldValue[i] = null;
            }
        }

        static bool IsValidSerializedInterfaceValue(UnityEngine.Object fieldValue, System.Type interfaceType)
            => fieldValue == null || fieldValue.GetType().GetInterfaces().Contains(interfaceType);
    }
}