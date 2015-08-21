using System;

namespace ene2
{
    public interface Token
    { String ToString(); }

    public class TokIdent   : Token { public String v; public TokIdent(String V) { v = V; } public static implicit operator IdentNode(TokIdent ident) { return new IdentNode(ident.v); } public override String ToString() { return v; } }
    public class TokString  : Token { public String v; public TokString(String V) { v = V; }  public override String ToString() { return v; } }
    public class TokNum     : Token { public Int32  v; public TokNum(Int32 V) { v = V; } public static implicit operator NumNode(TokNum num) { return new NumNode(num.v); } public override String ToString() { return v.ToString(); } }
    public class TokOp      : Token { public Operator operator_; public TokOp(Operator Operator_) { operator_ = Operator_; } public static implicit operator OperatorNode(TokOp op) { return new OperatorNode(op.operator_); } public override String ToString() { return operator_.ToString(); } }
    public class TokComma   : Token { public override String ToString() { return ","; } }
    public class TokCircle  : Token { public override String ToString() { return "°"; } }
    public class TokAssign  : Token { public override String ToString() { return "="; } }
    public class TokLBrk    : Token { public override String ToString() { return "("; } }
    public class TokRBrk    : Token { public override String ToString() { return ")"; } }
    public class TokLEBrk   : Token { public override String ToString() { return "["; } }
    public class TokREBrk   : Token { public override String ToString() { return "]"; } }
    public class TokLCBrk   : Token { public override String ToString() { return "{"; } }
    public class TokRCBrk   : Token { public override String ToString() { return "}"; } }
    public class TokDot     : Token { public override String ToString() { return "."; } }
    public class TokDDot    : Token { public override String ToString() { return ":"; } }
    public class TokDDDot   : Token { public override String ToString() { return "::"; } }
    public class TokSemi    : Token { public override String ToString() { return ";"; } }
    public class TokWhile   : Token { public override String ToString() { return "t_while"; } }
    public class TokFor     : Token { public override String ToString() { return "t_for"; } }
    public class TokIf      : Token { public override String ToString() { return "t_if"; } }
    public class TokASM     : Token { public override String ToString() { return "t_asm"; } }
    public class TokNSpace  : Token { public override String ToString() { return "t_namespace"; } }
    public class TokElse    : Token { public override String ToString() { return "t_else"; } }
    public class TokBreak   : Token { public override String ToString() { return "t_break"; } }
    public class TokReturn  : Token { public override String ToString() { return "t_return"; } }
    public class TokStruct  : Token { public override String ToString() { return "t_struct"; } }
    public class TokEOS     : Token { public override String ToString() { return "t_EOS"; } }

    public enum Operator : int
    {
        Add = 1,    // +
        Sub = 2,    // -
        Mul = 3,    // *
        Div = 4,    // /
        Drf = 5,    // pointer deref like * in C
        Equ = 6,    // ?
        Sml = 7,    // <
        Grt = 8,    // >
        Neq = 9,    // !
        Pop = 10,   // ^
        Cpy = 11    // $
    }
}
