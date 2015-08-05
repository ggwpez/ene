using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ene2
{
    public class Lexer
    {
        String toMatch = null;

        private Token number(Int32 s, out Int32 l)
        {
            int i; Boolean isDouble = false;
            for (i = s; i < toMatch.Length; i++)
                if (!Char.IsDigit(toMatch[i]))
                    break;

            if (i < toMatch.Length && toMatch[i] == '.')
            {
                isDouble |= true;
                for (i++; i < toMatch.Length; i++)
                    if (!Char.IsDigit(toMatch[i]))
                        break;
            }

            l = i-s;
            if (isDouble)
            {   //parse float
                throw new NotSupportedException("Float not supported");
            }
            else
                return (new TokNum(Int32.Parse(substring(s, l))));
        }

        private Token ident(Int32 s, out Int32 l)
        {
            int i;
            for (i = s; i < toMatch.Length; i++)
                if (!Char.IsLetterOrDigit(toMatch[i]) && toMatch[i] != '_')
                    break;

            l = i-s;
            return parse_ident(substring(s, l));
        }

        private Token parse_ident(String ident)
        {
            switch (ident) 
            {
                case "namespace":
                    return new TokNSpace();
                case "asm":
                    return new TokASM();
                case "if":
                    return new TokIf();
                case "else":
                    return new TokElse();
                case "while":
                    return new TokWhile();
                case "for":
                    return new TokFor();
                case "return":
                    return new TokReturn();
                case "struct":
                    return new TokStruct();
                case "break":
                    return new TokBreak();
                default:
                return new TokIdent(ident);
            }
        }

        private Token string_(Int32 s, out Int32 l)
        {
            Int32 i = s;
            while (toMatch[i] != '"')
                i++;
            i++;

            l = i-s;
            return new TokString(substring(s, l -1));
        }

        private Token ddot(Int32 s, out Int32 l)
        {
            l = 1;
            if (s == toMatch.Length -1)
                return new TokDDot();

            if (toMatch[s +1] == ':')
            {
                l = 2;
                return new TokDDDot();
            }

            return new TokDDot();
        }

        public Token[] tokenize(String input)
        {
            List<Token> toks = new List<Token>();
            toMatch = input;

            Int32 i = 0, l = 0;
            if (toMatch == null || toMatch.Length == 0)
                return new Token[0];

            while ((i += l) < toMatch.Length)
            {
                Char c = toMatch[i];

                if (operators.Any(e => e == c))
                { toks.Add(new TokOp(getOp(c))); l = 1; }
                else if (c == ':')
                { toks.Add(ddot(i, out l)); }
                else if (c == '.')
                { toks.Add(new TokDot());    l = 1; }
                else if (c == '°')
                { toks.Add(new TokCircle()); l = 1; }
                else if (c == ';')
                { toks.Add(new TokSemi());   l = 1; }
                else if (c == '=')
                { toks.Add(new TokAssign()); l = 1; }
                else if (Char.IsWhiteSpace(c))
                { l = 1; continue; }
                else if (c == '(')
                { toks.Add(new TokLBrk());  l = 1; }
                else if (c == ')')
                { toks.Add(new TokRBrk());  l = 1; }
                else if (c == '[')
                { toks.Add(new TokLEBrk()); l = 1; }
                else if (c == ']')
                { toks.Add(new TokREBrk()); l = 1; }
				else if (c == '{')
				{ toks.Add(new TokLCBrk()); l = 1; }
				else if (c == '}')
				{ toks.Add(new TokRCBrk()); l = 1; }
                else if (c == ',')
                { toks.Add(new TokComma()); l = 1; }
                else if (c == '"')
                    toks.Add(string_(++i, out l));
                else if (Char.IsDigit(c))
                    toks.Add(number(i, out l));
                else if (Char.IsLetter(c) || c == '_')
                    toks.Add(ident(i, out l));
                else { Console.WriteLine("# Error #\r\nOn: '" + c + '\''); return null; }
            }

            toMatch = null;
            return toks.ToArray();
        }

        private String substring(Int32 s, Int32 l)
        {
            Char[] arr = new Char[l];
            for (int i = 0; i < l; i++)
                arr[i] = toMatch[i + s];

            return new String(arr);
        }

        public Char[] operators = "+-*/~?<>!^".ToCharArray();
        private Operator getOp(Char c)
        {
            for (int i = 0; i < operators.Length; i++)
                if (operators[i] == c)
                    return (Operator)(i +1);

            new Error(Errors.Internal);
            return 0;
        }
    }
}