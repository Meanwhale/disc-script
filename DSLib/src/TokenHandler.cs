namespace DiscScriptCore
{
	using DiscScript;
	using System.Diagnostics.CodeAnalysis;

	internal class TokenHandler
	{
		public MMap Root { get; private set; } = new MMap();
		
		// Structs that are defined in script. they may or may not match the native struct definitions in serializer.
		// For example, if class has changed after serialization, it has different struct. It can still be deserialized
		// if there's no conflict.

		public Dictionary<string, ClassCSType> Structs = new Dictionary<string, ClassCSType>();

		// compile-time state

		private MList<IData> stack = new MList<IData>(); // first = top
		private string? collectionName;
		private int depth;
		private ClassCSType? currentDefinition = null;
		private const string DATA_ERROR = "data error", UEOL = "unexpected end of line";

		public TokenHandler()
		{
			stack.Add(Root); // always the last item
		}
		
		[DoesNotReturn]
		public void Trap(string msg, MError? err = null)
		{
			throw new MException(err == null ? MError.TOKEN_HANDLER : err, msg);
		}
		public void Assertion(bool b, string msg, MError? err = null)
		{
			if (b) return;
			Trap(msg, err);
		}

		internal void HandleLine(Token tree, int _depth)
		{
			if (MS.IsVerbose)
			{
				MS.WriteLine("handle line, depth: " + depth + ":");
				tree.PrintTree(true);
			}
			Assertion(stack.GetAt(stack.Size() - 1) == Root, "root is not there");
			Assertion(_depth < stack.Size() || (_depth == stack.Size() && collectionName != null) || currentDefinition != null, "stack error", MError.INDENTATION);
			depth = _depth;

			// TODO: struct definition --> state: jos rivissä on sisennyt niin menee definointiin
			// ja jos ei niin definointi loppuu. etsi luokka InTypeistä ja lisää memberit.
			// [] -> geneerisen muuttujan parametrit.

			var it = tree.child;
			if (it == null) return;

			// struct definition

			if (currentDefinition != null)
			{
				if (depth == 0)
				{
					MS.VerboseLine("end struct");
					currentDefinition = null;
				}
				else
				{
					Assertion(depth == 1, "struct definition indentation error", MError.INDENTATION);

					var typename = it.data;


					it = it.Next();
					
					if (it.type == TokenType.TEXT)
					{
						Assertion(it.next == null, "member name expected");

						// e.g. "int32 Count"
						var membername = it.data;
						ICSType t = GetCSTypeByName(typename);
						currentDefinition.Add(membername, t); 
					}
					else if (it.type == TokenType.SQUARE_BRACKETS)
					{
						// e.g. "map[ int32 DSClass.Article ] Articles"

						// read parameters

						var pars = ReadGenericParameters(it);

						var gt = GetGenericCSType(typename, pars);
						
						it = it.Next();

						currentDefinition.Add(it.data, gt); 
					}
					else Trap("struct member error");

					return;
				}
			}
			
			// callback

			if (it.type == TokenType.CALLBACK)
			{
				Assertion(depth == 0, "zero indentation expected", MError.INDENTATION);
				while (stack.Size() > 1) stack.RemoveAt(stack.Size() - 1);
				it = it.Next();
				if (it.data.Equals("struct"))
				{
					it = it.Next();
					Assertion(MS.ValidName(it.data), "invalid struct name: " + it.data);
					MS.VerboseLine("create struct: " + it.data);
					Assertion(it.next == null, UEOL);

					StartStruct(it.data);

					// TODO

					return;
				}
				else Trap("struct expected");
			}

			// depth check
			
			if (depth == stack.Size())
			{
				MS.VerboseLine("level up");
			}
			else if (depth == stack.Size() - 1)
			{
				MS.VerboseLine("same level");
			}
			else
			{
				int delta = stack.Size() - depth - 1;
				MS.VerboseLine("level down: " + delta);
				while(delta-->0)
				{
					stack.First().Close();
					stack.RemoveFirst();
				}
			}
			

			if (it.type == TokenType.TEXT || it.type == TokenType.INTEGER)
			{
				string key = it.data;
				it = it.next;

				if (it == null)
				{
					if (collectionName != null)
					{
						// first line on map, e.g.
						//	map
						//		this
						//			<next line>

						if (stack.First() is MMap top)
						{
							var map = new MMap();
							top.Add(collectionName, map);
							stack.AddFirst(map);
							collectionName = null;
						}
						else Trap(DATA_ERROR);
					}
					// just a map or list name. save it for later.
					collectionName = key;
					return;
				}

				if (it.type == TokenType.ASSIGN)
				{
					it = it.Next();
					AddKeyValue(key, GetDataFromToken(it));
				}
				else
				{
					Error("unexpected token: " + it.type);
				}
			}
			else if (it.type == TokenType.LIST_ITEM)
			{
				it = it.Next();
				if (it.next != null)
				{
					// map as a list item

					var key = it.data;
					it = it.next;
					Assertion(it.type == TokenType.ASSIGN, "assign expected");
					it = it.Next();
					var val = it.data;

					var map = new MMap();
					AddListItem(map);
					map.Add(key, new MText(val));
					stack.AddFirst(map);

				}
				else
				{
					var data = GetDataFromToken(it);
					Assertion(it.next == null, "");
					AddListItem(data);
				}
			}
			else if (it.type == TokenType.SQUARE_BRACKETS)
			{
				// typed key
				// e.g.
				//		[<ROOT>]
				//			[<BLOCK>]
				//				[<EXPR>]
				//					[DSClass.Person]
				//			[root]
				
				var child = it.Child().Child();
				Assertion(child.type == TokenType.TEXT, "type name expected");
				var typeName = child.data;

				it = it.Next();
				var key = it.data;
				Assertion(it.next == null, UEOL);

				MS.VerboseLine("typed key: type " + typeName + ", name " + key);

				Assertion(Structs.ContainsKey(typeName), "scripted struct not defined: " + typeName);
				

				var dataStruct = new MDataList(Structs[typeName]);
				
				AddKeyValue(key, dataStruct);
				stack.AddFirst(dataStruct);
			}
			else
			{
				Error("unexpected token: " + it.type);
			}
		}

		private ICSType [] ReadGenericParameters(Token it)
		{
			var pars = new MList<ICSType>();
			it = it.Child().Child();
			while (true)
			{
				var typeParam = GetCSTypeByName(it.data);
				pars.Add(typeParam);
				if (it.next == null) break;
				it = it.next;
			}
			return pars.ToArray();
		}

		public void Close()
		{
			while (stack.Size() > 0)
			{
				stack.First().Close();
				stack.RemoveFirst();
			}
		}

		private IData GetDataFromToken(Token it)
		{
			if (it.type == TokenType.TEXT || it.type == TokenType.INTEGER || it.type == TokenType.DECIMAL)
			{
				return new MText(it.data);
			}
			else if (it.type == TokenType.CURLY_BRACKETS)
			{
				return GetInLineMap(it); // in-line map
			}
			else if (it.type == TokenType.PARENTHESIS)
			{
				return GetInLineList(it); // in-line map
			}
			else if (it.type == TokenType.REFERENCE)
			{
				Assertion(it.data.Equals("null"), "null reference expected"); // only kind of reference supported... for now
				return MNull.Value;
			}
			throw new MException(MError.TOKEN_HANDLER, "unexpected token: " + it.type);
		}

		private MMap GetInLineMap(Token it)
		{
			// e.g. {k:v,lk:(1,2,3)}
			var map = new MMap();
			Assertion(it.type == TokenType.CURLY_BRACKETS, "map block expected");
			it = it.Child();
			Assertion(it.type == TokenType.BLOCK, "map block expected");
			it = it.Child();

			while (true)
			{
				if (it.type == TokenType.TEXT || it.type == TokenType.INTEGER)
				{
					string key = it.data;
					it = it.Next();
					Assertion(it.type == TokenType.ASSIGN, "assign expected");
				
					it = it.Next();
					var value = GetDataFromToken(it);

					map.Add(key, value);

					if (it.next == null) break;
					it = it.next;
					Assertion(it.type == TokenType.EXPRESSION_BREAK, "expression break or } expected");
					it = it.Next();
				}
				else Trap("valid map key expected, not: " + it.data);
			}

			return map; // TODO
		}

		private MDataList GetInLineList(Token it)
		{
			// e.g. (1, 2, {k:v})
			var list = new MDataList();
			Assertion(it.type == TokenType.PARENTHESIS, "list block expected");
			it = it.Child();
			Assertion(it.type == TokenType.BLOCK, "list block expected");
			it = it.Child();

			while (true)
			{
				// read list item
				
				var data = GetDataFromToken(it);
				list.Add(data);

				if (it.next != null)
				{
					it = it.next;
					Assertion(it.type == TokenType.EXPRESSION_BREAK, "expression break or ) expected");
					it = it.Next();
				}
				else break;
			}

			return list;
		}

		private void Error(string s)
		{
			throw new MException(MError.TOKEN_HANDLER, s);
		}
		private ICSType GetCSTypeByName(string typename)
		{
			ICSType? t = MSerializer.GetSimpleCSType(typename);
			if (t != null) return t;
			t = MSerializer.GetClassCSType(typename);
			if (t != null) return t;
			throw new MException(MError.TOKEN_HANDLER, "type not found: " + typename);
		}
		private SimpleCSType GetSimpleCSType(string typename)
		{
			var t = MSerializer.GetSimpleCSType(typename);
			if (t == null) throw new MException(MError.TOKEN_HANDLER, "simple type not found: " + typename);
			return t;
		}
		private GenericCSType GetGenericCSType(string typename, ICSType [] parameters)
		{
			var t = MSerializer.GetGenericCSType(typename, parameters);
			if (t == null) throw new MException(MError.TOKEN_HANDLER, "generic type not found: " + typename);
			return t;
		}

		private void StartStruct(string name)
		{
			Assertion(!Structs.ContainsKey(name), "not a struct: " + name);
			currentDefinition = Structs[name] = new ClassCSType(name, typeof(MDataList));
		}

		private void AddListItem(IData val)
		{
			if (collectionName != null)
			{
				// for example:
				
				//		stack_top
				//			k0: v0
				//			list			<-- list
				//				- item1

				if (stack.First() is MMap top)
				{
					var list = new MDataList();
					top.Add(collectionName, list);
					collectionName = null;
					list.Add(val);
					stack.AddFirst(list);
				}
				else Trap(DATA_ERROR);
			}
			else if (stack.First() is MDataList list)
			{
				list.Add(val);
			}
			//else if (stack.First() is MDataStruct str)
			//{
			//	str.Add(val);
			//}
			else Trap(DATA_ERROR);
		}

		private void AddKeyValue(string key, IData val)
		{
			if (depth == 0)
			{
				if (collectionName != null) throw new MException(MError.UNASSIGNED_NAME, collectionName);
				Root.Add(key, val);
			}
			else if (collectionName != null)
			{
				// for example:
				
				//		stack_top
				//			k0: v0
				//			collectionName		<-- map
				//				k1: v1

				if (stack.First() is MMap top)
				{
					var map = new MMap();
					top.Add(collectionName, map);
					collectionName = null;
					map.Add(key, val);
					stack.AddFirst(map);
				}
				else Trap(DATA_ERROR);
			}
			else if (stack.First() is MMap top)
			{
				top.Add(key, val);
			}
			else Trap(DATA_ERROR);
		}
	}
}
