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
        protected void dump() { Console.Error.WriteLine("{0} {1} {2} . {3} . {4}", _start, _end, GetType().Name, _str, getDemangledString()); }
        /// <summary>
        /// Get the demangled result of this piece
        /// </summary>
        /// <returns></returns>
        virtual public string getDemangledString() { throw new NotImplementedException(); }

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
        public override string getDemangledString() { return _strRes; }
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
            return new TemplateArguamentList().parse(src, ref pos, ref vType, ref vUiD).getDemangledString();
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
            "char16_t",
            "_T",
            "char32_t",
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
        virtual public String getDeclaration(String qID, bool bEnclose = false)
        {
            return StringHelper.glue(getTypeString(), qID);
        }
        virtual public void ajdustCVQ() { _strCVQualifier = ""; }
        public override string getDemangledString() { return getDeclaration("###"); }
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
                    Console.Error.WriteLine("{0} , {1}", i, vType[i].getDemangledString());
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
                        case 'S':
                        case 'U':
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
                        case '1': // pointer or l-value reference non-type template argument
                            // a full Declaration follows;
                            iProcessPos++;
                            Declaration Decl = new Declaration();
                            Decl.parse(src, ref iProcessPos, ref vType, ref vUiD);
                            retType = new TypeNonType("&" + Decl.QualifiedID.getDemangledString() + " /* " + Decl.getDemangledString() + " */");
                            break;
                        case 'S':
                            retType = new TypeNonType(""); // Empty expansion list for integral types/size_t
                            iProcessPos++;
                            break;
                        case '$':
                            iProcessPos++;
                            if (src[iProcessPos] == '$' && src[iProcessPos + 1] == 'V')
                            {   // "$$V" for empty template parameter list
                                retType = new TypeNonType("");
                                iProcessPos += 2;
                            }
                            else if (src[iProcessPos] == 'V')
                            { // empty template parameter expanstion pack
                                retType = new TypeNonType("");
                                iProcessPos++;
                            }
                            else if (src[iProcessPos] == 'T')
                            {
                                retType = new TypeSimple("std::nullptr_t");
                                iProcessPos++;
                            }
                            else if (src[iProcessPos] == 'Z')
                            { // separator between two empty template parameter expanstion packs
                                retType = new TypeNonType("");
                                iProcessPos++;
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
        private static readonly String[] str_cv =
        {
            "",
            "const",
            "volatile",
            "const volatile"
        };
        private bool _bMemThis = false;
        private bool _bIgnoreNormalCV = false;
        public bool bMemThis
        {
            get { return _bMemThis; }
        }
        public CVQ IgnoreNormalCV(bool ignore)
        {
            _bIgnoreNormalCV = ignore;
            return this;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _bMemThis = false;
            string suffix = "";
            switch (src[iProcessPos])
            {
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'G':
                case 'H':
                    if (src[iProcessPos] == 'E')
                    {
                        suffix = "__ptr64";
                        iProcessPos++;
                    }
                    else if (src[iProcessPos] == 'G')
                    {
                        suffix = "&";
                        iProcessPos++;
                    }
                    else if (src[iProcessPos] == 'H')
                    {
                        suffix = "&&";
                        iProcessPos++;
                    }
                    switch (src[iProcessPos])
                    {
                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                            _strRes = str_cv[src[iProcessPos] - 'A'];
                            break;
                        default:
                            throw new Exception("Unrecognized CV-Qualifier " + src[iProcessPos]);
                    }
                    if (_bIgnoreNormalCV && suffix == "")
                    {
                        _strRes = "";
                    }
                    else
                    {
                        _strRes += suffix;
                    }
                    break;
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                    _strRes = str_cv[src[iProcessPos] - 'Q'];
                    iProcessPos++;
                    _strRes = StringHelper.glue(_strRes, new NestNameSpecifier().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString());
                    iProcessPos--;
                    if (_bIgnoreNormalCV)
                    {
                        _strRes = "";
                    }
                    break;
                case '6':
                    _strRes = "";
                    break;
                case '8':
                    iProcessPos++;
                    _strRes = new NestNameSpecifier().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
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
        private Type _typeReferenced;
        public override String getTypeString()
        {
            return getDeclaration("");
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
#if REFINE__
                strDecl = StrCVQualifier + StringHelper.glue(_strReferenceType, qID);
#else
                strDecl = StrCVQualifier + StringHelper.glue(_strReferenceType == "* const" ? "*" : _strReferenceType, qID);
#endif
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
                    strDecl = StringHelper.glue(StrCVQualifier, StringHelper.glue(_strReferenceType == "* const" && qID.StartsWith("(") ? "*" : _strReferenceType, qID));
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
                        _strReferenceType = strTypeE[src[iProcessPos] - 'A'];
                        iProcessPos++;
                    }
                    else
                    {
                        _strReferenceType = strType[src[iProcessPos] - 'A'];
                    }
                    if (src[iProcessPos + 1] == '$' && src[iProcessPos + 2] == 'A')
                    {
                        _strReferenceType = "^" + _strReferenceType.Substring(1);
                        iProcessPos += 2;
                    }
                    break;
                case '?': // type transfered by value
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
                        case 'A': // function. (not pointer or reference to function, but a function)
                                  // can appear in template argument
                            break;
                        case 'B': // array, no CV qualifier (can appear in template argument)
                            noCV = true;
                            break;
                        case 'C': // used when a simple type need a cvq, like array element, template parameter, etc. only complicate type has a place to encode a cvq.
                            break;
                        case 'Q':
                            _strReferenceType = "&&";
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
                _strCVQualifier = cvq.parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
                _typeReferenced = new TypeFunctionBase(cvq.bMemThis).parse(src, ref iProcessPos, ref vType, ref vUiD).toType();
            }
            else
            {
                if (!noCV)
                {
                    _strCVQualifier = cvq.parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
                }
                _typeReferenced = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeClass : Type
    {
        private String _strClassQualifiedName;
        private String _classKey;
        public override string getTypeString()
        {
            return _classKey + " " + _strClassQualifiedName;
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
                    _classKey = StringHelper.glue(strType[cType - 'A'], infixEnum);
                    _strClassQualifiedName = new QualifiedID().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
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
        private Type _baseType;
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
            _strSubcript = "";

            long Dimension = StringComponent.getInteger(src, ref iProcessPos);
            for (long i = 0; i < Dimension; i++)
            {
                // a dimension of 0 is used of array of unknown bound
                // may appear in template argument
                long dim = StringComponent.getInteger(src, ref iProcessPos);
                string s_dim = "";
                if (dim > 0)
                {
                    s_dim = dim.ToString();
                }
                _strSubcript = _strSubcript + "[" + s_dim + "]";
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
        private Type _baseType;
        public override string getTypeString()
        {
            return getDeclaration("");
        }
        public override string getDeclaration(string qID, bool hasBrackt = false)
        {
            return _strAccess + _baseType.getDeclaration(StringHelper.glue(StrCVQualifier.EndsWith("::") ? "" : StrCVQualifier, qID));
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _baseType = new TypeSimple("");
            int iSpecialVariable = 0;
            switch (src[iProcessPos])
            {
                case '0':
                    _strAccess = "private: static ";
                    break;
                case '1':
                    _strAccess = "protected: static ";
                    break;
                case '2':
                    _strAccess = "public: static ";
                    break;
                case '3':
                    _strAccess = "";
                    break;
                case '4': // function scope static variable
                    _strAccess = "";
                    break;
                case '5':
                    _strAccess = "";
                    iSpecialVariable = 5;
                    break;
                case '6': // `vftable'
                    _strAccess = "";
                    iSpecialVariable = 6;
                    break;
                case '7': // `vbtable'
                    _strAccess = "";
                    iSpecialVariable = 7;
                    break;
                case '8':
                    _strAccess = "";
                    iSpecialVariable = 8;
                    break;
                case '9':
                    _strAccess = "";
                    iSpecialVariable = 9;
                    break;
                default:
                    throw new Exception("Illegal Variable Acess modifier: " + src[iProcessPos]);
                    //break;
            }
            iProcessPos++;

            if (iSpecialVariable < 5)
            {
                _baseType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }

            if (iSpecialVariable != 9 && iSpecialVariable != 8 && iSpecialVariable != 5)
            {
#if REFINE__
                // Top level CV for reference is stored with kind of reference
                // Final cv should be ignored.
                // Result for UnDecorateSymbolName is incorrect, in which the
                // final CV is used but the cv stored in the reference kind is
                // ignored.
                // Also applies to the `C::` part in pointer to member `int C::* p`
                // The result without REFINE__ is not the same as UnDecorateSymbolName
                CVQ cvq = new CVQ();
                if (_baseType is TypeReference)
                {
                    cvq.IgnoreNormalCV(true);
                }
                _strCVQualifier = cvq.parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
#else
                _strCVQualifier = (new CVQ()).parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
#endif
            }

            if (iSpecialVariable == 5)
            {
                _baseType = new TypeSuffix("{" + StringComponent.getInteger(src, ref iProcessPos).ToString() + "}'");///, 0, false, false);
            }

            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class TypeFunctionBase : Type
    {
        protected static readonly String[] strCallConv = {
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
            "__clrcall",
            "",
            "",
            "",
            "__vectorcall"
        };
        private String _strCallConversion;
        private String _strParamList;
        private String _strCVQThis = "";
        private String _strExceptionList;
        private Type _typeReturn;
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
                    _strCVQThis = "__ptr64";
                    iProcessPos++;
                }
                if (src[iProcessPos] == '$' && src[iProcessPos+1] =='A')
                {
                    // `this` is always ^ on ref class for WinRT, so it is not shown
                    // just likt `this` is always * for normal classes, but * is not shown at the end of the function
                    iProcessPos+=2;
                }
                _strCVQThis = new CVQ().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString() + _strCVQThis;
            }
            _strCallConversion = strCallConv[src[iProcessPos] - 'A'];
            iProcessPos++;
            if (src[iProcessPos] == '@')
            {
                iProcessPos++;
                _typeReturn = new TypeSimple("");
            }
            else
            {
                _typeReturn = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD);
            }
            ParameterClause p = new ParameterClause();
            _strParamList = "(" + p.parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString() + ")";
            _strExceptionList = p.parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();

            if (_strExceptionList == "...")
            {
                _strExceptionList = "";
            }
            else
            {
                _strExceptionList = " throw(" + _strExceptionList + ")";
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
            "`vector vbase copy constructor iterator'",
            "`managed vector vbase copy constructor`",
            "__J",
            "__K",
            "operator co_await",
            "operator <=>",
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
                    parseTemplateID(src, ref iProcessPos, ref vType, ref vUiD);
                }
                else if (iProcessPos == 2 && src.IndexOf("$initializer$") != -1) // special cases, for $initializer$
                {
                    iProcessPos--;
                    _strRes = new Declaration().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
                    _strRes = "`" + _strRes + "'";
                    _eUnqualifiedIdType = enumUnqualifiedID.enmIdentifier;
                    if (src[iProcessPos] != '@')
                    {
                        throw new Exception("Error $initializer$ sequence");
                    }
                    iProcessPos++;
                }
                else
                {
                    parseOperatorFunctionID(src, ref iProcessPos, ref vType, ref vUiD);
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
                    Console.Error.WriteLine("{0} : {1}", i, vUiD[i].getDemangledString());
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
        private void parseTemplateID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;

            List<UnqualifiedID> vUiDN = new List<UnqualifiedID>();
            List<Type> vTypeN = new List<Type>();
            UnqualifiedID TplName = new UnqualifiedID(true);
            TplName.parse(src, ref iProcessPos, ref vType, ref vUiDN);

            String TplParaList = (new TemplateArguamentList()).parse(src, ref iProcessPos, ref vTypeN, ref vUiDN).getDemangledString();
            if (TplParaList.EndsWith(">"))
            {
                TplParaList += " ";
            }

            UnqualifiedID TplID = TplName.AppendTplArgLst("<" + TplParaList + ">");
            _strRes = TplID._strRes;
            _eUnqualifiedIdType = TplID._eUnqualifiedIdType;
            pos = iProcessPos;
        }
        private void parseOperatorFunctionID(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
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
                    if (src[iProcessPos] == 'L' || src[iProcessPos] == 'M')
                    {
                        strOperatorID = strOperatorC__[src[iProcessPos] - 'A'];
                    }
                    else if (src[iProcessPos] == 'E')
                    {
                        iProcessPos++;
                        String Decl = "";
                        if (src[iProcessPos] == '?')
                        {
                            Decl = new Declaration().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
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
                            Decl = new Declaration().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
                        }
                        else
                        {
                            Decl = StringComponent.getString(src, ref iProcessPos);
                            iProcessPos--;
                        }
                        strOperatorID = "`dynamic atexit destructor for '" + Decl + "''";
                    }
                    else if (src[iProcessPos] == 'K')
                    {
                        iProcessPos++;
                        String name = StringComponent.getString(src, ref iProcessPos);
                        strOperatorID = "operator \"\" " + name;
                        iProcessPos--;
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
                            parseTemplateID(src, ref iProcessPos, ref vType, ref vUiD);
                            vUiD.Add(this);
                            break;
                        case '?':
                            _strRes = "`" + (new Declaration()).parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString() + "'";
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

    class NestNameSpecifier : StringComponent
    {
        UnqualifiedID _uidFirstQualifier;
        public UnqualifiedID uidFirstQualifier
        {
            get { return _uidFirstQualifier; }
        }

        public NestNameSpecifier AddQualifier(UnqualifiedID uID)
        {
            if (uID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmIdentifier || uID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmTemplateID)
            {
                _strRes = uID.getDemangledString() + "::" + _strRes;
            }
            else
            {
                throw new Exception("illegal qualifier");
            }
            return this;
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            _strRes = null;
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
                        Console.Error.WriteLine("{0} : {1}", i, vUiD[i].getDemangledString());
                    }
#endif
                }
                else
                {
                    uIDCNSName = new UnqualifiedID(false);
                    uIDCNSName.parse(src, ref iProcessPos, ref vType, ref vUiD);
                    if (_strRes == null)
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
        public QualifiedID(UnqualifiedID idt)
        {
            _strRes = idt.getDemangledString();
        }
        public QualifiedID(NestNameSpecifier Qualifier, UnqualifiedID uID)
        {
            _strRes = Qualifier.getDemangledString() + uID.getDemangledString();
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
                _strRes = uIDBaseID.getDemangledString();
                if (src[iProcessPos] != '@')
                {
                    throw new Exception("Error String Literal, not @ after _C");
                }
                iProcessPos++;
            }
            else
            {
                nnsQualifier = new NestNameSpecifier();
                nnsQualifier.parse(src, ref iProcessPos, ref vType, ref vUiD);

                if (uIDBaseID.eUnqualifiedIdType == UnqualifiedID.enumUnqualifiedID.enmCtorDtor)
                {   // base name can be template, so it may just CONTAIN the constructor
                    uIDBaseID.ReplaceCtorDtor(nnsQualifier.uidFirstQualifier);
                }
                _strRes = nnsQualifier.getDemangledString() + uIDBaseID.getDemangledString();
            }
            saveParseStatus(src, pos, iProcessPos);
            pos = iProcessPos;
            return this;
        }
    }

    class StringLiteral : StringComponent
    {
        private static String[] shortString = { ",", "/", "\\\\", ":", ".", " ", "\\n", "\\t", "'", "-" };
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != '_' || (src[iProcessPos + 1] != '0' && src[iProcessPos + 1] != '1'))
            {
                throw new Exception("Wrong String Literal Type" + src[iProcessPos] + src[iProcessPos + 1]);
            }
            iProcessPos += 2;
            long length = getInteger(src, ref iProcessPos);
            long uid = getInteger(src, ref iProcessPos);
            _strRes = "[" + length.ToString() + "]{" + String.Format("{0:X8}", uid) + "} \"";
            long clen = 0;
            int lastChar = -1;
            while (src[iProcessPos] != '@')
            {
                if (src[iProcessPos] == '?')
                {
                    iProcessPos++;
                    if (src[iProcessPos] == '$')
                    {
                        iProcessPos++;
                        int val = (src[iProcessPos] - 'A') * 16 + (src[iProcessPos + 1] - 'A');
                        lastChar = val;
                        if (val == 34)
                        {
                            _strRes += "\\\"";
                        }
                        else if (val > 32 && val < 127)
                        {
                            _strRes += (char)val;
                        }
                        else if (val == 13)
                        {
                            _strRes += "\\r";
                        }
                        else
                        {
                            _strRes += "\\x" + String.Format("{0:X2}", val);
                        }
                        iProcessPos += 2;
                    }
                    else if (src[iProcessPos] >= '0' && src[iProcessPos] <= '9')
                    {
                        lastChar = -1;
                        _strRes += shortString[src[iProcessPos] - '0'];
                        iProcessPos++;
                    }
                    else if ((src[iProcessPos] >= 'A' && src[iProcessPos] <= 'Z') || (src[iProcessPos] >= 'a' && src[iProcessPos] <= 'z'))
                    {
                        int val = src[iProcessPos] + 0x80;
                        _strRes += "\\x" + String.Format("{0:X2}", val);
                        iProcessPos++;
                    }
                    else
                    {
                        throw new Exception("Error in String literal");
                    }
                }
                else
                {
                    lastChar = src[iProcessPos];
                    _strRes += src[iProcessPos];
                    iProcessPos++;
                }
                clen++;
            }
            // lower version of VS will encode the last '\0' into the mangled name, but 
            // recent versions won't
            if (clen == length || (clen + 1 == length && lastChar != 0))
            {
                _strRes += "\"";
            }
            else
            {
                _strRes += "...";
            }
            iProcessPos++;
            pos = iProcessPos;
#if REFINE__
#else // UnDecorateSymbolName will not output the lenth and content part of string literal
            _strRes = "";
#endif
            return this;
        }
    }

    class Declaration : StringComponent
    {
        private QualifiedID qID;
        public QualifiedID QualifiedID
        {
            get { return qID; }
        }
        public override ParseBase parse(string src, ref int pos, ref List<Type> vType, ref List<UnqualifiedID> vUiD)
        {
            int iProcessPos = pos;
            if (src[iProcessPos] != '?')
            {
                throw new Exception("Declaration should begin with '?'");
            }
            iProcessPos++;
            qID = new QualifiedID(true);
            qID.parse(src, ref iProcessPos, ref vType, ref vUiD);
            String sIdent = "";
            if (qID.getDemangledString() == "$_C#")
            {
                sIdent = "`string'";
                sIdent += new StringLiteral().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString();
            }
            else if (Char.IsDigit(src[iProcessPos])) // variable
            {
                TypeVarType sType = new TypeVarType();
                sType.parse(src, ref iProcessPos, ref vType, ref vUiD);
                List<String> forDst = new List<string>();
                if (iProcessPos < src.Length)
                {
                    switch (src[iProcessPos])
                    {
                        default:
                            if (qID.getDemangledString().EndsWith("::`vftable'") || qID.getDemangledString().EndsWith("::`vbtable'") || qID.getDemangledString().EndsWith("::`RTTI Complete Object Locator'") || qID.getDemangledString().EndsWith("::`local vftable'"))
                            {
                                while (src[iProcessPos] != '@')
                                {
                                    forDst.Add(new QualifiedID().parse(src, ref iProcessPos, ref vType, ref vUiD).getDemangledString());
                                }
                                iProcessPos++;
                            }
                            else if (qID.getDemangledString().EndsWith("::__LINE__Var"))
                            {   // generated cont var for __LINE__ , may contain arbitrary hash to avoid confliction
                                if (src[iProcessPos] != '@' || iProcessPos < src.Length - 9)
                                {
                                    throw new Exception();
                                }
                                iProcessPos += 9;
                            }
                            break;
                    }
                }
                sIdent = sType.getDeclaration(qID.getDemangledString());
                if (forDst.Count > 0)
                {
                    sIdent = sIdent + "{for `" + String.Join("'s ", forDst) + "'}";
                }
            }
            else if (Char.IsLetter(src[iProcessPos])) // function
            {
                String sModifier;
                FunctionModifier funcM = new FunctionModifier();
                funcM.parse(src, ref iProcessPos, ref vType, ref vUiD);
                sModifier = funcM.getDemangledString();//getFunctionModifier(out bHasThis, out sThunkAdjustor);
                if (sModifier.Length > 0)
                {
                    sModifier = sModifier + " ";
                }
                TypeFunctionBase func = new TypeFunctionBase(funcM.bHasThis);
                func.parse(src, ref iProcessPos, ref vType, ref vUiD);
                String sFuncBody = func.getDeclaration(qID.getDemangledString() + funcM.sThunkAdjustor);
                sIdent = sModifier + sFuncBody;
            }
            else
            {
                if (src[iProcessPos] == '$')
                {
                    long val;
                    iProcessPos++;
                    switch (src[iProcessPos])
                    {
                        case '4':
                            {   // [thunk], `vector deleting destructor'
                                iProcessPos++;
                                long val1 = StringComponent.getInteger(src, ref iProcessPos);
#if REFINE__ // This is actually a possible minus offset repsented in unsigned integer
                                if (val1 > 0x80000000)
                                {
                                    val1 = val1 - 0x100000000;
                                }
#endif
                                long val2 = StringComponent.getInteger(src, ref iProcessPos);
                                String sModifier = "[thunk]:public: virtual ";
                                TypeFunctionBase func = new TypeFunctionBase(true);
                                func.parse(src, ref iProcessPos, ref vType, ref vUiD);
                                String sFuncBody = func.getDeclaration(qID.getDemangledString() + "`vtordisp{" + val1 + "," + val2 + "}' ");
                                sIdent = sModifier + sFuncBody;
                                break;
                            }
                        case 'A': // local static destructor helper
                            iProcessPos++;
                            Type sRetType = Type.GetTypeLikeID(src, ref iProcessPos, ref vType, ref vUiD, true);
                            if (src[iProcessPos] != 'A')
                            {
                                throw new Exception();
                            }
                            sIdent = "[thunk]:" + sRetType.getDeclaration(qID.getDemangledString() + "`local static destructor helper\'");
                            iProcessPos++;
                            break;
                        case 'B': // vcall
                            iProcessPos++;
                            val = StringComponent.getInteger(src, ref iProcessPos);
                            if (src.Substring(iProcessPos, 2) != "AE")
                            {
                                throw new Exception();
                            }
                            sIdent = "[thunk]: __thiscall " + qID.getDemangledString() + "{" + val.ToString() + "}";
                            iProcessPos += 2;
                            break;
                        case 'C': // virtual desplacement map
                            {
                                iProcessPos++;
                                QualifiedID qIDFor = new QualifiedID();
                                qIDFor.parse(src, ref iProcessPos, ref vType, ref vUiD);

                                sIdent = qID.getDemangledString() + "{for " + qIDFor.getDemangledString() + "}";
                            }
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
            dst = d.getDemangledString();
        }

        public String GetResult()
        {
            return dst;
        }

    }
}
