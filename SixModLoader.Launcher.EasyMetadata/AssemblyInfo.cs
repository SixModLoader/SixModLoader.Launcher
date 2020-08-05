using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SixModLoader.Launcher.EasyMetadata
{
    public class EasyCustomAttribute
    {
        public string Name { get; }
        public Type? Type { get; }
        public CustomAttributeValue<object>? Value { get; }

        public EasyCustomAttribute(string name, Type? type, CustomAttributeValue<object>? value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }

    public class AssemblyInfo
    {
        public AssemblyName Name { get; }
        public List<EasyCustomAttribute> Attributes { get; } = new List<EasyCustomAttribute>();

        public string Version { get; }

        public AssemblyInfo(string file)
        {
            using var stream = File.OpenRead(file);
            using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            Name = assembly.GetSafeAssemblyName();

            foreach (var attribute in assembly.GetCustomAttributes().Select(reader.GetCustomAttribute))
            {
                if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;

                var constructor = reader.GetMemberReference((MemberReferenceHandle) attribute.Constructor);
                var typeReference = reader.GetTypeReference((TypeReferenceHandle) constructor.Parent);

                var typeName = reader.GetString(typeReference.Namespace) + "." + reader.GetString(typeReference.Name);

                var resolutionScope = typeReference.ResolutionScope;
                if (resolutionScope.Kind == HandleKind.AssemblyReference)
                {
                    var assemblyReference = reader.GetAssemblyReference((AssemblyReferenceHandle) resolutionScope);
                    typeName += ", " + assemblyReference.GetSafeAssemblyName();
                }

                CustomAttributeValue<object>? value = null;

                try
                {
                    value = attribute.DecodeValue(new DummyProvider());
                }
                catch (Exception)
                {
                    // ignored
                }

                Attributes.Add(new EasyCustomAttribute(typeName, Type.GetType(typeName, false), value));
            }

            var versionAttribute = GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? GetCustomAttribute<AssemblyVersionAttribute>();
            Version = versionAttribute?.Value?.FixedArguments.Single().Value as string ?? Name.Version.ToString(3);
        }

        public EasyCustomAttribute? GetCustomAttribute<T>()
        {
            return Attributes.FirstOrDefault(x => x.Type == typeof(T));
        }

        internal class DummyProvider : ICustomAttributeTypeProvider<object>
        {
            public object GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return null!;
            }

            public object GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return null!;
            }

            public object GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return null!;
            }

            public object GetSZArrayType(object elementType)
            {
                return null!;
            }

            public object GetSystemType()
            {
                return null!;
            }

            public object GetTypeFromSerializedName(string name)
            {
                return null!;
            }

            public PrimitiveTypeCode GetUnderlyingEnumType(object type)
            {
                return PrimitiveTypeCode.String;
            }

            public bool IsSystemType(object type)
            {
                return false;
            }
        }
    }
}