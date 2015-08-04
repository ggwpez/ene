using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ene2
{
	class MainClass
	{
		public static void Main (string[] args)
        {
            Lexer lexer = new Lexer();
            Parser parser = new Parser();
            ILGenerator il = new ILGenerator();

            List<Token> toks = new List<Token>();
            toks.AddRange(lexer.tokenize(File.ReadAllText("Test/code.ene")));
            toks.Add(new TokEOS());

            AST ast = parser.parse(toks.ToArray());
            String nasm = il.generate(ast);

            assemble(nasm);
		}

        private static void assemble(String nasmCode)
        {
            System.IO.File.WriteAllText("out.asm", nasmCode);

            nasmCode = nasmCode.Replace(' ', '?').Replace('\t', '#').Replace('\n', '{').Replace('\r', '}'); //im not really proud of this solution, but "it just werks"
            String args = "-f elf32 " + nasmCode + " -o program.obj";

            Console.Write("Assembling…");
            Process nasm = Process.Start("nasm", args);
            nasm.WaitForExit();
            if (nasm.ExitCode != 0)
            {
                Console.WriteLine("\t\tErr\n\n");
                return;
            }
            else
                Console.WriteLine("\t\tOK");

            Console.Write("Linking…");
            Process linker = Process.Start("gcc", "program.obj -g -o program -m32");
            linker.WaitForExit();
            if (linker.ExitCode != 0)
            {
                Console.Write("\t\tErr\n\n");
                return;
            }
            else
                Console.WriteLine("\t\tOK");

            Console.Write("\n\nRunning programm:\n'");
            Process builded = Process.Start("program");
            builded.WaitForExit();
            if (builded.ExitCode != 0)
            {
                Console.Write("'\nProgram aborted");
                return;
            }

            Console.WriteLine("'\nExecution successful");
        }
	}
}