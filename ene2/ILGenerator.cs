using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;

namespace ene2
{
    public class ILGenerator
    {
        NamespaceManager scope;
        StringBuilder stbCode, stbBSS, stbData;
        TypeNode lastType = null;

        String inbuild = @"
boolNot:        ;0 -> 0xffffffff.   1, 830, 555 -> 0. implicit normalization.
test eax, eax
jnz .notzero
mov eax, dword 0xffffffff
ret
.notzero:
mov eax, dword 0
ret

boolNormalize:  ;0 -> 0.            1, 830, 555 -> 0xffffffff.
test eax, eax
jnz .notzero
mov eax, dword 0
ret
.notzero:
mov eax, dword 0xffffffff
ret
";

        TypeNode cArr, c32, c16, c8, ptr,   vArr,   v32,   v16,   v8, void_;

		public ILGenerator()
		{
			scope = new NamespaceManager("global");

            stbCode = new StringBuilder(inbuild);
            stbBSS  = new StringBuilder();
            stbData = new StringBuilder();

            //cArr  = new TypeNode(new IdentNode("cArr"),    4, -1, new IType[0]);

            c32   = new TypeNode(new IdentNode("c32"),   4, 4, new IType[0]);
            c16   = new TypeNode(new IdentNode("c16"),   2, 4, new IType[0]);
            c8    = new TypeNode(new IdentNode("c8"),    1, 4, new IType[0]);

            //ptrArr = new TypeNode(new IdentNode("ptrArr"), 4, -1, new IType[0], cArr);
            ptr = new TypeNode(new IdentNode("ptr"),    4, 4, new IType[0], void_);

            v32 = new TypeNode(new IdentNode("v32"), 4, 4, new IType[0], c32);    //== c32°
            v16 = new TypeNode(new IdentNode("v16"), 4, 4, new IType[0], c16);
            v8  = new TypeNode(new IdentNode("v8"),  4, 4, new IType[0], c8 );
            void_ = new TypeNode(new IdentNode("void"),  0, 0, new IType[0], null);
		}

        public String generate(AST ast)
        {
            registerGlobalFunctions((ProgramNode)ast);

            generateProgram((ProgramNode)ast);

            return String.Format("; ene V2\nsection .bss\n{0}\nsection .text\n{1}", stbBSS, stbCode);
        }

        private void generateProgram(ProgramNode ast)
        {
            if (!ast.namespace_.isEmpty)
                scope.enterOrCreateNamespace(ast.namespace_);

            if (!scope.isRegistered(new IdentNode("ptr")))
            {
                scope.register(ptr   );
                /*scope.register(ptrArr);*/

                scope.register(v32 );
                scope.register(v16 );
                scope.register(v8  );

                //scope.register(cArr  );
                scope.register(c32   );
                scope.register(c16   );
                scope.register(c8    );
                scope.register(void_ );
            }

            foreach (AST statement in ast.code)
                generateProgramStatement(statement);

            if (!ast.namespace_.isEmpty)
                scope.leaveNamespace(ast.namespace_.v.Count);
        }

        private void registerGlobalFunctions(ProgramNode ast)
        { registerGlobalFunctions(ast, new NamespaceIdentNode()); }
        private void registerGlobalFunctions(ProgramNode ast, NamespaceIdentNode addNamespaces)
        {
            NamespaceIdentNode nNode = new NamespaceIdentNode();
            nNode.Concat(addNamespaces);
            nNode.Concat(ast.namespace_);

            foreach (AST statement in ast.code)
            {
                if (statement is FunctionNode)
                {
                    if (!nNode.isEmpty)
                    {
                        FunctionNode f = (FunctionNode)statement;
                        scope.ensureCreateNamespace(nNode);
                        f.name.namespace_ = nNode;
                        scope.register(f);
                    }
                    else
                        scope.register((FunctionNode)statement);
                }
                else if (statement is ProgramNode)
                {
                    registerGlobalFunctions((ProgramNode)statement, nNode);
                }
            }
        }

        private void generateProgramStatement(AST ast)
        {
            if (ast is FunctionNode)
                generateFunction((FunctionNode)ast);
            else if (ast is ASMNode)
                generateASM((ASMNode)ast);
            else if (ast is VariableNode)
                generateVariableGlobal((VariableNode)ast);
            else if (ast is ProgramNode)
                generateProgram((ProgramNode)ast);
            else if (ast is StructNode)
                generateStruct((StructNode)ast);
        }

        private void generateBlock(BlockNode ast)
        {
            scope.enterScope();
            foreach (AST statement in ast.code)
                generateBlockStatement(statement);

            scope.leaveScope();
        }

        private void generateBlockStatement(AST ast)
        {
            if (ast is FunctionCallNode)
                generateFunctionCall((FunctionCallNode)ast);
            else if (ast is VariableNode)
                generateVariableLocal((VariableNode)ast);
            else if (ast is WhileNode)
                generateWhile((WhileNode)ast);
            else if (ast is IfNode)
                generateIf((IfNode)ast);
            else if (ast is ReturnNode)
                generateReturn((ReturnNode)ast);
            else if (ast is ASMNode)
                generateASM((ASMNode)ast);     
            else
                generateExpressionTerm((ExpressionTermNode)ast);
        }

        private void generateStruct(StructNode ast)
        {
            scope.enterOrCreateNamespace(ast.name.namespace_);

            if (scope.isRegistered(ast.name))
                new Error(Errors.LabelInUse, ast.name);

            foreach (AST statement in ast.member)
                generateStructStatement((AST)statement);
        }

        public void generateStructStatement(AST ast)
        {
            if (ast is FunctionNode)
                generateMemberFunction((FunctionNode)ast);
            else if (ast is VariableNode)
                generateVariableGlobal((VariableNode)ast);
            else
                new Error(Errors.Internal);
        }

        public void generateMemberVariable(VariableNode ast)
        {
            ast.name.namespace_ = scope.getNamespaceRelative();
            generateVariableGlobal(ast);
        }

        public void generateMemberFunction(FunctionNode ast)
        {
            ast.name.namespace_ = scope.getNamespaceRelative();
            generateFunction(ast);
        }

        private Int32 ifC = 0;
        private void generateIf(IfNode ast)
        {
            String name = "__if_" + ifC++.ToString();

            emitLn(name + ':');
            generateExpressionTerm(ast.condition);
            generatePop("eax");                         //value of condition

            emitLn("test eax, 0xffffffff");             //test = bitwise and
            emitLn("jz " + name + ".else");             //condition is false
            generateBlock(ast.trueContext);
            emitLn("jmp " + name + ".end");             //skipping else-block

            emitLn(name + ".else:");
            if (ast.falseContext != null)
                generateBlock(ast.falseContext);

            emitLn(name + ".end:");
        }

        private Int32 whileC = 0;
        private String whileContext = "while";
        private void generateWhile(WhileNode ast)
        {
            String name = whileContext + ".__while_" + whileC++.ToString();
            whileContext += name;
            emitLn(name + ".start:");

            generateExpressionTerm(ast.condition);

            generatePop("eax");                         //return value of the condition
            emitLn("test eax, 0xffffffff");             //test = bitwise and
            emitLn("jnz " + name + ".code");
            emitLn("jmp " + name + ".end");

            emitLn(name + ".code:");

            generateBlock(ast.block);

            emitLn("jmp " + name + ".start");
            emitLn(name + ".end:");
            whileContext = whileContext.Substring(whileContext.Length - name.Length);
        }

        private void generateVariableGlobal(VariableNode ast)
        {
            if (scope.isRegistered(ast.name))
                new Error(Errors.LabelInUse, ast.name);

            scope.register(ast);

            TypeNode pointedToType = fixType(ast.type);

            if (pointedToType.allocSize == 0)
                new Warning("'" + ast.name + "' has the size 0, sure you wanted that?!");

            ast.name.namespace_ = scope.getNamespaceRelative();
            emitBSSLn(ast.name.ToString() + ": resb " + pointedToType.allocSize);
        }

        private void generateVariableLocal(VariableNode ast)
        {
            if (scope.isRegistered(ast.name))
                new Error(Errors.LabelInUse, ast.name);

            ast.isLocal = true;                               //set to local
            scope.register(ast);

            TypeNode pointedToType = fixType(ast.type);

            scope.ebpOffset -= pointedToType.allocSize;
            ast.ebpOffset = scope.ebpOffset;

            if (pointedToType.allocSize == 0)
                new Warning("'" + ast.name + "' has the size 0, sure you wanted that?!");
        }

        private TypeNode getPropagateTypeDown(TypeNode ast)
        {
            TypeNode tmp = ast;

            while (tmp.pointsTo != null)
                tmp = (TypeNode)tmp.pointsTo;

            return tmp;
        }

        /// <summary>
        /// Fixs the type, so the .pointsTo type matched the one in the scope
        /// </summary>
        /// <returns>The type.</returns>
        /// <param name="variable">The .pointsTo type</param>
        private TypeNode fixType(TypeNode type)
        {
            TypeNode pointedToType;
            if (type.pointsTo != null)                   //its a pointer
            {
                pointedToType = getPropagateTypeDown(type);
                TypeNode newType = (TypeNode)scope.getObj(pointedToType.name);
                setPropagateTypeDown(type, newType);
            }
            else
                pointedToType = (TypeNode)scope.getObj(type.name);

            return pointedToType;
        }

        private void setPropagateTypeDown(TypeNode ast, TypeNode newType)
        {
            TypeNode tmp = ast;

            while (((TypeNode)tmp.pointsTo).pointsTo != null)
                tmp = (TypeNode)tmp.pointsTo;

            tmp.pointsTo = newType;
        }

        private void generateExpressionTerm(ExpressionTermNode ast)
        {
            foreach (AST expression in ast.expressions)
                generateExpression(expression);
        }

        private void generateExpression(AST ast)
        {
            if (ast is PushNode)
                generatePush((PushNode)ast);
            else if (ast is PopNode)
                generatePop((PopNode)ast);
            else if (ast is IdentNode)
                generateIdent((IdentNode)ast);
            else if (ast is OperatorNode)
                generateOperator((OperatorNode)ast);
            else if (ast is AssignNode)
                generateAssign((AssignNode)ast);
            else if (ast is FunctionCallNode)
                generateFunctionCall((FunctionCallNode)ast);
            else if (ast is ASMNode)
                generateASM((ASMNode)ast);
            else if (ast is TypeNode)
                generateType((TypeNode)ast);
            else
                new Error(Errors.Internal, ast);
        }

        private void generateFunction(FunctionNode ast)
        {
            Int32 localSize = 0, argSize = 0;
            List<VariableNode> localVars = new List<VariableNode>();

            foreach (AST line in ast.block.code)
                if (line is VariableNode)
                    localSize += ((TypeNode)scope.getObj(((VariableNode)line).type.name)).allocSize;

            foreach (AST line in ast.args.items)
                argSize += ((TypeNode)scope.getObj(((ArgNode)line).type.name)).allocSize;


            Int32 ebpOffset = 4;
            foreach (AST line in ast.args.items)
            {
                ArgNode arg = (ArgNode)line;

                Int32 tmpOff = ((TypeNode)scope.getObj(arg.type.name)).allocSize;
                ebpOffset += tmpOff;

                scope.register(new VariableNode(arg.name, arg.type, true, tmpOff == 0 ? -1 : ebpOffset));
            }


            emitLn(ast.name.ToString() + ':');
            emitLn("push ebp     ;stack frame begin");
            emitLn("mov ebp, esp");
            emitLn("sub esp, " + (localSize+argSize).ToString());

            generateBlock(ast.block);

            emitLn("mov esp, ebp ;stack frame end");
            emitLn("pop ebp");
            emitLn("ret " + (argSize).ToString());
        }

        private void generateFunctionCall(FunctionCallNode ast)
        {
            FunctionNode target = scope.getObj(ast.target) as FunctionNode;
            TypeNode returnType = scope.getObj(target.type.name) as TypeNode;

            if (target == null)
                new Error(Errors.LabelUnknown, ast.target);
            if (returnType == null)
                new Error(Errors.TypeUnknown, target.type);
            if (target.args.items.Length != ast.args.items.Length)
                new Warning(Warnings.CallArgumentsInvalid, ast.target);

            for (int i = ast.args.items.Length - 1; i >= 0; i--)    //push them backwards
                generateExpressionTerm((ExpressionTermNode)ast.args.items[i]);

            emitLn("call " + target.name);
            if (returnType.size != 0)
                emitLn("push eax");                     //save the return value
        }

        private void generateAssign(AssignNode ast)
        {
            if (lastType != null)
            {
                if (lastType.pointsTo == null)
                    new Warning(Warnings.ReferencingUnknownAdress, lastType);
                else if (lastType.pointsTo.type.allocSize == 0)
                    new Warning(Warnings.ReferencingNonAtomarType, lastType.pointsTo);
                else
                    lastType = (TypeNode)lastType.pointsTo;
            }

            generatePop("edi");
            generateExpressionTerm(ast.value);          //read the value, the var should be assigned to
            generatePush("edi");

            generateVariableWrite();                    //write it at the var's address
        }

        private void generateOperator(OperatorNode ast)
        {
            switch (ast.v) 
            {
                case Operator.Add:
                    generateAdd();
                    break;
                case Operator.Sub:
                    generateSub();
                    break;
                case Operator.Mul:
                    generateMul();
                    break;
                case Operator.Div:
                    generateDiv();
                    break;
                case Operator.Drf:
                    generateDrf();
                    break;
                case Operator.Equ:
                    generateEqu();
                    break;
                case Operator.Sml:
                    generateSml();
                    break;
                case Operator.Grt:
                    generateGrt();
                    break;
                case Operator.Neq:
                    generateNeq();
                    break;
                case Operator.Pop:
                    generatePopEBP();
                    break;
                default:
                    break;
            }
        }

        private void generateIdent(IdentNode ast)
        {
            scope.removeGlobalNamespace(ref ast);

            if (!scope.isRegistered(ast))
                new Error(Errors.LabelUnknown, ast);

            IType variableBase = scope.getObj(ast); 
            if (variableBase is VariableNode)               //its a variable
            {
                lastType = variableBase.type;
                VariableNode variable = (VariableNode)variableBase;

                if (variable.isLocal)
                {
                    //if (variable.ebpOffset == -1)
                      //  new Error(Errors.DereferencingGenericPtr, variable);

                    String adder = (variable.ebpOffset < 0 ? variable.ebpOffset.ToString() : '+' + variable.ebpOffset.ToString());
                    emitLn("lea ecx, [ebp" + adder + "]\t\t; pushing '" + variable.name + '\'');    //it resides on the stack
                    generatePush("ecx");
                }
                else
                    generatePush(new PushNode(ast));
            }
            else if (variableBase is TypeNode)                //its a type
                lastType = (TypeNode)variableBase;
        }

        private void generateReturn(ReturnNode ast)
        {
            emitLn("ret");
        }

        private void generateASM(ASMNode ast)
        {
            String[] lines = ast.code.v.Split('|');

            foreach (String line in lines)
                emitLn(line);
        }

#region The small generation methods
        private void generateAdd()
        {
            generatePop("eax");
            generatePop("ecx");
            emitLn("add eax, ecx");
            generatePush("eax");
        }

        private void generateSub()
        {
            generatePop("ecx");
            generatePop("eax");
            emitLn("sub eax, ecx");
            generatePush("eax");
        }
        
        private void generateMul()
        {
            generatePop("eax");
            generatePop("ecx");
            emitLn("mul ecx");
            generatePush("eax");
        }
        
        private void generateDiv()
        {
            generatePop("eax");
            generatePop("ecx");
            emitLn("xor edx, edx");
            emitLn("div ecx");
            generatePush("eax");
        }
        
        private void generateDrf()
        {
            generateVariableRead();
        }
        
        private void generateEqu()
        {
            generateNeq();
            generateBoolNot();
        }
        
        private void generateSml()
        {
            String name = newLabel();
            String ok = name + "_ok";
            String fail = name + "_fail";
            String end = name + "_end";
            generatePop("eax");
            generatePop("ecx");

            emitLn("cmp ecx, eax");
            emitLn("jl " + ok);
            emitLn("jmp " + fail);

            emitLn(ok + ':');
            emitLn("mov eax, dword 0xffffffff");
            emitLn("jmp " + end);

            emitLn(fail + ':');
            emitLn("mov eax, dword 0");
            emitLn("jmp " + end);

            emitLn(end + ':');
            generatePush("eax");
        }
        
        private void generateGrt()
        {
            String name = newLabel();
            String ok = name + "_ok";
            String fail = name + "_fail";
            String end = name + "_end";
            generatePop("eax");
            generatePop("ecx");

            emitLn("cmp ecx, eax");
            emitLn("jng " + fail);
            emitLn("jmp " + ok);

            emitLn(ok + ':');
            emitLn("mov eax, dword 0xffffffff");
            emitLn("jmp " + end);

            emitLn(fail + ':');
            emitLn("mov eax, dword 0");
            emitLn("jmp " + end);

            emitLn(end + ':');
            generatePush("eax");
        }
        
        private void generateNeq()
        {
            generatePop("eax");
            generatePop("ecx");
            emitLn("xor eax, ecx");
            emitLn("call boolNormalize");
            generatePush("eax");
        }

        private void generateBoolNot()
        {
            generatePop("eax");
            emitLn("call boolNot");
            generatePush("eax");
        }

        private void generatePush(PushNode ast)
        {
            if (ast.v is NumNode)
                lastType = c32;
            else if (ast.v is StringNode)
                lastType = vArr;
            else if (ast.v is IdentNode) { }                 //the type has already been dealt with
            else
                lastType = null;

            generatePush(ast.v.ToString());
        }

        private void generatePush(String v)
        {
            emitLn("push " + v);
            scope.push(4);
        }

        private void generateVariableWrite()
        {
            generatePop("ecx");
            generatePop("eax");

            if (lastType == null)
                new Error(Errors.DereferencingNull, "°");
            else
            switch (lastType.size)
            {
                case 1:
                    emitLn("mov byte [ecx], al");
                    break;
                case 2:
                    emitLn("mov word [ecx], ax");
                    break;
                case 4:
                    emitLn("mov dword [ecx], eax");
                    break;
                default:
                    new Error(Errors.UnsupportedOperation, lastType);
                    break;
            }
        }

        private void generateVariableRead()
        {
            TypeNode newType = lastType != null ? (TypeNode)lastType.pointsTo : null;
            String convStr = "";

            if (lastType == null || newType == null || lastType.allocSize == 0)
                new Warning(Warnings.DereferencingNonPtrType, lastType);
            else
            {
                convStr = "\t; " + lastType.ToString() + " -> " + (newType != null ? newType.ToString() : "void");
                lastType = newType;
            }

            generatePop("eax");
            if (lastType == null)
                new Error(Errors.DereferencingNull, "°");
            else
            switch (lastType.size)
            {
                case 1:
                    emitLn("mov al, byte [eax]" + convStr);
                    emitLn("and eax, 0xff");                //'movzx al, byte [eax]' is not working, so the higher part of eax must be cleared manually
                    break;
                case 2:
                    emitLn("mov ax, word [eax]" + convStr);
                    emitLn("and eax, 0xffff");              // "
                    break;
                case 4:
                    emitLn("mov eax, dword [eax]" + convStr);
                    break;
                default:
                    new Error(Errors.UnsupportedOperation, lastType);
                    break;
            }
            generatePush("eax");
        }

        private void generateType(TypeNode ast)
        {
            TypeNode pointedToType = fixType(ast);

            lastType = ast;
        }

        private void generatePop(PopNode ast)
        {
            generatePop("eax");
        }

        private void generatePopEBP()
        {
            emitLn("add esp, 4");
            scope.pop(4);
        }

        private void generatePop(String v)
        { 
            if (v == "")
                emitLn("add esp, 4");
            else
                emitLn("pop " + v);

            scope.pop(4);
        }
#endregion

        Int32 labelC = 0;
        private String actualLabel()
        {
            return "__lbl_" + (labelC).ToString();
        }

        private String newLabel()
        {
            return "__lbl_" + (labelC++).ToString();
        }

        private void emitLn(String code)
        { emitLn(code, stbCode); }
        private void emitBSSLn(String bss)
        { emitLn(bss, stbBSS); }
        private void emitDataLn(String data)
        { emitLn(data, stbData); }

        private void emitLn(String code, StringBuilder stb)
        {
            stb.AppendLine(code);
        }
    }
}