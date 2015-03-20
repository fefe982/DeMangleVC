//void func(int [5][5]) throw(...);
//enum E
//{
//    Ea,
//    Eb
//};
//namespace a
//{
//    class c
//    {
//    };
//    namespace
//    {
//        class b
//        {
//        public:
//            static E func1(c*);
//            static void func2()
//            {
//                class d
//                {
//                public:
//                    static void func3()
//                    {
//                        func1(new c);
//                    };
//                };
//                d::func3(); 
//            }
//        };
//    }
//}

//class Non_Trivial
//{
//public:
//    Non_Trivial()
//    {
//        _non = 10;
//    }
//    Non_Trivial(const Non_Trivial &that)
//    {
//        _non = that._non - 1;
//    }
//    int _non;
//};
//
//class A
//{
//public:
//    A(int i=0)
//    {
//        a = i;
//    }
//    A(const A &that, int i = 0)
//    {
//        a = i;
//    }
//    ~A()
//    {
//        a = 1;
//    }
//    int a;
//
//};
//class B:virtual A
//{
//    double b;
//};
//class C:virtual A
//{
//    float c;
//};
//
//class Test:B,C
//{
//public:
//
//};
//
//class D
//{
//public:
//    int ahah;
//    Non_Trivial a[5];
//};

extern const int *a;//[10];//={"kjhg","kj"};
const char b[][10] = {"asdf", "d"};
int main()
{
    static char c[][10]={"dasf", "daf"};
    const static char d[][10] = {"asdf", "d"};
    const char *f =c[0];
    f=d[0];
    const int *e=a;//[0]
    a = e;
    f=b[0];
//    //int i[5][5];
//    const static int i = 1;
//    const static int ii[2] = {};
//    const static int iii[3][5] = { {} };
//    const static int iiii[4][6][7] = {{{},{}}};
//    //func(i);
//    //a::b::func2();
//    const int * pi = &i;
//    Test cls[5];
//    Test *pCLS = new Test[5];
//    delete[] pCLS;
//    pCLS = new Test;
//    delete pCLS;
//
////    Test CLS = cls[0];
//
//    D d1[55];
//    d1[0].a[0]._non=9;
//    D d2=d1[0];
//    d2.a[0]._non=9;
}