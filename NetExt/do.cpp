/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "NetExt.h"
#include "SpecialCases.h"
#include "CLRHelper.h"
#include <string>





EXT_COMMAND(wdo,
			"Dump object. Use '!whelp wdo' for detailed help",
			"{mt;ed,o;;mt,Method table for value objects}"
			"{;e,r;;Address,Object Address}"
			"{start;ed,o;;Starting index to show in an array}"
			"{end;ed,o;;Ending index to show in an array}"
			"{forcearray;b,o;;For Byte[] and Char[] arrays show items not the string}"
			"{shownull;b,o;;For arrays will show items that are null}"
			"{noheader;b,o;;Display only object address and field and values}"
			"{noindex;b,o;;For arrays it will show values without the index}"
			"{tokens;b,o;;Display class and field token}"

			)
{
	DO_INIT_API;

	using namespace NetExtShim;
	CComPtr<IMDType> type;

	HRESULT hr = (pHeap == NULL);
	EXITPOINT("Unable to get managed heap");
	CLRDATA_ADDRESS mt=0;
	UINT64 start = 0;
	INT64 end = -1;
	if(HasArg("mt"))
		mt=GetArgU64("mt");
	if(HasArg("start"))
		start=GetArgU64("start");
	if(HasArg("end"))
		end=GetArgU64("end");

	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);


	bool shownull = HasArg("shownull");
	bool header = !HasArg("noheader");
	bool forcearray = HasArg("forcearray");
	bool showindex = !HasArg("noindex");
	bool tokens = HasArg("tokens");

	ObjDetail obj;
	try
	{
		if(!mt)
			obj.Request(addr);
		else
			obj.Request(addr, mt);
		if(!obj.IsValid())
			hr=E_FAIL;
	} catch(...)
	{
		hr=E_FAIL;
	}
	EXITPOINT("This is not a valid object");



	if(header) Out("Address: %p\n", addr);


	MD_TypeData data = obj.TypeData();

	if(obj.IsRuntime())
	{
		Out("*** Runtime Type: %S\n", obj.GetRuntimeTypeName().c_str());
	}

	if(obj.IsDebugModule())
	{
		Out("*** WARNING: Type from a module compiled in Debug Mode. Not recommended in production ***\n");
	}
	if(header) Out("Method Table/Token: %p/%x04 \n", obj.MethodTable(), obj.Token());


	if(header)
	{
		Out("Class Name: %S\n", obj.TypeName().c_str());
		Out("Size : %u\n", obj.Size());

	}
	if(obj.IsFree())
		return;
	if(header) Out("EEClass: %p\n", obj.EEClassAddr());

	if(data.rank > 0)
	{
		if(header) Out("Rank: %i ", obj.Rank());
		if(header) PrintArrayBounds(addr, obj.Rank());
		if(header) Out("\n");
		if(header) Out("Components: %i\n", obj.NumComponents());
		if(header) Out("Data Start: %p\n", obj.DataPtr());

		if(!forcearray && obj.NumComponents() > 0)
		{
			if(obj.TypeName() == L"System.Byte[]")
			{
				std::string strAddr(".printf \"%ma\",");
				strAddr.append(formathex(obj.DataPtr()));
				string result = Execute(strAddr);
				Out("%s\n", result.c_str());
				return;
			}
			if(obj.TypeName() == L"System.Char[]")
			{
				string strAddr(".printf \"%mu\",");
				strAddr.append(formathex(obj.DataPtr()));
				string result = Execute(strAddr);
				Out("%s\n", result.c_str());
				return;
			}
		}
		if(obj.NumComponents() > 0)
		{
			SpecialCases::DumpArray(addr,mt,start,end);
		}
		return;

	}

	if(header)
	{
		Out("Instance Fields: %i\n", obj.InstanceFieldsCount());
		Out("Static Fields: %i\n", obj.StaticFieldsCount());
		Out("Total Fields: %i\n", obj.InstanceFieldsCount()+obj.InstanceFieldsCount());
		Out("Heap/Generation: %i/%i\n", obj.Heap(), obj.Gen());
		Out("Module: %p", obj.Module());
		if(obj.Module() == 0)
			Out(" (Dynamic type)");
		Out("\n");
		Out("Assembly: %p\n", obj.Assembly());
		Out("Domain: %p\n", obj.Domain());
		Out("Assembly Name: %S\n", obj.AssemblyName().c_str());
		Out("Inherits: %S (%S)\n",obj.classObj.ChainTypeStr().c_str(),obj.classObj.ChainMTtr().c_str());
	}

	if(obj.IsString())
	{

		if(header)
			Out("String: %S\n", obj.String().c_str());
		else
			Out("%S\n", obj.String().c_str());
	}
	std::vector<FieldStore> fields = obj.classObj.Fields();
	for(int i=0;i<fields.size();i++)
	{

		MD_FieldData data = fields[i].FieldDesc;
		if(tokens)
			Out("%04x ", data.token);
		Out("%p", data.MethodTable);
		if(data.isStatic)
			Out(" Static ");
		else
			Out("        ");
		Out(" %40.40S", fields[i].mtName.c_str());




		Out(" +%04x %40.40S ",data.offset, fields[i].FieldName.c_str());

		CLRDATA_ADDRESS offset = ObjDetail::GetFieldAddress(addr,data,mt!=0);
		if(offset)
		{
			if(IsReal((CorElementType)data.corElementType) || IsInt((CorElementType)data.corElementType)
				|| (CorElementType)data.corElementType == ELEMENT_TYPE_BOOLEAN)
			{
				std::wstring vl = ObjDetail::ValueString(data, addr, mt!=0);
				Out("%S", vl.c_str());

				if(data.IsEnum)
				{
					SVAL v=GetValue(offset,(CorElementType)data.corElementType,data.MethodTable);
					Out(" %S", SpecialCases::GetEnumString(data, v.Value.u64).c_str());
				}
			} else
			{
				SVAL s = GetValue(offset,(CorElementType)data.corElementType,data.MethodTable,"<link cmd=\"!wdo %p\">%p</link>","<link cmd=\"!wdo -mt %p %p\">%S</link>", true);
				string pretty;
				try
				{
					if(IsCorObj((CorElementType)data.corElementType) || data.isString)
						pretty = SpecialCases::PrettyPrint(ObjDetail::GetPTR(offset),0);
					else
						pretty = SpecialCases::PrettyPrint(offset, data.MethodTable);
					if(pretty.size() > 0)
					{
						Out(" %s", pretty.c_str());
					}
				} catch(...)
				{
				}
			}
		} else
		{
			Out("%p", NULL);
		}
		Out("\n");


	}
	Out("\n");

}