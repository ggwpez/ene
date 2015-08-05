using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Policy;

namespace ene2
{
	public interface AST
	{
		String ToString();
	}

	public class BlockNode : AST
	{
		public AST[] code;

		public BlockNode(AST[] Code)
		{
			code = Code;
		}

		public override string ToString()
		{
			return "BlockNode";
		}
	}

	public class ProgramNode : AST
	{
        public AST[] code;
        public NamespaceIdentNode namespace_;

        public ProgramNode(AST[] Code)
        {
            code = Code;
            namespace_ = new NamespaceIdentNode();
        }

        public ProgramNode(AST[] Code, NamespaceIdentNode Namespace_)
		{
			code = Code;
            namespace_ = Namespace_;
		}

		public override string ToString()
		{
			return "ProgramNode";
		}
	}

	public class WhileNode : AST
	{
        public ExpressionTermNode condition;
		public BlockNode block;

        public WhileNode(ExpressionTermNode Condition, BlockNode Block)
		{
            condition = Condition;
			block = Block;
		}

		public override string ToString()
		{
            return "WhileNode";
		}
	}

	public class IfNode : AST
	{
        public ExpressionTermNode condition;
		public BlockNode trueContext, falseContext;

        public IfNode(ExpressionTermNode Condition, BlockNode TrueContext, BlockNode FalseContext)
		{
            condition = Condition;
            trueContext = TrueContext;
            falseContext = FalseContext;
		}

		public override string ToString()
		{
			return "IfNode";
		}
	}

    public class ExpressionNode : AST
    {
        public AST expression;

        public ExpressionNode(AST Expressoin)
        {
            expression = Expressoin;
        }

        public override string ToString()
        {
            return "ExpressionNode";
        }
    }

    public class ExpressionTermNode : AST
    {
        public AST[] expressions;

        public ExpressionTermNode(AST[] Expressoins)
        {
            expressions = Expressoins;
        }

        public override string ToString()
        {
            return "ExpressionTermNode";
        }
    }

	public class AssignNode : AST
	{
		public ExpressionTermNode value;

        public AssignNode(ExpressionTermNode Value)
		{
			value = Value;
		}

		public override string ToString()
		{
			return "AssignNode";
		}
	}

	public class ListNode : AST
	{
		public AST[] items;

		public ListNode(AST[] Items)
		{
			if (Items == null)
				items = new AST[0];
			else
				items = Items;
		}

		public override string ToString()
		{
			if (this.items.Length == 0)
				return "[]";

			StringBuilder stb = new StringBuilder();

			for (int i = 0; i < items.Length; i++)
				stb.Append(items[i].ToString() + (i != items.Length - 1 ? ", " : ""));

			return stb.ToString();
		}
	}

    public abstract class IType
    {
        public IdentNode name;
        public TypeNode type;
        public Int32 baseOffset;    //like in structs
    }

	public class FunctionNode : IType, AST
	{
		public ListNode args;
		public BlockNode block;

		public FunctionNode(IdentNode Name, TypeNode Type, ListNode Args, BlockNode Block)
		{
			base.name = Name;
            base.type = Type;
			args = Args;
            block = Block;
		}

		public override string ToString()
		{
            return "fN=" + base.name.ToString();
		}
	}

    public class StructNode : TypeNode
    {
        public StructNode(IdentNode Name, IType[] Member)
            : base(Name, -1, -1, Member, null)
        { }
    }

    public class VariableNode : IType, AST
    {
        public Boolean isLocal;
        public Int32 ebpOffset = 0;

        public VariableNode(IdentNode Name, TypeNode Type, Boolean IsLocal = false, Int32 EbpOffset = 0)
        {
            base.name = Name;
            base.type = Type;
            isLocal = IsLocal;
            ebpOffset = EbpOffset;
        }

        public override string ToString()
        {
            return "VariableName=" + base.name;;
        }
    }

	public class FunctionCallNode : AST
	{
		public IdentNode target;
		public ListNode args;

		public FunctionCallNode(IdentNode Target, ListNode Args)
		{
			target = Target;
			args = Args;
		}

		public override string ToString()
		{
			return "call ";
		}
	}

	public class BinaryNode : AST
	{
        public Operator operator_;
		public AST ast1, ast2;

        public BinaryNode(Operator Operator_, AST Ast1, AST Ast2)
		{
			operator_ = Operator_;
			ast1 = Ast1;
			ast2 = Ast2;
		}

		public override string ToString()
		{
            return ast1.ToString() + ' ' + (operator_ == Operator.Mul ? '*' : operator_ == Operator.Div ? '/' : operator_ == Operator.Add ? '+' : '/') + ' ' + ast2.ToString();
		}
	}

	public class UnaryNode : AST
	{
		public Operator operator_;
		public AST ast;

        public UnaryNode(Operator Operator_, AST Ast)
		{
			operator_ = Operator_;
			ast = Ast;
		}

		public override string ToString()
		{
            return operator_ == Operator.Sub ? '-' + ast.ToString() : '[' + ast.ToString() + ']';
		}
	}

	public class NumNode : AST
	{
		public Int32 v;

		public NumNode(Int32 V)
		{ v = V; }

		public override string ToString()
		{
			return v.ToString();
		}
	}

    public class NamespaceIdentNode : AST
    {
        public List<String> v;
        /// <summary>
        /// Skips the last namespace entry.
        /// </summary>
        /// <value>The local.</value>
        public List<String> local { get { return this.v.Take(this.v.Count -1).ToList(); } }
        public const string namespaceDelimiter = "@";

        public NamespaceIdentNode()
        {
            v = new List<String>();
        }

        public NamespaceIdentNode(List<String> V)
        {
            v = V;
        }

        public void Concat(NamespaceIdentNode n2)
        {
            this.v.AddRange(n2.v);
        }

        public Boolean isEmpty
        { get { return v == null || v.Count == 0; } }

        public override string ToString()
        {
            if (this.isEmpty)
                return String.Empty;
            else
                return String.Join(namespaceDelimiter, v);
        }
    }

    //smth like: point.y
    public class MemberAccessNode : AST
    {
        public AST member;

        public MemberAccessNode(AST Member)
        {
            member = Member;
        }

        public override string ToString()
        { return '.' + member.ToString(); }
    }

	public class IdentNode : AST
	{
		public String v;
        public NamespaceIdentNode namespace_;
        public Boolean hasNamespace { get { return this.namespace_ != null && !this.namespace_.isEmpty; } }

        public IdentNode(String V)
        {
            v = V;
            namespace_ = new NamespaceIdentNode();
        }

        public IdentNode(String V, NamespaceIdentNode Namespace_)
		{ 
            v = V;
            namespace_ = Namespace_;
        }

		public override string ToString()
		{
			if (this.hasNamespace)
                return namespace_.ToString() + NamespaceIdentNode.namespaceDelimiter + v;
            else
                return v;
		}
	}

	public class BoolNode : AST
	{
		public Boolean v;

		public BoolNode(Boolean V)
		{ v = V; }

		public override string ToString()
		{
			return v.ToString();
		}
	}

    public class PushNode : AST
    {
        public AST v;

        public PushNode(AST V)
        { v = V; }

        public override string ToString()
        {
            return "push " + v.ToString();
        }
    }

    public class PopNode : AST
    {
        public override string ToString()
        {
            return "add exp, 4";
        }
    }

    public class ASMNode : AST
    {
        public StringNode code;

        public ASMNode(StringNode Code)
        {
            code = Code;
        }
    }

	public class StringNode : AST
	{
		public String v;

		public StringNode(String V)
		{ v = V; }

		public override string ToString()
		{
			return v;
		}
	}

    public class OperatorNode : AST
    {
        public Operator v;

        public OperatorNode(Operator V)
        { v = V; }

        public override string ToString()
        {
            return v.ToString();
        }
    }

    public class ReturnNode : AST
    {
        public AST returnValue;

        public ReturnNode(AST ReturnValue)
        {
            returnValue = ReturnValue;
        }

        public override string ToString()
        { return "return"; }
    }

	public class BreakNode : AST
	{
		public override string ToString()
		{ return "break"; }
	}

	public class GoOnNode : AST
	{
		public override string ToString()
		{ return "goon"; }
	}

    public class TypeNode : IType, AST
    {
        public Int32 size;
        public IType[] member;
        public IType pointsTo;
        public Int32 allocSize;

        public TypeNode(IdentNode Name, Int32 Size, Int32 AllocSize, IType[] Member, IType PointsTo = null)
        {
            size = Size;
            allocSize = AllocSize;
            member = Member;
            pointsTo = PointsTo;
            base.name = Name;
            base.type = this;
        }

        public override string ToString()
        {
            String target = (pointsTo != null ? pointsTo.ToString() : "");

            if (name.v == "ptr")
                return target + '°';
            else
                return name.v + target;
        }
    }

    public class ArgNode : AST
    {
        public IdentNode name;
        public TypeNode type;

        public ArgNode(TypeNode Type, IdentNode Name)
        {
            type = Type;
            name = Name;
        }

        public override string ToString()
        {
            return "ArgNode";
        }
    }
}