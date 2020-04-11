#include<utility>
// The observation may not apply to all versions of VS
// and may change depending on different compiler flags

// standard types
signed char type_s_char;
char type_n_char;
unsigned char type_u_char;
short type_n_short;
unsigned short type_u_short;
int type_n_int;
unsigned int type_u_int;
long type_n_long;
unsigned long type_u_long;
long long type_n_long_long; // same as __int64
unsigned long long type_u_long_long; // same as unsigned __int64
float type_float_v;
double type_double_v;
long double type_long_double_v;
bool type_bool_v;
wchar_t type_wchar_v;
char16_t type_char16_v;
char32_t type_char32_v;

//extended types
__int8 exttype_int8_v; // same as char
unsigned __int8 exttype_uint8_v; // same as unsigned char
__int16 exttype_int16; // same as short
unsigned __int16 exttype_uint16; // same as unsigned short
__int32 exttype_int32; // same as int
unsigned __int32 exttype_uint32; // same as unsigned int
__int64 exttype_int64; // same as long long
unsigned __int64 exttype_uint64; // same as unsigned long long
//__int128 exttype_int128;
//unsigned __int128 exttype_uint128;

//cv qualifier
char cv____char = 0;
const char cv_c__char = 0;
volatile char cv__v_char = 0;
const volatile char cv_cv_char = 0;

// pointer
char* pointer____p____c;
const char* pointer____p_c__c = &cv_c__char;
volatile char* pointer____p__v_c;
const volatile char* pointer____p_cv_c = &cv_cv_char;

char* const pointer_c__p____c = &type_n_char;
char* volatile pointer__v_p____c;
char* const volatile pointer_cv_p____c = &type_n_char;

const char* const pointer_c__p_c__c = &cv_c__char;
const char* volatile pointer__v_p_c__c;
const char* const volatile pointer_cv_p_c__c = &cv_c__char;

auto dummy00 = &pointer_c__p____c;
auto dummy01 = &pointer_cv_p____c;
auto dummy02 = &pointer_c__p_c__c;
auto dummy03 = &pointer_cv_p_c__c;

// array
// array is treat as pointer
char array___[7];
const char array_c_[7] = {};
volatile char array__v[7];
const volatile char array_cv[7] = {};

// pointer to array
char (*parray____p_array___)[7] = &array___;
const char (*parray____p_array_c_)[7] = &array_c_;
volatile char (*parray____p_array__v)[7] = &array__v;
const volatile char (*parray____p_array_cv)[7] = &array_cv;

char(*const parray_c__p_array___)[7] = &array___;
char(*volatile parray__v_p_array___)[7] = &array___;
char(*const volatile parray_cv_p_array___)[7] = &array___;

auto dummy_arr_00 = &parray_c__p_array___;
auto dummy_arr_01 = &parray_cv_p_array___;

// reference
char& ref___ = cv____char;
const char& ref_c_ = cv_c__char;
volatile char& ref__v = cv__v_char;
const volatile char& ref_cv = cv_cv_char;

char(&refarray____p_array___)[7] = array___;
const char(&refarray____p_array_c_)[7] = array_c_;
volatile char(&refarray____p_array__v)[7] = array__v;
const volatile char(&refarray____p_array_cv)[7] = array_cv;

char&& rref___ = std::move(cv____char);
const char&& rref_c_ = std::move(cv_c__char);
volatile char&& rref__v = std::move(cv__v_char);
const volatile char&& rref_cv = std::move(cv_cv_char);

char(&&rrefarray____p_array___)[7] = std::move(array___);
const char(&&rrefarray____p_array_c_)[7] = std::move(array_c_);
volatile char(&&rrefarray____p_array__v)[7] = std::move(array__v);
const volatile char(&&rrefarray____p_array_cv)[7] = std::move(array_cv);

// class / struct / union / enum
class A {} class_class;
struct B {} class_struct;
union C {} class_union;
enum D {} class_enum;

const A class_class_c;
volatile A class_class_v;
const volatile A class_class_cv;

auto dummy_cls00 = &class_class_c;
auto dummy_cls01 = &class_class_cv;

enum class E {} class_enum_class;
enum class F :char {} class_enum_class_char;
