// See https://aka.ms/new-console-template for more information
using DiscScript;
using DiscScriptCore;

MS.WriteLine("DiscScript 0.1 (C) Meanwhale, 2024");

MSerializer.Init();

MS.Assertion(args.Length == 1, MError.CLI, "argument missing");

if (args[0].Equals("test"))
{
	Test.RunAll();
}
else if (args[0].Equals("filestest"))
{
	Test.FileTest();
}
else if (args[0].Equals("debug"))
{
	// ad-hoc debug
	Test.NullTest.Run();
}
else
{
	MS.Trap(MError.CLI, "wrongs argument: " + args[0]);
}