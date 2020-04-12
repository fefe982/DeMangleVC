#define TEST_DUP int aaa;
namespace aaa {
    namespace aab {
        namespace aac {
            class AAA {};
            namespace aac {
                namespace aad {
                    int aaa;
                    namespace aae {
                        int aaa;
                        namespace aaf {
                            int aaa;
                            namespace aag {
                                int aaa;
                                namespace aah {
                                    int aaa;
                                    namespace aai {
                                        int aaa;
                                        namespace aaj {
                                            int aaa;
                                            namespace aak {
                                                int aaa;
                                                namespace aal {
                                                    int aaa;
                                                    namespace aam {
                                                        int aaa;
                                                        namespace aan {
                                                            int aaa;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    namespace aac {
        namespace aab {
            class AAA {};
            void aaa() {
                static int aac;
                auto aab = &aac;
                static int aaa;
                auto p = &aaa;
            }
        }
    }
}

namespace abc {
    template <typename T> class CT {};
}

void func_dup_name(aaa::aab::aac::AAA*, aaa::aac::aab::AAA*) {

}

void func_temp_dup_name(abc::CT<abc::CT<int>>*) {}


namespace {
    int ÄãºÃ\u03C0 = 123;
}

#define TEST_SCOPE \
static int a; \
auto p1 = &a;

void func() {
    static int a;
    thread_local int b;

    auto p1 = &a;
    auto p2 = &b;
    {
        TEST_SCOPE;
        {
            TEST_SCOPE;
            {
                TEST_SCOPE;
                {
                    TEST_SCOPE;
                    {
                        TEST_SCOPE;
                        {
                            TEST_SCOPE;
                            {
                                TEST_SCOPE;
                                {
                                    TEST_SCOPE;
                                    {
                                        TEST_SCOPE;
                                        {
                                            TEST_SCOPE;
                                            {
                                                TEST_SCOPE;
                                                {
                                                    TEST_SCOPE;
                                                    {
                                                        TEST_SCOPE;
                                                        {
                                                            TEST_SCOPE;
                                                            {
                                                                TEST_SCOPE;
                                                                {
                                                                    TEST_SCOPE;
                                                                    {
                                                                        TEST_SCOPE;
                                                                        {
                                                                            TEST_SCOPE;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}