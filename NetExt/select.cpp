/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#ifndef _CLASSDEF_
#include "NetExt.h"
#include "Indexer.h"
#include "SpecialCases.h"
#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/replace.hpp>
#include <boost/algorithm/string/case_conv.hpp>
#include <boost/lexical_cast.hpp>


#endif


using namespace std;
using namespace boost::spirit;
using namespace boost::spirit::classic;
#define EVAL_DEBUG 0

#define mycout cout

#define binaryoper(name, str, oper) \
		void name(char const*, char const*) \
		{                                    \
			SVAL b = st.top();				\
			st.pop();						\
			SVAL a = st.top();			\
			st.pop();						\
			if (EVAL_DEBUG) mycout << str << a << " " << b <<  " = "; \
			st.push(oper); \
			if (EVAL_DEBUG) mycout << st.top() << "\n";         \
		};

#define unaryoper(name, str, oper) \
		void name(char const*, char const*) \
		{                                    \
			SVAL a = st.top();			\
			st.pop();						\
			if (EVAL_DEBUG) mycout << str << a  <<  " = "; \
			st.push(oper); \
			if (EVAL_DEBUG) mycout << st.top() << "\n";         \
		};

#define assert_param(x) \
	if(st.size()-funcss.top().size != x) \
	{									 \
	if (EVAL_DEBUG) mycout << "Invalid Parameters (expected/found): " << x  <<  "/" << st.size()-funcss.top().size << "\n" ; \
		while((st.size() > 0) && (st.size()>funcss.top().size)) \
		{									\
			st.pop();						\
		}									\
		funcss.pop();						\
		st.push(v);							\
		return;								\
	}										\

////////////////////////////////////////////////////////////////////////////
//
//  Semantic actions
//
////////////////////////////////////////////////////////////////////////////
namespace CALC
{
	enum funcsenum
	{
		fsqrt,
		fint,
		fmaxm,
		fcontains,
		faddr,
		fitems,
		ftypename,
		fmt,
		fsubstr,
		fisarray,
		farraysize,
		fif,
		fisobj,
		fisvalue,
		fpoi,
		fisnull,
		ffieldat,
		fwildcardmacth,
		ftodbgvar,
		ffromdbgvar,
		ftoken,
		fmodule,
		fdbgeval,
		fdbgrun,
		ffieldoffset,
		ffieldaddress,
		fisstaticfield,
		fisvaluefield,
		fisobjfield,
		ffieldtypename,
		ffieldmt,
		ffieldtoken,
		ffieldmodule,
		fmodulename,
		ffieldfromobj,
		ffieldfrommt,
		farrayitem,
		farraystart,
		farrayend,
		farrayitemsize,
		ftostring,
		ftonumberstring,
		ftoformatednumberstring,
		ftohexstring,
		ftypefrommt,
		fmethodfrommd,
		ftickstotimespan,
		ftickstodatetime,
		ftimespantoticks,
		fdatetoticks,
		fstring,
		fimplement,
		fchain,
		fcontainpointer,
		fcontainfieldoftype,
		fenumname,
		frawobj,
		frawfield,
		ftoguid,
		fnow,
		fmaskticks,
		farraydim,
		frank,
		fstrsize,
		freplace,
		fregex,
		fsplit,
		fsplitsize,
		ftokenize,
		fa,
		fstackroot,
		fthread,
		fenv,
		fipaddress,
		fisinstack,
		fhexstr,
		fval,
		fhtml,
		ftag,
		fxml,
		flpad,
		frpad,
		fltrim,
		frtrim,
		fupper,
		flower,
		fxmltree,
		fxmldoc,
		fpp
	};

	struct funcDef
	{
		funcsenum fenum;
		size_t size;
	};

	StackObj stackObj;
	std::stack<SVAL> st;
	std::stack<funcDef> funcss;
	ObjDetail *currObj;
	std::map<std::string,SVAL> vars;
	int total=0;
	DWORD_PTR Address;
	DWORD_PTR MT=0;
	void do_addfunc(funcsenum f)
	{
		funcDef def = {f, st.size()};
		funcss.push(def);
	}

	void do_pointer(UINT64 i)
	{
		SVAL v;
		v.SetPtr(i);
		st.push(v);
	}

    void    do_int(UINT64 i)
    {
		SVAL v;
		v=i;
		st.push(v);
		if (EVAL_DEBUG) mycout << "PUSH(" << v << ')' << endl;
    }

	void do_stdstring(const std::string& Str)
	{
		SVAL v;
		std::wstring s(CA2W(Str.c_str()));
		v=s;
		st.push(v);
		if (EVAL_DEBUG) cout << "PUSH(" << v << ')' << endl;
	}
	void do_stdstring(const std::wstring& WStr)
	{
		SVAL v;
		v=WStr;
		st.push(v);
		if (EVAL_DEBUG) cout << "PUSH(" << v << ')' << endl;
	}

	#define mesc(X,Y) \
    case X: \
	  s1.append(Y);\
	  break;

	void do_string(char const* begin, char const* end)
	{
		std::string s1;

		const char *it=begin+1;

		while(it < end-1)
		{
			if(*it=='\\')
			{
				it++;

				switch(*it)
				{
					 mesc('a',"\a")
					 mesc('b',"\b")
					 mesc('f',"\f")
					 mesc('n',"\n")
					 mesc('r',"\r")
					 mesc('t',"\t")
					 mesc('v',"\v")
					 mesc('\\',"\\")
					 mesc('\'',"\'")
					 mesc('\"',"\"")

				case 'x':
					int c=0;
					if(it+2 < end-1)
					{
						for(int i=0;i<2;i++)
						{
							c*=16;
							it++;
							char ch = *it;

							if(ch >= '0' && ch <='9')
							{
								c+=int(ch)-0x30;
							}
							ch&=0x20;
							if(ch>='A' && ch<='Z')
							{
								c+=int(ch)-0x31;
							}
						}
					}
					s1 += (char)c;

					break;
				}
			} else
			{
				s1+=*it;
			}
			it++;
		}

		do_stdstring(s1);
	}

    void    do_real(double i)
    {
		SVAL v;
		v=i;

		st.push(v);
		if (EVAL_DEBUG) mycout << "PUSH(" << v << ')' << endl;
    }

	void do_varinternal(CLRDATA_ADDRESS Addr, std::wstring Key, CLRDATA_ADDRESS MTable = 0)
	{
		varMap vars;
		std::vector<std::string> fields;
		std::string key(CW2A(Key.c_str()));

		fields.push_back(key);
		DumpFields(Addr, fields, MTable, &vars);
		SVAL i;
		if(vars.find(key) == vars.end())
		{
			if (EVAL_DEBUG) mycout << "Invalid Var " << key << " value assumed as 0" << endl;
			i=0;
			i.MakeInvalid();
		} else
		{
			i=vars[key];
		}
		if (EVAL_DEBUG) mycout << "PUSH(" << i << ')' << endl;
		st.push(i);
	}

	void do_var(const char* begin, const char* end)
	{
		varMap vars;
		std::vector<std::string> fields;
		std::string key(begin, end);

		fields.push_back(key);
		DumpFields(Address, fields, MT, &vars);
		SVAL i;
		if(vars.find(key) == vars.end())
		{
			if (EVAL_DEBUG) mycout << "Invalid Var " << key << " value assumed as 0" << endl;
			i=0;
			i.MakeInvalid();
		} else
		{
			i=vars[key];
		}
		if (EVAL_DEBUG) mycout << "PUSH(" << i << ')' << endl;
		st.push(i);
	}

	void do_varinternal(std::wstring Field)
	{
		std::string field(CW2A(Field.c_str()));
		do_var(field.begin()._Ptr, field.end()._Ptr);
	}
	//void    do_add(char const*, char const*)   { double a = st.top(); st.pop(); double b=st.top(); st.pop(); mycout << "ADD " << a << " " << b <<  " = "; st.push(a+b); mycout << st.top() << "\n"; }
	binaryoper(do_add, "ADD ", a+b);
	binaryoper(do_subt, "SUB ", a-b);

	binaryoper(do_mult, "MULT ", a*b);
	binaryoper(do_div, "DIV ", a/b);
	binaryoper(do_mod, "MOD ", a%b);

	//void    do_subt(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "SUBTRACT " << a << " " << b << " = "; st.push(a-b); mycout << st.top() << "\n";}
    //void    do_mult(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "MULTIPLY " << a << " " << b << " = "; st.push(a*b); mycout << st.top() << "\n";}
    //void    do_div(char const*, char const*)     { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "DIVIDE " << b << " " << a << " = "; st.push(b/a); mycout << st.top() << "\n";}
    //void    do_mod(char const*, char const*)     { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "MOD " << b << " " << a << " = "; st.push((INT64)b % (INT64)a); mycout << st.top() << "\n";}

	unaryoper(do_neg, "NEG ", -a);
    //void    do_neg(char const*, char const*)     { double a = st.top(); st.pop(); mycout << "NEGATE " << a << " " << " = "; st.push(-a); mycout << st.top() << "\n"; }
	void    do_power(char const*, char const*)     { SVAL a = st.top(); st.pop(); SVAL b=st.top(); st.pop();mycout << "POWER " << b << " " << a << " = "; do_real(pow(b.DoubleValue,a.DoubleValue)); mycout << st.top() << "\n";}
	//void    do_power(char const*, char const*) {};
	void    do_sqrt()     { SVAL a = st.top(); st.pop(); do_real(pow(a.DoubleValue, 0.5)); }
	void    do_toint()     { SVAL a = st.top(); st.pop(); do_int((int)a.DoubleValue);  }
	void    do_max()   { SVAL a = st.top(); st.pop(); SVAL b=st.top(); st.pop(); do_real(a.DoubleValue > b.DoubleValue ? a.DoubleValue : b.DoubleValue); }
	binaryoper(do_eq, "EQ ", a == b);
	binaryoper(do_ge, "GE ", a >= b);
	binaryoper(do_le, "LE ", a <= b);
	binaryoper(do_gt, "GT ", a > b);
	binaryoper(do_lt, "LT ", a < b);
	binaryoper(do_ne, "NE ", a != b);

    //void    do_eq(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "EQUAL " << b << " " << a << " = "; st.push(a == b); mycout << st.top() << "\n";}
    //void    do_ne(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "NOTEQUAL " << b << " " << a << " = "; st.push(a != b); mycout << st.top() << "\n";}
    //void    do_ge(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "GREATEREQUAL " << b << " " << a << " = "; st.push(b >= a); mycout << st.top() << "\n";}
    //void    do_le(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "LESSEQUAL " << b << " " << a << " = "; st.push(b <= a); mycout << st.top() << "\n";}
    //void    do_gt(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "GREAT " << b << " " << a << " = "; st.push(b > a); mycout << st.top() << "\n";}
    //void    do_lt(char const*, char const*)    { double a = st.top(); st.pop(); double b=st.top(); st.pop();mycout << "LESS " << b << " " << a << " = "; st.push(b < a); mycout << st.top() << "\n";}
    //void    do_not(char const*, char const*)     { double a = st.top(); st.pop(); mycout << "NOT " << a << " " << " = "; st.push(!a); mycout << st.top() << "\n"; }
	unaryoper(do_not, "NOT ", !a);

	binaryoper(do_and, "AND ", a && b);
	binaryoper(do_or, "OR ", a || b);
	binaryoper(do_bitand, "BITAND ", a & b);
	binaryoper(do_bitor, "BITOR ", a | b);

	void do_contains() { SVAL b = st.top(); st.pop(); SVAL a = st.top(); st.pop(); do_int(a.strValue.find(b.strValue) != string::npos); }
	void do_addr() { do_pointer(currObj->Address());}
	void do_items()
	{
		SVAL v=st.top();
		st.pop();
		if(!currObj->IsArray() ||  v.Value.u32 >= currObj->NumComponents())
		{
			SVAL j;
			st.push(j);
			return;
		}

		st.push(GetValue(v.Value.u32*currObj->InnerComponentSize()+currObj->DataPtr(), currObj->InnerComponentType(), currObj->InnerMT()));

		/* INCOMPLETE */
	}

	void do_rank() { do_int(currObj->Rank()); }

	void do_typename() { std::wstring str=currObj->TypeName(); do_stdstring(str);  }
	void do_mt() { do_pointer(currObj->MethodTable()); }
	void do_substr() { SVAL e=st.top(); st.pop(); SVAL b=st.top(); st.pop(); SVAL s=st.top(); st.pop(); if((b.DoubleValue>e.DoubleValue) || (b.DoubleValue>=s.strValue.size())) do_stdstring(L""); else do_stdstring(s.strValue.substr(b.DoubleValue, e.DoubleValue)); };
	void do_isarray() { do_int(currObj->IsArray()); }
	void do_arraysize() { do_int(currObj->NumComponents()); }
	void do_if() { SVAL ie=st.top(); st.pop(); SVAL ii=st.top(); st.pop(); SVAL exp=st.top(); st.pop(); if(exp.DoubleValue !=0) st.push(ii); else st.push(ie); }
	void do_isobj() {  do_int(currObj->IsObject()); }
	void do_isvalue() { do_int(currObj->IsValueType()); }
	void do_poi() { SVAL p=st.top(); st.pop(); do_pointer(ObjDetail::GetPTR(p.Value.u64)); }
	void do_isnull()
	{ 
		SVAL in=st.top();
		st.pop();
		SVAL v=st.top();
		st.pop();
		if(!v.IsValid || ((v.IsInt() || v.IsUnsigned()) && v.Value.u64==0) || (v.IsReal() && v.DoubleValue==0) || (v.IsString() && NULL == v.Value.ptr)) 
			st.push(in);
		else
			st.push(v);
	}
	void do_fieldat() { SVAL i=st.top(); st.pop(); SVAL v; do_varinternal(currObj->GetFieldsByName("*").at(i.DoubleValue).FieldName); }
	void do_wildcardmacth()
	{
		SVAL pat=st.top();
		st.pop();
		SVAL s=st.top();
		st.pop();
		do_int(g_ExtInstancePtr->MatchPattern(localCW2A(s.strValue.c_str()).c_str(), localCW2A(pat.strValue.c_str()).c_str(), false));
	}
	void do_todbgvar() { SVAL v=st.top(); st.pop(); SVAL dv=st.top(); st.pop(); do_int(SpecialCases::SetDbgVar(dv.Value.i32, v));}
	void do_fromdbgvar() { SVAL v=st.top(); st.pop(); st.push(SpecialCases::GetDbgVar(v.Value.i32)); }
	void do_token() { do_pointer(currObj->classObj.Token()); }
	void do_module() { do_pointer(currObj->Module());}
	void do_dbgeval() { SVAL v=st.top(); st.pop(); do_int(g_ExtInstancePtr->EvalExprU64(CW2A(v.prettyPrint.c_str())));}
	void do_dbgrun() { SVAL v=st.top(); st.pop(); std::string tempStr; tempStr.assign(CW2A(v.prettyPrint.c_str())); do_stdstring(EXT_CLASS::Execute(tempStr));}
	void do_fieldoffset() { SVAL v=st.top(); st.pop(); do_pointer(v.Offset); }
	void do_fieldaddress() { SVAL v=st.top(); st.pop(); do_pointer(v.Value.ptr); }
	void do_isstaticfield() { SVAL v=st.top(); st.pop(); do_int(v.IsStatic);}
	void do_isvaluefield() { SVAL v=st.top(); st.pop(); do_int(v.IsValueType);}
	void do_isobjfield() { SVAL v=st.top(); st.pop(); do_int(!v.IsValueType);}
	void do_fieldtypename() { SVAL v=st.top(); st.pop(); do_stdstring(v.typeName);}
	void do_fieldmt() { SVAL v=st.top(); st.pop(); do_pointer(v.MT);}
	void do_fieldtoken() { SVAL v=st.top(); st.pop(); do_int(v.Token);}
	void do_fieldmodule() { SVAL v=st.top(); st.pop(); do_pointer(v.Module); }
	void do_modulename() { SVAL v=st.top(); st.pop(); do_stdstring(currObj->AssemblyName()); }
	void do_fieldfromobj()
	{
		SVAL f=st.top();
		st.pop();
		SVAL o=st.top();
		st.pop();
		do_varinternal((CLRDATA_ADDRESS)o.DoubleValue, f.strValue.size() == 0 ? f.prettyPrint : f.strValue);
	}
	void do_fieldfrommt()
	{
		SVAL f=st.top();
		st.pop();
		SVAL m=st.top();
		st.pop();
		SVAL o=st.top();
		st.pop();
		do_varinternal((CLRDATA_ADDRESS)o.DoubleValue, f.strValue.size() == 0 ? f.prettyPrint : f.strValue,
			(CLRDATA_ADDRESS)m.DoubleValue);
	}
	void do_arraystart() { do_pointer(currObj->DataPtr()); }
	void do_arrayitemsize() { do_int(currObj->InnerComponentSize());}
	void do_arraydim() { SVAL v=st.top(); st.pop(); do_int( GetBound(currObj->Address(), currObj->Rank(),v.Value.u32)); };
	void do_tostring() { SVAL v=st.top(); st.pop(); do_stdstring(v.strValue.size() == 0 ? v.prettyPrint : v.strValue );}
	void do_tonumberstring() { SVAL v=st.top(); st.pop(); do_stdstring(v.strValue.size() == 0 ? v.prettyPrint : v.strValue);}
	void do_toformatednumberstring() {  SVAL v=st.top(); st.pop(); do_stdstring(formatnumber(v.DoubleValue)); }
	void do_tohexstring() {  SVAL v=st.top(); if(!v.IsValid) return; st.pop(); do_stdstring(formathex(v.Value.u64));}
	void do_typefrommt() {  SVAL v=st.top(); if(!v.IsValid) return; st.pop(); do_stdstring(GetMethodName(v.Value.u64));}
	void do_methodfrommd() {  SVAL v=st.top(); if(!v.IsValid) return; st.pop(); do_stdstring(GetMethodDesc(v.Value.u64));}
	void do_tickstotimespan()
	{
		//$tickstotimespan(0n10000000*(0n20+0n30*0n60+1*0n60*0n60))
		SVAL i=st.top();
		st.pop();

		ULONG64 ticks;
		if(i.IsReal())
		{
			ticks = (ULONG64)i.DoubleValue;
		} else
		{
			ticks = i.Value.u64;
		}

		do_stdstring(tickstotimespan(ticks));

	}
	void do_tickstodatetime()
	{
		SVAL i=st.top();
		st.pop();

		ULONG64 ticks = 0;
		if(i.IsReal())
		{
			ticks = (LONG64)i.DoubleValue;
		} else
		{
			ticks = i.Value.u64;
		}

		do_stdstring(tickstodatetime(ticks));
	}

	void do_maskticks()
	{
		SVAL i=st.top();
		st.pop();
		ULONG64 ticks = 0;
		if(i.IsReal())
		{
			ticks = (ULONG64)i.DoubleValue;
		} else
		{
			ticks = i.Value.u64;
		}

		do_int(ticks & TicksMask);

	}
	void do_timespantoticks()
	{
		SVAL s=st.top(); st.pop();
		SVAL m=st.top(); st.pop();
		SVAL h=st.top(); st.pop();

		do_int((UINT64)10000000*(s.DoubleValue+m.DoubleValue*60+h.DoubleValue*60*60));
	}
	void do_datetoticks()
	{
		SVAL d=st.top(); st.pop();
		SVAL m=st.top(); st.pop();
		SVAL y=st.top(); st.pop();
		do_int(SpecialCases::DateToTicks(y.Value.i64, m.Value.i64, d.Value.i64));
	}

	void do_now()
	{
		do_int(SpecialCases::TicksFromTarget());
	}

	void do_fstring()
	{
		if(currObj->IsString())
		{
			do_stdstring(ObjDetail::FullString(currObj->Address()));
		} else
		{
			do_stdstring(L"<invalid string>");
		}
	}

	void do_implement()
	{
		SVAL v=st.top(); st.pop();
		do_int(currObj->classObj.Implement(v.strValue.size() == 0 ? v.prettyPrint : v.strValue));
	}
	void do_chain()
	{
		do_stdstring(currObj->classObj.ChainTypeStr());
	}
	void do_containpointer()
	{
		SVAL s=st.top(); st.pop();

		varMap vars;
		std::vector<std::string> fields;
		fields.push_back("*");
		CLRDATA_ADDRESS MT=currObj->IsValueType() ? currObj->MethodTable() : 0;
		DumpFields(currObj->Address(), fields, MT, &vars);
		std::map<std::string, SVAL>::const_iterator it = vars.begin();
		while(it!=vars.end())
		{
			if(it->second.Value.u64 == s.Value.u64)
			{
				do_int(1);
				return;
			}
			it++;
		}
		do_int(0);
	}
	void do_containfieldoftype()
	{
		SVAL v=st.top(); st.pop();
		std::vector<FieldStore> fields = currObj->GetFieldsByName("*");
		for(int i=0; i<fields.size();i++)
		{
			if(g_ExtInstancePtr->MatchPattern(localCW2A(fields[i].mtName.c_str()).c_str(), localCW2A(v.strValue.size() == 0 ? v.prettyPrint.c_str() : v.strValue.c_str()).c_str(), false))
			{
				do_int(1);
				return;
			}
		}
		do_int(0);
	}

	void do_enumname()
	{
		SVAL v=st.top(); st.pop();
		if(!v.IsValid)
		{
			SVAL e;
			e.MakeInvalid();
			st.push(e);
			return;
		}
		do_stdstring(SpecialCases::GetEnumString(v.MT, v.Value.u64));
	}

	void do_rawfield()
	{
		SVAL v=st.top(); st.pop();
		SVAL e;
		e.MakeInvalid();
		if(v.MT == 0 || !v.IsValid)
		{
			st.push(e);
			return;
		}

		wstring mtName = GetMethodName(v.MT);
		if(mtName != L"System.Char[]" && mtName != L"System.Byte[]")
		{
			st.push(e);
			return;
		}

		do_stdstring(SpecialCases::GetRawArray(v.Value.ptr, true));
	}

	void do_rawobj()
	{
		SVAL e;
		e.MakeInvalid();

		wstring mtName = CALC::currObj->TypeName();
		if(mtName != L"System.Char[]" && mtName != L"System.Byte[]")
		{
			st.push(e);
			return;
		}

		do_stdstring(SpecialCases::GetRawArray(currObj->Address(), true));
	}

	void do_toguid()
	{
		SVAL v=st.top(); st.pop();
		SVAL e;
		e.MakeInvalid();
		if(v.MT == 0)
		{
			st.push(e);
			return;
		}
		wstring mtName = GetMethodName(v.MT);

		if(mtName != L"System.Guid")
		{
			st.push(e);
			return;
		}

		do_stdstring(SpecialCases::ToGuid(v.Value.ptr));
	}

	void do_strsize()
	{
		SVAL s=st.top(); st.pop();
		if(s.corType != ELEMENT_TYPE_STRING)
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		do_int(s.strValue.size());
	}

	void do_replace()
	{
		SVAL r=st.top(); st.pop();
		SVAL f=st.top(); st.pop();
		SVAL s=st.top(); st.pop();
		if(f.corBaseType != ELEMENT_TYPE_STRING || r.corType != ELEMENT_TYPE_STRING)
		{
			SVAL i;
			i.MakeInvalid();
			st.push(i);
			return;
		}
		boost::algorithm::replace_all(s.strValue.size()==0 ? s.prettyPrint : s.strValue, f.strValue, r.strValue);
		do_stdstring(s.strValue);
	}

	//
	// Example: $regex($dbgrun("kpL"), "^([0-9a-fA-F`]+)\\s+([0-9a-fA-F`]+)\\s+(.+)", "$3\n")
	// user32!ZwUserWaitMessage(void)+0xa
	// mscorwks!DoNDirectCallWorker(void)+0x62
	// System_Windows_Forms_ni!System.Windows.Forms.Application+ComponentManager.System.Windows.Forms.UnsafeNativeMethods.IMsoComponentManager.FPushMessageLoop(<HRESULT 0x80004001>)+0x7d4
	// System_Windows_Forms_ni!System.Windows.Forms.Application+ThreadContext.RunMessageLoopInner(<HRESULT 0x80004001>)+0x578
	// System_Windows_Forms_ni!System.Windows.Forms.Application+ThreadContext.RunMessageLoop(<HRESULT 0x80004001>)+0x65
	// Client!Client.Program.Main(<HRESULT 0x80004001>)+0x51
	// mscorwks!CallDescrWorker(void)+0x82
	// (...)
	void do_regex()
	{
		SVAL op = st.top(); st.pop(); // output pattern
		SVAL rp = st.top(); st.pop(); // regex pattern
		SVAL s = st.top(); st.pop(); // input string
		SVAL r;
		r.MakeInvalid();
		if(!s.IsString() || !rp.IsString() || !op.IsString())
		{
			st.push(r);
			return;
		}

		try
		{
			wregex reg(rp.strValue, regex_constants::ECMAScript | regex_constants::icase);
			wstring repStr = regex_replace(s.strValue, reg, op.strValue, regex_constants::format_no_copy);

			do_stdstring(repStr);
		} catch(...)
		{
			st.push(r);
		}
	}

	void do_split()
	{
		SVAL i = st.top(); st.pop();
		SVAL p = st.top(); st.pop();
		SVAL s = st.top(); st.pop();
		if(!s.IsString() || !p.IsString() || !(i.IsInt() || i.IsReal()))
		{
			SVAL k;
			k.MakeInvalid();
			st.push(k);
			return;
		}
		using namespace boost::algorithm;
		int c=0;
	    typedef split_iterator<wstring::iterator> string_split_iterator;
	    for(string_split_iterator It=
			make_split_iterator(s.strValue, first_finder(p.strValue, is_iequal()));
			It!=string_split_iterator();
			++It)
	    {
			if(c++ == (int)i.DoubleValue)
			{
				do_stdstring(boost::copy_range<std::wstring>(*It));
				return;
			}
	    }
		do_stdstring("");

	}

	void do_splitsize()
	{
		SVAL p = st.top(); st.pop();
		SVAL s = st.top(); st.pop();
		if(!s.IsString() || !p.IsString())
		{
			SVAL k;
			k.MakeInvalid();
			st.push(k);
			return;
		}
		using namespace boost::algorithm;
		int c=0;
	    typedef split_iterator<wstring::iterator> string_split_iterator;
	    for(string_split_iterator It=
			make_split_iterator(s.strValue, first_finder(p.strValue, is_iequal()));
			It!=string_split_iterator();
			++It)
				c++;
		do_int(c);

	}

	void do_tokenize()
	{
		SVAL i = st.top(); st.pop();
		SVAL s = st.top(); st.pop();
		if(!s.IsString() || !(i.IsInt() || i.IsReal()))
		{
			SVAL k;
			k.MakeInvalid();
			st.push(k);
			return;
		}
		using namespace boost::algorithm;

		std::vector< std::wstring > splitV;
		split( splitV, s.strValue, is_any_of(L" ,;|-<>{}:\n.*\n\t"), token_compress_on );
		if(splitV.size() > (int)i.DoubleValue)
		{
			do_stdstring(splitV[(int)i.DoubleValue]);
			return;
		}
		do_stdstring("");

	}

	void do_a()
	{
		SVAL f=st.top(); st.pop();
		SVAL a=st.top(); st.pop();
		f.fieldName = localCW2A(a.strValue.c_str());
		st.push(f);
	}

	void do_stackroot()
	{
		SVAL v = st.top(); st.pop();
		vector<ULONG> threads = stackObj.IsInStack(v.Value.ptr);
		string str;
		for(int i=0;i<threads.size(); i++)
		{
			if(str.size())
				str.append(","+boost::lexical_cast<std::string>(threads[i]));
			else
				str.append(boost::lexical_cast<std::string>(threads[i]));

		}
		do_stdstring(str);

	}

	void do_thread()
	{
		SVAL v=st.top(); st.pop();
		if(v.IsValid && v.Value.ptr != NULL)
		{
			
			ULONG ti = Thread::GetOSThreadIDByAddress(v.Value.ptr);
			if(ti==0)
			{
				SVAL v;
				v.MakeInvalid();
				st.push(v);
				return;
			}
			ULONG tid=0;
			g_ExtInstancePtr->m_System2->GetThreadIdBySystemId(ti, &tid);
			do_stdstring(boost::lexical_cast<std::wstring>(tid));

			return;
		}
		SVAL r;
		r.MakeInvalid();
		st.push(r);


	}

	void do_env()
	{
		SVAL s = st.top(); st.pop();
		auto envVars = EXT_CLASS::GetProcessEnvVar();

		wstring varStr = GetVar(s.strValue);

		do_stdstring(varStr);


	}

	void do_ipaddress()
	{
		SVAL ip = st.top(); st.pop();
		

		CLRDATA_ADDRESS addr = ip.Value.ptr;

		SVAL r;
		r.MakeInvalid();
		if(addr == NULL)
		{
			st.push(r);
			return;
		}

		wstring ipStr = SpecialCases::IPAddress(addr);
		if(ipStr.size() > 0)
		{
			do_stdstring(ipStr);
			return;
		}
		st.push(r);
	}


	void do_isinstack()
	{
		SVAL v = st.top(); st.pop();

		if(stackObj.IsInStack(v.Value.ptr).size())
		{
			do_int(1);
		} else
			do_int(0);
	}

	void do_hexstr()
	{
		SVAL v=st.top(); st.pop();
		SVAL e;
		e.MakeInvalid();
		if(v.MT == 0)
		{
			st.push(e);
			return;
		}

		ObjDetail obj(v.Value.ptr);
		wstring mtName = obj.TypeName();
		if(mtName != L"System.Char[]" && mtName != L"System.Byte[]" && mtName != L"System.Int16[]"  && mtName != L"System.UInt16[]")
		{
			st.push(e);
			return;
		}

		do_stdstring(SpecialCases::GetHexArray(v.Value.ptr));
	}

	void do_val()
	{
		SVAL s=st.top(); st.pop();
		SVAL i;
		i.MakeInvalid();
		try
		{
			i= boost::lexical_cast<INT64>(s.strValue);
			st.push(i);
		}
		catch(boost::bad_lexical_cast &)
		{
			st.push(i);
		}
	}

	void do_html()
	{
		SVAL s=st.top(); st.pop();
		wstring newStr;
		for(int i=0;i<s.strValue.size();i++)
		{
			switch(s.strValue[i])
			{
			case L'<':
				newStr+=L"&lt;";
				break;
			case L'>':
				newStr+=L"&gt;";
				break;
			case L'&':
				newStr+=L"&amp;";
				break;
			case L'"':
				newStr+=L"&quot;";
				break;
			case L'\n':
				newStr+=L"<br />";
				break;
			case L' ':
				if(i>0 && s.strValue[i-1] == L' ')
				{
					newStr += L"&nbsp;";
				} else
				{
					newStr += L' ';
				}
				break;
			default:
				if(s.strValue[i] > (WCHAR)127 || s.strValue[i] < (WCHAR)32)
				{
					newStr+=L"&#"+boost::lexical_cast<wstring>((UINT16)s.strValue[i])+L";"; 
				} else
					newStr+=s.strValue[i];
				break;
			}
		}
		do_stdstring(newStr);

	}

	void do_tag()
	{

	}

	void do_xml()
	{
		SVAL s=st.top(); st.pop();

		do_stdstring(SpecialCases::IndentedXml(s.strValue));
	}

	void do_xmltree()
	{
		SVAL s=st.top(); st.pop();

		do_stdstring(SpecialCases::XmlTree(s.strValue));
	}
	void do_lpad()
	{
		SVAL l=st.top(); st.pop();
		SVAL s=st.top(); st.pop();
		if(l.IsString() || !s.IsValid || (l.Value.i32 <=0))
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		if(s.strValue.size()==0) s.strValue = s.prettyPrint;
		if(l.Value.i32 > (int)s.strValue.size())
			s.strValue.insert(s.strValue.begin(), l.Value.i32-(int)s.strValue.size(), L' ');
		do_stdstring(s.strValue);
	}

	void do_rpad()
	{
		SVAL l=st.top(); st.pop();
		SVAL s=st.top(); st.pop();
		if(l.IsString() || !s.IsValid || (l.Value.i32 <=0) )
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		if(s.strValue.size()==0) s.strValue = s.prettyPrint;

		if(l.Value.i32 > (int)s.strValue.size())
			s.strValue.append(l.Value.i32-(int)s.strValue.size(), L' ');
		do_stdstring(s.strValue);

	}

	void do_ltrim()
	{
		SVAL s=st.top(); st.pop();
		if(!s.IsString())
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		
		boost::trim_left(s.strValue);
		do_stdstring(s.strValue);

	}

	void do_rtrim()
	{
		SVAL s=st.top(); st.pop();
		if(!s.IsString())
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		
		boost::trim_right(s.strValue);
		do_stdstring(s.strValue);

	}

	void do_upper()
	{
		SVAL s=st.top(); st.pop();
		if(!s.IsString())
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		
		boost::to_upper(s.strValue);
		do_stdstring(s.strValue);
	}

	void do_lower()
	{
		SVAL s=st.top(); st.pop();
		if(!s.IsString())
		{
			s.MakeInvalid();
			st.push(s);
			return;
		}
		
		boost::to_lower(s.strValue);
		do_stdstring(s.strValue);
	}

	void do_xmldoc()
	{
		SVAL v=st.top(); st.pop();
		CComBSTR str;
		if(pHeap->GetXmlString(v.Value.ptr, &str) != S_OK)
		{
			v.MakeInvalid();
			st.push(v);
		}
		wstring str1 = ToWString(str);
		do_stdstring(str1);

	}

	void do_pp()
	{
		SVAL v=st.top(); st.pop();
		
		if(v.Value.ptr == 0)
		{
			do_stdstring(v.strValue);
		}

		string result;
		if(v.IsValueType)
		{
			result = SpecialCases::PrettyPrint(v.Value.ptr, v.MT);
		} else
		{
			result = SpecialCases::PrettyPrint(v.Value.ptr);
		}

		if(result.size() == 0)
			do_stdstring(v.strValue);
		else
			do_stdstring(result);
	}

	void do_func(char const*, char const*)
	{
		if (EVAL_DEBUG) mycout << "params: " << st.size()-funcss.top().size << "\n";
		SVAL v;
		v.MakeInvalid();
		switch(funcss.top().fenum)
		{
			case ftoguid:
				assert_param(1);
				do_toguid();
				break;
			case frawobj:
				assert_param(0);
				do_rawobj();
				break;
			case frawfield:
				assert_param(1);
				do_rawfield();
				break;
			case fnow:
				assert_param(0);
				do_now();
				break;
			case fenumname:
				assert_param(1);
				do_enumname();
				break;
			case fsqrt:
				assert_param(1);
				do_sqrt();
				break;
			case fint:
				assert_param(1);
				do_toint();
				break;
			case fmaxm:
				assert_param(2);
				do_max();
				break;
			case faddr:
				assert_param(0);
				do_addr();
				break;
			case fcontains:
				assert_param(2);
				do_contains();
				break;
			case fitems:
				assert_param(1);
				do_items();
				break;
			case frank:
				assert_param(0);
				do_rank();
				break;

			case ftypename:
				assert_param(0);
				do_typename();
				break;
			case fmt:
				assert_param(0);
				do_mt();
				break;
			case fsubstr:
				assert_param(3);
				do_substr();
				break;
			case fisarray:
				assert_param(0);
				do_isarray();
				break;
			case farraysize:
				assert_param(0);
				do_arraysize();
				break;
			case farraydim:
				assert_param(1);
				do_arraydim();
				break;

			case fif:
				assert_param(3);
				do_if();
				break;
			case fisobj:
				assert_param(0);
				do_isobj();
				break;
			case fisvalue:
				assert_param(0);
				do_isvalue();
				break;
			case fpoi:
				assert_param(1);
				do_poi();
				break;
			case fisnull:
				assert_param(2);
				do_isnull();
				break;
			case ffieldat:
				assert_param(1);
				do_fieldat();
				break;
			case fwildcardmacth:
				assert_param(2);
				do_wildcardmacth();
				break;
			case ftodbgvar:
				assert_param(2);
				do_todbgvar();
				break;
			case ffromdbgvar:
				assert_param(2);
				do_fromdbgvar();
				break;
			case ftoken:
				assert_param(0);
				do_token();
				break;
			case fmodule:
				assert_param(0);
				do_module();
				break;
			case fdbgeval:
				assert_param(1);
				do_dbgeval();
				break;
			case fdbgrun:
				assert_param(1);
				do_dbgrun();
				break;
			case ffieldoffset:
				assert_param(1);
				do_fieldoffset();
				break;
			case ffieldaddress:
				assert_param(1);
				do_fieldaddress();
				break;
			case fisstaticfield:
				assert_param(1);
				do_isstaticfield();
				break;
			case fisvaluefield:
				assert_param(1);
				do_isvaluefield();
				break;
			case fisobjfield:
				assert_param(1);
				do_isobjfield();
				break;
			case ffieldtypename:
				assert_param(1);
				do_fieldtypename();
				break;
			case ffieldmt:
				assert_param(1);
				do_fieldmt();
				break;
			case ffieldtoken:
				assert_param(1);
				do_fieldtoken();
				break;
			case ffieldmodule:
				assert_param(1);
				do_fieldmodule();
				break;
			case fmodulename:
				assert_param(1);
				do_modulename();
				break;
			case ffieldfromobj:
				assert_param(2);
				do_fieldfromobj();
				break;
			case farraystart:
				assert_param(0);
				do_arraystart();
				break;
			case farrayitemsize:
				assert_param(0);
				do_arrayitemsize();
				break;
			case ftostring:
				assert_param(1);
				do_tostring();
				break;
			case ftonumberstring:
				assert_param(1);
				do_tonumberstring();
				break;
			case ftoformatednumberstring:
				assert_param(1);
				do_toformatednumberstring();
				break;
			case ftohexstring:
				assert_param(1);
				do_tohexstring();
				break;
			case ftypefrommt:
				assert_param(1);
				do_typefrommt();
				break;
			case fmethodfrommd:
				assert_param(1);
				do_methodfrommd();
				break;
			case ffieldfrommt:
				assert_param(3);
				do_fieldfrommt();
				break;
			case fchain:
				assert_param(0);
				do_chain();
				break;
			case fcontainpointer:
				assert_param(1);
				do_containpointer();
				break;
			case fimplement:
				assert_param(1);
				do_implement();
				break;
			case fcontainfieldoftype:
				assert_param(1);
				do_containfieldoftype();
				break;
			case ftickstotimespan:
				assert_param(1);
				do_tickstotimespan();
				break;
			case ftickstodatetime:
				assert_param(1);
				do_tickstodatetime();
				break;
			case ftimespantoticks:
				assert_param(3);
				do_timespantoticks();
				break;
			case fmaskticks:
				assert_param(1);
				do_maskticks();
				break;
			case fdatetoticks:
				assert_param(3);
				do_datetoticks();
				break;
			case fstring:
				assert_param(0);
				do_fstring();
				break;
			case fstrsize:
				assert_param(1);
				do_strsize();
				break;
			case freplace:
				assert_param(3);
				do_replace();
				break;
			case fregex:
				assert_param(3);
				do_regex();
				break;
			case fsplit:
				assert_param(3);
				do_split();
				break;
			case fsplitsize:
				assert_param(2);
				do_splitsize();
				break;
			case ftokenize:
				assert_param(2);
				do_tokenize();
				break;
			case fa:
				assert_param(2);
				do_a();
				break;
			case fstackroot:
				assert_param(1);
				do_stackroot();
				break;
			case fthread:
				assert_param(1);
				do_thread();
				break;
			case fenv:
				assert_param(1);
				do_env();
				break;
			case fipaddress:
				assert_param(1);
				do_ipaddress();
				break;
			case fisinstack:
				assert_param(1);
				do_isinstack();
				break;
			case fhexstr:
				assert_param(1);
				do_hexstr();
				break;
			case fval:
				assert_param(1);
				do_val();
				break;
			case fhtml:
				assert_param(1);
				do_html();
				break;
			case ftag:
				assert_param(2);
				do_tag();
				break;
			case fxml:
				assert_param(1);
				do_xml();
				break;
			case flpad:
				assert_param(2);
				do_lpad();
				break;
			case frpad:
				assert_param(2);
				do_rpad();
				break;
			case fltrim:
				assert_param(1);
				do_ltrim();
				break;
			case frtrim:
				assert_param(1);
				do_rtrim();
				break;
			case fupper:
				assert_param(1);
				do_upper();
				break;
			case flower:
				assert_param(1);
				do_lower();
				break;
			case fxmltree:
				assert_param(1);
				do_xmltree();
				break;
			case fxmldoc:
				assert_param(1);
				do_xmldoc();
				break;
			case fpp:
				assert_param(1);
				do_pp();
				break;



			default:
				if (EVAL_DEBUG) mycout << "Function is broken. You should not see this\n";
				st.push(v);
		}
		funcss.pop();
	}
}

using namespace CALC;

////////////////////////////////////////////////////////////////////////////
//
//  Expression Evaluation Grammar
//
////////////////////////////////////////////////////////////////////////////

struct calculator : public classic::grammar<calculator>
{
    template <typename ScannerT>
    struct definition
    {
        definition(calculator const& self)
        {
			classic::uint_parser<UINT64, 16> const
			hex64_p   = classic::uint_parser<UINT64, 16>();
			funcsym.add
				("$sqrt", fsqrt)
				("$int", fint)
				("$max", fmaxm)
				("$contains", fcontains)
				("$addr", faddr)
				("$items",fitems)
				("$typename", ftypename)
				("$mt", fmt)
				("$substr",fsubstr)
				("$isarray",fisarray)
				("$arraysize", farraysize)
				("$arraydim", farraydim)
				("$rank", frank)
				("$if",fif)
				("$isobj", fisobj)
				("$isvalue", fisvalue)
				("$poi", fpoi)
				("$isnull", fisnull)
				("$fieldat", ffieldat)
				("$wildcardmatch",fwildcardmacth)
				("$todbgvar", ftodbgvar)
				("$token", ftoken)
				("$module", fmodule)
				("$dbgeval", fdbgeval)
				("$dbgrun", fdbgrun)
				("$fieldoffset", ffieldoffset)
				("$fieldaddress", ffieldaddress)
				("$isstaticfield", fisstaticfield)
				("$isvaluefield", fisvaluefield)
				("$isobjfield", fisobjfield)
				("$fieldtypename", ffieldtypename)
				("$fieldmt", ffieldmt)
				("$fieldtoken", ffieldtoken)
				("$fieldmodule", ffieldmodule)
				("$modulename", fmodulename)
				("$fieldfromobj", ffieldfromobj)
				("$fieldfrommt", ffieldfrommt)
				("$arrayitem", farrayitem)
				("$arraystart", farraystart)
				("$arrayend", farrayend)
				("$arrayitemsize", farrayitemsize)
				("$tostring", ftostring)
				("$tonumberstring", ftonumberstring)
				("$toformattednumberstring", ftoformatednumberstring)
				("$tohexstring", ftohexstring)
				("$typefrommt", ftypefrommt)
				("$methodfrommd", fmethodfrommd)
				("$tickstotimespan", ftickstotimespan)
				("$tickstodatetime", ftickstodatetime)
				("$timespantoticks", ftimespantoticks)
				("$datetoticks", fdatetoticks)
				("$now", fnow)
				("$maskticks", fmaskticks)
				("$string", fstring)
				("$chain", fchain)
				("$containpointer", fcontainpointer)
				("$containfieldoftype", fcontainfieldoftype)
				("$implement", fimplement)
				("$rawobj", frawobj)
				("$rawfield", frawfield)
				("$toguid", ftoguid)
				("$enumname", fenumname)
				("$strsize", fstrsize)
				("$replace", freplace)
				("$regex", fregex)
				("$split", fsplit)
				("$splitsize", fsplitsize)
				("$tokenize", ftokenize)
				("$a", fa)
				("$stackroot", fstackroot)
				("$thread", fthread)
				("$env", fenv)
				("$ipaddress", fipaddress)
				("$isinstack", fisinstack)
				("$hexstr", fhexstr)
				("$val", fval)
				("$html", fhtml)
				("$tag", ftag)
				("$xml", fxml)
				("$lpad", flpad)
				("$rpad", frpad)
				("$ltrim", fltrim)
				("$rtrim", frtrim)
				("$upper", fupper)
				("$lower", flower)
				("$xmltree", fxmltree)
				("$xmldoc", fxmldoc)
				("$pp", fpp)
				;

				escapes.add("\\a", '\a')("\\b", '\b')("\\f", '\f')("\\n", '\n')
				  ("\\r", '\r')("\\t", '\t')("\\v", '\v')("\\\\", '\\')
				  ("\\\'", '\'')("\\\"", '\"');

            expression
                = conditional
					>> *(   (("==") >> conditional)[&do_eq]
                        |   (("!=") >> conditional)[&do_ne]
                        |   ((">=") >> conditional)[&do_ge]
                        |   (("<=") >> conditional)[&do_le]
                        |   (('>') >> conditional)[&do_gt]
                        |   (('<') >> conditional)[&do_lt]

                        )
					;
			conditional
				= term
                    >> *(   ('+' >> term)[&do_add]
                        |   ('-' >> term)[&do_subt]
                        |   ("&&" >> term)[&do_and]
                        |   ("||" >> term)[&do_or]
                        |   ('&' >> term)[&do_bitand]
                        |   ('|' >> term)[&do_bitor]

                        )
                ;

            term
                =   power
                    >> *(   ('*' >> power)[&do_mult]
                        |   ('/' >> power)[&do_div]
                        |   ('\\' >> power)[&do_mod]
                        )
                ;
			power =
				factor
				>> *(   ('^' >> factor)[&do_power]
					)
				;
            factor
				=   quoted_string
				| (('_' | (alpha_p ))  >> *('.' | alpha_p | digit_p | '_') >> *('[' >> +digit_p >> ']'))[&do_var]
				| number
				| funcs
                |   '(' >> expression >> ')'
                |   ('-' >> factor)[&do_neg]
                |   ('!' >> factor)[&do_not]
                |   ('+' >> factor)
                ;
			funcs
				= (funcsym[&do_addfunc]) >> ('(' >> *(expression % ',') >> ')')[&do_func]
				;
			number
				= strict_real_p[&do_real]
				| ("0n") >> (real_p)[&do_real]
				| "0x" >> (hex64_p[&do_int]) | (hex64_p[&do_int])
				;
				quoted_string = ('"' >> *((escapes) | ("\\x" >> hex_p) | (anychar_p -'"')) >> '"')[&do_string]
				;
        }

        rule<ScannerT> expression, conditional, term, power, factor, funcs, number, quoted_string;
		symbols<funcsenum> funcsym;
		symbols<char const> escapes;
        rule<ScannerT> const&
        start() const { return expression; }
    };
};

//#include "class.h"

// RVA - Relative Value Address
//----------------------------------------------------------------------------
//
// select extension command.
//
// This command displays the MethodTable of a .NET object
//
// The argument string means:
//
//   {;          - No name for the first argument.
//   x,          - The argument is a string until the end of the line.
//   r,          - The argument is required.
//   ;			 - There is no argument's default expression
//   Object;     - The argument's short description is "Object".
//   Object address - The argument's long description.
//   }           - No further arguments.
//
// This extension has a single, optional argument that
// is an expression for the PEB address.
//
//----------------------------------------------------------------------------
using namespace std;


SVAL WEval(std::string Expression)
{

		CALC::st.empty();
		CALC::funcss.empty();
		calculator calc;
		parse_info<> info =  parse(Expression.c_str(), calc, space_p);
		SVAL r;
		r.MakeInvalid();
		if(!info.full || CALC::st.size()!=1) return r;
		r = st.top();
		st.pop();
		return r;
}

EXT_COMMAND(weval,
            "Evaluate ad-hoc commands separated by commas. Use '!whelp weval' for detailed help",
			"{nofield;b,o;;Do not show field.}"
			"{nospace;b,o;;Do not print line feed or spaces between fields.}"
            "{;x,r;;Query;Full Query}")
{
		INIT_API();
		std::string list = GetUnnamedArgStr(0);
		bool nofield = HasArg("nofield");
		bool nospace = HasArg("nospace");
		stack<SVAL> invst;
		calculator calc;
		CALC::st.empty();
		CALC::funcss.empty();
		parse_info<> info =  parse(list.c_str(), calc % ',', space_p);
		while(CALC::st.size()>0)
		{
			invst.push(CALC::st.top());
			st.pop();
		}

		while(invst.size()>0)
		{
			SVAL v = invst.top();

			if(!nofield) Out("%s: ", v.fieldName.c_str());

			if(v.strValue.size() > 0)
				Out("%S", v.strValue.c_str());
			else
				Out("%S", v.prettyPrint.c_str());
			if(!nospace) Out("\n");
			invst.pop();
			if(IsInterrupted())
				return;
		}
		if(!info.full)
		{
			Out("Invalid syntax at %s\n", info.stop);
		}
};

void EXT_CLASS::wfrom_internal(FromFlags flags)
{
	if(flags.farray && flags.fobj)
	{
		Out("Parameter Error: You cannot use -obj and -array together\n");
	}

	std::unique_ptr<Heap> heap(new Heap());
	if(!heap->IsValid())
	{
		Out("Error: Unable to get heap info\n");
		return;
	}
	MatchingAddresses Addresses;
	if(flags.ftype)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}

		std::wstring wtypeStr((wchar_t*)CA2W(flags.typeStr.c_str()));

		indc->GetByType(wtypeStr, Addresses);
	}
	if(flags.ffieldName)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}
		indc->GetByFieldName(flags.fieldName, Addresses);
	}

	if(flags.ffieldtype)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}

		indc->GetByFieldType(flags.fieldType, Addresses);
	}

	if(flags.fimplement)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}

		indc->GetByDerive(flags.implStr, Addresses);
	}

	if(flags.fwithpointer)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}

		indc->GetWithPointers(Addresses);
	}

	if(flags.fmt)
	{
		if(!indc)
		{
			Dml("-type and -mt only work after <link cmd=\"!windex\">!windex</link>\n");
			return;
		}

		indc->GetByMethodName(flags.mtStr, Addresses);
	}
	AddressList addv;
	CALC::MT=0;

	if(flags.fobj)
	{
		addv.push_back(flags.obj);
		Addresses.push_back(&addv);
	}

	CLRDATA_ADDRESS localmt=0;
	if(flags.farray)
	{
		std::vector<CLRDATA_ADDRESS> addrs;
		addrs.clear();
		ObjDetail obj(flags.obj);
		if(!IsCorObj(obj.InnerComponentType()))
		{
			CALC::MT=obj.InnerMT();
		}

		SpecialCases::EnumArray(flags.obj, 0, 0,&addrs);
		addv.clear();
		for(int i=0;i<addrs.size();i++)
		{
			// for !wfrom do not add null values or the loop will stop at the first NULL
			if(addrs[i] == NULL)
			{
				continue;
			}
			addv.push_back((DWORD_PTR)addrs[i]);
		}
		if(addv.size() == 0)
		{
			return;
		}
		Addresses.push_back(&addv);
	}

	AddressEnum adenum;
	if(Addresses.size()==0) return;
	adenum.Start(Addresses);
	ULONG rows = 0;
	ULONG shown = 0;
	while(CALC::Address = adenum.GetNext())
	{
		//Out("Address: %x\n", CALC::Address);
		rows++;
		if(rows % 100 == 0 && IsInterrupted()) return;
		ObjDetail obj;
		if(CALC::MT == 0)
			obj.Request(CALC::Address);
		else
			obj.Request(CALC::Address, CALC::MT);

		if(IsInterrupted())
				return;

		CALC::currObj = &obj;
		std::string Where = flags.cmd;
		calculator calc;
		CALC::st.empty();
		CALC::funcss.empty();
		parse_info<> info = parse(Where.c_str(), *("where" >> calc) >> "select", space_p);
		// if there is no where let's add it
		if(CALC::st.size()==0)
		{
			// ignore the where if it is not there
			do_int(1);
		}
		if(CALC::st.size()==1)
		{
			SVAL v = CALC::st.top();
			//Out("%s: ", v.fieldName.c_str());
			//if(v.strValue.size() > 0)
			//	Out("%S\n", v.strValue.c_str());
			//else
			//	Out("%S\n", v.prettyPrint.c_str());
			CALC::st.pop();
			stack<SVAL> invst;
			if(v.Value.b)
			{
				shown++;
				string next = info.stop;
				boost::trim_right(next);
				info = parse(next.c_str(), calc % ',', space_p);
				while(CALC::st.size()>0)
				{
					invst.push(CALC::st.top());
					st.pop();
				}

				while(invst.size()>0)
				{
					SVAL v = invst.top();

					if(!flags.nofield) Out("%s: ", v.fieldName.c_str());

					if(v.strValue.size() > 0)
						Out("%S", v.strValue.c_str());
					else
						Out("%S", v.prettyPrint.c_str());
					if(!flags.nospace) Out("\n");
					invst.pop();
					if(IsInterrupted())
						return;
				}
				if(!info.full)
				{
					Out("Invalid syntax at %s\n", info.stop);
				}
				if(flags.nospace) Out("\n"); // line feed at the end at least
			}
		} else
		{
			Out("Invalid syntax at %s\n", info.stop);
		}
	}
	if(!flags.nofield && !flags.nospace)
	{
		Out("\n%S Object(s) listed\n", formatnumber((UINT64)shown).c_str());
		if(rows-shown > 0) Out("%S Object(s) skipped by filter\n", formatnumber((UINT64)(rows-shown)).c_str());
	}
}

EXT_COMMAND(wfrom,
            "Dump object fields from Address, Stack or GAC. Use '!whelp wfrom' for detailed help",
			"{nofield;b,o;;Do not show the field name in the result}"
			"{type;s,o;;type;List of types to include wildcards accepted (eg. -type *HttRequest,system.servicemodel.*)}"
			"{mt;s,o;;mt;List of Method Tables to include (eg. -mt 7012ab8,70ac080)}"
			"{fieldname;s,o;;fieldname;List of field names that the type must contain (eg. -fieldname *request,_wr)}"
			"{fieldtype;s,o;;fieldtype;List of field types that the type must contain (eg. -fieldtype *.String,*.object)}"
			"{implement;s,o;;implement;List of parent types that the type must implement (eg. -implement *.Exception,*.Array)}"
			"{withpointer;b,o;;withpointer;List all types which include pointer fields}"
			"{nospace;b,o;;Do not print line feed or spaces between fields.}"
			"{obj;ed,o;;object;object address}"
			"{array;ed,o;;object;array address}"
            "{;x,r;;Query;Full Query}")
{
	INIT_API();

	FromFlags flags;

	//flags.fgac = HasArg("gac");
	//flags.fstack = HasArg("stack");
	flags.ftype = HasArg("type");
	if(flags.ftype)
		flags.typeStr.assign(GetArgStr("type"));
	flags.ffieldName = HasArg("fieldname");
	if(flags.ffieldName)
		flags.fieldName.assign(GetArgStr("fieldname"));
	flags.ffieldtype = HasArg("fieldtype");
	if(flags.ffieldtype)
		flags.fieldType.assign(GetArgStr("fieldtype"));
	flags.fmt = HasArg("mt");
	if(flags.fmt)
		flags.mtStr.assign(GetArgStr("mt"));
	flags.fobj = HasArg("obj");
	if(flags.fobj)
		flags.obj = GetArgU64("obj");
	flags.nofield = HasArg("nofield");
	flags.farray = HasArg("array");
	if(flags.farray)
		flags.obj = GetArgU64("array");
	flags.nospace = HasArg("nospace");
	flags.fimplement = HasArg("implement");
	if(flags.fimplement)
		flags.implStr.assign(GetArgStr("implement"));
	flags.fwithpointer = HasArg("withpointer");
	flags.cmd = GetUnnamedArgStr(0);
	wfrom_internal(flags);
};
