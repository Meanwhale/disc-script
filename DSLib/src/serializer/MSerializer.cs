using System.Reflection;

namespace DiscScript
{
	public class DSClassAttribute : Attribute
	{
	}

	public class MSerializer
	{
		// type definition

		public static MList<Type> SerializableClasses = new MList<Type>();
		public static Dictionary<Type, ClassCSType> classes = new Dictionary<Type, ClassCSType>();
		public static Dictionary<Type, SimpleCSType> primitives = new Dictionary<Type, SimpleCSType>();
		public static Dictionary<Type, GenericCSType> generics = new Dictionary<Type, GenericCSType>();

		private static bool initialized = false;

		public static void Init()
		{
			if (initialized) return;
			initialized = true;
			GenerateInTypes();
			GetTypesWithDSClassAttribute(SerializableClasses);

			foreach(var x in SerializableClasses)
			{
				GetOrCreateCSType(x);
			}
			foreach(var c in classes.Values)
			{
				var s = c.ToString();
				if (s == null) throw new MException(MError.SERIALIZER, "null type");
				MS.VerboseLine(s);
			}
		}
		
		public static void WriteStructDefinitions(MOutput o)
		{
			// TODO: don't write all structs but only what's needed (find dependencies on init.)

			foreach(var x in SerializableClasses)
			{
				WriteStructDefinition(classes[x], o);
			}
		}
		private static void WriteStructDefinition(ClassCSType x, MOutput o)
		{
			o.WriteLine("$struct " + x.Name);
						
			// struct members
			foreach (var member in x.GetMembers())
			{
				var icstype = GetOrCreateCSType(member.CsType.RealType);

				o.Indent(1);
				icstype.WriteTypeName(o);
				o.Write(" ");
				o.WriteLine(member.Name);
			}
		}

		private static void GenerateInTypes()
		{
			primitives[typeof(string)] = new SimpleCSType("string", typeof(string));
			primitives[typeof(int)] = new SimpleCSType("int32", typeof(int));
			primitives[typeof(double)] = new SimpleCSType("float64", typeof(double));
		}
		
		public static SimpleCSType? GetSimpleCSType(string name)
		{
			foreach(var t in primitives.Values)
			{
				if (t.Name.Equals(name)) return t;
			}
			return null;
		}
		public static ClassCSType? GetClassCSType(string name)
		{
			foreach(var t in classes.Values)
			{
				if (t.Name.Equals(name)) return t;
			}
			return null;
		}
		public static GenericCSType? GetGenericCSType(string name, ICSType [] parameters)
		{
			foreach (var t in generics.Values)
			{
				if (t.Match(name, parameters)) return t;
			}
			return null;
		}
		public static ICSType? GetCSType(Type t)
		{
			if (primitives.ContainsKey(t)) return primitives[t];
			if (classes.ContainsKey(t)) return classes[t];
			if (generics.ContainsKey(t)) return generics[t];
			return null;
		}

		private static ICSType GetOrCreateCSType(Type? t)
		{
			// use to initialize serializable types

			if (t == null) throw new MException(MError.SERIALIZER, "null type");

			// generic types

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var keyType = t.GetTypeInfo().GenericTypeArguments[0];
				var valueType = t.GetTypeInfo().GenericTypeArguments[1];

				return generics[t] = new GenericCSType(
					"map",
					t,
					new ICSType []{
						GetOrCreateCSType(keyType),
						GetOrCreateCSType(valueType)
					});
			}
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
			{
				return generics[t] = new GenericCSType(
					"list",
					t,
					new ICSType []{ GetOrCreateCSType(t.GetTypeInfo().GenericTypeArguments[0])}
				);
			}
			if (t.IsArray)
			{
				return generics[t] = new GenericCSType(
					"list",
					t,
					new ICSType []{ GetOrCreateCSType(t.GetElementType())}
				);
			}

			if (primitives.ContainsKey(t)) return primitives[t];
			
			if (t.IsEnum)
			{
				// enum is like primitives
				return primitives[t] = new EnumCSType(t.FullName, t);
			}
			if (t.IsClass)
			{
				// add new class type

				if (t.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0)
				{
					if (t.FullName == null) throw new MException(MError.SERIALIZER, "class has no name: " + t);
					var classType = new ClassCSType(t.FullName, t);
					
					// create member list

					IList<FieldInfo> fields = new List<FieldInfo>(t.GetFields(BindingFlags.Public | BindingFlags.Instance));
			
					foreach (var fieldInfo in fields)
					{
						var memberType = GetOrCreateCSType(fieldInfo.FieldType);
						classType.Add(fieldInfo.Name, memberType);
					}

					classes[t] = classType;
					return classType;
				}
			}
			throw new MException(MError.SERIALIZER, "type is not serializable: " + t.FullName);
		}


		private static void GetTypesWithDSClassAttribute(MList<Type> types)
		{
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				AddTypesWithHelpAttribute(a, types);
			}
		}
		static void AddTypesWithHelpAttribute(Assembly assembly, MList<Type> types)
		{
			foreach(Type type in assembly.GetTypes()) {
				if (type.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0) {
					MS.VerboseLine("add serializable type: " + type.FullName);
					types.Add(type);
				}
			}
		}
	}
}
