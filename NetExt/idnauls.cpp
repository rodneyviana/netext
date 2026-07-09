#include "NetExt.h"
#include "SPCategories.h"
#ifdef GetContext
#undef GetContext
#endif
#include "inc\DbgModel.h"
#include "CLRHelper.h"



wchar_t Buffer[MAX_MTNAME] = { 0 };

struct UlsInstance
{
	UINT32 tag;
	CLRDATA_ADDRESS SevLevel;
	UINT32 Category;
	CLRDATA_ADDRESS Message;
	CLRDATA_ADDRESS  Sequence;
	CLRDATA_ADDRESS  Steps;
	UINT32 Index;
	std::wstring IDnaPosition()
	{
		swprintf_s((wchar_t*)&Buffer, MAX_MTNAME, L"%x:%x", Sequence, Steps);
		return (wchar_t*)&Buffer;
	}
};

#define IfFailedReturn(x) if(FAILED(x)) return E_FAIL;

void DisplayFound(std::string &FullMessage, std::string& Part)
{
	if (Part.size() == 0)
	{
		g_ExtInstancePtr->Out("%s", FullMessage.c_str());
		return;
	}
	size_t i = 0;
	size_t p = 0;
	while (i < FullMessage.size() - Part.size())
	{
		p = FullMessage.find(Part, i);
		if (p == std::string::npos)
		{
			g_ExtInstancePtr->Out("%s", FullMessage.substr(i).c_str());
			break;
		}
		g_ExtInstancePtr->Out("%s", FullMessage.substr(i, p - i).c_str());
		g_ExtInstancePtr->Dml("<col fg=\"wbg\" bg=\"srccmnt\">%s</col>", Part.c_str());
		i = p+Part.size();
	}
	

}

bool IsModuleEverLoaded(wstring ModuleName)
{
//#define Out g_ExtInstancePtr->Out
	auto moduleLower = ModuleName;
	std::transform(moduleLower.begin(), moduleLower.end(),
		moduleLower.begin(),
		towlower);
#if _DEBUG
	g_ExtInstancePtr->Out("Query [%s]\n", moduleLower.c_str());
#endif
	std::wstring query = L"@$curprocess.TTD.Events.Where(t => t.Type == \"ModuleLoaded\").Where(t => t.Module.Name.ToLower().Contains(\"";
	query.append(moduleLower);
	query.append(L"\"))");
	CComPtr<IHostDataModelAccess> client;
	HRESULT Status;
	Status = g_ExtInstancePtr->m_Client->QueryInterface(__uuidof(IHostDataModelAccess), (PVOID*)&client);
	if (Status != S_OK)
	{
		return false;
	}
	CComPtr<IDebugHost> pHost;
	CComPtr<IDataModelManager> pManager;
	if (FAILED(client->GetDataModel(&pManager, &pHost)))
	{
		g_ExtInstancePtr->Out("Data Model could not be acquired\n");
		return false;
	}

	CComPtr<IDebugHostEvaluator2> hostEval;
	CComPtr<IModelObject> spObject;
	pHost->QueryInterface(IID_PPV_ARGS(&hostEval));

	if (!SUCCEEDED(hostEval->EvaluateExtendedExpression(USE_CURRENT_HOST_CONTEXT, query.c_str(), nullptr, &spObject, nullptr)))
	{
		g_ExtInstancePtr->Out("Expression could not be evaluated\n");
		return false;
	};

	CComPtr<IModelObject> pListOfBreaks;
	CComPtr<IIterableConcept> spIterable;
	if (SUCCEEDED(spObject->GetConcept(__uuidof(IIterableConcept), (IUnknown**)&spIterable, nullptr)))
	{
		CComPtr<IModelIterator> spIterator;
		if (SUCCEEDED(spIterable->GetIterator(spObject, &spIterator)))
		{
			CComPtr<IKeyStore> spContainedMetadata;
			CComPtr<IModelObject> pBreakItem;
			HRESULT hr = spIterator->GetNext(&pBreakItem, 0, nullptr, &spContainedMetadata);
			if (hr == E_BOUNDS || hr == E_ABORT)
			{
				return false;
			}
			return true;
		}
	}
	return false;


}
HRESULT MoveTo(IModelObject *spStart)
{

									 //
									 // SeekTo is a key on the object just like anything else.  The value of the key is a method.
									 //
	CComPtr<IModelObject> spSeekToMethod;
	IfFailedReturn(spStart->GetKey(L"SeekTo", &spSeekToMethod, nullptr));

	//
	// Before we arbitrarily go about using it as a method, do some basic validation.
	//
	ModelObjectKind mk;
	IfFailedReturn(spSeekToMethod->GetKind(&mk));
	if(mk != ObjectMethod)
	{
		return E_FAIL;
	}

	//
	// ObjectMethod indicates that it is an IModelMethod packed into punkVal.  You can QI to be extra
	// safe if desired.
	//
	VARIANT vtMethod;
	IfFailedReturn(spSeekToMethod->GetIntrinsicValue(&vtMethod));
	//ASSERT(vtMethod.vt = VT_UNKNOWN); // guaranteed by ObjectMethod
	CComPtr<IModelMethod> spMethod;  // or whatever mechanism you want to guarantee the variant gets cleared.  variant_ptr, …
	spMethod.Attach(static_cast<IModelMethod *>(vtMethod.punkVal));

	//
	// Call the method (passing no arguments).  The result here is likely to be ObjectNoValue (there is no return value).
	//
	CComPtr<IModelObject> spCallResult;
	IfFailedReturn(spMethod->Call(spStart, 0, nullptr, &spCallResult, nullptr));


	return S_OK;
}

EXT_COMMAND(widnauls,
	"Command to list ULS position and tag and can be filtered by message or category",
	"{nomessage;b,o;;Do not show the ULS log message (faster processing).}"
	"{tag;s,r;;Tag to search for (e.g.: -tag b4ly). Use * for all tags. Required}"
	"{category;b,o;;Search text in Category or Product and not in message (e.g. -category Claims). Faster processing. Severity not searched}"
	"{message;b,o;;Search text in message and not in category or product (e.g. -message disk is full). Slower processing.}"
	"{;x,o;;Optional filter for message (-message) or category (-category) (e.g.: -message disk is full). Must be the last parameter}")
{
	wasInterrupted = false;
	UINT64 startTime, endTime;

	if(!IsiDNA())
	{
		Out("This dump is not of iDNA type. This command only works on iDNA dumps.\n");
		return;
	}
	std::string tag = GetArgStr("tag");

	std::string mess;
	if (HasUnnamedArg(0))
		mess = GetUnnamedArgStr(0);

	bool nomess = HasArg("nomessage");

	bool message = HasArg("message");

	bool catonly = HasArg("category");

	if (catonly && message)
	{
		Out("Error: You have to use either -category or -message. Never both\n\n");
		Out("No search was performed\n");
		return;
	}

	bool isOnetLoaded = IsModuleEverLoaded(L"onetnative.dll");
	bool isSPLoaded = IsModuleEverLoaded(L"Microsoft.Office.Server.Native.dll");

		if (!isOnetLoaded && !isSPLoaded)
	{
		Out("This process does not contain the necessary SharePoint modules\n");
		return;
	}

	std::wstring modules;

	if (isOnetLoaded)
	{
		modules.assign(L"\"onetnative!ULSSendFormattedTrace\"");
	}

	if (isSPLoaded)
	{
		if (modules.size() != 0)
		{
			modules.append(L", ");
		}
		modules.append(L"\"Microsoft_Office_Server_Native!ULSSendFormattedTrace\"");
	}


	if (message && tag == "*")
	{
		Out("Warning: When you combine -tag * and -message, it creates a very inefficient query.\n");
		Out("         -tag * will retrieve all ULS log entry and -message will require to move to an iDNA possition every time.\n");
		Out("         -tag <tag> will only retrieve the ULS logs with this tag and then move to the position to retrieve the message.\n");
		Out("Information: Notice that -tag * and -category is ok and still very fast as category is also filtered without moving to the position.\n\n");

	}

	if ((catonly || message) && mess.size() == 0)
	{
		Out("Error: -category or -message require a filter pattern\n");
		Out("Example: !idnauls -tag ag9cq -message User was authentticated\n");
		Dml("3dbba3:1254	SharePoint Foundation	Claims Authentication	High	ag9cq\t<b>User was authenticated</b>. Checking permissions.\n\n");
		Out("Example: !idnauls -tag * -category Web Content Management\n");
		Dml("3de9ae:14c0	<b>Web Content Management</b>	Publishing Cache	High	ag0ld\n");
		Out("No search was performed\n");
		return;
	}
	map<int, int> catVector;

	if (catonly)
	{
		
		catVector = SPCategories::GetListAreaName(mess);
		if (catVector.size() == 0)
		{
			Out("No category/product contains '%s'\n", mess.c_str());
			Out("No search was performed\n");
			return;
		}
		Out("Warning: The string '%s' will only be searched on Category/Product, not in message\n", mess.c_str());
		mess.clear();
	}


	if (tag != "*" && (tag.size() < 4 || tag.size() > 5))
	{
		Out("Tag: '%s' is invalid\n", tag.c_str());
		Out("It can be either '*' for all or be between 4 and 5 bytes\n");
		return;
	}
	unsigned int tagBin = StrToTag(tag);

	if (tagBin == 0 && tag != "*")
	{
		Out("Tag: '%s' is invalid\n", tag.c_str());
		Out("It does not contain a valid tag sequence\n");
		return;
	}


	if (tag == "*")
	{
		swprintf_s(Buffer, MAX_MTNAME, L"@$cursession.TTD.Calls(%s).Select(c=> new { param1 = c.Parameters[0], param2 = c.Parameters[1], param3 = c.Parameters[2], param4 = c.Parameters[3], start = c.TimeStart, sequence = c.TimeStart.Sequence, steps = c.TimeStart.Steps } )", modules.c_str());
	}
	else
	{
		swprintf_s(Buffer, MAX_MTNAME, L"@$cursession.TTD.Calls(%s).Where(c => c.Parameters[0] == 0x%x).Select(c=> new { param1 = c.Parameters[0], param2 = c.Parameters[1], param3 = c.Parameters[2], param4 = c.Parameters[3], start = c.TimeStart, sequence = c.TimeStart.Sequence, steps = c.TimeStart.Steps } )", modules.c_str(), tagBin);
	}

	std::wstring query(Buffer);

	Out("The TTD query to obtain a similar result is:\ndx -g %S\n", query.c_str());

#if _DEBUG

	Out("%s = ", tag.c_str());
	Out("%x	\n", tagBin);
	Out("dx %S\n", query.c_str());
#endif
	CComPtr<IHostDataModelAccess> client;
	HRESULT Status;
	REQ_IF(IHostDataModelAccess, client);

	CComPtr<IDebugHost> pHost;
	CComPtr<IDataModelManager> pManager;
	if (FAILED(client->GetDataModel(&pManager, &pHost)))
	{
		Out("Data Model could not be acquired\n");
		return;
	}

	CComPtr<IDebugHostEvaluator2> hostEval;
	CComPtr<IModelObject> spObject;
	pHost->QueryInterface(IID_PPV_ARGS(&hostEval));

	startTime = GetTickCount64();
	if (!SUCCEEDED(hostEval->EvaluateExtendedExpression(USE_CURRENT_HOST_CONTEXT, query.c_str(), nullptr, &spObject, nullptr)))
	{
		Out("Expression could not be evaluated\n");
		return;
	};


	CComPtr<IModelObject> pListOfBreaks;
	CComPtr<IIterableConcept> spIterable;
	if (SUCCEEDED(spObject->GetConcept(__uuidof(IIterableConcept), (IUnknown**)&spIterable, nullptr)))
	{
		CComPtr<IModelIterator> spIterator;
		if (SUCCEEDED(spIterable->GetIterator(spObject, &spIterator)))
		{
			//
			// We have an iterator.  Error codes have semantic meaning here.  E_BOUNDS indicates the end of iteration.  E_ABORT indicates that
			// the debugger host or application is trying to abort whatever operation is occurring.  Anything else indicates
			// some other error (e.g.: memory read failure) where the iterator MIGHT still produce values.

			std::vector<UlsInstance> queryResult; // It will store the list of parameters

			UINT32 Index = 0;
			//
			for (;;)
			{

				CComPtr<IModelObject> pBreakItem;
				CComPtr<IKeyStore> spContainedMetadata;

				HRESULT hr = spIterator->GetNext(&pBreakItem, 0, nullptr, &spContainedMetadata);
				if (hr == E_BOUNDS || hr == E_ABORT)
				{
					break;
				}


				if (FAILED(hr))
				{

					Out("There was a failure at an Item\n");
					continue;
					//
					// Decide how to deal with failure to fetch an element.  Note that pBreakItem *MAY* contain an error object
					// which has detailed information about why the failure occurred (e.g.: failure to read memory at address X).
					//
				}

				//
				// Read the values
				//
				CComPtr<IModelObject> tag;
				CComPtr<IModelObject> SevLevel;
				CComPtr<IModelObject> Category;
				CComPtr<IModelObject> Message;
				CComPtr<IModelObject> Start;
				CComPtr<IModelObject> Sequence;
				CComPtr<IModelObject> Steps;



				VARIANT vt_tag, vt_sevlevel, vt_category, vt_message, vt_sequence, vt_steps;

				if (FAILED(hr = pBreakItem->GetKeyValue(L"param1", &tag, NULL /* &spContainedMetadata */))) { Out("Error reading param1"); continue; }
				hr = tag->GetIntrinsicValue(&vt_tag);
				//hr = tag->GetIntrinsicValueAs(VT_INT_PTR, &vt_tag);
				if (FAILED(hr = pBreakItem->GetKeyValue(L"param2", &Category, NULL /* &spContainedMetadata */))) { Out("Error reading param2"); continue; };
				hr = Category->GetIntrinsicValue(&vt_category);
				if (FAILED(hr = pBreakItem->GetKeyValue(L"param3", &SevLevel, NULL /* &spContainedMetadata */))) { Out("Error reading param3"); continue; };
				hr = SevLevel->GetIntrinsicValue(&vt_sevlevel);

				if (FAILED(hr = pBreakItem->GetKeyValue(L"param4", &Message, NULL /* &spContainedMetadata */)))
				{
					Out("Error reading param4");		continue;
				};
				hr = Message->GetIntrinsicValue(&vt_message);
				if (FAILED(hr = pBreakItem->GetKeyValue(L"start", &Start, NULL /* &spContainedMetadata */)))
				{
					Out("Error reading start"); continue;
				};
				//hr = Start->GetIntrinsicValue(&vt_start); // It fails here because the type in ObjectSynthetic
				if (FAILED(hr = pBreakItem->GetKeyValue(L"sequence", &Sequence, NULL /* &spContainedMetadata */)))
				{
					Out("Error reading sequence");		continue;
				};
				hr = Sequence->GetIntrinsicValue(&vt_sequence);

				if (FAILED(hr = pBreakItem->GetKeyValue(L"steps", &Steps, NULL /* &spContainedMetadata */)))
				{
					Out("Error reading steps");	continue;
				};
				hr = Steps->GetIntrinsicValue(&vt_steps);

				if (IsInterrupted())
				{
					break;
				}
				UlsInstance obj;


				obj.Category = vt_category.uintVal;
				obj.Message = vt_message.llVal; //vt_category.llVal);
				obj.SevLevel = vt_sevlevel.uintVal;
				obj.Sequence = vt_sequence.uintVal;
				obj.Steps = vt_steps.llVal;
				obj.tag = vt_tag.uintVal;
				obj.Index = Index;
				if (catVector.size() > 0)
				{
					if (catVector.find((int)obj.Category) == catVector.end())
					{
						continue;
					}
				}

				// Only move if necessary
				bool show = true;
				string fullMess;
				
				if ((mess.size() > 0 || !nomess) && SUCCEEDED(MoveTo(Start)))
				{
					CComBSTR stringConv;
					CComPtr<IDebugHostContext> context;
					CComPtr<IDebugHostMemory> memory;
					if (SUCCEEDED(hr = pHost->QueryInterface(__uuidof(IDebugHostMemory), (void**)&memory)))
					{
						ULONG64 BytesRead = 0;
						Location loc;
						loc.HostDefined = 0;
						loc.Offset = obj.Message;
						if (SUCCEEDED(hr = memory->ReadBytes(USE_CURRENT_HOST_CONTEXT, loc, Buffer, MAX_MTNAME * 2, &BytesRead)))
						{
							
							Buffer[MAX_MTNAME - 1] = L'\0';
							fullMess = localCW2A(Buffer);
							if (mess.size() > 0)
							{
								show = fullMess.find(mess) != std::string::npos;
							}
						}

					}
					if (hr != S_OK)
					{
						fullMess = "*** Unable to read memory ***";
						show = true;
					}

				}
				if (show)
				{
					Index++;
					Dml("<link cmd=\"!tt %S\">%S</link>\t", obj.IDnaPosition().c_str(), obj.IDnaPosition().c_str());
					string area;
					string prod;
					string sev = SPCategories::GetSevLevel(obj.SevLevel);

					SPCategories::GetAreaName(obj.Category, area, prod);
					if (!nomess)
					{
						SYSTEMTIME time;
						if (!GetTime(time, true))
						{
							Out("??/??/???? ??:??:??.??\t");
						}
						else
						{
							Out("%02i/%02i/%04i %02i:%02i:%02i.%02i\t", time.wMonth, time.wDay, time.wYear, time.wHour,
								time.wMinute, time.wSecond, time.wMilliseconds / 10);
						}
					}
					Out("%s\t", area.c_str());
					Out("%s\t", prod.c_str());
					Out("%s\t", sev.c_str());



					Out("%s\t", TagToStr(obj.tag).c_str());
					
					if (!nomess)
					{
						if (mess.size() > 0 && !catonly)
							DisplayFound(fullMess, mess);
						else
							Out("%s", fullMess.c_str());
					}
					queryResult.push_back(obj);
					Out("\n");
				}



			}
			Out("%u Instances\n", Index);
			endTime = GetTickCount64();
			Out("Search took %f seconds\n", (float)(((float)endTime - (float)startTime) / (float)1000));
		}
	}
}
