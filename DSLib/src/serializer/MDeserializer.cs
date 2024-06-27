using System.Reflection;

namespace DiscScript
{
	using DiscScriptCore;

	public class MDeserializer
	{
		internal static T Read<T>(IData collection)
		{
			var t = typeof(T);
			var ti = t.GetTypeInfo();
			
			if (ti.IsClass && t.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0)
			{
				var x = Activator.CreateInstance(typeof(T));
				if (x != null)
				{
					T o = (T)x;
					return (T)Deserialize(o, collection, ti);
				}
			}
			throw new MException(MError.DESERIALIZER, "not serializable type: " + t.FullName);
		}
		
		internal static T Read<T>(string script)
		{
			var data = Parser.Read(script);
			return Read<T>(data);
		}

		// struct

		public static T ReadStruct<T>(string script)
		{
			MSerializer.Init();
			var data = Parser.Read(script);
			return Read<T>(data.GetValue("root"));
		}
		public static T ReadStruct<T>(MInput input)
		{
			MSerializer.Init();
			var data = Parser.Read(input);
			input.Close();
			return Read<T>(data.GetValue("root"));
		}

		// map

		private static object Deserialize(object o, IData collection, TypeInfo ti)
		{
			IList<FieldInfo> fields = new List<FieldInfo>(ti.GetFields(BindingFlags.Public | BindingFlags.Instance));
			
			foreach (var fieldInfo in fields)
			{
				MS.VerboseLine("write [" + fieldInfo.FieldType + "] " + fieldInfo.Name);
				
				// convert value from the map to CS type of value
				fieldInfo.SetValue(o, GetTypeValue(collection, fieldInfo.FieldType, fieldInfo.Name));
			}
			return o;
		}

		private static object? GetTypeValue(IData collection, Type t, string name)
		{
			var data = collection.GetValue(name);
			if (data == null) throw new MException(MError.DESERIALIZER, "value not found by name: " + name);
			return GetTypeValue(t, data);
		}

		private static object? GetTypeValue(Type t, IData data)
		{
			if (t == typeof(string)) return data.GetString();
			if (t == typeof(double)) return data.GetDouble();
			if (t == typeof(int)) return data.GetInt();
			
			if (data is MNull) return null;

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>) && data is MMap map)
			{
				// create dictionary of the same type

				var keyType = t.GetTypeInfo().GenericTypeArguments[0];
				var valueType = t.GetTypeInfo().GenericTypeArguments[1];
				Type dt = typeof(Dictionary<,>);
				Type[] typeArgs = {keyType, valueType};
				Type constructed = dt.MakeGenericType(typeArgs);

				var dictionary = Activator.CreateInstance(constructed);
				var addMethod = constructed.GetMethod("Add");
						
				if (dictionary == null) throw new MException(MError.DESERIALIZER, "dictionary can't be created");
				if (addMethod == null) throw new MException(MError.DESERIALIZER, "can't get dictionary's add method");

				// read key-value pairs

				foreach (var kv in map.Entries())
				{
					object key;
					if (keyType == typeof(int)) key = int.Parse(kv.Key);
					else if (keyType == typeof(string)) key = kv.Key;
					else throw new MException(MError.WRONG_KEY_TYPE);

					var value = GetTypeValue(valueType, kv.Value);

					addMethod.Invoke(dictionary, new object[]{ key, value });
				}
				return dictionary;
					
			}
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) && data is MDataList source)
			{
				var valueType = t.GetTypeInfo().GenericTypeArguments[0];
				Type lt = typeof(List<>);
				Type[] typeArgs = {valueType};
				Type constructed = lt.MakeGenericType(typeArgs);
						
				var target = Activator.CreateInstance(constructed);
				var addMethod = constructed.GetMethod("Add");
						
				if (target == null) throw new MException(MError.DESERIALIZER, "list can't be created");
				if (addMethod == null) throw new MException(MError.DESERIALIZER, "can't get list's add method");

				foreach(var item in source.GetValues())
				{
					var value = GetTypeValue(valueType, item);

					addMethod.Invoke(target, new object[] {value});
				}
				return target;
			}
			if (t.IsArray && data is MDataList src)
			{
				var valueType = t.GetElementType();
				if (valueType == null) throw new MException(MError.DESERIALIZER, "Can't get element type from array type: " + t);
				Array array = Array.CreateInstance(valueType, src.Size());
				int i = 0;
				foreach(var item in src.GetValues())
				{
					var value = GetTypeValue(valueType, item);
					array.SetValue(value, i++);
				}
				return array;

			}
			if (t.IsClass && data is MMap classMap)
			{
				// add new class type

				if (t.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0)
				{
					var c = Activator.CreateInstance(t);
					if (c == null) throw new MException(MError.DESERIALIZER, "Can't create instace of type: " + t);
					return Deserialize(c, classMap, t.GetTypeInfo());
				}
				MS.Trap(MError.DESERIALIZER, "class is not serializable: " + t);
			}
			if (t.IsClass && data.DataType is ClassCSType && data is MDataList strlist)
			{
				// list contains struct data, like MDataStruct

				if (t.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0)
				{
					var c = Activator.CreateInstance(t);
					if (c == null) throw new MException(MError.DESERIALIZER, "Can't create instace of type: " + t);
					return Deserialize(c, strlist, t.GetTypeInfo());
				}
				MS.Trap(MError.DESERIALIZER, "class is not serializable: " + t);
			}
			if (t.IsEnum)
			{
				var converter = new System.ComponentModel.EnumConverter(t);
				return converter.ConvertFrom(data.GetString());
			}
			throw new MException(MError.DESERIALIZER, "can't convert " + data + " to " + t);
		}
	}
}
