class __declspec(dllimport) IMPClass {
public:
    IMPClass(/*int* x*/);
    virtual ~IMPClass();
};

void local_vftable() {
    auto pIMP = new IMPClass[7];
}