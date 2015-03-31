using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DeMangleVC
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                StreamReader sr = new StreamReader(args[0]);
                String line;
                line = sr.ReadLine();
                char[] sep = { ' ', '\t' };
                while (line != null)
                {
                    int idx = line.IndexOfAny(sep);
                    if (idx > 0)
                    {
                        line = line.Substring(0, idx);
                    }
                    DeMangle dm = new DeMangle(line);
                    try
                    {
                        dm.Work();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(line);
                        Console.Error.WriteLine(dm.processPos);
                        Console.Error.WriteLine(e.Data);
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.Source);
                        Console.Error.WriteLine(e.StackTrace);
                    }
                    String res = dm.GetResult();
                    Console.WriteLine(res);
                    line = sr.ReadLine();
                }
            }
        }
    }

    class StringHelper
    {
        public static string glue(string l, string r)
        {
            if (l == "" || r == "")
            {
                return l + r;
            }
            else
            {
                return l + " " + r;
            }
        }
    }

    public enum enumTypes
    {
        enmSimple,
        enmReference,
        enmClass,
        enmFunctionBase,
        enmNamedVar,
        enmFunction,
        enmVarType,
        enmSpecialSuffix // some special non-var 'non type'
    };

    /// <summary>
    /// base class for all class that correspond to some symbols segment, like identifier, name, etc
    /// </summary>
    class ParseBase
    {
        /// <summary>
        /// Start position of the underlying string
        /// </summary>
        private int _start;
        /// <summary>
        /// End postion of the underlying string
        /// </summary>
        private int _end;
        /// <summary>
        /// The underlying string
        /// </summary>
        private string _str;
        /// <summary>
        /// Pase the input start at pos, and return the pos of the next char to be parsed
        /// </summary>
        /// <param name="intput"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        virtual public ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD) { throw new NotImplementedException(); }
        /// <summary>
        /// dump the underlying string and pased result
        /// </summary>
        public void dump() { Console.Error.WriteLine("{0} {1} {2} . {3} . {4}", _start, _end, GetType().Name, _str, getResult()); }
        /// <summary>
        /// Get the demangled result of this piece
        /// </summary>
        /// <returns></returns>
        virtual public string getResult() { throw new NotImplementedException(); }

        public Type toType() { return (Type)this; }

        protected void saveParseStatus(string src, int start, int end)
        {
            _start = start;
            _end = end;
            _str = src.Substring(start, end - start);
#if TRACE
            dump();
#endif
        }
    }

    class StringComponent : ParseBase
    {
        protected string _strRes = "";
        public override string getResult() { return _strRes; }
        public static string getString(string src, ref int pos)
        {
            int startPos = pos;
            int i = 0;
            while (src[pos + i] != '@')
            {
                i++;
            }
            if (i != 0)
            {
                pos += i + 1;
            }
            return src.Substring(startPos, i);
        }
        public static long getInteger(string src, ref int pos)
        {
            long val;
            int iProcessPos = pos;
            bool minus = false;
            if (src[iProcessPos] == '?')
            {
                minus = true;
                iProcessPos++;
            }
            if (Char.IsDigit(src[iProcessPos]))
            {
                val = src[iProcessPos] - '0' + 1;
                iProcessPos++;
            }
            else
            {
                val = 0;
                while (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'P')
                {
                    val = val * 16 + src[iProcessPos] - 'A';
                    iProcessPos++;
                }
                if (src[iProcessPos] != '@')
                {
                    throw new Exception("Error in Integer constant");
                }
                iProcessPos++;
            }
            if (minus)
            {
                val = -val;
            }
            pos = iProcessPos;
            return val;
        }
        public static string GetTemplateArgumentList(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            return new TemplateArguamentList().parse(src, ref pos, ref vType, ref vUiD).getResult();
        }
    }

    class FunctionModifier : StringComponent
    {
        protected static String[] strAControl = {
            "private:", "protected:", "public:"
        };
        protected static String[] strModifier = {
            "static", "virtual"
        };
        protected enum enumModifier { enmMstatic, enmMvirtual };
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            String sModifier;
            _bHasThis = true;
            //String tAccess = new TypeID();
            String tAccess = "";
            String sThunkPrefix = "";
            sModifier = "";
            _sThunkAdjustor = "";
            int iProcessPos = pos;
            if (src[iProcessPos] < 'Y')
            {
                tAccess = strAControl[(src[iProcessPos] - 'A') / 8];
            }
            switch (src[iProcessPos])
            {
            case 'A':
            case 'I':
            case 'Q':
                break;
            case 'C':
            case 'S':
            case 'K':
                _bHasThis = false;
                sModifier = strModifier[(int)enumModifier.enmMstatic];
                break;
            case 'E':
            case 'M':
            case 'U':
                sModifier = strModifier[(int)enumModifier.enmMvirtual];
                break;
            case 'G':
            case 'O':
            case 'W':
                iProcessPos++;
                long lAdjustor = StringComponent.getInteger(src, ref iProcessPos);
                sThunkPrefix = "[thunk]:";
                _sThunkAdjustor = "`adjustor{" + lAdjustor.ToString() + "}\' ";
                sModifier = strModifier[(int)enumModifier.enmMvirtual];
                iProcessPos--;
                break;
            case 'Y':
                _bHasThis = false;
                sModifier = "";
                break;
            default:
                throw new Exception();
            }
            iProcessPos++;
            if (tAccess != "" && sModifier != "")
            {
                tAccess += " ";
            }
            _strRes = sThunkPrefix + tAccess + sModifier;
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }

        private bool _bHasThis;
        private string _sThunkAdjustor;

        public string sThunkAdjustor { get { return _sThunkAdjustor; } }
        public bool bHasThis { get { return _bHasThis; } }
    }

    class ParameterClause : StringComponent
    {
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            StringBuilder PList = new StringBuilder();
            Type strParamType;

            if (src[iProcessPos] == '@')
            {
                PList.Append("");
                iProcessPos++;
            }
            else if (src[iProcessPos] == 'Z')
            {
                PList.Append("...");
                iProcessPos++;
            }
            else
            {
                strParamType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD, true);
                PList.Append(strParamType.getDeclaration(""));
                if (strParamType.getDeclaration("") != "void")
                {
                    while (src[iProcessPos] != '@' && src[iProcessPos] != 'Z')
                    {
                        strParamType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD, true);
                        PList.Append(",");
                        PList.Append(strParamType.getDeclaration(""));
                    }
                    if (src[iProcessPos] == 'Z')
                    {
                        PList.Append(",...");
                    }
                    iProcessPos++;
                }
            }
            _strRes = PList.ToString();
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class Type : ParseBase
    {
        #region enumeration constants
        protected static String[] strCallConv = {
            "__cdecl",
            "__cdecl",
            "__pascal",
            "__pascal",
            "__thiscall",
            "__thiscall",
            "__stdcall",
            "__stdcall",
            "__fastcall",
            "__fastcall",
            "",
            "",
            "__clrcall"
        };

        protected static String[] strType = {
            "&",                // reference
            "& volatile",
            "signed char",
            "char",
            "unsigned char",
            "short",
            "unsigned short",
            "int",
            "unsigned int",
            "long",
            "unsigned long",
            "L",
            "float",
            "double",
            "long double",
            "*",
            "* const",
            "* volatile",
            "* const volatile",
            "union",
            "struct",
            "class",
            "enum",
            "void",
            "[.]",
            "Z"
        };

        protected static String[] strTypeE = {
            "& __ptr64",                // reference
            "& __ptr64 volatile",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "* __ptr64",
            "* __ptr64 const",
            "* __ptr64 volatile",
            "* __ptr64 const volatile",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z"
        };

        protected static String[] strType_ = {
            "_A",
            "_B",
            "_C",
            "__int8",
            "unsigned __int8",
            "__int16",
            "unsigned __int16",
            "__int32",
            "unsigned __int32",
            "__int64",
            "unsigned __int64",
            "__int128",
            "unsigned __int128",
            "bool",
            "_O",
            "_P",
            "_Q",
            "_R",
            "_S",
            "_T",
            "_U",
            "_V",
            "wchar_t",
            "_X",
            "_Y",
            "_Z"
        };
        #endregion

        protected String _strCVQualifier = "";

        public String StrCVQualifier
        {
            get { return _strCVQualifier; }
        }
        virtual public String getTypeString() { throw new NotImplementedException(); }
        virtual public enumTypes getTypeKind() { throw new NotImplementedException(); }
        virtual public String getDeclaration(String qID, bool bEnclose = false)
        {
            return StringHelper.glue(getTypeString(), qID);
        }
        virtual public void ajdustCVQ() { _strCVQualifier = ""; }
        public override string getResult() { return getDeclaration("###"); }
        public static Type GetTypeLikeID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD, bool topLevelType = false)
        {
            Type retType;
            int iProcessPos = pos;
            switch (src[iProcessPos])
            {
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'M':
            case 'N':
            case 'O':
            case 'X':
                retType = new TypeSimple(strType[src[iProcessPos] - 'A']);
                iProcessPos++;
                break;
            case '?':
            case 'A':
            case 'B':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
                retType = new TypeReference().parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
                break;
            case 'T':
            case 'U':
            case 'V':
            case 'W':
                retType = new TypeClass().parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
                break;
            case 'Y':
                retType = new TypeArray().parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
                break;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                retType = vType[src[iProcessPos] - '0'];
#if TRACE
                Console.Error.WriteLine("dump vType");
                for (int i = 0; i < vType.Count; i++)
                {
                    Console.Error.WriteLine("{0} , {1}", i, vType[i].getResult());
                }
#endif
                iProcessPos++;
                break;
            case '_':
                iProcessPos++;
                switch (src[iProcessPos])
                {
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'W':
                    retType = new TypeSimple(strType_[src[iProcessPos] - 'A']);
                    iProcessPos++;
                    break;
                default:
                    throw new Exception("Type specific letter _" + new String(src[iProcessPos], 1) + " not found");
                }
                break;
            case '$': // special type or type like identifier
                iProcessPos++;

                switch (src[iProcessPos])
                {
                case '0': // integer
                    iProcessPos++;
                    retType = new TypeNonType(StringComponent.getInteger(src, ref iProcessPos).ToString());
                    break;
                case '1': // pointer
                    QualifiedID qID = new QualifiedID();
                    TypeVarType vTypeID = new TypeVarType();
                    iProcessPos++;
                    if (src[iProcessPos] != '?')
                    {
                        throw (new Exception("\'?\' expected in reference parameter"));
                    }
                    iProcessPos++;
                    qID.parse(src, ref iProcessPos, ref vType, ref vUiD);
                    vTypeID.parse(src, ref iProcessPos, ref vType, ref vUiD);
                    retType = new TypeNonType("&" + vTypeID.getDeclaration(qID.strQualifiedID));
                    break;
                case 'S':
                    retType = new TypeNonType(""); // Empty expansion list for integral types/size_t
                    iProcessPos++;
                    break;
                case '$': // Simple ref, will use the letter C,
                    iProcessPos++;
                    if (src[iProcessPos] == '$' && src[iProcessPos + 1] == 'V')
                    {   // "$$$V@" for empty template parameter list
                        retType = new TypeNonType("");
                        iProcessPos += 2;
                    }
                    else
                    {
                        iProcessPos = pos;
                        retType = new TypeReference().parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
                    }
                    break;
                default:
                    throw new Exception();
                }
                break;
            case '@':
                retType = new TypeNonType("");
                break;
            default:
                throw new Exception("Type specific letter " + new String(src[iProcessPos], 1) + " not found");
            }
            if (topLevelType && iProcessPos > pos + 1 && !(retType is TypeNonType))
            {
                vType.Add(retType);
            }
            pos = iProcessPos;
            return retType;
        }
    }

    class TypeSimple : Type
    {
        private String _strType;
        public TypeSimple(String type)
        {
            _strType = type;
        }
        public override String getTypeString()
        {
            return _strType;
        }
        public override enumTypes getTypeKind()
        {
            return enumTypes.enmSimple;
        }
        public override string getDeclaration(string qID, bool hasBrackt = false)
        {
            return StringHelper.glue(_strType, qID);
        }
    }

    class TypeNonType : TypeSimple
    {
        public TypeNonType(string type) : base(type) { }
    }

    class TypeSuffix : Type
    {
        private String _strSuffix;
        public String strSuffix
        {
            get { return _strSuffix; }
            set { _strSuffix = value; }
        }
        public TypeSuffix(string suffix)
        {
            _strSuffix = suffix;
        }
        public override string getDeclaration(string qID, bool bEnclose = false)
        {
            return qID + _strSuffix;
        }
    }

    class CVQ : StringComponent
    {
        private bool _bMemThis = false;
        public bool bMemThis
        {
            get { return _bMemThis; }
        }
        public override string getResult()
        {
            return _strRes;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _bMemThis = false;
            switch (src[iProcessPos])
            {
            case 'A':
                _strRes = "";
                break;
            case 'B':
                _strRes = "const";
                break;
            case 'C':
                _strRes = "volatile";
                break;
            case 'D':
                _strRes = "const volatile";
                break;
            case 'E':
                _strRes = "__ptr64";
                break;
            case 'Q':
                iProcessPos++;
                _strRes = new NestNameSpecifier().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                iProcessPos--;
                break;
            case '6':
                _strRes = "";
                break;
            case '8':
                iProcessPos++;
                _strRes = new NestNameSpecifier().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                _bMemThis = true;
                iProcessPos--;
                break;
            default:
                throw new Exception("Unrecognized CV-Qualifier " + src[iProcessPos]);
            }
            iProcessPos++;
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeReference : Type
    {
        private String _strReferenceType = "";

        public String StrReferenceType
        {
            get { return _strReferenceType; }
            set { _strReferenceType = value; }
        }
        private Type _typeReferenced;

        internal Type TypeReferenced
        {
            get { return _typeReferenced; }
            set { _typeReferenced = value; }
        }
        public override String getTypeString()
        {
            return getDeclaration("");
        }
        public override enumTypes getTypeKind()
        {
            return enumTypes.enmReference;
        }
        public override string getDeclaration(string qID, bool bEnclose = false)
        {
            String strDecl = "";
            if (_strReferenceType.Length > 2)
            {
                if (qID.StartsWith(_strReferenceType.Substring(2)))
                {
                    qID = qID.Substring(_strReferenceType.Length - 1);
                }
            }
            if (StrCVQualifier.EndsWith("::"))
            {
                strDecl = StrCVQualifier + StringHelper.glue(_strReferenceType, qID);
            }
            else
            {
#if REFINE__
                strDecl = StringHelper.glue(StrCVQualifier, StringHelper.glue(_strReferenceType, qID));
#else
                if (StrCVQualifier != "" && _strReferenceType == "" && qID == "")
                {   // Extra blank generated by UnDecorateSymbolName
                    strDecl = StrCVQualifier + " ";
                }
                else
                {
                    strDecl = StringHelper.glue(StrCVQualifier, StringHelper.glue(_strReferenceType, qID));
                }
#endif
            }
            return _typeReferenced.getDeclaration(strDecl, (_typeReferenced is TypeFunctionBase && _strReferenceType == "") ? false : true);
        }
        public override void ajdustCVQ()
        {
            _strReferenceType = _strReferenceType.Replace("* const", "*");
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            bool bHasThis;
            bool noCV = false;
            switch (src[iProcessPos])
            {
            case 'A':
            case 'B':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
                if (src[iProcessPos + 1] == 'E')
                {
                    StrReferenceType = strTypeE[src[iProcessPos] - 'A'];
                    iProcessPos++;
                }
                else
                {
                    StrReferenceType = strType[src[iProcessPos] - 'A'];
                }
                break;
            case '?': // type transfered by value
                //iProcessPos++;
                //StrCVQualifier = GetCVQVar();
                //TypeReferenced = Type.GetTypeLikeID(false);
                break;
            case '$':
                iProcessPos++;
                if (src[iProcessPos] != '$')
                {
                    throw new Exception("Illegal special type identifier");
                }
                iProcessPos++;
                switch (src[iProcessPos])
                {
                case 'A': // function pointers
                    //iProcessPos++;
                    //sType.StrCVQualifier = GetCVQFunc(out bHasThis);
                    //sType.TypeReferenced = GetFunctionBody(bHasThis);
                    break;
                case 'B': // in parameter, array, no CV qualifier
                    //iProcessPos++;
                    noCV = true;
                    //sType.TypeReferenced = GetTypeLikeID(false);
                    break;
                case 'C': // in template paramter list
                    //case '?': // type transfered by value
                    //iProcessPos++;
                    //sType.StrCVQualifier = GetCVQVar();
                    //sType.TypeReferenced = GetTypeLikeID(false);
                    break;
                case 'Q':
                    //iProcessPos++;
                    StrReferenceType = "&&";
                    //sType.StrCVQualifier = GetCVQVar();
                    //sType.TypeReferenced = GetTypeLikeID(false);
                    break;
                default:
                    throw new Exception("illegal Special Ref : " + src[iProcessPos]);
                }
                break;
            default:
                throw new Exception("Illegal leading char in Ref and Pointer Type : " + src[iProcessPos]);
            }
            iProcessPos++;
            CVQ cvq = new CVQ();
            if (Char.IsDigit(src[iProcessPos]))
            {
                _strCVQualifier = cvq.parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                TypeReferenced = new TypeFunctionBase(cvq.bMemThis).parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
            }
            else
            {
                if (!noCV)
                {
                    _strCVQualifier = cvq.parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                }
                TypeReferenced = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeClass : Type
    {
        private String _strClassQualifiedName;

        public String StrClassQualifiedName
        {
            get { return _strClassQualifiedName; }
            set { _strClassQualifiedName = value; }
        }
        private QualifiedID className;

        private bool _bTemplate;

        public bool BTemplate
        {
            get { return _bTemplate; }
            set { _bTemplate = value; }
        }
        private String _ClassKey;

        public String ClassKey
        {
            get { return _ClassKey; }
            set { _ClassKey = value; }
        }

        public override string getTypeString()
        {
            return _ClassKey + " " + _strClassQualifiedName;
        }
        public override enumTypes getTypeKind()
        {
            return enumTypes.enmClass;
        }

        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            char cType = src[iProcessPos];
            String infixEnum = "";
            if (cType == 'W')
            {
                iProcessPos++;
                if (src[iProcessPos] >= '0' && src[iProcessPos] <= '7')
                {
                    infixEnum = strType[src[iProcessPos] - '0' + 3] + " ";
                    if (infixEnum == "int ")
                    {
                        infixEnum = "";
                    }
                }
                else
                {
                    throw new Exception("illegal Enumeration : " + src[iProcessPos]);
                }
            }
            switch (cType)
            {
            case 'T':
            case 'U':
            case 'V':
            case 'W':
                iProcessPos++;
                ClassKey = StringHelper.glue(strType[cType - 'A'], infixEnum);
                StrClassQualifiedName = new QualifiedID().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                break;
            default:
                throw new Exception("illegal compound : " + cType);
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeArray : Type
    {
        private String _strSubcript;

        public String StrSubcript
        {
            get { return _strSubcript; }
            set { _strSubcript = value; }
        }
        private Type _baseType;

        internal Type BaseType
        {
            get { return _baseType; }
            set { _baseType = value; }
        }
        public override string getDeclaration(string qID, bool hasBrackt = false)
        {
            //String Decl = "";
            if (qID.IndexOf('*') >= 0 || qID.IndexOf('&') >= 0)
            {
#if REFINE__
#else
                // This should be a bug of UnDecorateSymbolName
                if (qID.StartsWith("* const "))
                {
                    qID = "*" + qID.Substring("* const".Length);
                }
#endif
                qID = "(" + qID + ")";
            }
            return _baseType.getDeclaration(qID + _strSubcript);
        }

        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != 'Y')
            {
                throw new Exception("Error Array Identifier");
            }
            iProcessPos++;
            StrSubcript = "";

            long Dimension = StringComponent.getInteger(src, ref iProcessPos);
            for (long i = 0; i < Dimension; i++)
            {
                StrSubcript = StrSubcript + "[" + StringComponent.getInteger(src, ref iProcessPos) + "]";
            }
            _baseType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeVarType : Type
    {
        private String _strAccess;

        public String StrAccess
        {
            get { return _strAccess; }
            set { _strAccess = value; }
        }
        private Type _type;

        public Type InnerType
        {
            get { return _type; }
            set { _type = value; }
        }
        public override enumTypes getTypeKind()
        {
            return enumTypes.enmVarType;
        }
        public override string getTypeString()
        {
            return getDeclaration("");
        }
        public override string getDeclaration(string qID, bool hasBrackt = false)
        {
            return _strAccess + _type.getDeclaration(StringHelper.glue(StrCVQualifier.EndsWith("::") ? "" : StrCVQualifier, qID));
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            Type BaseType = new TypeSimple("");
            int iSpecialVariable = 0;
            switch (src[iProcessPos])
            {
            case '0':
                StrAccess = "private: static ";
                break;
            case '1':
                StrAccess = "protected: static ";
                break;
            case '2':
                StrAccess = "public: static ";
                break;
            case '3':
                StrAccess = "";
                break;
            case '4': // function scope static variable
                StrAccess = "";
                break;
            case '5':
                StrAccess = "";
                iSpecialVariable = 5;
                break;
            case '6': // `vftable'
                StrAccess = "";
                iSpecialVariable = 6;
                break;
            case '7': // `vbtable'
                StrAccess = "";
                iSpecialVariable = 7;
                break;
            case '8':
                StrAccess = "";
                iSpecialVariable = 8;
                break;
            case '9':
                StrAccess = "";
                iSpecialVariable = 9;
                break;
            default:
                throw new Exception("Illegal Variable Acess modifier: " + src[iProcessPos]);
            //break;
            }
            iProcessPos++;

            if (iSpecialVariable < 5)
            {
                BaseType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }

            if (iSpecialVariable != 9 && iSpecialVariable != 8 && iSpecialVariable != 5)
            {
                _strCVQualifier = (new CVQ()).parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
            }

            if (iSpecialVariable == 5)
            {
                BaseType = new TypeSuffix("{" + StringComponent.getInteger(src, ref iProcessPos).ToString() + "}'");///, 0, false, false);
            }

            InnerType = BaseType;
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeFunctionBase : Type
    {
        private String _strCallConversion;

        public String StrCallConversion
        {
            get { return _strCallConversion; }
            set { _strCallConversion = value; }
        }
        private String _strParamList;

        public String StrParamList
        {
            get { return _strParamList; }
            set { _strParamList = value; }
        }
        private String _strCVQThis = "";

        public String StrCVQThis
        {
            get { return _strCVQThis; }
            set { _strCVQThis = value; }
        }
        private String _strExceptionList;

        public String StrExceptionList
        {
            get { return _strExceptionList; }
            set { _strExceptionList = value; }
        }
        private Type _typeReturn;

        internal Type TypeReturn
        {
            get { return _typeReturn; }
            set { _typeReturn = value; }
        }
        private bool _bHasThis;
        public TypeFunctionBase(bool bHasThis = false)
        {
            _bHasThis = bHasThis;
        }
        public override string getTypeString()
        {
            return getDeclaration("");
        }
        public override string getDeclaration(string qID, bool bEnclose = false)
        {
            String sFuncBody;
            String sFuncOut;
            if (qID.IndexOf("$B#") >= 0)
            {
                qID = qID.Replace("$B#", _typeReturn.getTypeString());
                _typeReturn = new TypeSimple("");
            }
            if (bEnclose)//bEncloseFuncName)
            {
#if REFINE__
                sFuncBody = "(" + StringHelper.glue(_strCallConversion, qID) + ")" + _strParamList + _strCVQThis;
#else
                if (qID.StartsWith("* *") ||
                    qID.StartsWith("* &") ||
                    qID.StartsWith("* __stdcall ") ||
                    qID.StartsWith("* __cdecl ") ||
                    qID.StartsWith("* __clrcall ") ||
                    qID.StartsWith("* __thiscall "))
                {
                    qID = "*" + qID.Substring(2);
                }
                sFuncBody = "(" + _strCallConversion + (qID.StartsWith("*") ? "" : " ") + qID + ")" + _strParamList + _strCVQThis;
#endif
            }
            else
            {
                sFuncBody = StringHelper.glue(_strCallConversion, qID) + _strParamList + _strCVQThis;
            }
#if REFINE__
            // UnDecorateSymboleName will add an extra blank at the end of const member function, which is not needed
#else
            if (_strCVQThis != "")
            {
                sFuncBody += " ";
            }
#endif
            _typeReturn.ajdustCVQ();
            sFuncOut = _typeReturn.getDeclaration(sFuncBody + _strExceptionList);
            return sFuncOut;
        }

        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (_bHasThis)
            {
                if (src[iProcessPos] == 'E')
                {
                    StrCVQThis = "__ptr64";
                    iProcessPos++;
                }
                StrCVQThis = new CVQ().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult() + StrCVQThis;
            }
            StrCallConversion = strCallConv[src[iProcessPos] - 'A'];
            iProcessPos++;
            if (src[iProcessPos] == '@')
            {
                iProcessPos++;
                TypeReturn = new TypeSimple("");
            }
            else
            {
                _typeReturn = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }
            ParameterClause p = new ParameterClause();
            StrParamList = "(" + p.parse(src, ref iProcessPos, ref vType, ref vUiD).getResult() + ")";
            StrExceptionList = p.parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();

            if (StrExceptionList == "...")
            {
                StrExceptionList = "";
            }
            else
            {
                StrExceptionList = " throw(" + StrExceptionList + ")";
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class UnqualifiedID : StringComponent
    {
        public static readonly String s_strCtorDtor = "$CtorDtor";

        protected static String[] strOperator = {
            UnqualifiedID.s_strCtorDtor,
            "~" + UnqualifiedID.s_strCtorDtor,
            "operator new",
            "operator delete",
            "operator=",
            "operator>>",
            "operator<<",
            "operator!",
            "operator==",
            "operator!="
        };

        protected static String[] strOperator_ = {
            "operator/=",
            "operator%=",
            "operator>>=",
            "operator<<=",
            "operator&=",
            "operator|=",
            "operator^=",
            "`vftable'",
            "`vbtable'",
            "`vcall'"
        };

        protected static String[] strOperatorC ={
            "operator[]",
            "operator $B#",    //operater type conversion
            "operator->",
            "operator*",
            "operator++",
            "operator--",
            "operator-",
            "operator+",
            "operator&",
            "operator->*",
            "operator/",
            "operator%",
            "operator<",
            "operator<=",
            "operator>",
            "operator>=",
            "operator,",
            "operator()",
            "operator~",
            "operator^",
            "operator|",
            "operator&&",
            "operator||",
            "operator*=",
            "operator+=",
            "operator-="
        };

        protected static String[] strOperatorC_ ={
            "`typeof'",
            "`local static guard'",
            "$_C#",    //string constanst
            "`vbase destructor'",
            "`vector deleting destructor'",
            "`default constructor closure'",
            "`scalar deleting destructor'",
            "`vector constructor iterator'",
            "`vector destructor iterator'",
            "`vector vbase constructor iterator'",
            "`virtual displacement map'",
            "`eh vector constructor iterator'",
            "`eh vector destructor iterator'",
            "`eh vector vbase constructor iterator'",
            "`copy constructor closure'",
            "_P",
            "_Q",
            "$_R#",     // used for RTTI
            "`local vftable'",
            "`local vftable constructor closure'",
            "operator new[]",
            "operator delete[]",
            "_W",
            "`placement delete closure'",
            "`placement delete[] closure'",
            "_Z"
        };

        protected static String[] strOperatorC__ = {
            "`managed vector constructor iterator'",
            "`managed vector destructor iterator'",
            "`eh vector copy constructor iterator'",
            "`eh vector vbase copy constructor iterator'",
            "`dynamic initializer for '.''",
            "`dynamic atexit destructor for '.''",
            "`vector copy constructor iterator'",
            "__H",
            "__I",
            "__J",
            "__K",
            "__L",
            "__M",
            "__N",
            "__O",
            "__P",
            "__Q",
            "__R",
            "__S",
            "__T",
            "__U",
            "__V",
            "__W",
            "__X",
            "__Y",
            "__Z"
        };

        public enum enumUnqualifiedID
        {
            enmEmpty,
            enmIdentifier,
            enmOperatorFunctionID,
            enmConversionFuctionID,
            enmCtorDtor,
            enmTemplateID,
            enmRTTISymbols,
            enmString
        };

        enumUnqualifiedID _eUnqualifiedIdType;
        public enumUnqualifiedID eUnqualifiedIdType
        {
            get { return _eUnqualifiedIdType; }
        }
        public UnqualifiedID(String Id, enumUnqualifiedID eUnqualifiedIdType)
        {
            _strRes = Id;
            _eUnqualifiedIdType = eUnqualifiedIdType;
        }
        private UnqualifiedID(UnqualifiedID idt)
        {
            _strRes = idt._strRes;
            _eUnqualifiedIdType = idt._eUnqualifiedIdType;
        }
        public UnqualifiedID()
        {
            _strRes = "";
            _eUnqualifiedIdType = enumUnqualifiedID.enmEmpty;
        }
        bool _baseName;
        public UnqualifiedID(bool baseName)
        {
            _baseName = baseName;
        }
        public UnqualifiedID AppendTplArgLst(String TplParaList)
        {
            UnqualifiedID idt = new UnqualifiedID(this);
            int insertionPoint = idt._strRes.Length;
            if (_eUnqualifiedIdType == enumUnqualifiedID.enmConversionFuctionID)
            {
                insertionPoint = idt._strRes.IndexOf(" $B#");
            }
#if REFINE__
            // UnDecorateSymboleName will not put a blank between operator?? and the following template parameter list,
            // causing confusion. e.g operator<<...> This piece of code adds an extra blank
            char chEnd = idt._strRes[insertionPoint - 1];
            if (!Char.IsLetterOrDigit(chEnd) && chEnd != '_' && chEnd != '#')//# is used in operator replacement
            {   // add blanks for operators, like operator< , etc.
                TplParaList = " " + TplParaList;
            }
#endif
            idt._strRes = idt._strRes.Insert(insertionPoint, TplParaList);
            switch (idt._eUnqualifiedIdType)
            {
            case enumUnqualifiedID.enmIdentifier:
                idt._eUnqualifiedIdType = enumUnqualifiedID.enmTemplateID;
                break;
            case enumUnqualifiedID.enmOperatorFunctionID:
            case enumUnqualifiedID.enmCtorDtor:
            case enumUnqualifiedID.enmConversionFuctionID:
                break;
            default:
                throw new Exception("illegal template");
            }
            return idt;
        }
        public UnqualifiedID ReplaceCtorDtor(UnqualifiedID uID)
        {
            if (uID.eUnqualifiedIdType != enumUnqualifiedID.enmIdentifier && uID.eUnqualifiedIdType != enumUnqualifiedID.enmTemplateID)
            {
                throw new Exception("invalid class id");
            }
            _strRes = _strRes.Replace(s_strCtorDtor, uID._strRes);
            return this;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (_baseName == true)
            {
                parseBaseName(src, ref iProcessPos, ref vType, ref vUiD);
            }
            else
            {
                parseClassNamespaceName(src, ref iProcessPos, ref vType, ref vUiD);
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
        private void parseBaseName(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] == '?')
            {
                iProcessPos++;
                if (src[iProcessPos] == '$')
                {
                    iProcessPos++;
                    getTemplateID(src, ref iProcessPos, ref vType, ref vUiD);
                }
                else
                {
                    getOperatorFunctionID(src, ref iProcessPos, ref vType, ref vUiD);
                }
            }
            else if (src[iProcessPos] >= '0' && src[iProcessPos] <= '9')
            {
                _strRes = vUiD[src[iProcessPos] - '0']._strRes;
                _eUnqualifiedIdType = vUiD[src[iProcessPos] - '0']._eUnqualifiedIdType;
                iProcessPos++;
#if TRACE
                Console.Error.WriteLine("trace vUiD");
                for (int i = 0; i < vUiD.Count; i++)
                {
                    Console.Error.WriteLine("{0} : {1}", i, vUiD[i].getResult());
                }
#endif
            }
            else
            {
                _strRes = StringComponent.getString(src, ref iProcessPos);
                _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
                vUiD.Add(this);
            }
            pos = iProcessPos;
        }
        private void getTemplateID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;

            List<UnqualifiedID> vUiDN = new List<UnqualifiedID>();
            List<Type> vTypeN = new List<Type>();
            UnqualifiedID TplName = new UnqualifiedID(true);
            TplName.parse(src, ref iProcessPos, ref vType, ref vUiDN);

            String TplParaList = (new TemplateArguamentList()).parse(src, ref iProcessPos, ref vTypeN, ref vUiDN).getResult();
            if (TplParaList.EndsWith(">"))
            {
                TplParaList += " ";
            }

            UnqualifiedID TplID = TplName.AppendTplArgLst("<" + TplParaList + ">");
            _strRes = TplID._strRes;
            _eUnqualifiedIdType = TplID._eUnqualifiedIdType;
            pos = iProcessPos;
        }
        private void getOperatorFunctionID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            String strOperatorID = "";
            _strRes = null;
            int iProcessPos = pos;
            if (src[iProcessPos] == '0' || src[iProcessPos] == '1')
            {
                _strRes = strOperator[src[iProcessPos] - '0'];
                _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmCtorDtor;
            }
            else if (src[iProcessPos] >= '2' && src[iProcessPos] <= '9')
            {
                strOperatorID = strOperator[src[iProcessPos] - '0'];
            }
            else if (src[iProcessPos] == 'B')
            {
                _strRes = strOperatorC[1];
                _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmConversionFuctionID;
            }
            else if (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'Z')
            {
                strOperatorID = strOperatorC[src[iProcessPos] - 'A'];
                if (strOperatorID[0] != '$' && strOperatorID[0] != 'o')
                {
                    throw new Exception("unknown operater " + src[iProcessPos]);
                }
            }
            else if (src[iProcessPos] == '_')
            {
                iProcessPos++;
                if (src[iProcessPos] >= '0' && src[iProcessPos] <= '9')
                {
                    strOperatorID = strOperator_[src[iProcessPos] - '0'];
                    if (strOperatorID[0] == '_')
                    {
                        throw new Exception("unknown operater _" + src[iProcessPos]);
                    }
                }
                else if (src[iProcessPos] == 'R')
                {
                    iProcessPos++;
                    switch (src[iProcessPos])
                    {
                    case '0':
                        iProcessPos++;
                        Type strClassType;
                        strClassType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
                        strOperatorID = strClassType.getDeclaration("") + " `RTTI Type Descriptor'";
                        break;
                    case '1':
                        int[] iNum = new int[4];
                        iProcessPos++;
                        for (int i = 0; i < 4; i++)
                        {
                            long num = StringComponent.getInteger(src, ref iProcessPos);
                            iNum[i] = (int)num;
                        }
                        strOperatorID = "`RTTI Base Class Descriptor at (" + iNum[0] + "," + iNum[1] + "," + iNum[2] + "," + iNum[3] + ")'";
                        break;
                    case '2':
                        iProcessPos++;
                        strOperatorID = "`RTTI Base Class Array'";
                        break;
                    case '3':
                        iProcessPos++;
                        strOperatorID = "`RTTI Class Hierarchy Descriptor'";
                        break;
                    case '4':
                        iProcessPos++;
                        strOperatorID = "`RTTI Complete Object Locator'";
                        break;
                    default:
                        throw new Exception("Invalid RTTI descriptor");
                    }
                    iProcessPos--;
                    _strRes = strOperatorID;
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmRTTISymbols;
                }
                else if (src[iProcessPos] == 'C')
                {
                    _strRes = "$_C#";
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmString;
                }
                else if (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'Z')
                {
                    strOperatorID = strOperatorC_[src[iProcessPos] - 'A'];
                    if (strOperatorID[0] == '_')
                    {
                        throw new Exception("unknown operater _" + src[iProcessPos]);
                    }
                }
                else if (src[iProcessPos] == '_')
                {
                    iProcessPos++;
                    if (src[iProcessPos] == 'E')
                    {
                        iProcessPos++;
                        String Decl = "";
                        if (src[iProcessPos] == '?')
                        {
                            Decl = new Declaration().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                        }
                        else
                        {
                            Decl = StringComponent.getString(src, ref iProcessPos);
                            iProcessPos--;
                        }
                        strOperatorID = "`dynamic initializer for '" + Decl + "''";
                    }
                    else if (src[iProcessPos] == 'F')
                    {
                        iProcessPos++;
                        String Decl = "";
                        if (src[iProcessPos] == '?')
                        {
                            Decl = Decl = new Declaration().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                        }
                        else
                        {
                            Decl = StringComponent.getString(src, ref iProcessPos);
                            iProcessPos--;
                        }
                        strOperatorID = "`dynamic atexit destructor for '" + Decl + "''";
                    }
                    else if (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'Z')
                    {
                        strOperatorID = strOperatorC__[src[iProcessPos] - 'A'];
                        if (strOperatorID[0] == '_')
                        {
                            throw new Exception("unknown operator __" + src[iProcessPos]);
                        }
                    }
                    else
                    {
                        throw new Exception("unknown operator __" + src[iProcessPos]);
                    }
                }
                else
                {
                    throw new Exception("unknown operator _" + src[iProcessPos]);
                }
            }
            else
            {
                throw new Exception("unknown operator " + src[iProcessPos]);
            }
            iProcessPos++;
            if (_strRes == null)
            {
                _strRes = strOperatorID;
                _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmOperatorFunctionID;
            }
            pos = iProcessPos;
        }
        private void parseClassNamespaceName(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            switch (src[iProcessPos])
            {
            case '?':
                iProcessPos++;
                switch (src[iProcessPos])
                {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                    long val = StringComponent.getInteger(src, ref iProcessPos);
                    _strRes = "`" + val.ToString() + "\'";
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
                    break;
                case 'A':
                    parseAnonymousNameSpace(src, ref iProcessPos);
                    vUiD.Add(this);
                    break;
                case '$':
                    iProcessPos++;
                    getTemplateID(src, ref iProcessPos, ref vType, ref vUiD);
                    vUiD.Add(this);
                    break;
                case '?':
                    _strRes = "`" + (new Declaration()).parse(src, ref iProcessPos, ref vType, ref vUiD).getResult() + "'";
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
                    break;
                default:
                    throw new Exception("unknow character after \'?\' : " + src[iProcessPos]);
                }
                break;
            default:
                _strRes = StringComponent.getString(src, ref iProcessPos);
                vUiD.Add(this);
                _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
                break;
            }
            pos = iProcessPos;
        }
        private void parseAnonymousNameSpace(string src, ref int pos)
        {
            int iProcessPos = pos;
            if (src.Substring(iProcessPos, 3) != "A0x")
            {
                throw new Exception("illegal anonymous namespace");
            }
            iProcessPos += 3;
            for (int i = 0; i < 8; i++)
            {
                char chr = src[iProcessPos];
                if (!((chr >= '0' && chr <= '9') ||
                    (chr >= 'a' && chr <= 'f')))
                {
                    throw new Exception("illegal anonymous namespace");
                }
                iProcessPos++;
            }
            if (src[iProcessPos] != '@')
            {
                throw new Exception("illegal anonymous namespace");
            }
            iProcessPos++;
            String StrAnonymousNamespace = "`anonymous namespace\'";
            _strRes = StrAnonymousNamespace;
            _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
            pos = iProcessPos;
        }
    }

    class TemplateArguamentList : StringComponent
    {
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            StringBuilder PList = new StringBuilder();
            String strTplParaType;
            //int iTmp;
            do
            {   // The existance of empty pack expansion "$$$V" "<>" made thing complicate.
                // It can appear anywhere in the argument list, as the paremater can contain multiple pack expansions,
                // e.g. template<class... _Types1, class... _Types2> inline pair<_Ty1, _Ty2>::pair(piecewise_construct_t, tuple<_Types1...> _Val1, tuple<_Types2...> _Val2)
                Type type;
                type = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD, true);
                strTplParaType = type.getTypeString();
                if (PList.Length > 0 && strTplParaType != "")
                {
                    PList.Append(",");
                }
                if (strTplParaType != "")
                {
                    PList.Append(strTplParaType);
                }
            } while (src[iProcessPos] != '@');
            iProcessPos++;
            _strRes = PList.ToString();
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class NestNameSpecifier : ParseBase
    {
        String _strNestNameSpecifier;
        public String strNestNameSpecifier
        {
            get { return _strNestNameSpecifier; }
        }
        UnqualifiedID _uidFirstQualifier;

        public UnqualifiedID uidFirstQualifier
        {
            get { return _uidFirstQualifier; }
        }

        public NestNameSpecifier AddQualifier(UnqualifiedID uID)
        {
            if (uID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmIdentifier || uID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmTemplateID)
            {
                _strNestNameSpecifier = uID.getResult() + "::" + _strNestNameSpecifier;
            }
            else
            {
                throw new Exception("illegal qualifier");
            }
            return this;
        }
        public override string getResult()
        {
            return _strNestNameSpecifier;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _strNestNameSpecifier = null;
            while (src[iProcessPos] != '@')
            {
                UnqualifiedID uIDCNSName;
                if (src[iProcessPos] >= '0' && src[iProcessPos] <= '9')
                {
                    uIDCNSName = vUiD[src[iProcessPos] - '0'];
                    iProcessPos++;
#if TRACE
                    Console.Error.WriteLine("trace vUiD");
                    for (int i = 0; i < vUiD.Count; i++)
                    {
                        Console.Error.WriteLine("{0} : {1}", i, vUiD[i].getResult());
                    }
#endif
                }
                else
                {
                    uIDCNSName = new UnqualifiedID(false);
                    uIDCNSName.parse(src, ref iProcessPos, ref vType, ref vUiD);
                    if (strNestNameSpecifier == null)
                    {
                        _uidFirstQualifier = uIDCNSName;
                    }
                }
                AddQualifier(uIDCNSName);
            }
            iProcessPos++;
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }

    }

    class QualifiedID : StringComponent
    {
        public String strQualifiedID
        {
            get { return _strRes; }
        }
        public QualifiedID(UnqualifiedID idt)
        {
            _strRes = idt.getResult();
        }
        public QualifiedID(NestNameSpecifier Qualifier, UnqualifiedID uID)
        {
            _strRes = Qualifier.strNestNameSpecifier + uID.getResult();
        }

        private bool _isDecl;
        public QualifiedID(bool isDecl = false)
        {
            _isDecl = isDecl;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            UnqualifiedID uIDBaseID = new UnqualifiedID(true);
            NestNameSpecifier nnsQualifier;
            int iProcessPos = pos;

            uIDBaseID.parse(src, ref iProcessPos, ref vType, ref vUiD);
            if (!_isDecl && iProcessPos - pos > 1 && uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmTemplateID)
            {
                vUiD.Add(uIDBaseID);
            }
            if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmString)
            {
                _strRes = uIDBaseID.getResult();
            }
            else
            {
                nnsQualifier = new NestNameSpecifier();
                nnsQualifier.parse(src, ref iProcessPos, ref vType, ref vUiD);

                if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmCtorDtor)
                {   // base name can be template, so it may just CONTAIN the constructor
                    uIDBaseID.ReplaceCtorDtor(nnsQualifier.uidFirstQualifier);
                }
                _strRes = nnsQualifier.strNestNameSpecifier + uIDBaseID.getResult();
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class Declaration : StringComponent
    {
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != '?')
            {
                throw new Exception("Declaration should begin with '?'");
            }
            iProcessPos++;
            QualifiedID qID = new QualifiedID(true);
            qID.parse(src, ref iProcessPos, ref vType, ref vUiD);
            String sIdent = "";
            if (qID.strQualifiedID == "$_C#")
            {
                sIdent = "`string'";
                iProcessPos = src.Length;
            }
            else if (Char.IsDigit(src[iProcessPos])) // variable
            {
                TypeVarType sType = new TypeVarType();
                sType.parse(src, ref iProcessPos, ref vType, ref vUiD);
                String forDst = "";
                if (iProcessPos < src.Length)
                {
                    switch (src[iProcessPos])
                    {
                    case 'A':
                        iProcessPos++;
                        break;
                    default:
                        if (qID.strQualifiedID.EndsWith("::`vftable'") || qID.strQualifiedID.EndsWith("::`vbtable'") || qID.strQualifiedID.EndsWith("::`RTTI Complete Object Locator'"))
                        {
                            if (src[iProcessPos] != '@')
                            {
                                forDst = new QualifiedID().parse(src, ref iProcessPos, ref vType, ref vUiD).getResult();
                            }
                            iProcessPos++;
                        }
                        break;
                    }
                }
                sIdent = sType.getDeclaration(qID.strQualifiedID);
                if (forDst != "")
                {
                    sIdent = sIdent + "{for `" + forDst + "'}";
                }
            }
            else if (Char.IsLetter(src[iProcessPos])) // function
            {
                String sModifier;
                FunctionModifier funcM = new FunctionModifier();
                funcM.parse(src, ref iProcessPos, ref vType, ref vUiD);
                sModifier = funcM.getResult();//getFunctionModifier(out bHasThis, out sThunkAdjustor);
                if (sModifier.Length > 0)
                {
                    sModifier = sModifier + " ";
                }
                TypeFunctionBase func = new TypeFunctionBase(funcM.bHasThis);
                func.parse(src, ref iProcessPos, ref vType, ref vUiD);
                String sFuncBody = func.getDeclaration(qID.strQualifiedID + funcM.sThunkAdjustor);
                sIdent = sModifier + sFuncBody;
            }
            else
            {
                if (src[iProcessPos] == '$')
                {   // currently we only meet this in vcall
                    long val;
                    iProcessPos++;
                    switch (src[iProcessPos])
                    {
                    case '4':
                        {   // [thunk], `vector deleting destructor'
                            iProcessPos++;
                            long val1 = StringComponent.getInteger(src, ref iProcessPos);
                            long val2 = StringComponent.getInteger(src, ref iProcessPos);
                            String sModifier = "[thunk]:public: virtual ";
                            TypeFunctionBase func = new TypeFunctionBase(true);
                            func.parse(src, ref iProcessPos, ref vType, ref vUiD);
                            String sFuncBody = func.getDeclaration(qID.strQualifiedID + "`vtordisp{" + val1 + "," + val2 + "}' ");
                            sIdent = sModifier + sFuncBody;
                            break;
                        }
                    case 'A':
                        iProcessPos++;
                        Type sRetType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD, true);
                        if (src[iProcessPos] != 'A')
                        {
                            throw new Exception();
                        }
                        sIdent = "[thunk]:" + sRetType.getDeclaration(qID.strQualifiedID + "`local static destructor helper\'");
                        iProcessPos++;
                        break;
                    case 'B':
                        iProcessPos++;
                        val = StringComponent.getInteger(src, ref iProcessPos);
                        if (src.Substring(iProcessPos, 2) != "AE")
                        {
                            throw new Exception();
                        }
                        sIdent = "[thunk]: __thiscall " + qID.strQualifiedID + "{" + val.ToString() + ",{flat}}' }'";
                        iProcessPos += 2;
                        break;
                    default:
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            _strRes = sIdent;
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class DeMangle
    {
        private String src;
        private String dst;
        private int iProcessPos;
        public int processPos { get { return iProcessPos; } }

        public DeMangle(String source)
        {
            src = source;
            dst = source;
        }

        public void Work()
        {
            Declaration d = new Declaration();
            List<Type> vType = new List<Type>();
            List<UnqualifiedID> vUiD = new List<UnqualifiedID>();
            iProcessPos = 0;
            d.parse(src, ref iProcessPos, ref vType, ref vUiD);
            if (iProcessPos < src.Length)
            {
                dst = src;
                throw new Exception("Symbols not exhausted");
            }
            dst = d.getResult();
        }

        public String GetResult()
        {
            return dst;
        }

    }
}
