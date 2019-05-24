using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using static System.Reflection.BindingFlags;

namespace Apkd.Weaver
{
    sealed class SerializationWeaver : IEarlyWeaver
    {
        ModuleDefinition module;
        TypeDefinition[] allTypes;
        PropertyDefinition[] allProperties;
        FieldDefinition[] allFields;

        int IWeaver.Priority => 0;

        public void Initialize(AssemblyDefinition assembly)
        {
            this.module = assembly.MainModule;

            this.allTypes = module.Types
                .SelectMany(x => x.NestedTypes)
                .Concat(module.Types)
                .ToArray();

            this.allFields = allTypes
                .SelectMany(x => x.Fields)
                .ToArray();

            this.allProperties = allTypes
                .SelectMany(x => x.Properties)
                .ToArray();
        }

        public void ProcessAssembly()
        {
            ReplaceSerializationAttributes();
            AllowPropertySerialization();
            AllowReadonlySerialization();
        }

        string GenerateFieldName(string propertyName)
        {
            string newName = "m_" + propertyName.Substring(0, 1).ToLower();
            if (propertyName.Length > 1)
                newName += propertyName.Substring(1);
            return newName;
        }

        void AllowPropertySerialization()
        {
            var properties = allProperties
                .Select(p => (p, attr: p.CustomAttributes.FirstOrDefault(x => x.Is<SAttribute>())))
                .Where(p => p.attr != null)
                .Select(p => (p.p, p.p.GetBackingField(), p.attr));

            foreach (var (property, field, sattr) in properties)
            {
                if (field.HasAttribute<UnityEngine.SerializeField>())
                    throw new System.NotSupportedException($"Property {property.DeclaringType.Name}.{property.Name} is marked with [S] but backing field {field.Name} is already serialized.");

                string oldName = sattr
                    .ConstructorArguments
                    .FirstOrDefault().Value as string;

                if (!string.IsNullOrWhiteSpace(oldName))
                {
                    var attr = field.AddAttribute<UnityEngine.Serialization.FormerlySerializedAsAttribute>(module);
                    attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, oldName));
                }

                property.RemoveAttribute<SAttribute>();
                field.AddAttribute<UnityEngine.SerializeField>(module);

                foreach (var attr in property.CustomAttributes)
                    field.CustomAttributes.Add(attr);

                property.RemoveAttribute(x => x.FullName.StartsWith("Apkd.Inject"));
                field.Name = GenerateFieldName(property.Name);
            }
        }

        void ReplaceSerializationAttributes()
        {
            foreach (var field in allFields.Where(f => f.HasAttribute<HAttribute>()))
            {
                field.RemoveAttribute<HAttribute>();
                field.AddAttribute<UnityEngine.HideInInspector>(module);
            }
            foreach (var property in allProperties.Where(f => f.HasAttribute<HAttribute>()))
            {
                property.RemoveAttribute<HAttribute>();
                property.AddAttribute<UnityEngine.HideInInspector>(module);
            }
            foreach (var field in allFields.Where(f => f.HasAttribute<SAttribute>()))
            {
                var newName = field
                    .CustomAttributes
                    .FirstOrDefault(a => a.Is<SAttribute>())
                    .ConstructorArguments
                    .FirstOrDefault().Value as string;

                if (!string.IsNullOrWhiteSpace(newName) && !field.DeclaringType.Fields.Any(f => f.Name == newName))
                    field.Name = newName;

                field.RemoveAttribute<SAttribute>();
                field.AddAttribute<UnityEngine.SerializeField>(module);
            }
        }

        void AllowReadonlySerialization()
        {
            var fields = allFields
                .Where(f => f.IsInitOnly)
                .Where(f => f.HasAttribute<UnityEngine.SerializeField>());

            foreach (var field in fields)
                field.IsInitOnly = false;
        }
    }
}