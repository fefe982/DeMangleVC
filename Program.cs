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
                char [] sep = {' ', '\t'};
                while (line != null)
                {
                    int idx = line.IndexOfAny(sep);
                    if (idx > 0)
                    {
                        line = line.Substring(0, idx);
                    }
                    DeMangel dm = new DeMangel(line);
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
        virtual public int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD) { throw new NotImplementedException(); }
        /// <summary>
        /// dump the underlying string and pased result
        /// </summary>
        public void dump() { Console.Error.WriteLine("{0} {1} {2} . {3} . {4}", _start, _end, GetType().Name, _str, getResult()); }
        /// <summary>
        /// Get the demangled result of this piece
        /// </summary>
        /// <returns></returns>
        virtual public string getResult() { throw new NotImplementedException(); }

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
        public static string getString(string src, int pos, out int newPos)
        {
            int iProcessPos = pos;
            int i = 0;
            while (src[pos + i] != '@')
            {
                i++;
            }
            newPos = pos + i;
            if (i != 0)
            {
                newPos++;
            }
            return src.Substring(pos, i);
        }
        public static long getInteger(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            IntStr intStr = new IntStr();
            pos = intStr.parse(src, pos, ref vType, ref vUiD);
            return intStr.val;
        }
        public static string GetTemplateArgumentList(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            TemplateArguamentList list = new TemplateArguamentList();
            pos = list.parse(src, pos, ref vType, ref vUiD);
            return list.getResult();
        }
    }

    class IntStr : StringComponent
    {
        private long _val;
        public long val { get { return _val; } }
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            bool minus = false;
            if (src[iProcessPos] == '?')
            {
                minus = true;
                iProcessPos++;
            }
            if (Char.IsDigit(src[iProcessPos]))
            {
                _val = src[iProcessPos] - '0' + 1;
                iProcessPos++;
            }
            else
            {
                _val = 0;
                while (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'P')
                {
                    _val = val * 16 + src[iProcessPos] - 'A';
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
                _val = -_val;
            }
            _strRes = _val.ToString();
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                long lAdjustor = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
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
            return iProcessPos;
        }

        private bool _bHasThis;
        private string _sThunkAdjustor;

        public string sThunkAdjustor
        {
            get { return _sThunkAdjustor; }
        }
        public bool bHasThis { get { return _bHasThis; } }
    }

    class ParameterClause : StringComponent
    {
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
            return iProcessPos;
        }
    }

    class Type : ParseBase
    {
        #region enumeration constants

        protected enum enumAControl { enmACprivate, enmACprotected, enmACpublic };



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
        //private enum enumCallConv { enmCCcdecl, enmCCstdcall, enmCCthiscall, enmCCfastcall };

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

        private String _strCVQualifier = "";

        public String StrCVQualifier
        {
            get { return _strCVQualifier; }
            set { _strCVQualifier = value; }
        }
        virtual public String getTypeString() { throw new NotImplementedException(); }
        virtual public enumTypes getTypeKind() { throw new NotImplementedException(); }
        virtual public String getDeclaration(String qID, bool bEnclose = false)
        {
            return StringHelper.glue(getTypeString(), qID);
        }
        virtual public void ajdustCVQ() { _strCVQualifier = ""; }
        public override string getResult() { return getDeclaration("###"); }
        public static Type GetTypeLikeID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD, bool Push = false)
        {
            Type retType;
            int iProcessPos = pos;
            TypeReference refType = new TypeReference();
            TypeClass clsType = new TypeClass();
            TypeArray arrType = new TypeArray();
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
                iProcessPos = refType.parse(src, pos, ref vType, ref vUiD);
                retType = refType;
                if (Push)
                {
                    vType.Add(retType);
                }
                break;
            case 'T':
            case 'U':
            case 'V':
            case 'W':
                iProcessPos = clsType.parse(src, pos, ref vType, ref vUiD);
                retType = clsType;
                if (Push)
                {
                    vType.Add(retType);
                }
                break;
            case 'Y':
                iProcessPos = arrType.parse(src, pos, ref vType, ref vUiD);
                retType = arrType;
                if (Push)
                {
                    vType.Add(retType);
                }
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
                    if (Push)
                    {
                        vType.Add(retType);
                    }
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
                    IntStr intStr = new IntStr();
                    iProcessPos = intStr.parse(src, iProcessPos, ref vType, ref vUiD);
                    retType = new TypeSimple(intStr.getResult());
                    //vType.Add(retType);
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
                    iProcessPos = qID.parse(src, iProcessPos, ref vType, ref vUiD);
                    iProcessPos = vTypeID.parse(src, iProcessPos, ref vType, ref vUiD);
                    retType = new TypeSimple("&" + vTypeID.getDeclaration(qID.strQualifiedID));
                    if (Push)
                    {
                        vType.Add(retType);
                    }
                    break;
                case 'S':
                    retType = new TypeSimple(""); // Empty expansion list for integral types/size_t
                    iProcessPos++;
                    break;
                case '$': // Simple ref, will use the letter C,
                    iProcessPos++;
                    if (src[iProcessPos] == '$' && src[iProcessPos + 1] == 'V')
                    {   // "$$$V@" for empty template parameter list
                        retType = new TypeSimple("");
                        iProcessPos += 2;
                    }
                    else
                    {
                        iProcessPos = refType.parse(src, pos, ref vType, ref vUiD);
                        retType = refType;
                        if (Push)
                        {
                            vType.Add(retType);
                        }
                    }
                    break;
                default:
                    throw new Exception();
                }
                break;
            case '@':
                retType = new TypeSimple("");
                break;
            default:
                throw new Exception("Type specific letter " + new String(src[iProcessPos], 1) + " not found");
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
        public override int parse(string intput, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            throw new NotImplementedException();
        }
    }

    class TypeSpecial : Type
    {
        private String _strSuffix;
        public String strSuffix
        {
            get { return _strSuffix; }
            set { _strSuffix = value; }
        }
        public TypeSpecial(string suffix)
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
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _bMemThis = false;
            NestNameSpecifier nested = new NestNameSpecifier();
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
                iProcessPos = nested.parse(src, iProcessPos, ref vType, ref vUiD);
                _strRes = nested.getResult();
                iProcessPos--;
                break;
            case '6':
                _strRes = "";
                break;
            case '8':
                iProcessPos++;
                iProcessPos = nested.parse(src, iProcessPos, ref vType, ref vUiD);
                _strRes = nested.getResult();
                _bMemThis = true;
                iProcessPos--;
                break;
            default:
                throw new Exception("Unrecognized CV-Qualifier " + src[iProcessPos]);
            }
            iProcessPos++;
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                iProcessPos = cvq.parse(src, iProcessPos, ref vType, ref vUiD);
                StrCVQualifier = cvq.getResult();
                TypeReferenced = new TypeFunctionBase(cvq.bMemThis);
                iProcessPos = TypeReferenced.parse(src, iProcessPos, ref vType, ref vUiD);
            }
            else
            {
                if (!noCV)
                {
                    iProcessPos = cvq.parse(src, iProcessPos, ref vType, ref vUiD);
                    StrCVQualifier = cvq.getResult();
                }
                TypeReferenced = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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

        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                className = new QualifiedID();
                iProcessPos = className.parse(src, iProcessPos, ref vType, ref vUiD);
                StrClassQualifiedName = className.strQualifiedID;
                break;
            default:
                throw new Exception("illegal compound : " + cType);
            }
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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

        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != 'Y')
            {
                throw new Exception("Error Array Identifier");
            }
            iProcessPos++;
            StrSubcript = "";

            long Dimension = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
            for (long i = 0; i < Dimension; i++)
            {
                long val = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
                StrSubcript = StrSubcript + "[" + val + "]";
            }
            _baseType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                CVQ cvqVar = new CVQ();
                iProcessPos = cvqVar.parse(src, iProcessPos, ref vType, ref vUiD);
                StrCVQualifier = cvqVar.getResult();
            }

            if (iSpecialVariable == 5)
            {
                BaseType = new TypeSpecial("{" + StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD) + "}'");///, 0, false, false);
            }

            InnerType = BaseType;
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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

        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (_bHasThis)
            {
                if (src[iProcessPos] == 'E')
                {
                    StrCVQThis = "__ptr64";
                    iProcessPos++;
                }
                CVQ cvq = new CVQ();
                iProcessPos = cvq.parse(src, iProcessPos, ref vType, ref vUiD);
                StrCVQThis = cvq.getResult() + StrCVQThis;
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
            iProcessPos = p.parse(src, iProcessPos, ref vType, ref vUiD);
            StrParamList = "(" + p.getResult() + ")";
            iProcessPos = p.parse(src, iProcessPos, ref vType, ref vUiD);
            StrExceptionList = p.getResult();

            if (StrExceptionList == "...")
            {
                StrExceptionList = "";
            }
            else
            {
                StrExceptionList = " throw(" + StrExceptionList + ")";
            }
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
        }
    }

    class UnqualifiedID : ParseBase
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
        private List<Type> vType;

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

        String _strUnqualifiedID;
        public String strUnqualifiedID
        {
            get { return _strUnqualifiedID; }
        }
        enumUnqualifiedID _eUnqualifiedIdType;
        public enumUnqualifiedID eUnqualifiedIdType
        {
            get { return _eUnqualifiedIdType; }
        }
        public UnqualifiedID(String Id, enumUnqualifiedID eUnqualifiedIdType)
        {
            _strUnqualifiedID = Id;
            _eUnqualifiedIdType = eUnqualifiedIdType;
        }
        private UnqualifiedID(UnqualifiedID idt)
        {
            _strUnqualifiedID = idt._strUnqualifiedID;
            _eUnqualifiedIdType = idt._eUnqualifiedIdType;
        }
        public UnqualifiedID()
        {
            _strUnqualifiedID = "";
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
            int insertionPoint = idt._strUnqualifiedID.Length;
            if (_eUnqualifiedIdType == enumUnqualifiedID.enmConversionFuctionID)
            {
                insertionPoint = idt.strUnqualifiedID.IndexOf(" $B#");
            }
#if REFINE__
            // UnDecorateSymboleName will not put a blank between operator?? and the following template parameter list,
            // causing confusion. e.g operator<<...> This piece of code adds an extra blank
            char chEnd = idt._strUnqualifiedID[insertionPoint - 1];
            if (!Char.IsLetterOrDigit(chEnd) && chEnd != '_' && chEnd != '#')//# is used in operator replacement
            {   // add blanks for operators, like operator< , etc.
                TplParaList = " " + TplParaList;
            }
#endif
            idt._strUnqualifiedID = idt._strUnqualifiedID.Insert(insertionPoint, TplParaList);
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
            _strUnqualifiedID = _strUnqualifiedID.Replace(s_strCtorDtor, uID._strUnqualifiedID);
            return this;
        }
        public override string getResult() { return _strUnqualifiedID; }
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            ///// TODO:
            int iProcessPos;
            if (_baseName == true)
            {
                iProcessPos = parseBaseName(src, pos, ref vType, ref vUiD);
            }
            else
            {
                iProcessPos = parseClassNamespaceName(src, pos, ref vType, ref vUiD);
            }
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
        }
        private int parseBaseName(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] =='?')
            {
                iProcessPos++;
                if (src[iProcessPos] == '$')
                {
                    iProcessPos++;
                    iProcessPos = getTemplateID(src, iProcessPos, ref vType, ref vUiD);
                }
                else
                {
                    iProcessPos = getOperatorFunctionID(src, iProcessPos, ref vType, ref vUiD);
                }
            }
            else if (src[iProcessPos] >= '0' && src[iProcessPos]<='9')
            {
                _strUnqualifiedID = vUiD[src[iProcessPos]-'0']._strUnqualifiedID;
                // anything from back ref acts just like an identifier, this prevents it is added to backref again as a template ID
                _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
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
                _strUnqualifiedID = StringComponent.getString(src, iProcessPos, out iProcessPos);
                _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
                vUiD.Add(this);
            }
            return iProcessPos;
        }
        private int getTemplateID(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            
            List<UnqualifiedID> vUiDN = new List<UnqualifiedID>();
            List<Type> vTypeN = new List<Type>();
            UnqualifiedID TplName = new UnqualifiedID(true);
            String TplParaList;
            UnqualifiedID TplID;
            iProcessPos = TplName.parse(src, iProcessPos, ref vType, ref vUiDN);

            /// TODO:
            TplParaList = StringComponent.GetTemplateArgumentList(src, ref iProcessPos, ref vTypeN, ref vUiDN);
            if (TplParaList.EndsWith(">"))
            {
                TplParaList += " ";
            }

            TplID = TplName.AppendTplArgLst("<" + TplParaList + ">");
            _strUnqualifiedID = TplID._strUnqualifiedID;
            _eUnqualifiedIdType = TplID._eUnqualifiedIdType;
            return iProcessPos;
        }
        private int getOperatorFunctionID(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            String strOperatorID = "";
            _strUnqualifiedID = null;
            int iProcessPos = pos;
            if (src[iProcessPos] == '0' || src[iProcessPos] == '1')
            {
                _strUnqualifiedID = strOperator[src[iProcessPos] - '0'];
                _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmCtorDtor;
            }
            else if (src[iProcessPos] >= '2' && src[iProcessPos] <= '9')
            {
                strOperatorID = strOperator[src[iProcessPos] - '0'];
            }
            else if (src[iProcessPos] == 'B')
            {
                _strUnqualifiedID= strOperatorC[1];
                _eUnqualifiedIdType= UnqualifiedID.enumUnqualifiedID.enmConversionFuctionID;
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
                            long num = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
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
                    _strUnqualifiedID = strOperatorID;
                    _eUnqualifiedIdType= UnqualifiedID.enumUnqualifiedID.enmRTTISymbols;
                }
                else if (src[iProcessPos] == 'C')
                {
                    _strUnqualifiedID = "$_C#";
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
                            Declaration d = new Declaration();
                            iProcessPos = d.parse(src, iProcessPos, ref vType, ref vUiD);
                            Decl = d.getResult();
                        }
                        else
                        {
                            Decl = StringComponent.getString(src, iProcessPos, out iProcessPos);
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
                            Declaration d = new Declaration();
                            iProcessPos = d.parse(src, iProcessPos, ref vType, ref vUiD);
                            Decl = d.getResult();
                        }
                        else
                        {
                            Decl = StringComponent.getString(src, iProcessPos, out iProcessPos);
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
            if (_strUnqualifiedID == null)
            {
                _strUnqualifiedID = strOperatorID;
                _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmOperatorFunctionID;
            }
            return iProcessPos;
        }
        private int parseClassNamespaceName(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                    long val = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
                    _strUnqualifiedID = "`" + val.ToString() + "\'";
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
                    break;
                case 'A':
                    iProcessPos = parseAnonymousNameSpace(src, iProcessPos);
                    vUiD.Add(this);
                    break;
                case '$':
                    iProcessPos++;
                    iProcessPos = getTemplateID(src, iProcessPos, ref vType, ref vUiD);
                    vUiD.Add(this);
                    break;
                case '?':
                    //iProcessPos++;
                    Declaration d = new Declaration();
                    iProcessPos = d.parse(src, iProcessPos, ref vType, ref vUiD);
                    _strUnqualifiedID = "`" + d.getResult() + "'";
                    _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
                    break;
                default:
                    throw new Exception("unknow character after \'?\' : " + src[iProcessPos]);
                }
                break;
            default:
                _strUnqualifiedID = StringComponent.getString(src, iProcessPos, out iProcessPos);
                vUiD.Add(this);
                _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
                break;
            }
            return iProcessPos;
        }
        private int parseAnonymousNameSpace(string src, int pos)
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
            _strUnqualifiedID = StrAnonymousNamespace;
            _eUnqualifiedIdType = UnqualifiedID.enumUnqualifiedID.enmIdentifier;
            return iProcessPos;
        }
    }

    class TemplateArguamentList : StringComponent
    {
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
            return iProcessPos;
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
                _strNestNameSpecifier = uID.strUnqualifiedID + "::" + _strNestNameSpecifier;
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
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                    iProcessPos = uIDCNSName.parse(src, iProcessPos, ref vType, ref vUiD);
                    if (strNestNameSpecifier == null)
                    {
                        _uidFirstQualifier = uIDCNSName;
                    }
                }
                AddQualifier(uIDCNSName);
            }
            iProcessPos++;
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
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
            _strRes = idt.strUnqualifiedID;
        }
        public QualifiedID(NestNameSpecifier Qualifier, UnqualifiedID uID)
        {
            _strRes = Qualifier.strNestNameSpecifier + uID.strUnqualifiedID;
        }

        private bool _isDecl;
        public QualifiedID(bool isDecl = false)
        {
            _isDecl = isDecl;
        }
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            UnqualifiedID uIDBaseID = new UnqualifiedID(true);
            NestNameSpecifier nnsQualifier;
            int iProcessPos = pos;

            iProcessPos = uIDBaseID.parse(src, iProcessPos, ref vType, ref vUiD);
            if (!_isDecl && uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmTemplateID)
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
                iProcessPos = nnsQualifier.parse(src, iProcessPos, ref vType, ref vUiD);

                if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmCtorDtor)
                {   // base name can be template, so it may just CONTAIN the constructor
                    uIDBaseID.ReplaceCtorDtor(nnsQualifier.uidFirstQualifier);
                }
                _strRes = nnsQualifier.strNestNameSpecifier + uIDBaseID.strUnqualifiedID;
            }
            saveParseStatus(src, pos, iProcessPos);
            return iProcessPos;
        }
    }

    class Declaration : StringComponent
    {
        public override int parse(string src, int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != '?')
            {
                throw new Exception("Declaration should begin with '?'");
            }
            iProcessPos++;
            QualifiedID qID = new QualifiedID(true);
            iProcessPos = qID.parse(src, iProcessPos, ref vType, ref vUiD);
            String sIdent = "";
            if (qID.strQualifiedID == "$_C#")
            {
                sIdent = "`string'";
                iProcessPos = src.Length;
            }
            else if (Char.IsDigit(src[iProcessPos])) // variable
            {
                TypeVarType sType = new TypeVarType();
                iProcessPos = sType.parse(src, iProcessPos, ref vType, ref vUiD);
                String forDst = "";
                if (iProcessPos < src.Length)
                {
                    switch(src[iProcessPos])
                    {
                    case 'A':
                        iProcessPos++;
                        break;
                    default:
                        if (qID.strQualifiedID.EndsWith("::`vftable'") || qID.strQualifiedID.EndsWith("::`vbtable'") || qID.strQualifiedID.EndsWith("::`RTTI Complete Object Locator'"))
                        {
                            if (src[iProcessPos] != '@')
                            {
                                QualifiedID q = new QualifiedID();
                                iProcessPos = q.parse(src, iProcessPos, ref vType, ref vUiD);
                                forDst = q.getResult();
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
                bool bHasThis;
                String sThunkAdjustor;
                FunctionModifier funcM = new FunctionModifier();
                iProcessPos = funcM.parse(src, iProcessPos, ref vType, ref vUiD);
                sModifier = funcM.getResult();//getFunctionModifier(out bHasThis, out sThunkAdjustor);
                if (sModifier.Length > 0)
                {
                    sModifier = sModifier + " ";
                }
                TypeFunctionBase func = new TypeFunctionBase(funcM.bHasThis);
                iProcessPos = func.parse(src, iProcessPos, ref vType, ref vUiD);
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
                            long val1 = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
                            long val2 = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
                            String sModifier = "[thunk]:public: virtual ";
                            TypeFunctionBase func = new TypeFunctionBase(true);
                            iProcessPos = func.parse(src, iProcessPos, ref vType, ref vUiD);
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
                        val = StringComponent.getInteger(src, ref iProcessPos, ref vType, ref vUiD);
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
            return iProcessPos;
        }
    }

    class DeMangel
    {
        #region variables and properties

        private String src;
        public String srcLine
        {
            get { return src; }
        }
        private String dst;

        private Stack<List<Type>> vType;
        private Stack<List<UnqualifiedID>> vUiD;

        private int iProcessPos;
        public int processPos
        {
            get { return iProcessPos; }
        }

        #endregion

        #region enumeration constants
        private static String[] strAControl = {
            "private:", "protected:", "public:"
        };
        private enum enumAControl { enmACprivate, enmACprotected, enmACpublic };

        private static String[] strModifier = {
            "static", "virtual"
        };
        private enum enumModifier { enmMstatic, enmMvirtual };

        private static String[] strCallConv = {
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
        //private enum enumCallConv { enmCCcdecl, enmCCstdcall, enmCCthiscall, enmCCfastcall };

        private static String[] strType = {
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

        private static String[] strTypeE = {
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

        private static String[] strType_ = {
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


        private static String[] strOperator = {
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

        private static String[] strOperator_ = {
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

        private static String[] strOperatorC ={
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

        private static String[] strOperatorC_ ={
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

        private static String[] strOperatorC__ = {
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

        #endregion

        public DeMangel(String source)
        {
            src = source;
            dst = source;
            vType = new Stack<List<Type>>();
            vType.Push(new List<Type>());
            vUiD = new Stack<List<UnqualifiedID>>();
            vUiD.Push(new List<UnqualifiedID>());
        }

        public void Work()
        {
            Declaration d = new Declaration();
            List<Type> vType = new List<Type>();
            List<UnqualifiedID> vUiD = new List<UnqualifiedID>();
            int pos = d.parse(src, 0, ref vType, ref vUiD);
            if (pos < src.Length)
            {
                dst = src;
                throw new Exception("Symbols not exhausted");
            }
            dst = d.getResult();
            //dst = GetDeclaration();
            //if (iProcessPos < src.Length)
            //{
            //    dst = src;
            //    throw new Exception("symbols not exhausted");
            //}
        }

        public String GetDeclaration()
        {
            if (src[iProcessPos] != '?')
            {
                return "";
            }
            iProcessPos++;
            QualifiedID qID = GetQualifiedID(false);
            String sIdent = "";
            if (qID.strQualifiedID == "$_C#")
            {
                sIdent = "`string'";
                iProcessPos = src.Length;
            }
            else if (Char.IsDigit(src[iProcessPos])) // variable
            {
                TypeVarType sType = GetVaraibleType();
                String forDst = "";
                if (iProcessPos < src.Length)
                {
                    switch(src[iProcessPos])
                    {
                    case 'A':
                        iProcessPos++;
                        break;
                    default:
                        if (qID.strQualifiedID.EndsWith("::`vftable'") || qID.strQualifiedID.EndsWith("::`vbtable'") || qID.strQualifiedID.EndsWith("::`RTTI Complete Object Locator'"))
                        {
                            if (src[iProcessPos] != '@')
                            {
                                forDst = GetQualifiedID(true).strQualifiedID;
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
                bool bHasThis;
                String sThunkAdjustor;
                sModifier = getFunctionModifier(out bHasThis, out sThunkAdjustor);
                if (sModifier.Length > 0)
                {
                    sModifier = sModifier + " ";
                }
                String sFuncBody = GetFunctionBody(bHasThis).getDeclaration(qID.strQualifiedID + sThunkAdjustor);
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
                            long val1 = GetInteger();
                            long val2 = GetInteger();
                            String sModifier = "[thunk]:public: virtual ";
                            TypeFunctionBase func = GetFunctionBody(true);
                            String sFuncBody = func.getDeclaration(qID.strQualifiedID + "`vtordisp{" + val1 + "," + val2 + "}' ");
                            sIdent = sModifier + sFuncBody;
                            break;
                        }
                    case 'A':
                        iProcessPos++;
                        Type sRetType = GetReturnType();
                        if (src[iProcessPos] != 'A')
                        {
                            throw new Exception();
                        }
                        sIdent = "[thunk]:" + sRetType.getDeclaration(qID.strQualifiedID + "`local static destructor helper\'");
                        iProcessPos++;
                        break;
                    case 'B':
                        iProcessPos++;
                        val = GetInteger();
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
            return sIdent;
        }

        public String GetResult()
        {
            return dst;
        }

        private UnqualifiedID GetTemplateID(bool Push)
        {
            UnqualifiedID TplName;
            String TplParaList;
            UnqualifiedID TplID;

            vUiD.Push(new List<UnqualifiedID>());
            TplName = GetBaseName(Push);
            vType.Push(new List<Type>());

            TplParaList = GetTemplateArgumentList();
            if (TplParaList.EndsWith(">"))
            {
                TplParaList += " ";
            }

            vUiD.Pop();
            vType.Pop();

            TplID = TplName.AppendTplArgLst("<" + TplParaList + ">");

            if (Push && TplID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmTemplateID)
            {
                vUiD.Peek().Add(TplID);
            }

            return TplID;
        }

        private UnqualifiedID GetBaseName(bool Push)
        {
            UnqualifiedID identRet = new UnqualifiedID();
            switch (src[iProcessPos])
            {
            case '?':
                iProcessPos++;
                if (src[iProcessPos] == '$')
                {
                    iProcessPos++;
                    identRet = GetTemplateID(Push);
                }
                else
                {
                    identRet = GetOperatorFunctionID();
                }
                break;
            default:
                identRet = GetIdentifier();
                break;
            }
            return identRet;
        }

        private UnqualifiedID GetOperatorFunctionID()
        {
            String strOperatorID = "";
            UnqualifiedID operatorFunctionID = null;
            if (src[iProcessPos] == '0' || src[iProcessPos] == '1')
            {
                operatorFunctionID = new UnqualifiedID(strOperator[src[iProcessPos] - '0'], UnqualifiedID.enumUnqualifiedID.enmCtorDtor);
            }
            else if (src[iProcessPos] >= '2' && src[iProcessPos] <= '9')
            {
                strOperatorID = strOperator[src[iProcessPos] - '0'];
            }
            else if (src[iProcessPos] == 'B')
            {
                operatorFunctionID = new UnqualifiedID(strOperatorC[1], UnqualifiedID.enumUnqualifiedID.enmConversionFuctionID);
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
                        Type strClassType = GetTypeLikeID(false);
                        strOperatorID = strClassType.getDeclaration("") + " `RTTI Type Descriptor'";
                        break;
                    case '1':
                        int[] iNum = new int[4];
                        iProcessPos++;
                        for (int i = 0; i < 4; i++)
                        {
                            iNum[i] = (int)GetInteger();
                        }
                        strOperatorID = "`RTTI Base Class Descriptor at (" + iNum[0] + "," + iNum[1] + "," + iNum[2] + "," + iNum[3] + ")\'";
                        break;
                    case '2':
                        iProcessPos++;
                        strOperatorID = "`RTTI Base Class Array\'";
                        break;
                    case '3':
                        iProcessPos++;
                        strOperatorID = "`RTTI Class Hierarchy Descriptor\'";
                        break;
                    case '4':
                        iProcessPos++;
                        strOperatorID = "`RTTI Complete Object Locator\'";
                        break;
                    default:
                        throw new Exception("Invalid RTTI descriptor");
                    }
                    return new UnqualifiedID(strOperatorID, UnqualifiedID.enumUnqualifiedID.enmRTTISymbols);
                }
                else if (src[iProcessPos] == 'C')
                {
                    operatorFunctionID = new UnqualifiedID("$_C#", UnqualifiedID.enumUnqualifiedID.enmString);
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
                            Decl = GetDeclaration();
                        }
                        else
                        {
                            Decl = GetIdentifier().strUnqualifiedID;
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
                            Decl = GetDeclaration();
                        }
                        else
                        {
                            Decl = GetIdentifier().strUnqualifiedID;
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
            if (operatorFunctionID == null)
            {
                operatorFunctionID = new UnqualifiedID(strOperatorID, UnqualifiedID.enumUnqualifiedID.enmOperatorFunctionID);
            }
            return operatorFunctionID;
        }
        /// <summary>
        /// Get template parameter list
        /// may contain type, reference, integer...
        /// </summary>
        /// <returns>list string</returns>
        private String GetTemplateArgumentList()
        {
            StringBuilder PList = new StringBuilder();
            String strTplParaType;
            //int iTmp;
            do
            {   // The existance of empty pack expansion "$$$V" "<>" made thing complicate.
                // It can appear anywhere in the argument list, as the paremater can contain multiple pack expansions,
                // e.g. template<class... _Types1, class... _Types2> inline pair<_Ty1, _Ty2>::pair(piecewise_construct_t, tuple<_Types1...> _Val1, tuple<_Types2...> _Val2)
                strTplParaType = GetTypeLikeID(true).getDeclaration("");
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
            return PList.ToString();
        }

        private long GetInteger()
        {
            bool minus = false;
            long val;
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
            return val;
        }

        private QualifiedID GetQualifiedID(bool Push)
        {
            UnqualifiedID uIDBaseID;
            NestNameSpecifier nnsQualifier;
            UnqualifiedID uIDFisrtQualifier;

            uIDBaseID = GetBaseName(Push);

            if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmString)
            {
                return new QualifiedID(uIDBaseID);
            }

            nnsQualifier = GetNestedNameSpecifier(out uIDFisrtQualifier);

            if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmCtorDtor)
            {   // base name can be template, so it may just CONTAIN the constructor
                uIDBaseID.ReplaceCtorDtor(uIDFisrtQualifier);
            }

            return new QualifiedID(nnsQualifier, uIDBaseID);
        }

        /// <summary>
        /// Get qualifiers. The first one is returned through strFirstQualifier, for use of
        /// constructors and destructors
        /// </summary>
        /// <param name="strFirstQualifier"></param>
        /// <returns></returns>
        private NestNameSpecifier GetNestedNameSpecifier(out UnqualifiedID uIDFirstQualifier)
        {
            NestNameSpecifier NNS = new NestNameSpecifier();
            uIDFirstQualifier = new UnqualifiedID();
            while (src[iProcessPos] != '@')
            {
                UnqualifiedID uIDCNSName = GetClassNamespaceName();
                if (NNS.strNestNameSpecifier == "")
                {
                    uIDFirstQualifier = uIDCNSName;
                }
                NNS.AddQualifier(uIDCNSName);
            }
            iProcessPos++;
            return NNS;
        }

        private NestNameSpecifier GetNestedNameSpecifier()
        {
            UnqualifiedID uIDDummy;
            return GetNestedNameSpecifier(out uIDDummy);
        }

        private UnqualifiedID GetClassNamespaceName()
        {
            UnqualifiedID uID;
            switch (src[iProcessPos])
            {
            case '?':
                iProcessPos++;
                switch (src[iProcessPos])
                {
                case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8': case '9':
                case 'B':case 'C':case 'D':case 'E':case 'F':case 'G':case 'H':case 'I':case 'J':
                case 'K':case 'L':case 'M':case 'N':case 'O':case 'P':
                    long val = GetInteger();
                    uID = new UnqualifiedID("`" + val.ToString() + "\'", UnqualifiedID.enumUnqualifiedID.enmIdentifier);
                    break;
                case 'A':
                    uID = GetAnonymousNameSpace();
                    break;
                case '$':
                    iProcessPos++;
                    uID = GetTemplateID(true);
                    break;
                case '?':
                    //iProcessPos++;
                    uID = new UnqualifiedID("`" + GetDeclaration() + "'", UnqualifiedID.enumUnqualifiedID.enmIdentifier);
                    break;
                default:
                    throw new Exception("unknow character after \'?\' : " + src[iProcessPos]);
                }
                break;
            default:
                uID = GetIdentifier();
                break;
            }
            return uID;
        }

        private UnqualifiedID GetAnonymousNameSpace()
        {
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
            UnqualifiedID uIDAnonymousNamespace = new UnqualifiedID(StrAnonymousNamespace, UnqualifiedID.enumUnqualifiedID.enmIdentifier);
            vUiD.Peek().Add(uIDAnonymousNamespace);
            return uIDAnonymousNamespace;
        }

        private UnqualifiedID GetIdentifier()
        {
            UnqualifiedID retID;
            if (Char.IsDigit(src[iProcessPos]))
            {
                retID = vUiD.Peek()[src[iProcessPos] - '0'];
                iProcessPos += 1;
            }
            else
            {
                int i = 0;
                while (src[iProcessPos + i] != '@')
                {
                    i++;
                }
                retID = new UnqualifiedID(src.Substring(iProcessPos, i), UnqualifiedID.enumUnqualifiedID.enmIdentifier);
                vUiD.Peek().Add(retID);
                if (i != 0)
                {
                    iProcessPos += i + 1;
                }
            }
            return retID;
        }

        private String getFunctionModifier(out bool bHasThis, out String sThunkAdjustor)
        {
            String sModifier;
            bHasThis = true;
            //String tAccess = new TypeID();
            String tAccess = "";
            String sThunkPrefix = "";
            sModifier = "";
            sThunkAdjustor = "";
            if (src[iProcessPos] < 'Y')
            {
                tAccess = strAControl[(src[iProcessPos] - 'A') / 8];
            }
            switch (src[iProcessPos])
            {
            case 'A':case 'I':case 'Q':
                break;
            case 'C':case 'S':case 'K':
                bHasThis = false;
                sModifier = strModifier[(int)enumModifier.enmMstatic];
                break;
            case 'E':case 'M':case 'U':
                sModifier = strModifier[(int)enumModifier.enmMvirtual];
                break;
            case 'G':case 'O':case 'W':
                iProcessPos++;
                long lAdjustor = GetInteger();
                sThunkPrefix = "[thunk]:";
                sThunkAdjustor = "`adjustor{" + lAdjustor.ToString() + "}\' ";
                sModifier = strModifier[(int)enumModifier.enmMvirtual];
                iProcessPos--;
                break;
            case 'Y':
                bHasThis = false;
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
            sModifier = sThunkPrefix + tAccess + sModifier;
            return sModifier;
        }

        private String GetCallConv()
        {
            String conv;
            if (src[iProcessPos] >= 'A' && src[iProcessPos] <= 'M')
            {
                conv = strCallConv[src[iProcessPos] - 'A'];
            }
            else
            {
                throw new Exception("Calling conversion " + src[iProcessPos] + " not known");
            }
            iProcessPos++;
            return conv;
        }

        private Type GetReturnType()
        {
            Type ReturnType;
            if (src[iProcessPos] == '@')
            {
                iProcessPos++;
                ReturnType = new TypeSimple("");
            }
            else
            {
                ReturnType = GetTypeLikeID(false);
            }
            //ReturnType.RemoveCVQualifier();
            return ReturnType;
        }
        /// <summary>
        /// Get a type
        /// </summary>
        /// <param name="Push">whether to push the type into the type stack</param>
        /// <param name="iInsPos">if a variable is defined as the type, where should the variable be</param>
        /// <returns>Type string</returns>
        private Type GetTypeLikeID(bool Push)
        {
            Type retType;
            switch (src[iProcessPos])
            {
            case 'C':case 'D':case 'E':case 'F':case 'G':case 'H':
            case 'I':case 'J':case 'K':case 'M':case 'N':case 'O':case 'X':
                retType = new TypeSimple(strType[src[iProcessPos] - 'A']);
                iProcessPos++;
                break;
            case '?':
                retType = GetSpecialRef(Push);
                break;
            case 'A':case 'B':
            case 'P':case 'Q':case 'R':case 'S':
                retType = GetReferenceType(Push);
                break;
            case 'T':case 'U':case 'V':case 'W':
                retType = GetCompoundType(Push);
                break;
            case 'Y':
                retType = GetArrayType(Push, true);
                break;
            case '0':case '1':case '2':case '3':case '4':
            case '5':case '6':case '7':case '8':case '9':
                retType = vType.Peek()[src[iProcessPos] - '0'];
                iProcessPos++;
                break;
            case '_':
                iProcessPos++;
                switch (src[iProcessPos])
                {
                case 'D':case 'E':case 'F':case 'G':case 'H':case 'I':
                case 'J':case 'K':case 'L':case 'M':case 'N':case 'W':
                    retType = new TypeSimple(strType_[src[iProcessPos] - 'A']);
                    iProcessPos++;
                    break;
                default:
                    throw new Exception("Type specific letter _" + new String(src[iProcessPos], 1) + " not found");
                }
                if (Push)
                {
                    vType.Peek().Add(retType);
                }
                break;
            case '$': // special type or type like identifier
                iProcessPos++;

                switch (src[iProcessPos])
                {
                case '0': // integer
                    iProcessPos++;
                    long val = GetInteger();
                    //retPara = val.ToString();
                    retType = new TypeSimple(val.ToString());
                    break;
                case '1': // pointer
                    QualifiedID qID;
                    Type vTypeID;
                    iProcessPos++;
                    if (src[iProcessPos] != '?')
                    {
                        throw (new Exception("\'?\' expected in reference parameter"));
                    }
                    iProcessPos++;
                    qID = GetQualifiedID(true);
                    vTypeID = GetVaraibleType();
                    //retPara = "&" + new Declarator(vTypeID, qID).strDeclarator;//vTypeID.Insert(qID).strType;
                    retType = new TypeSimple("&" + vTypeID.getDeclaration(qID.strQualifiedID));
                    break;
                case 'S':
                    retType = new TypeSimple(""); // Empty expansion list for integral types/size_t
                    iProcessPos++;
                    break;
                case '$': // Simple ref, will use the letter C,
                    iProcessPos++;
                    if (src[iProcessPos] == '$' && src[iProcessPos + 1] == 'V')
                    {   // "$$$V@" for empty template parameter list
                        retType = new TypeSimple("");
                        iProcessPos += 2;
                    }
                    else
                    {
                        retType = GetSpecialRef(true);
                    }
                    break;
                default:
                    throw new Exception();
                }
                break;
            default:
                throw new Exception("Type specific letter " + new String(src[iProcessPos], 1) + " not found");
            }
            return retType;
        }

        private TypeClass GetCompoundType(bool bPush)
        {
            char cType = src[iProcessPos];
            //String subName = "";
            //String retType = "";
            String infixEnum = "";
            TypeClass sType = new TypeClass();// = new TypeID();
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
                sType.ClassKey = StringHelper.glue(strType[cType - 'A'], infixEnum);
                sType.StrClassQualifiedName = GetQualifiedID(true).strQualifiedID;
                //sType = new TypeID(strType[cType - 'A'] + " " + infixEnum + GetQualifiedID(true).strQualifiedID, -1, true, false);
                if (bPush)
                {
                    vType.Peek().Add(sType);
                }
                break;
            default:
                throw new Exception("illegal compound : " + cType);
            }
            return sType;
        }

        private TypeReference GetSpecialRef(bool bPush)
        {
            TypeReference sType = new TypeReference();

            switch (src[iProcessPos])
            {
            case 'A': // function pointers
                iProcessPos++;
                bool bHasThis;
                sType.StrCVQualifier = GetCVQFunc(out bHasThis);
                sType.TypeReferenced = GetFunctionBody(bHasThis);
                break;
            case 'B': // in parameter, array, no CV qualifier
                iProcessPos++;
                sType.TypeReferenced = GetTypeLikeID(false);
                break;
            case 'C': // in template paramter list
            case '?': // type transfered by value
                iProcessPos++;
                sType.StrCVQualifier = GetCVQVar();
                sType.TypeReferenced = GetTypeLikeID(false);
                break;
            case 'Q':
                iProcessPos++;
                sType.StrReferenceType = "&&";
                sType.StrCVQualifier = GetCVQVar();
                sType.TypeReferenced = GetTypeLikeID(false);
                break;
            default:
                throw new Exception("illegal Special Ref : " + src[iProcessPos]);
            }
            if (bPush)
            {
                vType.Peek().Add(sType);
            }
            return sType;
        }

        private TypeReference GetReferenceType(bool bPush)
        {
            //String suffix = "";
            //String CVQ = "";
            //Type subType;
            TypeReference sType = new TypeReference();
            bool bHasThis;
            //int iInsPos;

            switch (src[iProcessPos])
            {
            case 'A':case 'B':
            case 'P':case 'Q':case 'R':case 'S':
                if (src[iProcessPos + 1] == 'E')
                {
                    sType.StrReferenceType = strTypeE[src[iProcessPos] - 'A'];
                    iProcessPos++;
                }
                else
                {
                    sType.StrReferenceType = strType[src[iProcessPos] - 'A'];
                }
                break;
            default:
                throw new Exception("Illegal leading char in Ref and Pointer Type : " + src[iProcessPos]);
            }
            iProcessPos++;
            if (Char.IsDigit(src[iProcessPos]))
            {
                sType.StrCVQualifier = GetCVQFunc(out bHasThis);
                sType.TypeReferenced = GetFunctionBody(bHasThis);
            }
            else
            {
                sType.StrCVQualifier = GetCVQVar();
                sType.TypeReferenced = GetTypeLikeID(false);
            }
            if (bPush)
            {
                vType.Peek().Add(sType);
            }
            return sType;
        }

        private Type GetArrayType(bool bPush, bool bEnclose)
        {
            if (src[iProcessPos] != 'Y')
            {
                throw new Exception("Error Array Identifier");
            }
            iProcessPos++;
            //String sub = "";
            TypeArray retType = new TypeArray();
            retType.StrSubcript = "";

            long Dimension = GetInteger();
            for (long i = 0; i < Dimension; i++)
            {
                retType.StrSubcript = retType.StrSubcript + "[" + GetInteger() + "]";
            }
            retType.BaseType = GetTypeLikeID(false);


            //Type = subType.Insert(sub, 0, false, true);
            //if (bPush)
            //{
            //    vType.Peek().Add(Type);
            //}
            return retType;
        }

        private TypeReference GetFunctionPointer(bool bMemThis/*, CVQualifier strCVQalifier, String strPointer*/)
        {
            TypeReference retType = new TypeReference();
            retType.StrReferenceType = "*";
            //String sFuncBody;
            //String sFuncName;
            //sFuncName = ""; //strCVQalifier.makeID("@");
            //int iInsPos;
            retType.TypeReferenced = GetFunctionBody(bMemThis);
            //iInsPos = sFuncBody.IndexOf("@");
            //sFuncBody = sFuncBody.Replace("@", strPointer);
            //iInsPos++;
            return retType;//new TypeID(sFuncBody,iInsPos,true,false);
        }

        private TypeFunctionBase GetFunctionBody(bool bHasThis)
        {
            TypeFunctionBase retType = new TypeFunctionBase();

            if (bHasThis)
            {
                if (src[iProcessPos] == 'E')
                {
                    retType.StrCVQThis = "__ptr64";
                    //sCVQThis.setSuffix("__ptr64");
                    iProcessPos++;
                }
                //sCVQThis.setPrefix(GetCVQVar().getPrefix());
                retType.StrCVQThis = GetCVQVar() + retType.StrCVQThis;
            }
            retType.StrCallConversion = GetCallConv();
            retType.TypeReturn = GetReturnType();
            retType.StrParamList = "(" + GetParameterDeclarationClause() + ")";
            retType.StrExceptionList = GetParameterDeclarationClause();

            if (retType.StrExceptionList == "...")
            {
                retType.StrExceptionList = "";
            }
            else
            {
                retType.StrExceptionList = " throw(" + retType.StrExceptionList + ")";
            }
            return retType;
        }
        private String GetCVQVar()
        {
            String CVQ;
            switch (src[iProcessPos])
            {
            case 'A':
                iProcessPos++;
                CVQ = "";
                break;
            case 'B':
                iProcessPos++;
                CVQ = "const";
                break;
            case 'C':
                iProcessPos++;
                CVQ = "volatile";
                break;
            case 'D':
                iProcessPos++;
                CVQ = "const volatile";
                break;
            case 'E':
                iProcessPos++;
                CVQ = "__ptr64";
                break;
            case 'Q':
                iProcessPos++;
                CVQ = GetNestedNameSpecifier().strNestNameSpecifier;
                break;
            default:
                throw new Exception("Unrecognized CV-Qualifier " + src[iProcessPos]);
            }

            return CVQ;
        }
        private String GetCVQFunc(out bool bMemThis)
        {
            String CVQ = "";
            //bFunc = false;
            bMemThis = false;
            switch (src[iProcessPos])
            {
            case '6':
                CVQ = "";
                break;
            case '8':
                iProcessPos++;
                CVQ = GetNestedNameSpecifier().strNestNameSpecifier;
                bMemThis = true;
                iProcessPos--;
                break;
            default:
                throw new Exception("Unrecognized CV-Qualifier " + new String(src[iProcessPos], 1));
            }
            iProcessPos++;
            return CVQ;
        }
        private String GetParameterDeclarationClause()
        {
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
                strParamType = GetTypeLikeID(true);
                PList.Append(strParamType.getDeclaration(""));
                if (strParamType.getDeclaration("") != "void")
                {
                    while (src[iProcessPos] != '@' && src[iProcessPos] != 'Z')
                    {
                        strParamType = GetTypeLikeID(true);
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
            return PList.ToString();
        }

        private TypeVarType GetVaraibleType()
        {
            TypeVarType retType = new TypeVarType();
            Type BaseType = new TypeSimple("");
            int iSpecialVariable = 0;
            switch (src[iProcessPos])
            {
            case '0':
                retType.StrAccess = "private: static ";
                break;
            case '1':
                retType.StrAccess = "protected: static ";
                break;
            case '2':
                retType.StrAccess = "public: static ";
                break;
            case '3':
                retType.StrAccess = "";
                break;
            case '4': // function scope static variable
                retType.StrAccess = "";
                break;
            case '5':
                retType.StrAccess = "";
                iSpecialVariable = 5;
                break;
            case '6': // `vftable'
                retType.StrAccess = "";
                iSpecialVariable = 6;
                break;
            case '7': // `vbtable'
                retType.StrAccess = "";
                iSpecialVariable = 7;
                break;
            case '8':
                retType.StrAccess = "";
                iSpecialVariable = 8;
                break;
            case '9':
                retType.StrAccess = "";
                iSpecialVariable = 9;
                break;
            default:
                throw new Exception("Illegal Variable Acess modifier: " + src[iProcessPos]);
            //break;
            }
            iProcessPos++;

            if (iSpecialVariable < 5)
            {
                BaseType = GetTypeLikeID(false);
            }

            if (iSpecialVariable != 9 && iSpecialVariable != 8 && iSpecialVariable != 5)
            {
                retType.StrCVQualifier = GetCVQVar();
            }

            if (iSpecialVariable == 5)
            {
                long l = GetInteger();
                BaseType = new TypeSpecial("{" + l + "}'");///, 0, false, false);
            }

            retType.InnerType = BaseType;
            return retType;
        }
    }
}
