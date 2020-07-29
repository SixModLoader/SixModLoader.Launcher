using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SixModLoader.Launcher
{
    public class AssemblyInfo
    {
        public string Version { get; }

        public AssemblyInfo(string file)
        {
            using var stream = File.OpenRead(file);
            using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            foreach (var attribute in assembly.GetCustomAttributes().Select(reader.GetCustomAttribute))
            {
                if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;

                var constructor = reader.GetMemberReference((MemberReferenceHandle) attribute.Constructor);
                var type = reader.GetTypeReference((TypeReferenceHandle) constructor.Parent);

                if (reader.GetString(type.Namespace) == typeof(AssemblyInformationalVersionAttribute).Namespace && reader.GetString(type.Name) == nameof(AssemblyInformationalVersionAttribute))
                {
                    var value = attribute.DecodeValue(new DummyProvider());
                    Version = (string) value.FixedArguments.Single().Value;
                }
            }
        }
        
        internal class DummyProvider : ICustomAttributeTypeProvider<object>
        {
            public object GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return null;
            }

            public object GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return null;
            }

            public object GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                return null;
            }

            public object GetSZArrayType(object elementType)
            {
                return null;
            }

            public object GetSystemType()
            {
                return null;
            }

            public object GetTypeFromSerializedName(string name)
            {
                return null;
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