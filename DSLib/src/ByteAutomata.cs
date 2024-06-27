namespace DiscScriptCore
{
	using DiscScript;
	using System.Diagnostics.CodeAnalysis;

	public class ByteAutomata
	{
		internal static bool Debug = false;

		//internal bool ok;
		internal byte[] tr;
		internal int currentInput;
		internal byte currentState;
		internal Dictionary<int, string> stateNames = new Dictionary<int, string>();
		internal MS.MAction[] actions = new MS.MAction[128];
		internal byte stateCounter;
		internal byte actionCounter; // 0 = end

		// running:
		internal byte inputByte = 0;
		internal int index = 0;
		internal int lineNumber = 0;
		internal bool stayNextStep = false;
		internal bool running = false;

		internal byte[] buffer;
		internal byte[] tmp;


		public const int MAX_STATES = 32;
		public const int BUFFER_SIZE = 1024;
		public const int MAX_NAME_LENGTH = 512; // C# variable max. length is 511 (+ \0)

		public ByteAutomata()
		{
			stateCounter = 0;
			actionCounter = 0;
			tr = new byte[MAX_STATES * 256];
			for (int i = 0; i < MAX_STATES * 256; i++) tr[i] = 0xff;

			buffer = new byte[BUFFER_SIZE];
			tmp = new byte[BUFFER_SIZE];

			currentState = 0xff; // cause error if not Reset before parsing
		}

		public void Reset(byte initState)
		{
			currentInput = -1;
			currentState = initState;
			inputByte = 0xff;
			index = 0;
			lineNumber = 0;
			stayNextStep = false;
			running = false;
		}

		//

		public void Print()
		{
			for (int i = 0; i <= stateCounter; i++)
			{
				MS.WriteLine("state: " + i);

				for (int n = 0; n < 256; n++)
				{
					byte foo = tr[(i * 256) + n];
					if (foo == 0xff) MS.Write(".");
					else MS.WriteLine(foo.ToString());
				}
				MS.WriteLine("");
			}
		}

		public byte AddState(string stateName)
		{
			stateCounter++;
			stateNames[stateCounter] = stateName;
			return stateCounter;
		}

		public void Transition(byte state, string input, MS.MAction? action)
		{
			byte actionIndex = 0;
			if (action != null)
			{
				actionIndex = AddAction(action);
			}

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);

			int i = 0;
			while (i < input.Length)
			{
				tr[(state * 256) + bytes[i]] = actionIndex;
				i++;
			}
		}

		public void Transition(byte state, byte inputByte, MS.MAction? action)
		{
			byte actionIndex = 0;
			if (action != null)
			{
				actionIndex = AddAction(action);
			}
			tr[(state * 256) + inputByte] = actionIndex;
		}

		public void FillTransition(byte state, MS.MAction action)
		{
			byte actionIndex = 0;
			if (action != null) actionIndex = AddAction(action);

			for (int i = 0; i < 256; i++)
			{
				tr[(state * 256) + i] = actionIndex;
			}
		}

		public byte AddAction(MS.MAction action)
		{
			actionCounter++;
			actions[actionCounter] = action;
			return actionCounter;
		}


		public void Next(byte nextState)
		{
			currentState = nextState;

			if (Debug) MS.WriteLine("next state: " + stateNames[(int)currentState]);
		}

		// NOTE: don't use exceptions. On error, use error print and set ok = false

		public void Step(byte input)
		{
			if (Debug) MS.WriteLine(MS.ToSafeChar(input));

			currentInput = input;
			int index = (currentState * 256) + input;
			byte actionIndex = tr[index];


			if (actionIndex == 0) return; // stay on same state and do nothing else

			// NOTE: this could possibly be optimized by filling 0xff and other error cases with error MAction.

			if (actionIndex == 0xff)
			{
				Trap("unexpected char: " + MS.ToSafeChar(input) + " code = " + input);
			}

			MS.MAction act = actions[actionIndex];

			if (act == null) Trap("invalid action index: " + actionIndex);

			act();
		}

		public int GetIndex()
		{
			return index;
		}

		public int GetInputByte()
		{
			return inputByte;
		}

		public void Stay()
		{
			// same input byte on next step
			Assertion(!stayNextStep, "'stay' is called twice");
			stayNextStep = true;
		}
		public byte GetByte(int index)
		{
			return buffer[index];
		}
		public string GetString(int start, int length)
		{
			Assertion(length < MAX_NAME_LENGTH, "name is too long");

			for (int i = 0; i < length; i++)
			{
				tmp[i] = buffer[start++ % BUFFER_SIZE];
			}

			return System.Text.Encoding.UTF8.GetString(tmp, 0, length);
		}
		public char[] GetChars(int start, int length)
		{
			Assertion(length < MAX_NAME_LENGTH, "name is too long");

			var c = new char[length];

			for (int i = 0; i < length; i++)
			{
				c[i] = (char)buffer[start++ % BUFFER_SIZE];
			}

			return c;
		}

		public void Run(MInput input)
		{
			inputByte = 0xff;
			index = 0;
			lineNumber = 1;
			stayNextStep = false;
			running = true;

			while (!input.End() || stayNextStep)
			{
				if (!stayNextStep)
				{
					index++;
					inputByte = input.ReadByte();
					buffer[index % BUFFER_SIZE] = inputByte;
					if (inputByte == 10) lineNumber++; // line break
				}
				else
				{
					stayNextStep = false;
				}
				Step(inputByte);
			}

			if (!stayNextStep) index++;
		}

		[DoesNotReturn]
		public void Trap(string msg)
		{
			PrintError();
			throw new MException(MError.BYTE_AUTOMATA, msg);
		}
		public void Assertion(bool b, string msg)
		{
			if (b) return;
			Trap(msg);
		}

		public void PrintError()
		{
			MS.ErrorPrinter.WriteLine("-------------------------------------------");
			MS.ErrorPrinter.WriteLine("Parser state: " + stateNames[currentState]);
			MS.ErrorPrinter.Write("Line " + lineNumber + ": \"");

			// print nearby code
			int start = index - 1;
			while (start > 0 && index - start < BUFFER_SIZE && (char)buffer[start % BUFFER_SIZE] != '\n')
			{
				start--;
			}
			while (++start < index)
			{
				MS.ErrorPrinter.WriteSafeByteChar(buffer[start % BUFFER_SIZE]);
			}
			MS.ErrorPrinter.WriteLine("\"");
		}

	}
}
