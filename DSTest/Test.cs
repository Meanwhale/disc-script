namespace DiscScriptCore
{
	using DiscScript;
	using System;

	public class Test
	{
		public static MList<UnitTest> testList = new MList<UnitTest>();
		public class UnitTest
		{
			public delegate void testDelegate();
			public string TestName;
			public testDelegate test;
			private bool enabled;

			public UnitTest(string testName, testDelegate test, bool _enabled = true)
			{
				TestName = testName;
				this.test = test;
				enabled = _enabled;
				if (enabled) testList.Add(this);
			}
			public bool Run()
			{
				if (!enabled) return true;
				MS.WriteLine("TEST "+ TestName);
				/*try
				{*/
					test();
					MS.WriteLine("OK");
					return true;
				/*}
				catch (System.Exception e)
				{
					MS.WriteLine("\n\n" + TestName + " TEST FAILED");
					if (e is MException me) MS.WriteLine(me.info);
					MS.WriteLine(e.ToString());
					return false;
				}*/
			}
		}
	
		public static void Assertion(bool b)
		{
			MS.Assertion(b, MError.TEST, "");
		}
		

		public static UnitTest NullTest = new UnitTest("Null", () =>
		{
			var data = Parser.Read("$struct DSClass.Article\n  string Content\n  int32 Count\n[DSClass.Article] root\n  - %null\n  - 1");
			
			var o = new MBufferOutput();
			MapSerializer.Write(data, o);
			MS.VerboseLine(o.GetString());
		});

		public static UnitTest[] tests = new UnitTest[] {
			
			new UnitTest("Hierarchy", () =>
			{
				// TODO
			}),
			new UnitTest("Primitive types", () =>
			{
				var data = Parser.Read(
					"short: 32767\n" +
					"ushort: 32768\n" +
					"i32: 2147483647\n" +
					"u32: 2147483649\n" +
					"n32: -123\n" +
					"i64: 9223372036854775807\n"+
					"u64: 9223372036854775808\n"+
					"f:1.23\n" +
					"d: -1.23\n" +
					"dec: 134.56\n" +
					"t: Hello\n" +
					"u: \"Päivää\"\n" +
					"c: a\n" +
					"ubyte: 200\n" +
					"sbyte: -1\n" +
					"bt: True\n" +
					"bf: false");
				Assertion(data["short"].GetShort() == 32767); // max
				Assertion(data["ushort"].GetUShort() == 32768);
				Assertion(data["i32"].GetInt() == 2147483647);
				Assertion(data["u32"].GetUInt() == 2147483649);
				Assertion(data["n32"].GetInt() == -123);
				Assertion(data["i64"].GetLong() == 9223372036854775807); // max
				Assertion(data["u64"].GetULong() == 9223372036854775808);
				Assertion(data["f"].GetFloat() == 1.23f);
				Assertion(data["d"].GetDouble() == -1.23);
				Assertion(data["dec"].GetDecimal() == 134.56m);
				Assertion(data["t"].GetString() == "Hello");
				Assertion(data["u"].GetString() == "Päivää"); // u for unicode
				Assertion(data["c"].GetChar() == 'a');
				Assertion(data["ubyte"].GetByte() == 200);
				Assertion(data["sbyte"].GetSByte() == -1);
				Assertion(data["bt"].GetBool() == true);
				Assertion(data["bf"].GetBool() == false);
			}),
			new UnitTest("Open parenthesis error", () =>
			{
				try
				{
					Parser.Read("a: (1,2");
					Assertion(false);
				}
				catch (MException e)
				{
					Assertion(e.error == MError.BYTE_AUTOMATA);
				}
			}),
			new UnitTest("Indentation error", () =>
			{
				try
				{
					var data = Parser.Read("a\n  - 1\n - 2");
					Assertion(false);
				}
				catch (MException e)
				{
					Assertion(e.error == MError.INDENTATION);
				}
			}),
			new UnitTest("Map serializer", () =>
			{
				// TODO
			}),
			new UnitTest("Struct serializer", () =>
			{
				Person p = new ();
				var o = new MBufferOutput();
				StructSerializer.Write(p, o);
				MS.VerboseLine(o.Buffer.ToString());
				var p2 = MDeserializer.ReadStruct<Person>(o.Buffer.ToString());

				MS.VerboseLine(o.Buffer.ToString());

				Assertion(p.Match(p2));
			}),
			new UnitTest("Encoding", () =>
			{
				// with UTF-8 BOM
				var data = Parser.Read(new MInputArray(new byte [] {
					0xef, 0xbb, 0xbf, // BOM
					0x78, 0x3a, 0x22, 0xc3, 0xb6, 0x22 // x:"ö"
					}));
				Assertion(data["x"].GetString().Equals("ö"));
				
				// without UTF-8 BOM
				data = Parser.Read(new MInputArray(new byte [] {
					0x78, 0x3a, 0x22, 0xc3, 0xb6, 0x22 // x:"ö"
				}));
				Assertion(data["x"].GetString().Equals("ö"));

				// encoding error
				try
				{
					data = Parser.Read(new MInputArray(new byte [] {
						0x78, 0x3b, 0x22 // not UTF-8 BOM
					}));
					Assertion(false);
				}
				catch (MException e)
				{
					Assertion(e.error == MError.BYTE_AUTOMATA);
				}
			}),
			new UnitTest("Conversion error", () =>
			{
				try
				{
					var data = Parser.Read("a: 1.23");
					Assertion(data["a"].GetDouble() == 1.23);
					data["a"].GetInt(); // throws
					Assertion(false);
				}
				catch (MException e)
				{
					Assertion(e.error == MError.CONVERSION);
				}
			}),
			NullTest
		};

		public static void RunAll()
		{
			foreach (var t in tests)
			{
				if (!t.Run()) break; // if test failed, stop
			}
		}

		public static void FileTest()
		{
			const string FILE_NAME = "ds_output_test.is";

			Person p = new ();
			var fout = new MFileOutput(FILE_NAME);
			StructSerializer.Write(p, fout);
			
			var fin = new MSFileInput(FILE_NAME);
			var p2 = MDeserializer.ReadStruct<Person>(fin);

			Assertion(p.Match(p2));
		}
	}
}