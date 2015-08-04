using System;
using System.Collections.Generic;
using System.Linq;

namespace ene2
{
    public class Parser
    {
        private Token[] toMatch;

        public AST parse(Token[] input)
        {
            toMatch = input;

            Int32 l = 0;
            AST programAST = null;
            programAST = parseProgram(0, out l);

            if (toMatch[l] is TokEOS)
                return programAST;
            else
                new Error("Parsing haltet at: '" + toMatch[l] + '\''); 

            return null;
        }

        private AST parseProgram(Int32 s, out Int32 l)
        {
            List<AST> code = new List<AST>();
            Int32 statementL; l = 0; 
            AST statement = null;

            while (!(toMatch[s +l] is TokEOS) && (statement = parseProgramStatement(s +l, out statementL)) != null)
            {
                l += statementL;
                code.Add(statement);
            }

            return new ProgramNode(code.ToArray());
        }

        private AST parseProgramStatement(Int32 s, out Int32 l)
        {
            if (toMatch[s] is TokIdent)
                return parseSingleIdentProgram(s, out l);                   //first one is ident
            if (toMatch[s] is TokNSpace)
                return parseNamespaceBlock(s, out l);
            else if (toMatch[s] is TokASM)
                return parseASM(s, out l);
            else if (toMatch[s] is TokStruct)
                return parseStruct(s, out l);
            else if (toMatch[s] is TokSemi)
            { l = 1; return new ExpressionTermNode(new AST[0]); }
            //else
            //{return null;}
                //new Error(Errors.NotAwaitedToken, toMatch[s]);

            l = 0; return null;
        }

        private AST parseSingleIdentProgram(Int32 s, out Int32 l)           //in a program
        {
            assert(typeof(TokIdent), toMatch[s]);

            if (toMatch[s +1] is TokDDot || toMatch[s +1] is TokCircle)     //global var definition
                return parseVariableDefinition(s, out l);
            else if (toMatch[s +1] is TokIdent)                             //global function definition
                return parseFunctionDefinition(s, out l);
            else
                new Error(Errors.NotAwaitedToken, toMatch[s +1]);

            l = 0; return null;
        }

        private AST parseNamespaceBlock(Int32 s, out Int32 l)
        {
            assert(typeof(TokNSpace), toMatch[s]);

            Int32 nameL;
            AST nName = parseNamespaceName(s +1, out nameL);
            l = nameL +1;

            assert(typeof(TokLCBrk), toMatch[s +l]); l++;

            Int32 progL;
            AST progBlock = parseProgram(s +l, out progL);
            l += progL;

            assert(typeof(TokRCBrk), toMatch[s +l]); l++;

            if (progBlock is ProgramNode)
            {
                ProgramNode progAST = (ProgramNode)progBlock;
                progAST.namespace_ = (NamespaceIdentNode)nName;
                return progAST;
            }
            else
                return new ProgramNode(new AST[] { progBlock }, (NamespaceIdentNode)nName);
        }

        private AST parseNamespaceName(Int32 s, out Int32 l)
        {
            List<String> namespace_ = new List<String>();
            assert(typeof(TokIdent), toMatch[s]); l = 0;

            while (toMatch[s +l +1] is TokDDDot)
            {
                if (toMatch[s +l] is TokIdent)
                    namespace_.Add(((TokIdent)toMatch[s +l]).v);
                else
                    new Error(Errors.AwaitedToken, typeof(TokIdent), toMatch[s +l +1].GetType());

                l += 2;
            }
            namespace_.Add(((TokIdent)toMatch[s +l]).v); l++;

            return new NamespaceIdentNode(namespace_);
        }

        private AST parseBlock(Int32 s, out Int32 l)
        {
            List<AST> code = new List<AST>();
            Int32 statementL; l = 0; 
            AST statement = null;

            if (toMatch[s] is TokSemi)
            {
                l = 1;
                return new BlockNode(new AST[0]);
            }

            assert(typeof(TokLCBrk), toMatch[s]);l++;                       // {
                             
            if (toMatch[s +1] is TokRCBrk)                                  //empty block
            {
                l = 2;
                return new BlockNode(new AST[0]);
            }

            while (!(toMatch[s +l] is TokRCBrk))
            {
                statement = parseBlockStatement(s +l, out statementL);
                if (statementL == 0)
                    throw new Exception();

                l += statementL;
                code.Add(statement);
            } l++;

            if (toMatch[s +l] is TokSemi) l++;                              // ;

            return new BlockNode(code.ToArray());
        }

        private AST parseBlockStatement(Int32 s, out Int32 l)
        {
            if (toMatch[s] is TokIdent)
                return parseSingleIdentBlock(s, out l);
            else if (toMatch[s] is TokWhile)
                return parseWhile(s, out l);
            else if (toMatch[s] is TokIf)
                return parseIf(s, out l);
            else if (toMatch[s] is TokReturn)
                return parseReturn(s, out l);
            else if (toMatch[s] is TokASM)
                return parseASM(s, out l);
            else
                return parseExpressionTerm(s, out l);
        }

        private AST parseSingleIdentBlock(Int32 s, out Int32 l)             //in a block
        {
            assert(typeof(TokIdent), toMatch[s]);
            parseIdent(s, out l);                                           //get length of the ident

            if (toMatch[s +l] is TokDDot || toMatch[s +l] is TokCircle)     //local var definition
                return parseVariableDefinition(s, out l);
            else if (toMatch[s +l] is TokLBrk || toMatch[s +l] is TokDDDot) //function call
                return parseFunctionCall(s, out l);
            else                                                            //just two pushes
                return parseExpressionTerm(s, out l);
        }

        /* struct stack
         * [packed]
         * {
         *      c32°°: start;
         *      c32°:  offset;
         * };
         */
        private AST parseStruct(Int32 s, out Int32 l)
        {
            assert(typeof(TokStruct), toMatch[s]);
            assert(typeof(TokIdent),  toMatch[s +1]);

            Int32 blockL; l = 2;
            BlockNode blockAST = (BlockNode)parseStructBlock(s +l, out blockL);
            IdentNode sName = (IdentNode)(TokIdent)toMatch[s +1];
            List<String> namespace_ = new List<String>();
            namespace_.Add(sName.v);
            sName.namespace_ = new NamespaceIdentNode(namespace_);

            IType[] member = new IType[blockAST.code.Length];                       //c# why cant i just cast to (IType[] ?!
            for (int i = 0; i < member.Length; i++)
                member[i] = (IType)blockAST.code[i];

            l += blockL;
            if (toMatch[s +l +1] is TokSemi)
                l++;

            return new StructNode(sName, member);
        }

        private AST parseStructBlock(Int32 s, out Int32 l)
        {
            List<AST> member = new List<AST>();
            Int32 statementL; l = 0; 
            AST statement = null;

            if (toMatch[s] is TokSemi)
            {
                l = 1;
                return new BlockNode(new AST[0]);
            }

            assert(typeof(TokLCBrk), toMatch[s]); l++;

            if (toMatch[s +1] is TokRCBrk)
            {
                l = 2;
                return new BlockNode(new AST[0]);
            }

            while (!(toMatch[s +l] is TokRCBrk))
            {
                statement = parseStructStatement(s +l, out statementL);
                if (statementL == 0)
                    throw new Exception();

                l += statementL;
                member.Add(statement);
            } l++;

            if (toMatch[s +l] is TokSemi) l++;

            return new BlockNode(member.ToArray());
        }

        private AST parseStructStatement(Int32 s, out Int32 l)
        {
            if (toMatch[s] is TokIdent)
                return parseSingleIdentStruct(s, out l);
            else
            {
                l = 0;
                new Error(Errors.NotAwaitedToken, toMatch[s]);
                return null;
            }
        }

        private AST parseSingleIdentStruct(Int32 s, out Int32 l)
        {
            assert(typeof(TokIdent), toMatch[s]);
            parseIdent(s, out l);

            if (toMatch[s +l] is TokDDot || toMatch[s +l] is TokCircle)     //member variable definition
                return parseVariableDefinition(s, out l);
            else if (toMatch[s +1] is TokIdent)                             //member function definition
                return parseFunctionDefinition(s, out l);
            else
            {
                l = 0;
                new Error(Errors.NotAwaitedToken, toMatch[s]);
                return null;
            }              
        }        

        private AST parseSingleIdentExpression(Int32 s, out Int32 l)
        {
            assert(typeof(TokIdent), toMatch[s]);

            if (toMatch[s +1] is TokLBrk)
                return parseFunctionCall(s, out l);
            else
                return parseExpression(s, out l);
        }

        private AST parseIdent(Int32 s, out Int32 l)    //with namespace-access
        {
            List<String> namespace_ = new List<String>();
            assert(typeof(TokIdent), toMatch[s]); l = 1;

            if (toMatch[s +1] is TokDDDot)  //it has a namespace
            {   l = 0;
                while (toMatch[s +l +1] is TokDDDot)
                {
                    if (toMatch[s +l] is TokIdent)
                        namespace_.Add(((TokIdent)toMatch[s +l]).v);
                    else
                        new Error(Errors.AwaitedToken, typeof(TokIdent), toMatch[s +l +1].GetType());

                    l += 2;
                }
                return new IdentNode(((TokIdent)toMatch[s +l++]).v, new NamespaceIdentNode(namespace_));
            }
            else
                return (IdentNode)(TokIdent)toMatch[s];   
        }

        private AST parseExpressionTerm(Int32 s, out Int32 l)
        {
            List<AST> term = new List<AST>();
            Int32 expressionL = 0; l = 0;
            AST expressionAST = null;

            while (true)
            {
                //Token tmp = toMatch[s +l];

                if (toMatch[s +l] is TokAssign)
                    expressionAST = parseAssign(s +l, out expressionL);
                else if (toMatch[s +l] is TokNum)
                    expressionAST = parseExpression(s +l, out expressionL);
                else if (toMatch[s +l] is TokIdent)
                    expressionAST = parseSingleIdentExpression(s +l, out expressionL);
                else if (toMatch[s +l] is TokOp)
                    expressionAST = parseExpression(s +l, out expressionL);
                else if (toMatch[s +l] is TokASM)
                    expressionAST = parseASM(s +l, out expressionL);
                else if (toMatch[s +l] is TokString)
                    expressionAST = parseString(s +l, out expressionL);
                else if (toMatch[s +l] is TokSemi)
                { l += 1; break; }
                else if (toMatch[s +l] is TokRBrk || toMatch[s +l] is TokComma)
                    break;
                else
                    new Error(Errors.NotAwaitedToken, toMatch[s +l]);

                l += expressionL;
                term.Add(expressionAST);

                if (toMatch[s +l] is TokSemi) break;
                //if (expressionAST is AssignNode && toMatch[s +l] is TokSemi) 
                //    l--;
                if (toMatch[s +l] is TokSemi) break;
            }

            return new ExpressionTermNode(term.ToArray());
        }

        private AST parseExpression(Int32 s, out Int32 l)
        {
            if (toMatch[s] is TokIdent)
            {
                AST typeAST = parseType(s, out l);

                if (l == 1)
                    return (IdentNode)parseIdent(s, out l);     //dont convert to PushNode, maybe is a cast
                else
                    return typeAST;
            }
            else if (toMatch[s] is TokNum)
            {
                l = 1;
                return new PushNode((NumNode)(TokNum)toMatch[s]);
            }
            else if (toMatch[s] is TokOp)
            {
                l = 1;
                return (OperatorNode)(TokOp)toMatch[s];
            }
            else
                new Error(Errors.NotAwaitedToken, toMatch[s]);

            l = 0; return null;
        }

        private AST parseFunctionDefinition(Int32 s, out Int32 l)   //int get_l(ptr str) { … };
        {
            AST typeAST = parseType(s, out l);

            assert(typeof(TokIdent), toMatch[s +l]);
            IdentNode fName = (IdentNode)(TokIdent)toMatch[s +l++];

            Int32 argListL;
            AST argList = parseArgList(s +l, out argListL);
            l += argListL;

            Int32 blockL;
            AST block = parseBlock(s +l, out blockL);
            l += blockL;

            if (toMatch[s +l] is TokSemi)                                   //can match ;
                l++;

            return new FunctionNode(fName, (TypeNode)typeAST, (ListNode)argList, (BlockNode)block);
        }

        private AST parseFunctionCall(Int32 s, out Int32 l)
        {
            assert(typeof(TokIdent), toMatch[s]);
            IdentNode target = (IdentNode)parseIdent(s, out l);

            Int32 argListL;
            AST argList = parseList(s +l, out argListL);
            l += argListL +1;

            return new FunctionCallNode(target, (ListNode)argList);
        }

        private AST parseIf(Int32 s, out Int32 l)
        {
            assert(typeof(TokIf), toMatch[s]);
            assert(typeof(TokLBrk),  toMatch[s +1]);

            Int32 conditionL;
            AST condition = parseExpressionTerm(s +2, out conditionL);
            l = conditionL +2;

            assert(typeof(TokRBrk),  toMatch[s +l++]);

            Int32 blockL;
            AST trueBlock  = parseBlock(s +l, out blockL);
            AST falseBlock = null;
            l += blockL; blockL = 0;

            if (toMatch[s +l] is TokElse)
                falseBlock = parseBlock(s +(++l), out blockL);
            l += blockL;

            return new IfNode((ExpressionTermNode)condition, (BlockNode)trueBlock, falseBlock != null ? (BlockNode)falseBlock : null);
        }

        private AST parseWhile(Int32 s, out Int32 l)
        {
            assert(typeof(TokWhile), toMatch[s]);
            assert(typeof(TokLBrk),  toMatch[s +1]);

            Int32 conditionL;
            AST condition = parseExpressionTerm(s +2, out conditionL);
            l = conditionL +2;

            assert(typeof(TokRBrk),  toMatch[s +l++]);

            Int32 blockL;
            AST block = parseBlock(s +l, out blockL);
            l += blockL;

            return new WhileNode((ExpressionTermNode)condition, (BlockNode)block);
        }

        private AST parseVariableDefinition(Int32 s, out Int32 l)  //ptr tmp;
        {
            assert(typeof(TokIdent), toMatch[s]);

            Int32 typeL;
            AST typeAST = parseType(s, out typeL);
            l = typeL;
            assert(typeof(TokDDot), toMatch[s +l]); l++;

            IdentNode varName = (IdentNode)(TokIdent)toMatch[s +l];
            l++;

            assert(typeof(TokSemi), toMatch[s +l]);
            l++;

            return new VariableNode(varName, (TypeNode)typeAST);
        }

        private AST parseType(Int32 s, out Int32 l)
        {
            assert(typeof(TokIdent), toMatch[s]);
            l = 1;

            List<TypeNode> types = new List<TypeNode>();
            types.Add(new TypeNode((IdentNode)(TokIdent)toMatch[s], 4, 4, null, null));

            while (toMatch[s +l] is TokCircle)
            {
                l++;
                types.Add(new TypeNode(new IdentNode("ptr"), 4, 4, null, types.Last()));
            }

            return types.Last();
        }

        private AST parseList(Int32 s, out Int32 l)                         // (1, 5, aye, 8 +ene)
        {
            List<AST> args = new List<AST>();
            Int32 argL = 0; l = 0;

            assert(typeof(TokLBrk), toMatch[s +(l++)]);                     // (

            if (toMatch[s +1] is TokRBrk)                                   // actualisi is empty ()
            {   
                l++;
                return new ListNode(new AST[0]);
            }

            AST arg0 = parseExpressionTerm(s +1, out argL); 
            args.Add(arg0);

            while ((l += argL) != 0 && toMatch[s +l] is TokComma)
            {
                args.Add(parseExpressionTerm(s + ++l, out argL));
            }

            assert(typeof(TokRBrk), toMatch[s +l++]);                         //  )

            return new ListNode(args.ToArray());
        }

        private AST parseArgList(Int32 s, out Int32 l)                 // (ptr address, c32 length, c32 offset)
        {
            List<AST> args = new List<AST>();
            Int32 argL = 0; l = 0;

            assert(typeof(TokLBrk), toMatch[s +l++]);                     // (

            if (toMatch[s +1] is TokRBrk)                                   // actualisi is empty ()
            {   
                l++;
                return new ListNode(new AST[0]);
            }

            AST arg0 = parseArg(s +l, out argL); 
            args.Add(arg0);

            while ((l += argL) != 0 && toMatch[s +l] is TokComma)
            {
                args.Add(parseArg(s + ++l, out argL));
            }

            assert(typeof(TokRBrk), toMatch[s +l++]);                         //  )

            return new ListNode(args.ToArray());
        }

        private AST parseAssign(Int32 s, out Int32 l)
        {
            assert(typeof(TokAssign), toMatch[s]);                         //  )

            Int32 valueL;
            AST valueAST = parseExpressionTerm(s+1, out valueL);
            l = valueL +1;

            return new AssignNode((ExpressionTermNode)valueAST);
        }

        private AST parseArg(Int32 s, out Int32 l)                      // c32°° address
        {
            assert(typeof(TokIdent), toMatch[s]);
            AST typeAST = parseType(s, out l);
            assert(typeof(TokIdent), toMatch[s +l]);
            l += 1;

            IdentNode argName = (IdentNode)(TokIdent)toMatch[s +l -1];

            return new ArgNode((TypeNode)typeAST, argName);
        }

        private AST parseASM(Int32 s, out Int32 l)
        {
            assert(typeof(TokASM),    toMatch[s]);
            assert(typeof(TokLBrk),   toMatch[s +1]);
            assert(typeof(TokString), toMatch[s +2]);
            assert(typeof(TokRBrk),   toMatch[s +3]);

            l = 4;
            return new ASMNode(new StringNode(((TokString)toMatch[s +2]).v));
        }

        private AST parseString(Int32 s, out Int32 l)
        {
            assert(typeof(TokString), toMatch[s]);

            l = 1;
            return new StringNode(((TokString)toMatch[s]).v);
        }

        private AST parseReturn(Int32 s, out Int32 l)
        {
            assert(typeof(TokReturn), toMatch[s]);
            l = 1;

            Int32 retVL;
            AST retValue = parseExpressionTerm(s +l, out retVL);
            l += retVL;

            return new ReturnNode();
        }

        private Boolean assert(System.Type awaited, Token got)
        { return assert(awaited, got.GetType()); }
        private Boolean assert(System.Type awaited, System.Type got)
        {
            if (awaited != got)
                new Error(Errors.AwaitedToken, awaited, got);
            else
                return false;

            return true;
        }
    }
}