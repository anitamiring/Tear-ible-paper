using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static System.Text.Encoding;

namespace Apkd.Weaver
{
    static class TypeName<T>
    {
        public static string Value { get; } = typeof(T).DeclaringType != null
            ? typeof(T).DeclaringType.FullName + "/" + typeof(T).Name
            : typeof(T).FullName;
    }

    static class WeaverExtensions
    {
        public static void RemoveAttribute<T>(this ICustomAttributeProvider member) where T : System.Attribute
        {
            CustomAttribute attr;
            while ((attr = member.CustomAttributes.FirstOrDefault(x => x.Is<T>())) != default)
                member.CustomAttributes.Remove(attr);
        }

        public static void RemoveAttribute(this ICustomAttributeProvider member, Predicate<TypeReference> predicate)
        {
            CustomAttribute attr;
            while ((attr = member.CustomAttributes.FirstOrDefault(x => predicate(x.AttributeType))) != default)
                member.CustomAttributes.Remove(attr);
        }

        public static CustomAttribute AddAttribute<T>(this ICustomAttributeProvider member, ModuleDefinition module, Func<System.Reflection.ConstructorInfo, bool> predicate = null) where T : System.Attribute
        {
            predicate = predicate ?? (x => true);
            var attr = new CustomAttribute(module.ImportReference(typeof(T).GetConstructors().First(predicate)));
            member.CustomAttributes.Add(attr);
            return attr;
        }

        public static CustomAttribute AddAttribute(this ICustomAttributeProvider member, TypeReference attribute, bool allowRepeating = false)
        {
            if (!allowRepeating && member.HasAttribute(x => x.AttributeType.FullName == attribute.FullName))
                return null;
            var attr = new CustomAttribute(attribute.Module.ImportReference(attribute.Resolve().Methods.FirstOrDefault(x => x.IsConstructor)));
            member.CustomAttributes.Add(attr);
            return attr;
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider type) where T : System.Attribute
            => type.CustomAttributes.Any(x => x.Is<T>());

        public static bool HasAttribute(this ICustomAttributeProvider type, System.Func<CustomAttribute, bool> predicate)
            => type.CustomAttributes.Any(predicate);

        public static bool IsCompilerGenerated(this ICustomAttributeProvider type)
            => type.HasAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();

        public static void StripType(this TypeDefinition type)
        {
            type.Methods.Clear();
            type.Properties.Clear();
            type.Fields.Clear();
            type.Interfaces.Clear();
            type.CustomAttributes.Clear();
        }

        public static bool BaseTypesInclude(this TypeReference type, Func<TypeReference, bool> filter)
        {
            try
            {
                while ((type = type.Resolve().BaseType) != null)
                    if (filter(type))
                        return true;
            }
            catch { }
            return false;
        }

        public static bool DerivesFrom<T>(this TypeReference type)
            => type.BaseTypesInclude(x => x.Is<T>());

        public static FieldDefinition GetBackingField(this PropertyDefinition property, bool @static = false)
            => property
                ?.GetMethod
                ?.Body
                ?.Instructions
                ?.FirstOrDefault(x => x.OpCode == (@static ? OpCodes.Ldsfld : OpCodes.Ldfld))
                ?.Operand as FieldDefinition;

        static readonly SHA256Managed sha = new SHA256Managed();
        static readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        static readonly Dictionary<TypeReference, int> nameCounterDict = new Dictionary<TypeReference, int>();

        public static string ScrambleString(string input)
            => BitConverter.ToString(sha.ComputeHash(UTF8.GetBytes(input + "04dc0d59ad0320fc3c85029af"))).Replace("-", "");

        public static bool Is<T>(this TypeDefinition type)
            => type.FullName == typeof(T).FullName;

        public static bool Is<T>(this TypeReference type)
            => type.FullName == typeof(T).FullName;

        public static bool Is<T>(this CustomAttribute attribute)
            => attribute.AttributeType.FullName == typeof(T).FullName;

        public static bool PropertyEquals<T>(this CustomAttribute attribute, string key, T value)
            => attribute.Properties.Any(x => x.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase) && x.Argument.Value.Equals(value));

        public static bool IsInternalToAssembly(this TypeDefinition type)
        {
            if (type.IsNestedPublic && !type.DeclaringType.IsInternalToAssembly())
                return false;

            if (type.IsNestedPrivate)
                return true;

            if (type.IsPublic)
                return false;

            return true;
        }

        public static bool IsInternalToAssembly(this PropertyDefinition property)
        {
            if (property.GetMethod?.IsInternalToAssembly() ?? true)
                if (property.SetMethod?.IsInternalToAssembly() ?? true)
                    return true;

            return false;
        }

        public static bool IsInternalToAssembly(this FieldDefinition field)
        {
            if (field.DeclaringType.IsInternalToAssembly())
                return true;

            if (!field.IsPublic)
                return true;

            return false;
        }

        public static bool IsInternalToAssembly(this MethodDefinition method)
        {
            if (method.DeclaringType.IsInternalToAssembly())
                return true;

            if (!method.IsPublic)
                return true;

            return false;
        }

        public static MethodDefinition GetBaseMethod(this MethodDefinition self)
        {
            TypeDefinition ResolveBaseType(TypeDefinition type)
            {
                if (type == null)
                    return null;

                var b = type.BaseType;
                if (b == null)
                    return null;

                return b.Resolve();
            }
            if (self == null)
                throw new ArgumentNullException("self");
            if (!self.IsVirtual)
                return self;
            if (self.IsNewSlot)
                return self;

            var base_type = ResolveBaseType(self.DeclaringType);
            while (base_type != null)
            {
                var @base = GetMatchingMethod(base_type, self);
                if (@base != null)
                    return @base;

                base_type = ResolveBaseType(base_type);
            }

            return self;
        }

        public static MethodDefinition GetOriginalBaseMethod(this MethodDefinition self)
        {
            if (self == null)
                throw new ArgumentNullException("self");

            while (true)
            {
                var @base = self.GetBaseMethod();
                if (@base == self)
                    return self;

                self = @base;
            }
        }

        static MethodDefinition GetMatchingMethod(TypeDefinition type, MethodDefinition method)
            => MetadataResolver.GetMethod(type.Methods, method);

        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
        {
            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstance(args))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParam in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));

            return reference;
        }

        public static GenericInstanceMethod MakeGenericInstance(this MethodReference method, params TypeReference[] genericArguments)
        {
            if (method.GenericParameters.Count != genericArguments.Length)
                throw new ArgumentException($"{method.GenericParameters.Count} != {genericArguments.Length}");

            var instance = new GenericInstanceMethod(method);

            foreach (var arg in genericArguments)
            {
                instance.GenericArguments.Add(arg);
            }
            return instance;
        }

        public static GenericInstanceType MakeGenericInstance(this TypeReference self, params TypeReference[] genericArguments)
        {
            if (self.GenericParameters.Count != genericArguments.Length)
                throw new ArgumentException($"{self.GenericParameters.Count} != {genericArguments.Length}");

            var instance = new GenericInstanceType(self);

            foreach (var argument in genericArguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }
    }
}
