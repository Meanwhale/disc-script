using System.Collections;
using System.Reflection;

namespace DiscScript
{
	public class MapSerializer
	{
		// serialize without struct
		// write key-value pairs
		
		public static void Write(MMap map, MOutput o)
		{
			map.Write(o, 0, false);
			o.Close();
		}
		public static void Write(object obj, MOutput o)
		{
			MSerializer.Init();
			SerializeClassWithNames(obj, 0, false, o);
			o.Close();
		}

		private static void SerializeClassWithNames(object obj, int depth, bool dontIndentFirst, MOutput o)
		{
			var objType = obj.GetType();
			
			MS.Assertion (objType.GetCustomAttributes(typeof(DSClassAttribute), true).Length > 0, MError.MAP_SERIALIZER, "can't serialize type: " + objType);
			
			IList<FieldInfo> fields = new List<FieldInfo>(objType.GetFields(BindingFlags.Public | BindingFlags.Instance));
			
			// data

			foreach (var fieldInfo in fields)
			{
				var value = fieldInfo.GetValue(obj);
				
				if (!dontIndentFirst)
				{
					o.Indent(depth);
				}
				else
				{
					dontIndentFirst = false;
				}
				o.Write(fieldInfo.Name);
				SerializeFieldWithNames(value, fieldInfo.FieldType, false, depth, o);
			}
		}
		
		private static void SerializeListValue(object item, int depth, MOutput o)
		{
			o.Indent(depth);
			o.Write("- ");
			SerializeFieldWithNames(item, item.GetType(), true, depth, o);
		}
		
		private static void SerializeKV(DictionaryEntry kv, int depth, MOutput o)
		{
			o.Indent(depth);
			o.Write(kv.Key);
			if (kv.Value == null) return;
			SerializeFieldWithNames(kv.Value, kv.Value.GetType(), false, depth, o);
		}

		private static void SerializeFieldWithNames(object? value, Type t, bool listItem, int depth, MOutput o)
		{
			if (value == null)
			{
				o.WriteLine("");
				return;
			}

			if (value is IDictionary dict)
			{
				if (!listItem) o.WriteLine("");
				foreach(DictionaryEntry kv in dict)
				{
					SerializeKV(kv, depth + 1, o);
				}
			}
			else if (value is IList list)
			{
				o.WriteLine("");
				foreach(var item in list)
				{
					SerializeListValue(item, depth + 1, o);
				}
			}
			else if (MSerializer.primitives.ContainsKey(t))
			{
				var csType = MSerializer.primitives[t];
				if (!listItem) o.Write(": ");
				//csType.WriteValue(o, value);
				
				if (value is string) o.Write('"').Write(value).Write('"');
				else csType.WriteValue(o, value);

				o.LineBreak();
			}
			else if (t.GetTypeInfo().IsClass)
			{
				if (!listItem) o.WriteLine("");
				SerializeClassWithNames(value, depth + 1, listItem, o);
			}
			else
			{
				MS.Trap(MError.MAP_SERIALIZER, "can't serialize type: " + t);
			}
		}

	}
}
