/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#ifndef _CLASSDEF_
#include "clrhelper.h"
#endif
#include "selectparser.h"
#include "SpecialCases.h"

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
// test !wselect * from 00000001556EF770
EXT_COMMAND(wselect,
            "Dump object fields from Address, Stack or GAC. Use '!whelp wselect' for detailed help",
            "{;x,r;;Query;Full Query}")
{
	string selText("wselect ");
	selText.append(GetUnnamedArgStr(0));
	sqlparser::selectobj sql;
	string err = sqlparser::ParseSql(selText, (sqlparser::selectobj *)&sql);
	if(err.size() > 0)
	{
		Out("Syntax error:\n%s\n", err.c_str());
		return;
	}

	// remove later
	//std::string temp("");
	//for(int i=0; i < sql._fieldlist.size(); i++)
	//{
	//	if(temp.length() > 0) temp +=", ";
	//	temp += "[" + sql._fieldlist[i] + "]";
	//}
	//Out("*DEBUG*Top: %i\n", sql._top);
	//Out("*DEBUG*Fields: %s\n", temp.c_str());
	//Out("*DEBUG*From: %s\n", sql._obj.c_str());

	INIT_API();

	UINT64 addr=EvalExprU64(sql._obj.c_str());
	//boost::spirit::qi::uint_parser<UINT64, 16, 1, -1> bighex;
	//bool r=boost::spirit::qi::parse(sql._obj.begin(), sql._obj.end(), bighex , addr);
	ObjDetail obj;
	CLRDATA_ADDRESS parMT = 0;
	if(sql._mt == "")
		obj.Request(addr);
	else
	{
		parMT = EvalExprU64(sql._mt.c_str());
		obj.Request(addr, parMT);
	}
	string knownType = SpecialCases::PrettyPrint(addr, parMT);
	vector<CLRDATA_ADDRESS> listAddr;
	listAddr.clear();
	bool bArr = obj.IsArray();
	if(!bArr)
	{
		listAddr.push_back(addr);
	} else
	{
		SpecialCases::EnumArray(addr, 0, &obj, &listAddr);
	}
	CLRDATA_ADDRESS base = addr;
	DWORD rank = obj.Rank();
	bool bInnerType = !IsCorObj(obj.InnerComponentType());
	CLRDATA_ADDRESS mt = obj.InnerMT();
	int index=0;
	Out("[%S", obj.TypeName().c_str());
	if(obj.IsRuntime())
		Out(" = Runtime Type: %S", obj.GetRuntimeTypeName().c_str());
	Out("]\n");
	if(knownType != "") Out("Known Type Value: %s\n", knownType.c_str());

	for(vector<CLRDATA_ADDRESS>::const_iterator it=listAddr.begin(); it!=listAddr.end(); it++)
	{
		if(IsInterrupted()) return;

		if(*it == NULL)
		{
			index++;
			continue;
		}
		if(bArr)
		{
			Out("***************\n");
			PrintArrayIndex(base, index++, rank);
			if(bInnerType)
			{
				Dml("<link cmd=\"!wselect mt %p * from %p\">mt %p %p</link>\n", mt, *it, mt, *it);
				obj.Request(*it, mt);
			} else
			{
				Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",*it, *it);
				obj.Request(*it);
			}
			addr=*it;
			Out("[%S]\n", obj.TypeName().c_str());
		}
		vector<FieldStore> fields;
		for(int s=0; s < sql._fieldlist.size(); s++)
		{
			if(IsInterrupted()) return;
			ObjDetail *currObj = (ObjDetail *)&obj;
			ObjDetail tempObj;
			vector<string> fieldsDepth;
			bool p=boost::spirit::qi::parse(sql._fieldlist[s].begin(), sql._fieldlist[s].end(),
				+(boost::spirit::qi::alnum | '_' | ':' | '$' | '[' | ']' | '*' | '?') % '.' ,
				fieldsDepth);
			if(fieldsDepth.size() <= 1)
				fields = obj.GetFieldsByName(sql._fieldlist[s]);
			else
			{

				vector<string>::const_iterator it = fieldsDepth.begin();
				while(it!=fieldsDepth.end())
				{
					if(IsInterrupted()) return;

					fields = currObj->GetFieldsByName(*it);
					if(fields.size() != 1)
					{
						Err("Field %s: <INVALID>\n", sql._fieldlist[s].c_str());
						break;
					}
					if(*it == *fieldsDepth.rbegin())
						break;
					if(IsCorObj((CorElementType)fields[0].FieldDesc.corElementType))
					{
						CLRDATA_ADDRESS offset = ObjDetail::GetFieldAddress(currObj->Address(), fields[0].FieldDesc, currObj->IsValueType(), currObj);
						CLRDATA_ADDRESS fieldAddr = ObjDetail::GetPTR(offset);
						if(!tempObj.Request(fieldAddr))
						{
							fields.clear();
							Err("%s: <INVALID>\n", sql._fieldlist[s].c_str());
							break;
						}
						currObj = (ObjDetail*)&tempObj;

						if(!currObj->IsValid())
						{
							// obj is NULL by some extent
							break;
						}
					} else
					if((CorElementType)fields[0].FieldDesc.corElementType == ELEMENT_TYPE_VALUETYPE)
					{
						if(!tempObj.Request(ObjDetail::GetFieldAddress(currObj->Address(), fields[0].FieldDesc, currObj->IsValueType(), currObj), fields[0].FieldDesc.MethodTable))
						{
							fields.clear();
							Err("%s: <INVALID>\n", sql._fieldlist[s].c_str());
							break;
						}

						currObj = (ObjDetail*)&tempObj;
						if(!currObj->IsValid())
						{
							// obj is NULL by some extent
							break;
						}
					} else
					// it is not a pointer so it has to be last
					{
						if(*it != *fieldsDepth.rbegin())
						{
							Err("Field %S: <INVALID>\n", sql._fieldlist[s]);
							break;
						}
					}
					it++;
				}
				if(fields.size()>0) fields[0].FieldName.assign(CA2W(sql._fieldlist[s].c_str()));
			}
			if(currObj->IsString())
			{
					Out("String: %S\n", ObjDetail::String(addr).c_str());
			}

			for(int i=0; i<fields.size(); i++)
			{
				if(IsInterrupted()) return;

				if(fields[i].FieldDesc.isStatic && !fields[i].FieldDesc.isThreadStatic)
				{
					Out("static ");
				}
				if(fields[i].FieldDesc.isThreadStatic)
				{
					Out("thread local ");
				}

				if(IsCorObj((CorElementType)fields[i].FieldDesc.corElementType))
				{
					CLRDATA_ADDRESS ptr = ObjDetail::GetPTR(ObjDetail::GetFieldAddress(currObj->Address(), fields[i].FieldDesc, currObj->IsValueType(),currObj));
					if(fields[i].mtName == L"System.Object" || fields[i].mtName == L"System.__Canon")
					{
						Dml("%S %S = <link cmd=\"!wselect * from %p\">%p</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr, ptr);
						if(ptr!=NULL)
						{
							ObjDetail objTemp(ptr);
							if(objTemp.IsValid())
							{
								if(objTemp.IsString())
								{
									Out(" %S",
										objTemp.String().c_str());;
								}
							}
							string pretty = SpecialCases::PrettyPrint(ptr);
							if(pretty.size() > 0)
							{
								Out(" %s", pretty.c_str());
							}
						}
						Out("\n");
					} else
					{
						if(fields[i].FieldDesc.isString)
						{
							Dml("%S %S = <link cmd=\"!wselect * from %p\">%p</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr, ptr);
							Out(" %S\n",
								currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
						} else
						{
							Dml("%S %S = <link cmd=\"!wselect * from %p\">%S</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr,
								currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
							string pretty = SpecialCases::PrettyPrint(ptr);
							if(pretty.size() > 0)
							{
								Out(" %s", pretty.c_str());
							}
							Out("\n");
						}
					}
				} else
				if((CorElementType)fields[i].FieldDesc.corElementType == ELEMENT_TYPE_VALUETYPE)
				{
					CLRDATA_ADDRESS ptr = ObjDetail::GetFieldAddress(addr, fields[i].FieldDesc, currObj->IsValueType(),currObj);
					//Dml("<link cmd=\"!wdo -mt %p %p\">%S</link>", field->MTOfType ,ptr, ObjDetail::ValueString(*field, addr, true).c_str());
					Dml("%S %S = <link cmd=\"!wselect mt %p * from %p\">%S</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(),
						fields[i].FieldDesc.MethodTable, ptr,
						currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
					wstring methName = GetMethodName(fields[i].FieldDesc.MethodTable);
					if(methName == L"System.DateTime")
					{
						ExtRemoteData dt(ptr, sizeof(UINT64));
						UINT64 ticks = dt.GetUlong64();
						Out(" %S", tickstodatetime(ticks).c_str());
					}
					if(methName == L"System.TimeSpan")
					{
						ExtRemoteData dt(ptr, sizeof(UINT64));
						UINT64 ticks = dt.GetUlong64();
						Out(" %s", tickstotimespan(ticks).c_str());
					}
					if(methName == L"System.Guid")
					{
						Out(" %s",SpecialCases::ToGuid(ptr).c_str());
					}

					Out("\n");
				} else
				{
					Out("(%s)%S %S = %S", TypeString((CorElementType)fields[i].FieldDesc.corElementType).c_str(), fields[i].mtName.c_str(), fields[i].FieldName.c_str(),
						obj.ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
					CLRDATA_ADDRESS ptr = ObjDetail::GetFieldAddress(addr, fields[i].FieldDesc, currObj->IsValueType(),currObj);
					if(IsInt((CorElementType)fields[i].FieldDesc.corElementType) && SpecialCases::IsEnumType(fields[i].FieldDesc.MethodTable))
					{
						UINT64 v;
						ExtRemoteData dt(ptr,IntSize((CorElementType)fields[i].FieldDesc.corElementType));

						switch(IntSize((CorElementType)fields[i].FieldDesc.corElementType))
						{
							case 1:
								v = dt.GetUchar();
								break;
							case 2:
								v = dt.GetUshort();
								break;
							case 4:
								v = dt.GetUlong();
								break;
							case 8:
								v = dt.GetUlong64();
								break;
						}
						Out(" %S", SpecialCases::GetEnumString(fields[i].FieldDesc, v).c_str());
					}
					Out("\n");
				}
			}
		}
	}
};
