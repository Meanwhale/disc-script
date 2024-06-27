namespace DiscScriptCore
{
	using DiscScript;
	using System;

	public class Parser
	{
		public const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
		public const string numbers = "1234567890";
		public const string hexNumbers = "1234567890abcdefABCDEF";
		public const string indentationChars = " \t";
		public const string whiteSpace = " \t\n";
		public const string linebreak = "\n\r";
		public const string expressionBreak = ",";
		public const string dotChar = ".";
		public const string assignChar = ":";
		public const string listItemChar = "-";
		public const string callbackChar = "$";
		public const string referenceChar = "%";
		public const string blockStart = "([{";
		public const string blockEnd = ")]}";

		// characters that make break when reading text or number value
		public static readonly string breakers = " \t\n\r,:" + blockStart + blockEnd;
		
		private const int MAX_QUOTE_SIZE = 4096;
		private static readonly byte[] quoteBuffer = new byte[MAX_QUOTE_SIZE];

		private static int lastStart, depth, quoteIndex;
		private static Token? root;
		private static Token? currentBlock;
		private static Token? currentExpr;
		private static Token? currentToken;

		private static void Next(byte state)
		{
			// transition to a next state
			lastStart = automata.GetIndex();
			automata.Next(state);
		}
		private static void NextCont(byte state)
		{
			// continue with same token,
			// so don't reset the start index
			automata.Next(state);
		}
		private static void Stay()
		{
			automata.Stay();
		}
		private static void AddQuoteByte(int i)
		{
			if (quoteIndex >= MAX_QUOTE_SIZE)
			{
				automata.Trap("text is too long");
			}
			quoteBuffer[quoteIndex++] = (byte)i;
		}

		private static void AddHexByte()
		{
			// eg. "\xF4"
			//       ^lastStart
			byte high = automata.GetByte(lastStart + 1);
			byte low = automata.GetByte(lastStart + 2);
			byte b = (byte)(((HexCharToByte(high) << 4) & 0xf0) | HexCharToByte(low));
			AddQuoteByte(b);
		}
		public static byte HexCharToByte(byte c)
		{
			int code = c & 0xff;
			if (code >= '0' && code <= '9') return (byte)(code - '0');
			if (code >= 'a' && code <= 'f') return (byte)(0xa + code - 'a');
			if (code >= 'A' && code <= 'F') return (byte)(0xa + code - 'A');
			automata.Trap("wrong hex character: " + c);
			return 0;
		}
		private static void AddExprBreak()
		{
			MS.VerboseLine("NEW TOKEN: <EXPRESSION_BREAK>");
			AddToken(new Token(currentExpr, TokenType.EXPRESSION_BREAK, ""));
		}
		private static void AddToken(TokenType tokenType)
		{
			string data = lastStart >= 0 ? automata.GetString(lastStart, automata.GetIndex() - lastStart) : "";
			MS.VerboseLine("NEW TOKEN: "+ data + " <" + tokenType + ">");
			AddToken(new Token(currentExpr, tokenType, data));
		}
		private static void AddQuote()
		{
			// convert raw bytes to UTF-8

			string data = System.Text.Encoding.UTF8.GetString(quoteBuffer, 0, quoteIndex);

			MS.VerboseLine("NEW TOKEN: "+ data + " <QUOTE TEXT>");
			AddToken(new Token(currentExpr, TokenType.TEXT, data));
		}
		private static void AddToken(Token token)
		{
			if (currentExpr == null) automata.Trap("parse error"); 
			if (currentToken == null) currentExpr.child = token;
			else currentToken.next = token;
			currentExpr.numChildren++;
			currentToken = token;
			lastStart = automata.GetIndex();
		}

		private static void AddBlock()
		{
			if (currentExpr == null) automata.Trap("parse error"); 
			int inputByte = automata.GetInputByte();
			TokenType blockType = TokenType.NONE;
			if (inputByte == '(') blockType = TokenType.PARENTHESIS;
			else if (inputByte == '[') blockType = TokenType.SQUARE_BRACKETS;
			else if (inputByte == '{') blockType = TokenType.CURLY_BRACKETS;
			else { automata.Trap("unhandled block start: " + inputByte); }
			lastStart = -1;
			Token block = new Token(currentExpr, blockType, blockType.ToString());
			if (currentToken == null) currentExpr.child = block;
			else currentToken.next = block;
			currentExpr.numChildren++;
			currentBlock = block;
			Token expr = new Token(currentBlock, TokenType.BLOCK, "<BLOCK>");
			currentBlock.child = expr;
			currentExpr = expr;
			currentToken = null;
		}
		private static void EndBlock()
		{
			if (currentExpr == null || currentExpr.parent == null || currentBlock == null || currentBlock.parent == null) automata.Trap("parse error"); 
			if (currentBlock == null) automata.Trap("unexpected block end");
			int inputByte = automata.GetInputByte();
			// Check that block-end character is the right one.
			// The 'type' is block start/end character's ASCII code.
			if (currentBlock.type == TokenType.PARENTHESIS) { automata.Assertion(inputByte == ')', "invalid block end; parenthesis was expected"); }
			else if (currentBlock.type == TokenType.SQUARE_BRACKETS) { automata.Assertion(inputByte == ']', "invalid block end; square bracket was expected"); }
			else if (currentBlock.type == TokenType.CURLY_BRACKETS) { automata.Assertion(inputByte == '}', "invalid block end; curly bracket was expected"); }
			else { automata.Trap("unhandled block end: " + inputByte); }
			lastStart = -1;
			currentToken = currentBlock;
			currentExpr = currentToken.parent;
			currentBlock = currentExpr.parent;
		}
		
		private static MList<char[]> indentationStack = new MList<char[]>();

		private static void EndIndentation()
		{	
			if (lastStart >= automata.GetIndex())
			{
				// root level
				indentationStack.RemoveAll();
				depth = 0;
			}
			else
			{
				var ind = automata.GetChars(lastStart, automata.GetIndex() - lastStart);
				if (indentationStack.Size() == 0)
				{
					indentationStack.Add(ind);
					depth = 1;
				}
				else
				{
					// get depth	

					int offset = 0; // offset for current line's indentation
					var it = indentationStack.Iterator();
					depth = 0;
					while(it.Next())
					{
						// match indentation
						
						var stack = it.Value;
						if (ind.Length - offset < stack.Length)  throw new MException(MError.INDENTATION);
						for(int i=0; i<stack.Length; i++)
						{
							if (stack[i] != ind[offset+i]) throw new MException(MError.INDENTATION);
						}
						depth++;
						offset += stack.Length;

						if (offset == ind.Length)
						{
							// less or equal indentation, remove rest if any
							
							while(it.Next()) it.Remove();
							return;
						}
					}
					// more indentation: add rest to the stack
					depth ++;
					var add = new char[ind.Length - offset];
					for(int i=0; i < add.Length; i++)
					{
						add[i] = ind[offset + i];
					}
					indentationStack.AddLast(add);
				}
			}
		}

		// static part
		
		private static ByteAutomata automata;
		static Parser()
		{
			automata = new ByteAutomata();
			InitAutomata();
		}

		private static byte stateBOM1, stateBOM2, stateBOM3, stateSpace, stateName, stateInteger, stateStart, stateQuote, stateEscape, stateHex, stateMinus, stateDecimal, stateReference;

		private static void InitAutomata()
		{
			var ba = automata; // shorter name...
			
			stateBOM1 = ba.AddState("BOM_1"); // states to skip UTF-8 BOM start: https://en.wikipedia.org/wiki/Byte_order_mark#UTF-8
			stateBOM2 = ba.AddState("BOM_2");
			stateBOM3 = ba.AddState("BOM_3");

			stateStart = ba.AddState("line start");
			stateSpace = ba.AddState("space");
			stateName = ba.AddState("name");
			stateInteger = ba.AddState("integer");
			stateQuote = ba.AddState("quote");
			stateEscape = ba.AddState("escape");
			stateHex = ba.AddState("hex");
			stateMinus = ba.AddState("minus");
			stateDecimal = ba.AddState("decimal");
			stateReference = ba.AddState("reference");
			
			// file start may have UTF-8 BOM bytes 0xEF, 0xBB, 0xBF

			const string UTF8_BOM_ERROR_MSG = "error in file start: UTF-8 or ASCII format expected";
			
			ba.FillTransition(stateBOM1, () => { Stay(); Next(stateStart); });
			ba.Transition(stateBOM1, 0xef, () => Next(stateBOM2));

			ba.FillTransition(stateBOM2, () => automata.Trap(UTF8_BOM_ERROR_MSG));
			ba.Transition(stateBOM2, 0xbb, () => Next(stateBOM3));

			ba.FillTransition(stateBOM3, () => automata.Trap(UTF8_BOM_ERROR_MSG));
			ba.Transition(stateBOM3, 0xbf, () => { Next(stateStart); lastStart++; } ); // move lastStart one forward from BOM byte


			// line start. read indentation depth
			
			ba.Transition(stateStart, indentationChars, null);
			ba.Transition(stateStart, letters, () => { EndIndentation(); Next(stateName); });
			ba.Transition(stateStart, numbers, () => { EndIndentation(); Next(stateInteger); });
			ba.Transition(stateStart, "-", () => { EndIndentation(); Next(stateMinus); }); 
			ba.Transition(stateStart, listItemChar, () => { EndIndentation(); AddToken(TokenType.LIST_ITEM); Next(stateSpace); });
			ba.Transition(stateStart, callbackChar, () => { EndIndentation(); AddToken(TokenType.CALLBACK); Next(stateSpace); });
			ba.Transition(stateStart, blockStart, () => { EndIndentation(); AddBlock(); Next(stateSpace); });
			ba.Transition(stateStart, linebreak, () => { NewLine(); });

			// space state

			ba.Transition(stateSpace, whiteSpace, null);
			ba.Transition(stateSpace, "-", () => { Next(stateMinus); });
			ba.Transition(stateSpace, referenceChar, () => { Next(stateReference); lastStart = automata.GetIndex() + 1; });
			ba.Transition(stateSpace, letters, () => { Next(stateName); });
			ba.Transition(stateSpace, numbers, () => { Next(stateInteger); });
			ba.Transition(stateSpace, expressionBreak, () => { AddExprBreak(); });
			ba.Transition(stateSpace, linebreak, LineBreak);
			ba.Transition(stateSpace, blockStart, () => { AddBlock(); });
			ba.Transition(stateSpace, blockEnd, () => { EndBlock(); });
			ba.Transition(stateSpace, "\"", () => { Next(stateQuote); quoteIndex = 0; });
			ba.Transition(stateSpace, assignChar, () => { AddToken(TokenType.ASSIGN); Next(stateSpace); });
			
			// reference

			ba.Transition(stateReference, letters, null);
			ba.Transition(stateReference, breakers, () => { AddToken(TokenType.REFERENCE); Stay(); Next(stateSpace); });

			// name: text without quote marks

			ba.Transition(stateName, letters, null);
			ba.Transition(stateName, numbers, null);
			ba.Transition(stateName, dotChar, null);

			ba.Transition(stateName, breakers, () => { AddToken(TokenType.TEXT); Stay(); Next(stateSpace); });

			// integer and decimal numbers, positive and negative
			
			ba.Transition(stateInteger, numbers, null);
			ba.Transition(stateInteger, ".", () => { NextCont(stateDecimal); });

			ba.Transition(stateInteger, breakers, () => { AddToken(TokenType.INTEGER); Stay(); Next(stateSpace); });

			ba.Transition(stateMinus, numbers, () => { NextCont(stateInteger); }); // change state without reseting starting index
			ba.Transition(stateMinus, ".", () => { NextCont(stateDecimal); });

			ba.Transition(stateDecimal, numbers, null);

			ba.Transition(stateDecimal, breakers, () => { AddToken(TokenType.DECIMAL); Stay(); Next(stateSpace); });

			// quote
			
			ba.FillTransition(stateQuote, () => { AddQuoteByte(automata.currentInput); });
			ba.Transition(stateQuote, linebreak, () => { MS.Assertion(false, MError.BYTE_AUTOMATA, "line break inside a quotation"); });
			ba.Transition(stateQuote, "\"", () => { lastStart++; AddQuote(); Next(stateSpace); });
			ba.Transition(stateQuote, "\\", () => { Next(stateEscape); });

			ba.FillTransition(stateEscape, () => { automata.Trap("invalid escape character in quotes"); });

			// standard escape character literals: https://en.cppreference.com/w/cpp/language/escape

			ba.Transition(stateEscape, "'",  () => { AddQuoteByte(0x27); Next(stateQuote); });
			ba.Transition(stateEscape, "\"", () => { AddQuoteByte(0x22); Next(stateQuote); });
			ba.Transition(stateEscape, "?",  () => { AddQuoteByte(0x3f); Next(stateQuote); });
			ba.Transition(stateEscape, "\\", () => { AddQuoteByte(0x5c); Next(stateQuote); });
			ba.Transition(stateEscape, "a",  () => { AddQuoteByte(0x07); Next(stateQuote); });
			ba.Transition(stateEscape, "b",  () => { AddQuoteByte(0x08); Next(stateQuote); });
			ba.Transition(stateEscape, "f",  () => { AddQuoteByte(0x0c); Next(stateQuote); });
			ba.Transition(stateEscape, "n",  () => { AddQuoteByte(0x0a); Next(stateQuote); });
			ba.Transition(stateEscape, "r",  () => { AddQuoteByte(0x0d); Next(stateQuote); });
			ba.Transition(stateEscape, "t",  () => { AddQuoteByte(0x09); Next(stateQuote); });
			ba.Transition(stateEscape, "v",  () => { AddQuoteByte(0x0b); Next(stateQuote); });

			ba.Transition(stateEscape, "x", () => { Next(stateHex); });

			ba.FillTransition(stateHex, () => { automata.Trap("invalid hexadecimal byte"); });
			ba.Transition(stateHex, hexNumbers, () => { if (automata.GetIndex() - lastStart >= 2) { AddHexByte(); Next(stateQuote); } });
		}


		private static void LineBreak()
		{
			if (root == null || handler == null) automata.Trap("parse error");
			automata.Assertion(currentBlock == null, "missing ending parenthesis");
			if (root.numChildren > 0) handler.HandleLine(root, depth);
			NewLine();
		}

		private static void NewLine()
		{
			if (automata.currentState != stateStart)
			{
				ResetLine();
				automata.Next(stateStart); // set first state
			}
			lastStart = automata.GetIndex() + 1; // move lastStart one forward from line break char
		}

		private static void ResetLine()
		{
			root = new Token(null, 0, "<ROOT>");
			currentExpr = root;
			currentBlock = null;
			currentToken = null;
		}

		private static TokenHandler? handler;

		public static MMap Read(string code)
		{
			return Read(new MInputArray(code));
		}
		public static MMap Read(MInput input)
		{
			// NOTE: because this is static, parser and automata might be
			// in previous parsing state, so let's reset.

			lastStart = depth = quoteIndex = 0;
			
			automata.Reset(stateBOM1);
			
			ResetLine();

			handler = new TokenHandler();

			automata.Run(input);
			
			// close up
			automata.Step((byte)'\n');
			automata.Step((byte)'\n');
			handler.Close(); // close structs etc.

			MS.VerboseLine("\nFINISHED!");

			return handler.Root;
		}
	}
}
