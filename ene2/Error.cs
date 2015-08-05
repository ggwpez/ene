using System;
//using Console = Repo.Reporter;
using System.Text;

namespace ene2
{
    public class Error
    {
        public Error(String v)
        { new Error(Errors.Unknown, v); }
        public Error(Errors num, params Object[] v)
        {
            StringBuilder stb = new StringBuilder("Error: ");

            switch (num) 
            {
                case Errors.NotAwaitedToken:
                    stb.Append("Was not awaiting token '" + v[0].ToString() + '\'');
                    break;
                case Errors.AwaitedToken:
                    stb.Append("Awaited token '" + v[0].ToString() + "' but got '" + v[1].ToString() + '\'');
                    break;
                case Errors.ForbiddenInStruct:
                    stb.Append("Expressions of the type '" + v[0].ToString() + "' are not allowed in structs, aka '" + v[1].ToString() + '\'');
                    break;
                case Errors.LabelInUse:
                    stb.Append("Label '" + v[0].ToString() + "' already in use.");
                    break;
                case Errors.LabelUnknown:
                    stb.Append("Label '" + v[0].ToString() + "' unknown.");
                    break;
                case Errors.TypeUnknown:
                    stb.Append("Type '" + v[0].ToString() + "' unknown.");
                    break;
                case Errors.NamespaceUnknown:
                    stb.Append("Namespace '" + v[0].ToString() + "' unknown.");
                    break;
                case Errors.MemberUnknown:
                    stb.Append("Namespace '" + v[0].ToString() + "' in structure '" + v[1].ToString() + "' unknown.");
                    break;
                case Errors.Internal:
                    stb.Append("Internal error");
                    break;
                case Errors.DereferencingGenericPtr:
                    stb.Append("You cant dereferenciate a generic pointer, aka 'ptr' or 'void*': " + v[0].ToString());
                    break;
                case Errors.DereferencingNull:
                    stb.Append("You are reading a 0-value @'" + v[0].ToString() + "' aka '0~' ?!");
                    break;
                case Errors.UnsupportedOperation:
                    stb.Append("The operation performed on '" + v[0].ToString() + "' is not supported yet.");
                    break;
                default:
                    stb.Append(v[0].ToString());
                    break;
            }

            Console.WriteLine(stb.ToString());
            throw new Exception(stb.ToString());
        }
    }

    public class Warning
    {
        public Warning(String v)
        { new Warning(Warnings.Unknown, v); }
        public Warning(Warnings num, params Object[] v)
        {
            StringBuilder stb = new StringBuilder("Warning: ");

            switch (num) 
            {
                case Warnings.CallArgumentsInvalid:
                    stb.Append("Argument count for call of '" + v[0].ToString() + "' is invalid.");
                    break;
                case Warnings.DereferencingNonPtrType:
                    stb.Append("You are reading @'" + v[0].ToString() + "' but its not a pointer ?!");
                    break;
                case Warnings.ReferencingUnknownAdress:
                    stb.Append("You are writing @'" + v[0].ToString() + "' and i dont know the type…");
                    break;
                case Warnings.ReferencingNonAtomarType:
                    stb.Append("You are writing @'" + v[0].ToString() + "' but the type has the size 0.");
                    break;
                case Warnings.ExpressionIgnored:
                    stb.Append("The expression '" + v[0].ToString() + "' has been ignored, due to illegal placement.");
                    break;
                default:
                    stb.Append(v[0].ToString());
                    break;
            }

            Console.WriteLine(stb.ToString());
        }
    }

    public enum Errors : int
    {
        NotAwaitedToken,
        AwaitedToken,
        ForbiddenInStruct,
        LabelInUse,
        LabelUnknown,
        TypeUnknown,
        NamespaceUnknown,
        MemberUnknown,
        Internal,
        DereferencingGenericPtr,
        DereferencingNull,
        UnsupportedOperation,
        Unknown
    }

    public enum Warnings : int
    {
        CallArgumentsInvalid,
        DereferencingNonPtrType,
        ReferencingUnknownAdress,
        ReferencingNonAtomarType,
        ExpressionIgnored,
        Unknown
    }
}