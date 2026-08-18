/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "specialcases.h"
#include "Indexer.h"
#include <cstring>
#include <map>


#define GetArraySel(title, query) \
    if(v.Value.ptr) \
	{ \
		if(strlen(title)) \
		{ \
		Out("\n"); \
		Out(title); \
		Out("\n================================\n"); \
		} \
		FromFlags flags; \
		ZeroMemory(&flags, sizeof(flags)); \
		flags.cmd = query; \
		flags.farray = true; \
		flags.nofield = true; \
		flags.nospace = true; \
		flags.obj = v.Value.ptr; \
		wfrom_internal(flags); \
	}

#define GetObjSel(title, query) \
    if(v.Value.ptr) \
	{ \
		Out("\n"); \
		if(strlen(title)) \
		{ \
			Out(title); \
			Out("\n================================\n"); \
		} \
		FromFlags flags; \
		ZeroMemory(&flags, sizeof(flags)); \
		flags.cmd = query; \
		flags.fobj = true; \
		flags.nofield = true; \
		flags.nospace = true; \
		flags.obj = v.Value.ptr; \
		wfrom_internal(flags); \
	}

void EXT_CLASS::HashInternal(CLRDATA_ADDRESS Addr)
{
	if(Addr == NULL)
	{
		Out("Hash Table is null\n");
	}
	ObjDetail obj(Addr);
	if(!obj.IsValid() || !obj.classObj.Implement(L"System.Collections.Hashtable"))
	{
		Out("Object at %p is invalid or not of type [System.Collections.Hashtable]\n", Addr);
	}
	std::vector<std::string> fields;
	fields.push_back("buckets");
	fields.push_back("count");
	varMap fieldV;
	DumpFields(Addr,fields,0,&fieldV);
	int n=fieldV["count"].Value.i32;
	CLRDATA_ADDRESS bucket = fieldV["buckets"].Value.ptr;
	CLRDATA_ADDRESS mt = fieldV["buckets"].MT;
	//Out("Items   : %i\n", n);
	Out("Buckets : %p\n\n", bucket);
	if(n==0 || bucket == NULL)
	{
		Out("Empty table\n");
		return;
	}
	std::vector<CLRDATA_ADDRESS> addresses;
	obj.Request(bucket);
	mt=obj.InnerMT();
	SpecialCases::EnumArray(bucket,0,NULL,&addresses);
	std::vector<CLRDATA_ADDRESS>::const_iterator it=addresses.begin();
	int i=0;
	while(it!=addresses.end())
	{
		if(*it != NULL)
		{
			fieldV.clear();
			fields.clear();
			fields.push_back("key");
			fields.push_back("val");
			fields.push_back("hash_coll");
			DumpFields(*it,fields,mt,&fieldV);
			if(fieldV["hash_coll"].Value.i32 != 0)
			{
				Out("[%i]:=========================================\n",i++);
				fields.pop_back();
				DumpFields(*it,fields,mt);
			}
		}
		it++;
	}
}


struct IntPair
{
	int count;
	int finished;
};

#define UpdatePair(Key, Value) \
{\
	string svalue(CW2A(Value.c_str())); \
	string KeyPair = Key+"="+svalue; \
    if(summary.count(KeyPair) == 0) \
	{ \
		IntPair nullPair = {0}; \
		summary[KeyPair] = nullPair; \
	} \
	summary[KeyPair].count++; \
	if(finished) summary[KeyPair].finished++; \
};


EXT_COMMAND(wruntime,
	"Dump Http Runtime information as Active Requests and App Domain Id",
	"{{custom}}"
	)
{
	INIT_API();
	Out("Runtime Settings per Application Pool\n");
	FromFlags flags;
	ZeroMemory(&flags, sizeof(flags));
	flags.cmd = "where ((!_beforeFirstRequest) || _shutdownReason) \"\\n=========================================================================\\n\",\"Address         : \",$addr(),\"\\nFirst Request   : \",$tickstodatetime(_firstRequestStartTime.dateData),\"\\nRuning Time     : \",$tickstotimespan($now() - _firstRequestStartTime.dateData),\"\\nApp Pool User   : \",_wpUserId,\"\\nTrust Level     : \",_trustLevel,\"\\nApp Domnain Id  : \",_appDomainId,\"\\nDebug Enabled   : \",$if(_debuggingEnabled,\"True (Not recommended in production)\",\"False\"),\"\\nActive Requests : \",_activeRequestCount,\"\\nPath            : \",_appDomainAppPath,$if(_isOnUNCShare,\" (in a share)\",\" (local disk)\"),\"\\nTemp Folder     : \",_tempDir,\"\\nCompiling Folder: \",_codegenDir,\"\\nShutdown Reason : \",$if(_shutdownReason,$enumname(_shutdownReason)+\" at \"+$tickstodatetime(_lastShutdownAttemptTime.dateData),\"Not shutting down\"),\"\\n\\n\",$if(_shutdownReason,_shutDownMessage+\"n\"+_shutDownStack,\"\")";
	
	flags.ftype = true;
	flags.nofield = true;
	flags.nospace = true;
	flags.typeStr = "System.Web.HttpRuntime";
	wfrom_internal(flags);
}

EXT_COMMAND(wtoken,
	"Dump all security tokens or matching a token filter criteria. Use '!whelp wtoken' for detailed help",
	"{;e,o;;Address, HttpContext or SessionSecurityToken Address (optional)}"
	"{name;s,o;;Dump only with this name. Optional (e.g -name *name)}"
	"{value;s,o;;Dump only if value match. Optional (e.g. -value a4ghj8abcd*)}"
	)
{
	INIT_API();
	CLRDATA_ADDRESS addr = 0;
	bool httpContext = false;
	if(HasUnnamedArg(0)) 
	{
			addr=GetUnnamedArgU64(0);
			ObjDetail obj(addr);
			if(!obj.IsValid())
			{
				Out("Error: Could not find a valid object at %p\n", addr);
				return;
			}
			if(obj.TypeName() != L"System.Web.HttpContext" && !obj.classObj.Implement(L"Microsoft.IdentityModel.Tokens.SessionSecurityToken")
				&& !obj.classObj.Implement(L"System.IdentityModel.Tokens.SessionSecurityToken"))
			{
				Out("Error: Expected type HttpContext or SessionSecurityToken. Found: %S\n", obj.TypeName().c_str());
				return;
			}
			httpContext = (obj.TypeName() == L"System.Web.HttpContext");
	}
	string name;
	string value;

	if(HasArg("name")) name=GetArgStr("name");
	if(HasArg("value")) value=GetArgStr("value");
	if(!indc && !addr)
	{
		Dml("To list all cookies, run <link cmd=\"!windex;!wtoken\">!windex</link> first\n");
		return;
	}
	if(httpContext)
	{
		std::vector<std::string> fields;
		fields.push_back("_items");
		fields.push_back("_request._url.m_String");
		fields.push_back("_request._filePath._virtualPath");
		varMap fieldV;
		DumpFields(addr,fields,0,&fieldV);
		Out("HttpContext    :  ");
		Dml("<link cmd=\"!whttp %p\">%p</link> ", addr, addr);

		if(fieldV["_request._url.m_String"].strValue.size() > 0)
		{
			Out("%S\n\n",fieldV["_request._url.m_String"].strValue.c_str());
		} else
		{
			Out("%S\n\n",fieldV["_request._filePath._virtualPath"].strValue.c_str());
		}

		addr = fieldV["_items"].Value.ptr;


		if(!addr)
		{
			Out("This HttpContext object has no claims associated with it\n");
			return;
		}
		std::vector<varMap> items;
		SpecialCases::DumpHash(addr, "*.IdentityModel.Tokens.SessionSecurityToken*", "", &items);
		if(items.size() == 0)
		{
			Out("This HttpContext object has no claims associated with it\n");
			return;
		}
		varMap v = items[0];
		addr = v["val"].Value.ptr;
		if(!addr)
		{
			Out("This HttpContext object has no claims associated with it\n");
			return;
		}
	}
	
	MatchingAddresses addresses;
	AddressList al;
	al.push_back(addr);
	std::map<string, IntPair> summary;

	if(addr)
	{
		addresses.push_back(&al);
	} else
	{
		indc->GetByType(L"*.IdentityModel.Tokens.SessionSecurityToken", addresses);
	}
	AddressEnum adenum;
	if(addresses.size()==0)
	{
		Out("Found no SessionSecurityToken object in heap\n");
		return;
	}
	AddressList tempVector;


	adenum.Start(addresses);


	//std::map<string, long> sumList;
	wstring url;
	int statusCode;
	wstring status;
	INT64 ticks;
	while(CLRDATA_ADDRESS curr=adenum.GetNext())
	{
		if(IsInterrupted())
			return;
		Out("%p ",curr);
		ObjDetail obj(curr);
		Out("%S\n", obj.TypeName().c_str());

		std::vector<std::string> fields;

		bool wif10 = false;
		string authType = "m_authenticationType";
		string nameType = "m_nameType";
		string roleType = "m_roleType";
		string bootstrap = "m_bootstrapContext";
		string itemsField = "_claimsPrincipal.m_identities._items";
		string escapedQuery = "\"Type           : \",m_type,\"\\nIssuer         : \", m_issuer,\"\\nOriginal Issuer: \",m_originalIssuer,\"\\nValue          : \",m_value,\"\\n============================================================================================================\"";
		string claimsField = "m_instanceClaims._items";

		if(obj.TypeName() == L"Microsoft.IdentityModel.Tokens.SessionSecurityToken")
		{
			wif10 = true;
			itemsField = "_claimsPrincipal._identities._collection._items";

			authType = "_authenticationType";
			nameType = "_nameClaimType";
			roleType = "_roleClaimType";
			bootstrap = "_bootstrapToken";
			escapedQuery = "\"Type           : \",_type,\"\\nIssuer         : \", _issuer,\"\\nOriginal Issuer: \",_originalIssuer,\"\\nValue          : \",_value,\"\\n============================================================================================================\"";
			claimsField = "_claims._claims._items";
		} else
		{
			SVAL v;
			v.SetPtr(curr);
			GetObjSel("Session Security Token", "\"\\nAddress     : \",$addr(),\"\\nEndpoint    : \",_endpointId,\"\\nId          : \",_id,\"\\nCurrent Time: \",$tickstodatetime($now()),\"\\nValid From  : \",$tickstodatetime(_validFrom.dateData),\"\\nValid To    : \",$tickstodatetime(_validTo.dateData),\"\\nStatus      : \",$if(((($now() - _validFrom.dateData) > 0) && (($now() - _validTo.dateData) < 0)),\"Valid\\n\",\"Expired\\n\")");
		}
		//Out("\nDEBUG:  Items Fields: %s at %p\n", itemsField.c_str(), curr);
		fields.push_back(itemsField);
		varMap fieldV;

		Out("\n");
		DumpFields(curr,fields,0,&fieldV);
		//Out("\nDEBUG:  Result: %p\n", fieldV[itemsField].Value.ptr);
		vector<CLRDATA_ADDRESS> items;

		if(!fieldV[itemsField].Value.ptr)
		{
			Out("Claims Collection at %p is empty\n", curr);
		} else
		{
			SpecialCases::EnumArray(fieldV[itemsField].Value.ptr, 0, NULL, &items);
		}
		if(IsInterrupted())
			return;

		if(items.size() > 0)
		{
			for(int i=0; i < items.size(); i++)
			{
				if(items[i])
				{
					if(IsInterrupted())
						return;
					fieldV.clear();
					fields.clear();
					fields.push_back(claimsField);
					fields.push_back(authType);
					fields.push_back(nameType);
					fields.push_back(roleType);
					fields.push_back(bootstrap);

					//Out("\nDEBUG:  Array Addr: %p\n", items[i]);
					DumpFields(items[i],fields,0,&fieldV);
					Out("Authentication Type: %S\n", fieldV[authType].strValue.c_str());
					Out("Name Claim Type    : %S\n", fieldV[nameType].strValue.c_str());
					Out("Role Claim Type    : %S\n", fieldV[roleType].strValue.c_str());
					Out("Bootstrap Token    : %p\n", fieldV[bootstrap].Value.ptr);

					//Out("\nDEBUG:  Claims Addr: %p\n", fieldV[claimsField].Value.ptr);
					SVAL v;
					v.SetPtr(fieldV[claimsField].Value.ptr);
					//Out("\nDEBUG:  v Addr: %p\n", v.Value.ptr);
					GetArraySel("Claims", escapedQuery);
				}
			}
		}

	}
}

EXT_COMMAND(wcookie,
	"Dump all cookies for all context, a single context or matching a cookie filter criteria. Use '!whelp wcookie' for detailed help",
	"{;e,o;;Address, HttpContext Address (optional)}"
	"{name;s,o;;Dump only with this name. Optional (e.g -name FedAuth*)}"
	"{value;s,o;;Dump only if value match. Optional (e.g. -value a4ghj8abcd*)}"
	"{summary;b,o;;Show only the summary (count by key=value)}"
	"{min;e,o;;Only show when Total>=min. Optional (e.g. -min 2)}"
	)
{
	INIT_API();
	CLRDATA_ADDRESS addr = 0;
	if(HasUnnamedArg(0)) 
	{
			addr=GetUnnamedArgU64(0);
			ObjDetail obj(addr);
			if(!obj.IsValid())
			{
				Out("Error: Could not find a valid object at %p\n", addr);
				return;
			}
			if(obj.TypeName() != L"System.Web.HttpContext")
			{
				Out("Error: Expected of type System.Web.HttpContext. Found: %S\n", obj.TypeName().c_str());
				return;
			}
	}
	string name;
	string value;
	UINT64 min=0;
	if(HasArg("name")) name=GetArgStr("name");
	if(HasArg("value")) value=GetArgStr("value");
	if(HasArg("min")) min=GetArgU64("min");
	bool fSummary = HasArg("summary");
	//DumpNamedKeys(addr);
	if(!indc && !addr)
	{
		Dml("To list all cookies, run <link cmd=\"!windex;!wcookie -summary\">!windex</link> first\n");
		return;
	}
	MatchingAddresses addresses;
	AddressList al;
	al.push_back(addr);
	std::map<string, IntPair> summary;

	if(addr)
	{
		addresses.push_back(&al);
	} else
	{
		indc->GetByType(L"System.Web.HttpContext", addresses);
	}
	AddressEnum adenum;
	if(addresses.size()==0)
	{
		Out("Found no HttpContext object in heap\n");
		return;
	}
	AddressList tempVector;


	adenum.Start(addresses);
	std::vector<std::string> fields;
	fields.push_back("_request._cookies");
	fields.push_back("_request._url.m_String");
	fields.push_back("_request._filePath._virtualPath");
	fields.push_back("_response._statusCode");
	fields.push_back("_response._statusDescription");
	fields.push_back("_response._completed");
	fields.push_back("_finishPipelineRequestCalled");
	fields.push_back("_utcTimestamp.dateData");
	//std::map<string, long> sumList;
	wstring url;
	int statusCode;
	wstring status;
	INT64 ticks;
	while(CLRDATA_ADDRESS curr=adenum.GetNext())
	{
		if(IsInterrupted())
			return;
		varMap fieldV;
		namedKey listCookies;

		DumpFields(curr,fields,0,&fieldV);
		if(fieldV["_request._cookies"].Value.ptr)
		{
			bool finished = fieldV["_response._completed"].Value.b || fieldV["_finishPipelineRequestCalled"].Value.b;
			listCookies.clear();
			DumpNamedKeys(fieldV["_request._cookies"].Value.ptr, "", &listCookies);
			namedKey::const_iterator it;
			

			int total=0;
			int printed=0;
			bool justOnce = false;
			for(it=listCookies.begin(); it!=listCookies.end(); it++)
			{
				if(IsInterrupted())
					return;

				if(!name.size() || MatchPattern(it->first.c_str(), name.c_str()))
				{

					if(!fSummary)
					{


						if(!justOnce)
						{

							url = fieldV["_request._url.m_String"].strValue;
							if(url.size() == 0) url = fieldV["_request._filePath._virtualPath"].strValue;
							statusCode = fieldV["_response._statusCode"].Value.i32;
							status = fieldV["_response._statusDescription"].strValue;
							ticks = SpecialCases::TicksFromTarget() - fieldV["_utcTimestamp.dateData"].Value.i64;
						}

					}
					for(vector<SVAL>::const_iterator si=it->second.begin(); si!=it->second.end(); si++)
					{
						if(IsInterrupted())
							return;
						std::vector<std::string> fields;
						fields.push_back("_multiValue");
						fields.push_back("_stringValue");
						varMap fieldsV;
						DumpFields(si->Value.ptr, fields, 0, &fieldsV);

						if(fieldsV["_stringValue"].Value.ptr)
						{
							total++;
							if(value.size() == 0 ||  MatchPattern(CW2A(fieldsV["_stringValue"].strValue.c_str()), value.c_str()))
							{
								printed++;
								if(!fSummary)
								{
									if(!justOnce)
									{
										justOnce = true;
										Dml("<link cmd=\"!whttp %p\">%p</link> ", curr, curr);
										Out("%S ", url.c_str());
										Out("(%i %S) ", statusCode, status.c_str());
										if(finished)
										{
											Out("Finished\n");
										} else
										{
											Out("Running (%s)\n",tickstotimespan(ticks).c_str());
										}
										Out("======================================================================================\n");
									}
									Out("%s=%S\n", it->first.c_str(), fieldsV["_stringValue"].strValue.c_str());
								} else
								{
									UpdatePair(it->first, fieldsV["_stringValue"].strValue);
								}
							}
						} else
						{
							namedKey nk;
							DumpNamedKeys(fieldsV["_multiValue"].Value.ptr, it->first, &nk);


							for(namedKey::const_iterator ni=nk.begin(); ni!=nk.end(); ni++)
							{

								for(vector<SVAL>::const_iterator fi = ni->second.begin(); fi!=ni->second.end(); fi++)
								{
									total++;
									if(value.size() == 0 || MatchPattern(CW2A(fi->strValue.c_str()), value.c_str()))
									{	

										printed++;
										if(fSummary)
										{
											UpdatePair(it->first, fi->strValue);
										} else
										{
											if(!justOnce)
											{
												justOnce = true;
												Dml("<link cmd=\"!whttp %p\">%p</link> ", curr, curr);
												Out("%S ", url.c_str());
												Out("(%i %S) ", statusCode, status.c_str());
												if(finished)
												{
													Out("Finished\n");
												} else
												{
													Out("Running (%s)\n",tickstotimespan(ticks).c_str());
												}
												Out("======================================================================================\n");
											}
											Out("%s=%S\n", it->first.c_str(), fi->strValue.c_str());
										}
									}
								}
							}
							
						}
					}
					
				}
			}
			if(!fSummary && printed)
			{
				Out("\n%i printed\n\n", printed);
				Out("======================================================================================\n");
			}

		}
	}
	//
	if(fSummary)
	{
			{
				Out("Action Total Finished Cookie=Value\n");
				Out("======================================================================================\n");

				for(map<string, IntPair>::const_iterator it=summary.begin(); it!=summary.end(); it++)
				{
					if(IsInterrupted())
						return;
					if(it->second.count >= min)
					{
						unsigned equal = it->first.find("=");
						if(equal == string::npos)
						{
							Out("(error) ", it->second.count); // should never goes here
						} else
						{
							Dml("<link cmd=\"!wcookie -name %s -value %s\">(list)</link> ", 
								it->first.substr(0,equal).c_str(),
								it->first.substr(equal+1).c_str()
								);
						}
				
						Out("%5i %9i %s\n", it->second.count, it->second.finished, it->first.c_str());
					}
				}
			}
	}
}



EXT_COMMAND(wkeyvalue,
            "Dump NameObjectCollection types. Use '!whelp wkeyvalue' for detailed help",
			"{;e,r;;Address,NameObjectCollection Address}"
			)
{
	INIT_API();
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	DumpNamedKeys(addr);
}

EXT_COMMAND(wdict,
            "Dump Dictionary. Use '!whelp wdict' for detailed help",
			"{;e,r;;Address,Dictionary Address}"
			)
{
	INIT_API();
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj(addr);
	if(!obj.IsValid() || !obj.classObj.Implement(L"System.Collections.Generic.Dictionary*"))
	{
		Out("Object at %p is invalid or not of type [System.Object System.Collections.Generic.Dictionary]\n", addr);
		return;
	}
	std::vector<std::string> fields;
	// .NET Core renamed Dictionary<K,V>'s top-level fields with an underscore prefix
	// (entries -> _entries, buckets -> _buckets, count -> _count); the per-entry
	// fields read further down (key/value/next) are unchanged in both frameworks
	fields.push_back("_entries");
	fields.push_back("entries");
	fields.push_back("_buckets");
	fields.push_back("buckets");
	fields.push_back("_count");
	fields.push_back("count");
	// .NET Core 3.0 also rewrote the _buckets encoding itself: a bucket value of 0
	// means empty (desktop used -1 as the empty sentinel and stored the real,
	// 0-based entry index directly), and a non-zero value stores (realIndex + 1).
	// _fastModMultiplier was added in that same rewrite (for the Lemire fast-modulo
	// bucket hash), so its presence reliably tells us which encoding is in play.
	// The per-entry "next" chain field is unaffected by either scheme.
	fields.push_back("_fastModMultiplier");
	varMap fieldV;
	DumpFields(addr,fields,0,&fieldV);
	bool modernBuckets = fieldV.find("_fastModMultiplier") != fieldV.end();

	varMap::iterator f;
	f = fieldV.find("_entries"); if(f == fieldV.end()) f = fieldV.find("entries");
	if(f == fieldV.end())
	{
		Out("Could not locate the Dictionary's internal fields\n");
		return;
	}
	CLRDATA_ADDRESS entries = f->second.Value.ptr;

	f = fieldV.find("_buckets"); if(f == fieldV.end()) f = fieldV.find("buckets");
	if(f == fieldV.end())
	{
		Out("Could not locate the Dictionary's internal fields\n");
		return;
	}
	CLRDATA_ADDRESS buckets = f->second.Value.ptr;

	f = fieldV.find("_count"); if(f == fieldV.end()) f = fieldV.find("count");
	int n = (f == fieldV.end()) ? 0 : f->second.Value.i32;

	CLRDATA_ADDRESS mt = 0;
	if(n==0 || entries == NULL)
	{
		Out("Empty Dictionary\n");
		return;
	}
	ObjDetail objBuckets(buckets);
	if(!objBuckets.IsValid())
	{
		Out("Dictionary consistency compromised\n");
		return;
	}

	Out("Items   : %i\n", n);
	//Out("Entries : %p\n\n", entries);
	std::vector<CLRDATA_ADDRESS> addresses;

	obj.Request(entries);
	mt=obj.InnerMT();

	SpecialCases::EnumArray(entries,0,NULL,&addresses);
	int c=0;

	for(int i=0;i<objBuckets.NumComponents();i++)
	{
		ExtRemoteData rm(objBuckets.DataPtr()+i*objBuckets.InnerComponentSize(),sizeof(int));
		INT32 raw=rm.GetLong();
		INT32 s;
		if(modernBuckets)
		{
			if(raw == 0) continue; // empty bucket, nothing chained here
			s = raw - 1;
		}
		else
		{
			s = raw; // desktop: 0-based index directly, -1 means empty
		}
		while(s!=-1 && c<=n)
		{
			if(s < 0 || (size_t)s >= addresses.size())
			{
				Out("\nWARNING: Bucket points outside the entries array (index %i). Dictionary consistency compromised.\n", s);
				return;
			}
			fieldV.clear();
			fields.clear();
			fields.push_back("key");
			fields.push_back("value");
			fields.push_back("next");
			DumpFields(addresses[s],fields,mt,&fieldV);
			Out("[%i]:==============================================(Physical Index: %i)\n",c++, s);
			fields.pop_back();
			DumpFields(addresses[s],fields,mt);
			s=fieldV["next"].Value.i32;
		}
		if(s!=-1)
		{
			Out("\nWARNING: Circular reference was detected. In runtime, a walk through this dictionary will hang the thread with high CPU.\n");
			return;
		}
	}
}

EXT_COMMAND(whashset,
            "Dump HashSet<T>. Use '!whelp whashset' for detailed help",
			"{;e,r;;Address,HashSet Address}"
			)
{
	// .NET Core/.NET 5+ HashSet<T> only, verified live against a .NET 10 dump.
	// Classic .NET Framework HashSet<T> used a different internal layout entirely
	// (m_buckets/m_slots, Slot { hashCode, value, next }) that has not been
	// implemented here - this command will simply fail to locate fields on a
	// desktop dump rather than misinterpret it.
	INIT_API();
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj(addr);
	if(!obj.IsValid() || !obj.classObj.Implement(L"System.Collections.Generic.HashSet*"))
	{
		Out("Object at %p is invalid or not of type [System.Object System.Collections.Generic.HashSet]\n", addr);
		return;
	}
	std::vector<std::string> fields;
	fields.push_back("_entries");
	fields.push_back("_buckets");
	fields.push_back("_count");
	// Same modern bucket-encoding rewrite as Dictionary<K,V> (see !wdict): a bucket
	// value of 0 means empty, and a non-zero value stores (realIndex + 1). Presence
	// of _fastModMultiplier confirms this is the modern (Core) layout.
	fields.push_back("_fastModMultiplier");
	varMap fieldV;
	DumpFields(addr,fields,0,&fieldV);

	if(fieldV.find("_fastModMultiplier") == fieldV.end())
	{
		Out("Object at %p does not have the expected .NET Core HashSet<T> layout (classic .NET Framework HashSet<T> is not supported)\n", addr);
		return;
	}

	varMap::iterator f;
	f = fieldV.find("_entries");
	if(f == fieldV.end())
	{
		Out("Could not locate the HashSet's internal fields\n");
		return;
	}
	CLRDATA_ADDRESS entries = f->second.Value.ptr;

	f = fieldV.find("_buckets");
	if(f == fieldV.end())
	{
		Out("Could not locate the HashSet's internal fields\n");
		return;
	}
	CLRDATA_ADDRESS buckets = f->second.Value.ptr;

	f = fieldV.find("_count");
	int n = (f == fieldV.end()) ? 0 : f->second.Value.i32;

	if(n==0 || entries == NULL)
	{
		Out("Empty HashSet\n");
		return;
	}
	ObjDetail objBuckets(buckets);
	if(!objBuckets.IsValid())
	{
		Out("HashSet consistency compromised\n");
		return;
	}

	Out("Items   : %i\n", n);
	std::vector<CLRDATA_ADDRESS> addresses;

	obj.Request(entries);
	CLRDATA_ADDRESS mt=obj.InnerMT();

	SpecialCases::EnumArray(entries,0,NULL,&addresses);
	int c=0;

	for(int i=0;i<objBuckets.NumComponents();i++)
	{
		ExtRemoteData rm(objBuckets.DataPtr()+i*objBuckets.InnerComponentSize(),sizeof(int));
		INT32 raw=rm.GetLong();
		if(raw == 0) continue; // empty bucket, nothing chained here
		INT32 s = raw - 1;
		while(s!=-1 && c<=n)
		{
			if(s < 0 || (size_t)s >= addresses.size())
			{
				Out("\nWARNING: Bucket points outside the entries array (index %i). HashSet consistency compromised.\n", s);
				return;
			}
			fieldV.clear();
			fields.clear();
			fields.push_back("Value");
			fields.push_back("Next");
			DumpFields(addresses[s],fields,mt,&fieldV);
			Out("[%i]:==============================================(Physical Index: %i)\n",c++, s);
			fields.pop_back();
			DumpFields(addresses[s],fields,mt);
			s=fieldV["Next"].Value.i32;
		}
		if(s!=-1)
		{
			Out("\nWARNING: Circular reference was detected. In runtime, a walk through this set will hang the thread with high CPU.\n");
			return;
		}
	}
}

EXT_COMMAND(whash,
            "Dump Hash Table. Use '!whelp whash' for detailed help",
			"{;e,r;;Address,Hash Table Address}"
			)
{
	INIT_API();
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	HashInternal(addr);
}

#define PRINT_SVALSTR(str,patt) \
	v=fieldV[str]; \
	if(v.IsValid && v.Value.ptr != 0 && v.strValue.size()>0) Out(patt, v.strValue.c_str());

#define PRINT_SVALINT(str,patt,tp) \
	v=fieldV[str]; \
	if(v.IsValid) Out(patt, v.Value.tp);

EXT_COMMAND(wconfig,
            "Dump Config file lines in memory. Use '!whelp wconfig' for detailed help",
			"{{custom}}"
			)
{
	INIT_API();
	if(!indc)
	{
		Dml("To list .config file lines, run <link cmd=\"!windex;!wconfig\">!windex</link> first\n");
		return;
	}
	MatchingAddresses addresses;
	addresses.clear();
	indc->GetByDerive("System.Configuration.SectionXmlInfo", addresses);
	AddressEnum adenum;
	if(addresses.size()==0) return;
	adenum.Start(addresses);
	std::map<std::string, CLRDATA_ADDRESS> ordered;
	std::map<std::string, CLRDATA_ADDRESS>::const_iterator it;
	while(CLRDATA_ADDRESS curr=adenum.GetNext())
	{
		std::vector<std::string> fields;
		fields.push_back("_filename");
		fields.push_back("_lineNumber");
		fields.push_back("_rawXml");

		varMap fieldV;
		DumpFields(curr,fields,0,&fieldV);
		if(fieldV["_rawXml"].Value.ptr==0) continue;
		std::string str;
		str.assign(CW2A(fieldV["_filename"].strValue.c_str()));
		ordered[str+formathex(0x10000+fieldV["_lineNumber"].Value.u32)]=curr;
	}
	for(it=ordered.begin(); it != ordered.end(); it++)
	{
		FromFlags flags;
		ZeroMemory(&flags, sizeof(flags));
		flags.cmd = "\"<--\\nKey: \", _configKey,\"\\nDefinition Config Path: \",_definitionConfigPath,\"\\nFilename: \", _filename, \"\\nLine: \",_lineNumber,\"\\n -->\\n\",\"\\n\",_rawXml";
		flags.fobj = true;
		flags.nofield = true;
		flags.nospace = true;
		flags.obj = it->second;
		wfrom_internal(flags);
	}
}

struct HttpFlags
{
	bool frunning;
	bool fwiththread;
	bool forder;
	bool fstatus;
	bool fnotstatus;
	bool fverb;
	int status;
	int notstatus;
	std::string verb;
};
EXT_COMMAND(whttp,
            "Dump HttpContext. Use '!whelp whttp' for detailed help",
			"{;e,o;;Address,HttpContext Address}"
			"{status;s,o;;Dump only with this status (e.g 500)}"
			"{notstatus;s,o;;Dump only if not this status (e.g 200)}"
			"{verb;s,o;;Dump only with this verb (e.g POST)}"
			"{running;b,o;;List only HttpRequests not finished}"
			"{withthread;b,o;;List only HttpRequests with thread alive}"
			"{order;b,o;;Order by request time}"

			)
{
	INIT_API();

	std::vector<std::string> fields;
	fields.push_back("_thread");
	fields.push_back("_response._statusCode");
	fields.push_back("_response._completed");
	fields.push_back("_finishPipelineRequestCalled");
	fields.push_back("_request._httpMethod");
	if(!HasUnnamedArg(0))
	{
		HttpFlags flags;
		if(!indc)
		{
			Dml("To list HttpContext summary, run <link cmd=\"!windex;!whttp\">!windex</link> first\n");
			return;
		}
		flags.frunning = HasArg("running");
		flags.fwiththread = HasArg("withthread");
		flags.forder = HasArg("order");
		flags.fstatus = HasArg("status");
		flags.fnotstatus = HasArg("notstatus");
		flags.fverb = HasArg("verb");
		if(flags.fverb) flags.verb = GetArgStr("verb");
		try
		{
			flags.status = flags.fstatus ? boost::lexical_cast<int>(GetArgStr("status")) : -1;
			flags.notstatus = flags.fnotstatus ? boost::lexical_cast<int>(GetArgStr("notstatus")) : -1;
		} catch(...)
		{
			Out("Error: please enter a numeric value for status (e.g 200)\n");
			return;
		}
		
		
		MatchingAddresses addresses;
		addresses.clear();
		std::multimap<UINT64, CLRDATA_ADDRESS> ordered;

		indc->GetByType(L"System.Web.HttpContext", addresses);
		AddressEnum adenum;
		if(addresses.size()==0)
		{
			Out("Found no HttpContext object in heap\n");
			return;
		}
		AddressList tempVector;
		if(flags.forder)
		{
			adenum.Start(addresses);
			std::vector<std::string> fields;
			fields.push_back("_utcTimestamp.dateData");
			while(CLRDATA_ADDRESS curr=adenum.GetNext())
			{
				varMap fieldV;
				DumpFields(curr,fields,0,&fieldV);
				//fieldV["_utcTimestamp.dateData"].Value.u64
				ordered.insert(std::pair<UINT64, CLRDATA_ADDRESS>(fieldV["_utcTimestamp.dateData"].Value.u64, curr));
			}
			addresses.clear();
			std::multimap<UINT64, CLRDATA_ADDRESS>::iterator it;
			
			for (it=ordered.begin(); it!=ordered.end(); ++it)
				tempVector.push_back(it->second);
			addresses.push_back(&tempVector);
		}

		adenum.Start(addresses);

		UINT64 count=0;
		UINT64 skipped=0;
		if(flags.forder)
			Out("HttpContext      Start Time                  Thread Time Out Running  Status Verb     Url\n");
		else
			Out("HttpContext    Thread Time Out Running  Status Verb     Url\n");

		while(CLRDATA_ADDRESS curr=adenum.GetNext())
		{
			count++;
			if(IsInterrupted())
				return;
			bool printIt = true;

			if(flags.fwiththread || flags.frunning || flags.fstatus || flags.fnotstatus || flags.fverb)
			{
				varMap fieldV;
				DumpFields(curr,fields,0,&fieldV);
				if(printIt && flags.fwiththread && fieldV["_thread"].Value.ptr == 0) printIt = false;
				if(printIt && flags.frunning && (fieldV["_response._completed"].Value.b || fieldV["_finishPipelineRequestCalled"].Value.b)) printIt = false;
				if(printIt && flags.fstatus && (fieldV["_response._statusCode"].Value.i32 != flags.status)) printIt = false;
				if(printIt && flags.fnotstatus && (fieldV["_response._statusCode"].Value.i32 == flags.status)) printIt = false;
				if(printIt && flags.fverb && !g_ExtInstancePtr->MatchPattern(flags.verb.c_str(), localCW2A(fieldV["_request._httpMethod"].strValue.c_str()).c_str()))
					printIt = false;
				
			}
			if(!printIt)
			{
				skipped++;
				continue;
			}
			Dml("<link cmd=\"!whttp %p\">%p</link> ",curr, curr);
			if(sizeof(void*) == 4)
				Out("        ");

			FromFlags flagsQ;
			ZeroMemory(&flagsQ, sizeof(flagsQ));
			if(flags.forder)
				if(NET2)
					flagsQ.cmd = "$rpad($tickstodatetime(_utcTimestamp.dateData), 0n30), $if(!_thread, \"  --\",$lpad($thread(_thread.DONT_USE_InternalThread),4)),\" \",$if((_timeoutSet==1),$tickstotimespan(_timeout._ticks), \"Not set \"), \" \", $if($isnull(_response._completed,0) || _finishPipelineRequestCalled,\"Finished\", $tickstotimespan($maskticks($now())-$maskticks(_utcTimestamp.dateData))), \" \", $replace($lpad(_response._statusCode,8),\"0n\",\"\"),\" \", $rpad($isnull(_request._httpMethod,\"NA\"),8), \" \", $isnull(_request._url.m_String, _request._filePath._virtualPath)";
				else
					flagsQ.cmd = "$rpad($tickstodatetime(_utcTimestamp.dateData), 0n30), $if(!_thread, \"  --\",$lpad($thread(_thread.DONT_USE_InternalThread),4)),\" \",$if($isnull(_timeoutSet,_threadAbortOnTimeout),$tickstotimespan($isnull(_timeoutTicks,_timeout._ticks)), \"Not set \"), \" \", $if($isnull(_response._completed,0) || _finishPipelineRequestCalled,\"Finished\", $tickstotimespan($maskticks($now())- $maskticks(_utcTimestamp.dateData))), \" \", $replace($lpad(_response._statusCode,8),\"0n\",\"\"),\" \", $rpad($isnull(_request._httpMethod,\"NA\"),8), \" \", $isnull(_request._url.m_String, _request._filePath._virtualPath)";
			else
				if(NET2)
					flagsQ.cmd = "$if(!_thread, \"  --\",$lpad($thread(_thread.DONT_USE_InternalThread),4)),\" \",$if((_timeoutSet==1),$tickstotimespan(_timeout._ticks), \"Not set \"), \" \", $if($isnull(_response._completed,0) || _finishPipelineRequestCalled,\"Finished\", $tickstotimespan($maskticks($now())-$maskticks(_utcTimestamp.dateData))), \" \", $replace($lpad(_response._statusCode,8),\"0n\",\"\"),\" \", $rpad($isnull(_request._httpMethod,\"NA\"),8), \" \", $isnull(_request._url.m_String, _request._filePath._virtualPath)";
				else
					flagsQ.cmd = "$if(!_thread, \"  --\",$lpad($thread(_thread.DONT_USE_InternalThread),4)),\" \",$if($isnull(_timeoutSet,_threadAbortOnTimeout),$tickstotimespan($isnull(_timeoutTicks,_timeout._ticks)), \"Not set \"), \" \", $if($isnull(_response._completed,0) || _finishPipelineRequestCalled,\"Finished\", $tickstotimespan($maskticks($now())-$maskticks(_utcTimestamp.dateData))), \" \", $replace($lpad(_response._statusCode,8),\"0n\",\"\"),\" \", $rpad($isnull(_request._httpMethod,\"NA\"),8), \" \", $isnull(_request._url.m_String, _request._filePath._virtualPath)";
			flagsQ.fobj = true;
			flagsQ.nofield = true;
			flagsQ.nospace = true;
			flagsQ.obj = curr;
			wfrom_internal(flagsQ);
		}
		Out("\n%S HttpContext object(s) found matching criteria\n", formatnumber(count-skipped).c_str());
		if(skipped > 0)
			Out("\n%S HttpContext object(s) skipped by filter\n", formatnumber(skipped).c_str());

		Out("\nYou may also be interested in\n"
			      "================================\n");
		Dml("Dump HttpRuntime info: <link cmd=\"!wruntime\">!wruntime</link>\n");

		return;
	}
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj(addr);
	if(!obj.IsValid() || obj.TypeName() != L"System.Web.HttpContext")
	{
		Out("Object at %p is invalid or not of type [System.Web.HttpContext]\n", addr);
		return;
	}
	fields.clear();
	fields.push_back("_request._httpMethod");
	fields.push_back("_request._url.m_String");
	fields.push_back("_request._contentType");
	fields.push_back("_request._contentLength");
	fields.push_back("_request._referrer");
	fields.push_back("_request._pathTranslated");
	fields.push_back("_request._form._entriesTable");   // Key[n], Value[n]._items[0]
	fields.push_back("_request._form._entriesTable.count");   // Key[n], Value[n]._items[0]

	fields.push_back("_request._headers._entriesArray._items");   // Key[n], Value[n]._items[0]
	fields.push_back("_request._serverVariables._entriesArray._items");   // Value[n].Name, Value[n].Value
	fields.push_back("_request._cookies._entriesArray._items");  // Value[n]._name, Value[n]._stringValue
	fields.push_back("_request._rawContent._length");
	fields.push_back("_request._rawContent._data");
	fields.push_back("_request._rawContent._completed");
	fields.push_back("_request._rawContent._chunkOffset");
	fields.push_back("_request._rawContent._chunkLength");
	fields.push_back("_request._rawContent._expectedLength");
	fields.push_back("_items");
	fields.push_back("_utcTimestamp.dateData");


	fields.push_back("_errors._items");  // _message[n]

	string timeoutStartTicksFields = "_timeoutStartTime.dateData";
	string timeoutTicksField = "_timeout._ticks";
	string timeoutSetField = "_timeoutSet";

	// For .NET 4.0 that is not 4.5+
	fields.push_back(timeoutStartTicksFields); 
	fields.push_back(timeoutTicksField);
	fields.push_back(timeoutSetField);

	//


	if(!NET2)
	{
		timeoutStartTicksFields = "_timeoutStartTimeUtcTicks";
		timeoutTicksField = "_timeoutTicks";

		fields.push_back(timeoutStartTicksFields); 
		fields.push_back(timeoutTicksField);


	}


	fields.push_back("_finishPipelineRequestCalled");
	fields.push_back("_threadAbortOnTimeout"); // for .NET 4.5+
	fields.push_back("_thread.DONT_USE_InternalThread");
	fields.push_back("_thread.m_ManagedThreadId");
	fields.push_back("_response._completed");
	fields.push_back("_response._ended");
	fields.push_back("_response._statusCode");
	fields.push_back("_response._contentType");
	fields.push_back("_response._customHeaders._items");   // _unknownHeader[n], _knownHeaderIndex[n]=-1 or number, _value
	fields.push_back("_response._charSet");
	fields.push_back("_response._writer._stream");  // still to resolve
	fields.push_back("_response._statusDescription");
	fields.push_back("_request._filePath._virtualPath");




	varMap fieldV;
	DumpFields(addr,fields,0,&fieldV);
	SVAL v;

	Out("\nContext Info\n"
		"================================\n");
	Out("Address           : %p\n", addr);
	Out("Target/Dump Time  : %S\n", tickstodatetime(SpecialCases::TicksFromTarget()).c_str());
	Out("Request Time      : %S\n",tickstodatetime(fieldV["_utcTimestamp.dateData"].Value.u64).c_str());
	if(!(fieldV["_response._completed"].Value.b  || fieldV["_finishPipelineRequestCalled"].Value.b))
		Out("Running time      : %s\n",tickstotimespan((SpecialCases::TicksFromTarget() & TicksMask) - (fieldV["_utcTimestamp.dateData"].Value.u64 & TicksMask)).c_str());

	ULONG curTime;
	curTime = EvalExprU64("@$dbgtime");
	if(fieldV.find("_threadAbortOnTimeout") != fieldV.end())
	{
		timeoutSetField = "_threadAbortOnTimeout";

	}

	if(fieldV.find("_timeout._ticks") != fieldV.end())
	{
		timeoutStartTicksFields = "_timeoutStartTime.dateData";
	    timeoutTicksField = "_timeout._ticks";
	}

	if(fieldV[timeoutSetField].Value.b)
	{
		Out("Timeout           : %s\n", tickstotimespan(fieldV[timeoutTicksField].Value.u64).c_str());
		Out("Timeout Start Time: %S\n",tickstodatetime(fieldV[timeoutStartTicksFields].Value.u64).c_str());
		Out("Timeout Limit Time: %S\n",tickstodatetime(fieldV[timeoutStartTicksFields].Value.u64+fieldV[timeoutTicksField].Value.u64).c_str());


	}
	v=fieldV["_thread.DONT_USE_InternalThread"];
	if(v.IsValid && v.Value.ptr != NULL)
	{

		ULONG td = Thread::GetOSThreadIDByAddress(v.Value.ptr);
		if(td != 0)
		{
			Out("Managed Thread Id : ");
			Dml("<link cmd=\"~~[%x]s\">%x</link>\n", td, td);
		}

		PRINT_SVALINT("_thread.m_ManagedThreadId", "Managed Thread Id : %x\n",i32);
	}
	v=fieldV["_items"];
	if(v.IsValid && v.Value.ptr!=0) Dml("HttpContext.Items[]: <link cmd=\"!whash %p\">%p</link>\n", v.Value.ptr, v.Value.ptr);
	Out("\nRequest Info\n"
		"================================\n");
	PRINT_SVALSTR("_request._httpMethod","%S ");
	if(fieldV["_request._url.m_String"].strValue.size() > 0)
	{
			Out("%S\n",fieldV["_request._url.m_String"].strValue.c_str());
	} else
	{
			Out("%S\n",fieldV["_request._filePath._virtualPath"].strValue.c_str());
	}

	PRINT_SVALSTR("_request._contentType", "Content Type    : %S\n");
	PRINT_SVALINT("_request._contentLength", "Content Length  : %i\n",i32);
	//int rlength = v.Value.i32;
	PRINT_SVALSTR("_request._referrer.m_String", "Referrer        : %S\n");
	PRINT_SVALSTR("_request._pathTranslated", "Target in Server: %S\n");
	//fields.push_back("_request._form._entriesArray");   // Key[n], Value[n]._items[0]
	//fields.push_back("_request._headers._entriesArray._items");   // Key[n], Value[n]._items[0]
	//fields.push_back("_request._serverVariables._entriesArray._items");   // Value[n].Name, Value[n].Value
	//fields.push_back("_request._cookies._entriesArray._items");  // Value[n]._name, Value[n]._stringValue
	v=fieldV["_request._rawContent._length"];
	std::string postContent;
	if(v.IsValid && v.Value.i32 == fieldV["_request._rawContent._expectedLength"].Value.i32
		&& fieldV["_request._rawContent._completed"].Value.b
		&& v.Value.i32>0 &&
		fieldV["_request._rawContent._data"].Value.ptr != NULL)
	{
		postContent = SpecialCases::GetRawArray(fieldV["_request._rawContent._data"].Value.ptr);
		Out("Body:\n[--- Start ---]\n%s\n[---  End ---]\n",SpecialCases::GetRawArray(fieldV["_request._rawContent._data"].Value.ptr).c_str());
		
	}
	v=fieldV["_request._form._entriesTable.count"];

	if(v.Value.i32>0)
	{
		v=fieldV["_request._form._entriesTable"];
		if(v.IsValid && v.Value.ptr!=0) Dml("Forms[]           : <link cmd=\"!whash %p\">%p</link>\n", v.Value.ptr, v.Value.ptr);
	}

	Out("\nResponse Info\n"
		"================================\n");

	if(!(fieldV["_response._completed"].Value.b  || fieldV["_finishPipelineRequestCalled"].Value.b))
		Out("Warning: Response has not completed\n");
	Out("Status          : %i (%S)\n", fieldV["_response._statusCode"].Value.i32,
		fieldV["_response._statusDescription"].strValue.c_str());
		PRINT_SVALSTR("_response._contentType","Content Type    : %S\n");

	if(fieldV["_finishPipelineRequestCalled"].Value.b) //  _response._ended
		Out("Warning: Finish request pipeline has been called\n");

	v=fieldV["_request._serverVariables._entriesArray._items"];
	if(v.Value.ptr != NULL)
	{
		Out("\nServer Variables\n"
			"================================\n");

		FromFlags flags;
		ZeroMemory(&flags, sizeof(flags));
		flags.cmd = "where (($addr()!=0) && (Value.value != \"NULL\") && (Value.value != \"\") ) select Key,\": \",Value.value";
		flags.farray = true;
		flags.nofield = true;
		flags.nospace = true;
		flags.obj = v.Value.ptr;
		wfrom_internal(flags);
	}

			Out("\nYou may also be interested in\n"
			      "================================\n");
			Dml("Dump HttpContext fields: <link cmd=\"!wselect * from %p\">!wselect * from %p</link>\n", addr, addr);
			Dml("Find all stack roots   : <link cmd=\"!wfrom -obj %p select $a(&quot;Count  &quot;, $if($strsize($stackroot($addr())),$splitsize($stackroot($addr()),&quot;,&quot;),0)), $a(&quot;Threads&quot;,$stackroot($addr()))\">!wfrom -obj %p select $a(\"Count  \", $if($strsize($stackroot($addr())),$splitsize($stackroot($addr()),\",\"),0)), $a(\"Threads\",$stackroot($addr()))</link>\n", addr, addr);
			Dml("Dump all cookies       : <link cmd=\"!wcookie %p\">!wcookie %p</link>\n", addr, addr);
			Dml("Dump all claims        : <link cmd=\"!wtoken %p\">!wtoken %p</link>\n", addr, addr);

			if(g_ExtInstancePtr->MatchPattern(postContent.c_str(), "*<*>*</*>*", false))
			{
				Dml("Xml Formatted Request  : <link cmd=\"!wfrom -nofield -nospace -obj %p select $xml($rawfield(_request._rawContent._data))\">!wfrom -obj %p select $xml($rawfield(_request._rawContent._data))</link>\n", addr, addr);
				Dml("Xml Tree of Request    : <link cmd=\"!wfrom -nofield -nospace -obj %p select $xmltree($rawfield(_request._rawContent._data))\">!wfrom -obj %p select $xmltree($rawfield(_request._rawContent._data))</link>\n", addr, addr);

			}
}

// Kestrel connection types deriving from HttpProtocol; the generic argument is the hosting TContext.
// The type-pattern parser (VectorSplit) does not accept '<' '>' so '?' stands in for the bracket
#define KESTREL_TYPES L"*.Kestrel.Core.Internal.Http.Http1Connection?*,*.Kestrel.Core.Internal.Http2.Http2Stream?*,*.Kestrel.Core.Internal.Http3.Http3Stream?*"
// Request start time lives in the activity feature (HostingApplicationDiagnostics sets it per request)
#define KESTREL_START_FIELD "_currentIHttpActivityFeature._Activity_k__BackingField._StartTimeUtc_k__BackingField"

// Kestrel's HttpMethod enum (byte-backed); Custom and above fall back to _methodText
static const char* KestrelVerb(int Method)
{
	static const char* verbs[] = {"GET","PUT","DELETE","POST","HEAD","TRACE","PATCH","CONNECT","OPTIONS"};
	if(Method >= 0 && Method < (int)(sizeof(verbs)/sizeof(verbs[0])))
		return verbs[Method];
	return "";
}

EXT_COMMAND(whttpcore,
            "Dump ASP.NET Core (Kestrel) requests. Use '!whelp whttpcore' for detailed help",
			"{;e,o;;Address,Kestrel connection (Http1Connection/Http2Stream/Http3Stream) Address}"
			"{status;s,o;;Dump only with this status (e.g 500)}"
			"{notstatus;s,o;;Dump only if not this status (e.g 200)}"
			"{verb;s,o;;Dump only with this verb (e.g POST)}"
			"{running;b,o;;List only requests still in the application pipeline}"
			"{order;b,o;;Order by request start time}"
			)
{
	INIT_API();

	// AppStarted in Kestrel's RequestProcessingStatus (stable value since ASP.NET Core 3.0)
	const int appStarted = 3;
	// $now()/GetCurrentTimeDate has no data source in an ELF core, so elapsed time is Windows-only
	bool showRunning = !isLinuxTarget && SpecialCases::TicksFromTarget() != 0;

	if(!HasUnnamedArg(0))
	{
		HttpFlags flags;
		if(!indc)
		{
			Dml("To list ASP.NET Core requests, run <link cmd=\"!windex;!whttpcore\">!windex</link> first\n");
			return;
		}
		flags.frunning = HasArg("running");
		flags.forder = HasArg("order");
		flags.fstatus = HasArg("status");
		flags.fnotstatus = HasArg("notstatus");
		flags.fverb = HasArg("verb");
		if(flags.fverb) flags.verb = GetArgStr("verb");
		try
		{
			flags.status = flags.fstatus ? boost::lexical_cast<int>(GetArgStr("status")) : -1;
			flags.notstatus = flags.fnotstatus ? boost::lexical_cast<int>(GetArgStr("notstatus")) : -1;
		} catch(...)
		{
			Out("Error: please enter a numeric value for status (e.g 200)\n");
			return;
		}

		MatchingAddresses addresses;
		addresses.clear();
		indc->GetByType(KESTREL_TYPES, addresses);
		if(addresses.size()==0)
		{
			Out("Found no Kestrel connection object in heap. Is this an ASP.NET Core (Kestrel) dump?\n");
			return;
		}

		std::vector<std::string> fields;
		fields.push_back("_Path_k__BackingField");
		fields.push_back("_statusCode");
		fields.push_back("_requestProcessingStatus");
		fields.push_back("_methodText");
		fields.push_back("_Method_k__BackingField");
		fields.push_back(KESTREL_START_FIELD "._dateData");

		AddressEnum adenum;
		AddressList tempVector;
		if(flags.forder)
		{
			std::multimap<UINT64, CLRDATA_ADDRESS> ordered;
			adenum.Start(addresses);
			while(CLRDATA_ADDRESS curr=adenum.GetNext())
			{
				if(IsInterrupted())
					return;
				varMap fieldV;
				DumpFields(curr,fields,0,&fieldV);
				UINT64 key = 0;
				varMap::iterator f = fieldV.find(KESTREL_START_FIELD "._dateData");
				if(f != fieldV.end()) key = f->second.Value.u64 & TicksMask;
				ordered.insert(std::pair<UINT64, CLRDATA_ADDRESS>(key, curr));
			}
			addresses.clear();
			std::multimap<UINT64, CLRDATA_ADDRESS>::iterator it;
			for (it=ordered.begin(); it!=ordered.end(); ++it)
				tempVector.push_back(it->second);
			addresses.push_back(&tempVector);
		}

		adenum.Start(addresses);

		UINT64 count=0;
		UINT64 skipped=0;
		if(showRunning)
			Out("Address          Thrd Start Time (UTC)       Running   State           Status Verb     Url\n");
		else
			Out("Address          Thrd Start Time (UTC)       State           Status Verb     Url\n");

		while(CLRDATA_ADDRESS curr=adenum.GetNext())
		{
			count++;
			if(IsInterrupted())
				return;

			varMap fieldV;
			DumpFields(curr,fields,0,&fieldV);
			varMap::iterator f;

			// A connection between requests resets its path; no path means no current request.
			// Null strings come back as strValue L"NULL" with Value.ptr == 0, so test the pointer
			bool printIt = false;
			f = fieldV.find("_Path_k__BackingField");
			if(f != fieldV.end()) printIt = (f->second.Value.ptr != 0);
			if(printIt && flags.frunning)
			{
				int procStatus = -1;
				f = fieldV.find("_requestProcessingStatus");
				if(f != fieldV.end()) procStatus = f->second.Value.i32;
				if(procStatus != appStarted) printIt = false;
			}
			if(printIt && (flags.fstatus || flags.fnotstatus))
			{
				int status = -1;
				f = fieldV.find("_statusCode");
				if(f != fieldV.end()) status = f->second.Value.i32;
				if(flags.fstatus && status != flags.status) printIt = false;
				if(flags.fnotstatus && status == flags.notstatus) printIt = false;
			}
			if(printIt && flags.fverb)
			{
				std::string verb;
				f = fieldV.find("_methodText");
				if(f != fieldV.end() && f->second.Value.ptr != 0)
					verb = localCW2A(f->second.strValue.c_str());
				if(verb.size() == 0)
				{
					f = fieldV.find("_Method_k__BackingField");
					if(f != fieldV.end()) verb = KestrelVerb(f->second.Value.i32);
				}
				if(!g_ExtInstancePtr->MatchPattern(flags.verb.c_str(), verb.c_str()))
					printIt = false;
			}
			if(!printIt)
			{
				skipped++;
				continue;
			}
			Dml("<link cmd=\"!whttpcore %p\">%p</link> ",curr, curr);
			if(sizeof(void*) == 4)
				Out("        ");

			FromFlags flagsQ;
			ZeroMemory(&flagsQ, sizeof(flagsQ));
			// $isnull() only understands field arguments (a function result is always taken as
			// null), so function-produced columns are used bare here
			std::string cmd =
				"$if($strsize($stackroot($addr())), $lpad($stackroot($addr()),4), \"  --\"), \" \", "
				"$rpad($tickstodatetime($maskticks(" KESTREL_START_FIELD "._dateData)),0n22), \" \", ";
			if(showRunning)
				cmd += "$rpad($tickstotimespan($maskticks($now())-$maskticks(" KESTREL_START_FIELD "._dateData)),0n9), \" \", ";
			cmd +=
				"$rpad($enumname(_requestProcessingStatus),0n15), \" \", "
				"$replace($lpad(_statusCode,8),\"0n\",\"\"), \" \", "
				"$rpad($isnull(_methodText, $upper($enumname(_Method_k__BackingField))),8), \" \", "
				"_Scheme_k__BackingField, \"://\", $isnull(_HttpRequestHeaders_k__BackingField._headers._Host._values,\"\"), _Path_k__BackingField, $isnull(_QueryString_k__BackingField,\"\")";
			flagsQ.cmd = cmd;
			flagsQ.fobj = true;
			flagsQ.nofield = true;
			flagsQ.nospace = true;
			flagsQ.obj = curr;
			wfrom_internal(flagsQ);
		}
		Out("\n%S Kestrel request(s) found matching criteria\n", formatnumber(count-skipped).c_str());
		if(skipped > 0)
			Out("%S connection(s) skipped (no current request or filtered out)\n", formatnumber(skipped).c_str());
		if(!showRunning)
			Out("Note: Running time is not shown because the dump has no capture time (Linux). Start Time is UTC\n");
		Out("Note: Status is provisional (default 200) until State reaches HeadersCommitted\n");
		return;
	}

	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj(addr);
	bool isKestrel = obj.IsValid() &&
		(obj.TypeName().find(L"Http1Connection") != std::wstring::npos
		|| obj.TypeName().find(L"Http2Stream") != std::wstring::npos
		|| obj.TypeName().find(L"Http3Stream") != std::wstring::npos
		|| obj.TypeName().find(L"HttpProtocol") != std::wstring::npos);
	if(!isKestrel)
	{
		Out("Object at %p is invalid or not a Kestrel connection (Http1Connection/Http2Stream/Http3Stream)\n", addr);
		return;
	}

	Out("\nRequest Info\n"
		"================================\n");
	FromFlags flagsQ;
	ZeroMemory(&flagsQ, sizeof(flagsQ));
	std::string cmd =
		"$a(\"Address        \",$addr()),"
		"$a(\"Type           \",$typename()),"
		"$a(\"Connection Id  \",$isnull(_ConnectionIdFeature_k__BackingField,\"\")),"
		"$a(\"Request Id     \",$isnull(_requestId,\"\")),"
		"$a(\"State          \",$enumname(_requestProcessingStatus)),"
		"$a(\"Started (UTC)  \",$tickstodatetime($maskticks(" KESTREL_START_FIELD "._dateData))),";
	if(showRunning)
		cmd += "$a(\"Running        \",$tickstotimespan($maskticks($now())-$maskticks(" KESTREL_START_FIELD "._dateData))),";
	cmd +=
		"$a(\"Verb           \",$isnull(_methodText, $upper($enumname(_Method_k__BackingField)))),"
		"$a(\"Url            \",_Scheme_k__BackingField+\"://\"+$isnull(_HttpRequestHeaders_k__BackingField._headers._Host._values,\"\")+_Path_k__BackingField+$isnull(_QueryString_k__BackingField,\"\")),"
		"$a(\"Status         \",$replace($lpad(_statusCode,1),\"0n\",\"\")),"
		"$a(\"Http Version   \",$enumname(_httpVersion)),"
		"$a(\"Endpoint       \",$isnull(_endpoint._DisplayName_k__BackingField,\"\")),"
		"$a(\"Client         \",$ipaddress(_RemoteIpAddress_k__BackingField)+\":\"+$replace($lpad(_RemotePort_k__BackingField,1),\"0n\",\"\")),"
		"$a(\"Server         \",$ipaddress(_LocalIpAddress_k__BackingField)+\":\"+$replace($lpad(_LocalPort_k__BackingField,1),\"0n\",\"\")),"
		"$a(\"Host           \",$isnull(_HttpRequestHeaders_k__BackingField._headers._Host._values,\"\")),"
		"$a(\"User Agent     \",$isnull(_HttpRequestHeaders_k__BackingField._headers._UserAgent._values,\"\")),"
		"$a(\"Content Type   \",$isnull(_HttpRequestHeaders_k__BackingField._headers._ContentType._values,\"\")),"
		"$a(\"Thread(s)      \",$if($strsize($stackroot($addr())),$stackroot($addr()),\"--\"))";
	flagsQ.cmd = cmd;
	flagsQ.fobj = true;
	flagsQ.obj = addr;
	wfrom_internal(flagsQ);

	Out("\nYou may also be interested in\n"
	      "================================\n");
	Dml("Dump all fields  : <link cmd=\"!wselect * from %p\">!wselect * from %p</link>\n", addr, addr);
	Dml("Find stack roots : <link cmd=\"!wfrom -obj %p select $a(&quot;Threads&quot;,$stackroot($addr()))\">!wfrom -obj %p select $a(\"Threads\",$stackroot($addr()))</link>\n", addr, addr);
}

const char classVisib[][40]={"private", "public", "/*nested*/ public", "/*nested*/ private", "protected", "internal protected", "internal /*protected*/"};
const char fieldVisib[][40]={"private", "protected", "internal protected", "internal public", "protected", "internal protected", "public"};

std::string GetAttributes(IMetaDataImport *mi, mdToken Token)
{
	MDUTF8CSTR name;
	mdFieldDef tokens[64]={0};
	HCORENUM henum = 0;
	PCCOR_SIGNATURE ppvSigBlob=NULL;
	UVCP_CONSTANT ppValue=NULL;
	ULONG total = 0;
	HRESULT hr;
	//
	//	Display all custom attributes
	//
	if(mi->EnumCustomAttributes(&henum,Token,0,tokens,64,&total) == S_OK)
	{
		std::string strAttr;
		for(int i=0;i<total;i++)
		{
			mdToken mdTypeDef=0;
			DWORD pdwAttr,pdwCPlusTypeFlag=0;
			ULONG pchField, pcchValue, pcbSigBlob=0;
			hr=mi->GetCustomAttributeProps(tokens[i], NULL, &mdTypeDef, NULL, NULL);
			if(hr==S_OK)
			{
				mi->GetNameFromToken(mdTypeDef, &name);
				if(strAttr.size() > 0) strAttr.append(", ");
				strAttr.append(name);
			}
		}
		if(strAttr.size() > 0)
		{
			return "["+strAttr+"]";
		}
	}

	return "";
}

void SplitType(const std::wstring& FullName, std::wstring& NameSpace, std::wstring& TypeName)
{
	TypeName = FullName;
	NameSpace.clear();
	size_t found = FullName.find_last_of(L".");
	if(found != wstring::npos)
	{
		NameSpace = FullName.substr(0, found);
		TypeName = FullName.substr(found+1);
	}
	return;
}

/*
DacpCodeHeaderData GetCode(CLRDATA_ADDRESS MT, ULONG32 Slot)
{
		CLRDATA_ADDRESS entry;
		CLRDATA_ADDRESS mtLocal;

	
		HRESULT hr=sosData->GetMethodTableSlot(MT, Slot, &entry);
		DacpCodeHeaderData code;
		ZeroMemory(&code,sizeof(code));
		if(hr==S_OK) hr=code.Request(sosData, entry);
		return code;
}
*/


EXT_COMMAND(wclass,
            "Dump Class Layout. Use '!whelp wclass' for detailed help",
			"{;e,r;;Address,Class Method Table}"
			)
{
	DO_INIT_API;

	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	pRuntime->DumpClass(addr);
	Out("\n");
}

EXT_COMMAND(wservice,
            "Dump WCF Services. Use '!whelp wservice' for detailed help",
			"{;e,o;;Address,WCF Service Address}"
			)
{
	DO_INIT_API;
	if(!HasUnnamedArg(0))
	{
		if(!indc)
		{
			Dml("To list ServiceHost summary, run <link cmd=\"!windex;!wservice\">!windex</link> first\n");
			return;
		}
		MatchingAddresses addresses;
		addresses.clear();
		indc->GetByDerive("System.ServiceModel.ServiceHostBase", addresses);
		AddressEnum adenum;
		UINT64 count=0;
		if(addresses.size())
		{
		adenum.Start(addresses);

		if(sizeof(void*)==8)
			Out("Address\t\tState        EndPoints BaseAddresses  Behaviors Throttled   Calls/Max   Sessions/Max    ConfigName,.NET Type\n");
		else
			Out("Address\tState        EndPoints BaseAddresses  Behaviors Throttled   Calls/Max   Sessions/Max    ConfigName,.NET Type\n");
		while(CLRDATA_ADDRESS curr=adenum.GetNext())
		{
			count++;
			Dml("<link cmd=\"!wservice %p\">%p</link>\t",curr, curr);
			FromFlags flags;
			ZeroMemory(&flags, sizeof(flags));
			flags.cmd = "$enumname(state), \"\\t\\t\", description.endpoints.items._size, \"\\t\\t\",baseAddresses.items._size, \"\\t\\t\", description.behaviors.dict.count-description.behaviors.dict.freeCount, \"\\t\", $if(serviceThrottle.isActive!=0,\"True  \",\"False\"),\"\\t\", serviceThrottle.calls.count,\"/\", serviceThrottle.calls.capacity,\"\\t\",serviceThrottle.sessions.count, \"/\",serviceThrottle.sessions.capacity,\"\\t\\\"\",description.configurationName,\"\\\",\",$isnull($typefrommt(description.serviceType.m_handle.m_ptr),\"Type Not Loaded\")";
			flags.fobj = true;
			flags.nofield = true;
			flags.nospace = true;
			flags.obj = curr;
			wfrom_internal(flags);
		}
		}

		// CoreWCF hosts (ASP.NET Core / self-hosted, including Linux dumps). The object model
		// mirrors System.ServiceModel with modernized (_ prefixed / backing) field names
		MatchingAddresses coreAddresses;
		indc->GetByDerive("CoreWCF.ServiceHostBase", coreAddresses);
		if(coreAddresses.size())
		{
			adenum.Start(coreAddresses);
			Out("\nCoreWCF Services\n");
			if(sizeof(void*)==8)
				Out("Address\t\tState        EndPoints BaseAddresses  ActiveCalls    ConfigName,.NET Type\n");
			else
				Out("Address\tState        EndPoints BaseAddresses  ActiveCalls    ConfigName,.NET Type\n");
			while(CLRDATA_ADDRESS curr=adenum.GetNext())
			{
				count++;
				Dml("<link cmd=\"!wservice %p\">%p</link>\t",curr, curr);
				FromFlags flags;
				ZeroMemory(&flags, sizeof(flags));
				flags.cmd = "$enumname(_State_k__BackingField), \"\\t\\t\", _Description_k__BackingField._Endpoints_k__BackingField.items._size, \"\\t\\t\", _InternalBaseAddresses_k__BackingField._Items_k__BackingField._size, \"\\t\\t\", _instances._BusyCount_k__BackingField, \"\\t\\\"\", _Description_k__BackingField._configurationName, \"\\\",\", $typename()";
				flags.fobj = true;
				flags.nofield = true;
				flags.nospace = true;
				flags.obj = curr;
				wfrom_internal(flags);
			}
		}
		Out("\n%S ServiceHost object(s) found\n", formatnumber(count).c_str());
		return;
	}
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj(addr);
	bool isCoreWcf = obj.IsValid() && obj.classObj.Implement(L"CoreWCF.ServiceHostBase");
	if(!obj.IsValid() || (!isCoreWcf && !obj.classObj.Implement(L"System.ServiceModel.ServiceHostBase")))
	{
		Out("Object at %p is invalid or not of type [System.ServiceModel.ServiceHostBase] or [CoreWCF.ServiceHostBase]\n", addr);
		return;
	}
	if(isCoreWcf)
	{
		std::vector<std::string> coreFields;
		coreFields.push_back("_InternalBaseAddresses_k__BackingField._Items_k__BackingField._items");
		coreFields.push_back("_ChannelDispatchers_k__BackingField._Items_k__BackingField._items");
		coreFields.push_back("_Description_k__BackingField._Endpoints_k__BackingField.items._items");
		varMap fieldV;
		DumpFields(addr,coreFields,0,&fieldV);
		SVAL v;

		v.SetPtr(addr);
		GetObjSel("Service Info",
			"select \"Address            : \",$addr(),"
			"\"\\nRuntime Type       : \",$typename(),"
			"\"\\nConfiguration Name : \",_Description_k__BackingField._configurationName,"
			"\"\\nNamespace          : \",_Description_k__BackingField._Namespace_k__BackingField,"
			"\"\\nState              : \",$enumname(_State_k__BackingField),"
			"\"\\nEndPoints          : \",_Description_k__BackingField._Endpoints_k__BackingField.items._size,"
			"\"\\nBase Addresses     : \",_InternalBaseAddresses_k__BackingField._Items_k__BackingField._size,"
			"\"\\nActive Calls       : \",_instances._BusyCount_k__BackingField,"
			"\"\\nOpen Timeout       : \",$tickstotimespan(_openTimeout._ticks),"
			"\"\\nClose Timeout      : \",$tickstotimespan(_closeTimeout._ticks),"
			"\"\\nEvents Raised      : \",$if(_raisedClosed + _raisedClosing + _raisedFaulted, $if(_raisedClosed,\"ClosedEvent \",\"\")+$if(_raisedClosing,\"ClosingEvent \",\"\")+$if(_raisedFaulted,\"FaultedEvent \",\"\"),\"No Event raised\")");

		// "select _string" alone trips the expression parser (works fine as a nested
		// path segment), so read the field indirectly
		v=fieldV["_InternalBaseAddresses_k__BackingField._Items_k__BackingField._items"];
		GetArraySel("Service Base Addresses", "select $fieldfromobj($addr(),\"_string\")");

		v=fieldV["_ChannelDispatchers_k__BackingField._Items_k__BackingField._items"];
		GetArraySel("Channels",
			"select \"Address            : \",$addr(),\"\\nBinding Name       : \",$isnull(_BindingName_k__BackingField,\"\"),\"\\nState              : \",$enumname(_State_k__BackingField),\"\\nAborted            : \",$if(_Aborted_k__BackingField==0,\"No\",\"Yes\"),$if(_ListenUri_k__BackingField,\"\\nListener URI       : \"+_ListenUri_k__BackingField._string,\"\"),\"\\n\"");

		v=fieldV["_Description_k__BackingField._Endpoints_k__BackingField.items._items"];
		if(v.IsValid && v.Value.ptr != NULL)
		{
			vector<CLRDATA_ADDRESS> epList;
			SpecialCases::EnumArray(v.Value.ptr, 0, NULL, &epList);
			Out("\nEndpoints\n");
			Out("\n================================\n");
			for(int i=0;i<epList.size(); i++)
			{
				v.SetPtr(epList[i]);
				GetObjSel("",
					"select \"Address            : \",$addr(),\"\\nConfiguration Name : \",_contract._ConfigurationName_k__BackingField,\"\\nContract Namespace : \",_contract._ns,\"\\nSession Mode       : \",$enumname(_contract._sessionMode),\"\\nListening Mode     : \",$enumname(_listenUriMode),$if(_id,\"\\nEndpoint Id        : \"+_id,\"\"),$if(_Address_k__BackingField._Uri_k__BackingField,\"\\nURI                : \"+_Address_k__BackingField._Uri_k__BackingField._string,\"\")");
				std::vector<std::string> epFields;
				varMap epV;
				epFields.push_back("_Binding_k__BackingField");
				epFields.push_back("_contract");
				DumpFields(epList[i],epFields,0,&epV);
				SVAL b = epV["_Binding_k__BackingField"];
				if(b.IsValid && b.Value.ptr)
				{
					// Field set varies per concrete Binding type (BasicHttpBinding, NetTcpBinding,
					// custom bindings, ...); $containfieldoftype guards each optional field so this
					// stays generic instead of erroring on bindings that lack a given field
					v.SetPtr(b.Value.ptr);
					GetObjSel("Binding",
						"select \"Address            : \",$addr(),"
						"\"\\nType               : \",$typename(),"
						"$if($containfieldoftype(\"_name\"),\"\\nName               : \"+_name,\"\"),"
						"$if($containfieldoftype(\"_namespaceIdentifier\"),\"\\nNamespace          : \"+_namespaceIdentifier,\"\"),"
						"$if($containfieldoftype(\"_openTimeout\"),\"\\nOpen Timeout       : \"+$tickstotimespan(_openTimeout._ticks),\"\"),"
						"$if($containfieldoftype(\"_closeTimeout\"),\"\\nClose Timeout      : \"+$tickstotimespan(_closeTimeout._ticks),\"\"),"
						"$if($containfieldoftype(\"_receiveTimeout\"),\"\\nReceive Timeout    : \"+$tickstotimespan(_receiveTimeout._ticks),\"\"),"
						"$if($containfieldoftype(\"_sendTimeout\"),\"\\nSend Timeout       : \"+$tickstotimespan(_sendTimeout._ticks),\"\"),"
						"$if($containfieldoftype(\"_MessageEncoding_k__BackingField\"),\"\\nMessage Encoding   : \"+$enumname(_MessageEncoding_k__BackingField),\"\")");
					Out("Full Binding Detail: ");
					Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",b.Value.ptr, b.Value.ptr);
				}
				b = epV["_contract"];
				if(b.IsValid && b.Value.ptr)
				{
					Out("Contract           : ");
					Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",b.Value.ptr, b.Value.ptr);
				}
			}
		}
		return;
	}
	std::vector<std::string> fields;
	fields.push_back("aborted");
	fields.push_back("closeCalled");
	fields.push_back("exceptionQueue");
	fields.push_back("onClosingCalled");
	fields.push_back("onClosedCalled");
	fields.push_back("onOpeningCalled");
	fields.push_back("onOpenedCalled");
	fields.push_back("raisedClosed");
	fields.push_back("raisedClosing");
	fields.push_back("raisedFaulted");
	fields.push_back("traceOpenAndClose");
	fields.push_back("state");

	fields.push_back("Closed");  // Value[n]._name, Value[n]._stringValue
	fields.push_back("Closing");
	fields.push_back("Faulted");
	fields.push_back("Opened");
	fields.push_back("Opening");

	fields.push_back("initializeDescriptionHasFinished");

	fields.push_back("baseAddresses");
	fields.push_back("baseAddresses.items._items"); // select m_String
	fields.push_back("channelDispatchers.items._items"); // !wfrom -nofield -nospace -array 000000015ff4a858 select "Binding Name       : ",bindingName, "\nActive or Aborted  : ",$if(aborted==0,"Active","Aborted"),"\nState              : ",$enumname(state),"\nTransaction Type   : ",$if(transactionIsolationLevelSet!=0,$enumname(transactionIsolationLevel),"No transaction"),$if(transactionIsolationLevelSet==0,", Timeout: "+$tickstotimespan(transactionTimeout._ticks),""),"\n"
	fields.push_back("description.endpoints.items._items");
	fields.push_back("extensions.items._items");
	fields.push_back("description.behaviors.items._items");

	varMap fieldV;
	DumpFields(addr,fields,0,&fieldV);
	SVAL v;

	v.SetPtr(addr);
	GetObjSel("Service Info","select \"Address            : \",$addr(),\"\\nConfiguration Name : \",description.configurationName, \"\\nState              : \",$enumname(state), \"\\nEndPoints          : \", description.endpoints.items._size, \"\\nBase Addresses     : \",$isnull(baseAddresses.items._size,0), \"\\nBehaviors          : \", $isnull(description.behaviors.dict.count-description.behaviors.dict.freeCount,0), \"\\nRuntime Type       : \",$typefrommt(description.serviceType.m_handle.m_ptr),\"\\nIs Throttled?      : \",$if(serviceThrottle.isActive,\"True\",\"False\"),\"\\nCalls/Max Calls    : \",serviceThrottle.calls.count, \"/\",serviceThrottle.calls.capacity, \"\\nSessions/Max       : \",serviceThrottle.sessions.count,\"/\", serviceThrottle.sessions.capacity,\"\\nEvents Raised      : \",$if(raisedClosed + raisedClosing + raisedFaulted, $if(raisedClosed,\"ClosedEvent \",\"\")+$if(raisedClosing,\"ClosingEvent \",\"\")+$if(raisedFaulted,\"FaltedEvent \",\"\"),\"No Event raised\"),\"\\nHandles Called     : \",$if(closeCalled + onClosingCalled + onClosedCalled + onOpeningCalled + onOpenedCalled, $if(closeCalled,\"ClosedCalled \",\"\")+$if(onClosingCalled,\"OnClosingHandle \",\"\")+$if(onClosedCalled,\"OnClosedHandle \",\"\")+$if(onOpeningCalled,\"OnOpeningHandle \",\"\")+$if(onOpenedCalled,\"OnOpenedHandle \",\"\"),\"No handle called\"),\"\\nSession Mode       : \", $if(sessionMode,\"True\",\"False\")");


	v=fieldV["extensions.items._items"];
	if(v.IsValid && v.Value.ptr)
	{
		Out("Extensions         : ");
		Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",v.Value.ptr, v.Value.ptr);
	}
	v=fieldV["description.behaviors.items._items"];
	if(v.IsValid && v.Value.ptr != NULL)
	{
		GetArraySel("Service Behaviors",
			"where ($typename() == \"System.ServiceModel.ServiceBehaviorAttribute\") select \"Concurrency Mode   : \",$enumname(concurrencyMode), \"\\nInstance Mode      : \", $enumname(instanceMode), \"\\nAdd Error in Faults: \", $if(includeExceptionDetailInFaults, \"true\", \"false\"), \"\\nMax Items Obj Graph: \",maxItemsInObjectGraph, \"\\nIsolation Level    : \", $enumname(transactionIsolationLevel), \"\\nSession Shutdown   : \", $if(automaticSessionShutdown, \"Automatic\", \"Explicit\")");
		GetArraySel("",
			"where ($typename() == \"System.ServiceModel.Activation.AspNetCompatibilityRequirementsAttribute\") select \"ASP.NET Compatib   : \",$enumname(requirementsMode)");
		GetArraySel("",
			"where ($typename() == \"System.ServiceModel.Description.ServiceMetadataBehavior\") select \"Http Get Enabled   : \", $if(httpGetEnabled, \"true\", \"false\"), \"\\nHttps Get Enabled  : \", $if(httpsGetEnabled, \"true\", \"false\"), $if(httpGetUrl, \"\\nHttps Get Url      : \"+httpGetUrl.m_String,\"\"),$if(httpsGetUrl, \"\\nHttps Get Url      : \"+httpsGetUrl.m_String,\"\"), \"\\nMex Enabled        : \", $if(mexContract, \"true\", \"false\")");
		Out("All Service Behav  : ");
		Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",v.Value.ptr, v.Value.ptr);

	}
	v=fieldV["baseAddresses.items._items"];
	GetArraySel("Service Base Addresses", "select m_String");

	v=fieldV["channelDispatchers.items._items"];
	GetArraySel("Channels",
		"select \"Address            : \",$addr(),\"\\nListener URI       : \",listener.uri.m_String, \"\\nBinding Name       : \",bindingName, \"\\nAborted            : \",$if(aborted==0,\"No\",\"Yes\"),\"\\nState              : \",$enumname(state),\"\\nTransaction Type   : \",$if(transactionIsolationLevelSet!=0,$enumname(transactionIsolationLevel),\"No transaction\"),$if(transactionIsolationLevelSet!=0,\", Timeout: \"+$tickstotimespan(transactionTimeout._ticks),\"\"),\"\\n\",\"Listener State     : \",$enumname(listener.state),\"\\nTimeout settings   : \", \"Open [\",$tickstotimespan(listener.closeTimeout._ticks),\"] Close [\",$tickstotimespan(listener.openTimeout._ticks),\"] Receive: [\",$tickstotimespan(listener.receiveTimeout._ticks),\"] Send: [\"+$tickstotimespan(listener.sendTimeout._ticks),\"]\",\"\\nServer Capabilities: SupportsServerAuth \",$if(listener.securityCapabilities.supportsServerAuth==0,\"[No ]\",\"[Yes]\"),\" SupportsClientAuth \",$if(listener.securityCapabilities.supportsClientAuth==0,\"[No ]\",\"[Yes]\"),\" SupportsClientWinIdent \", $if(listener.securityCapabilities.supportsClientWindowsIdentity==0,\"[No ]\",\"[Yes]\"),\"\\nRequest Prot Level : \",$enumname(listener.securityCapabilities.requestProtectionLevel),\"\\nResponse Prot Level: \",$enumname(listener.securityCapabilities.responseProtectionLevel),\"\\nEvents Raised      : \",$if(raisedClosed + raisedClosing + raisedFaulted, $if(raisedClosed,\"ClosedEvent \",\"\")+$if(raisedClosing,\"ClosingEvent \",\"\")+$if(raisedFaulted,\"FaltedEvent \",\"\"),\"No Event raised\"),\"\\nHandles Called     : \",$if(closeCalled + onClosingCalled + onClosedCalled + onOpeningCalled + onOpenedCalled, $if(closeCalled,\"ClosedCalled \",\"\")+$if(onClosingCalled,\"OnClosingHandle \",\"\")+$if(onClosedCalled,\"OnClosedHandle \",\"\")+$if(onOpeningCalled,\"OnOpeningHandle \",\"\")+$if(onOpenedCalled,\"OnOpenedHandle \",\"\"),\"No handle called\"),\"\\nSession Mode       : \", $if(session,\"True\",\"False\"),\"\\n\"");

	v=fieldV["description.endpoints.items._items"];

	if(v.IsValid && v.Value.ptr != NULL)
	{
		vector<CLRDATA_ADDRESS> addBinding;
		SpecialCases::EnumArray(v.Value.ptr, 0, NULL, &addBinding);
		Out("\nEndpoints\n");
		Out("\n================================\n");

		for(int i=0;i<addBinding.size(); i++)
		{
			v.SetPtr(addBinding[i]);
			GetObjSel("",
				"select \"Address            : \",$addr(),\"\\nURI                : \",address.uri.m_String, \"\\nIs Anonymous       : \",$if(isAnonymous, \"True \",\"False\"),\"\\nConfiguration Name : \",contract.configurationName, \"\\nType Name          : \",$typefrommt(contract.contractType.m_handle.m_ptr), $if(name.encoded,\"\\nXml Name (encoded) : \"+name.encoded,\"\"), $if(name.decoded, \"\\nXml Name (decoded) : \"+name.decoded,\"\"), \"\\nListening Mode     : \",$enumname(listenUriMode)");
			std::vector<std::string> fields;
			varMap fieldV;
			fields.push_back("contract.contractType.m_handle.m_ptr");
			fields.push_back("behaviors.items._items");
			fields.push_back("binding");
			DumpFields(v.Value.ptr,fields,0,&fieldV);
			v=fieldV["contract.contractType.m_handle.m_ptr"];
			if(v.IsValid && v.Value.ptr)
			{
				Out("Class Definition   : ");
				Dml("<link cmd=\"!wclass %p\">%p</link>",v.Value.ptr, v.Value.ptr);
				Out(" %S\n",GetMethodName(v.Value.ptr).c_str());
			}
			v=fieldV["behaviors.items._items"];
			if(v.IsValid && v.Value.ptr)
			{
				Out("Behaviors          : ");
				Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",v.Value.ptr, v.Value.ptr);
			}
			v=fieldV["binding"];
			if(v.IsValid && v.Value.ptr)
			{
				Out("Binding            : ");
				Dml("<link cmd=\"!wselect * from %p\">%p</link>\n",v.Value.ptr, v.Value.ptr);
			}
		}
	}



}
	EXT_COMMAND(wxml,
				"Dump XML Document or XML Node. Use '!whelp wkeyvalue' for detailed help",
				"{;e,r;;Address,Address of the XML Element}"
				)
	{
		DO_INIT_API;
		CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
		ObjDetail Obj(addr);
		HRESULT hr = E_FAIL;
		if(Obj.IsValid() && Obj.classObj.Implement(L"System.Xml.XmlNode"))
		{
			hr = pHeap->DumpXml(addr);
		}
		if(hr != S_OK)
		{
			Out("There is no valid XML object at %p\n", addr);
		}
	}

struct ModuleFlags
{
	bool fname;
	bool fcompany;
	bool forder;
	bool fdebug;
	bool fmanaged;
	bool fnoms;
	bool ffullpath;
	bool fsaveto;
	bool fdomain;
	CLRDATA_ADDRESS AppDomain;
	std::string saveTo;
	std::string name;
	std::string company;
};

EXT_COMMAND(wmodule,
	"Dump all modules in process which can be filtered by name, company, debug mode, etc. Use '!whelp wmodule' for detailed help",
	"{name;s,o;;Dump only modules with this name pattern. Optional (e.g -name Contoso.Classes*)}"
	"{company;s,o;;Dump only modules with this company name pattern. Optional (e.g. -company Contoso*)}"
	"{debug;b,o;;Show only managed modules compiled in debug mode}"
	"{managed;b,o;;Show only managed modules}"
	"{noms;b,o;;Hide all modules which company attribute is Microsoft Corporation (show non-system modules)}"
	"{order;b,o;;Sort output bu module name}"
	"{fullpath;b,o;;Show module full path (instead of just name}"
	"{domain;e,o;;Only show modules from a particular domain (e.g. -domain 07fff8090)}"
	"{saveto;x,o;;Save the selected modules to a folder. Optional (e.g. -saveto c:\\My Modulestemp\\)}"
	)
{
		DO_INIT_API;
		ModuleFlags flags;

		flags.fname = HasArg("name");
		flags.fcompany = HasArg("company");
		flags.fdebug= HasArg("debug");
		flags.fmanaged = HasArg("managed");
		flags.fnoms = HasArg("noms");
		flags.forder = HasArg("order");
		flags.ffullpath = HasArg("fullpath");
		flags.fsaveto = HasArg("saveto");
		flags.fdomain = HasArg("domain");
		flags.AppDomain = 0;

		if(flags.fdomain)
		{
			flags.AppDomain = GetArgU64("domain");
		}

		if(flags.fname)
			flags.name = GetArgStr("name");

		if(flags.fcompany)
			flags.company = GetArgStr("company");

		if(flags.fsaveto)
			flags.saveTo = GetArgStr("saveto");

		if(pTarget->DumpModules((LPSTR)flags.name.c_str(), (LPSTR)flags.company.c_str(), (LPSTR)flags.saveTo.c_str(), flags.fdebug,
            flags.fmanaged, flags.fnoms, flags.forder, flags.ffullpath, flags.AppDomain) != S_OK)
		{
			Out("UNEXPECTED: Unable to dump modules\n");
		};




}

EXT_COMMAND(wtime,
	"Show UTC and local time. Use '!whelp wtime' for detailed help",
	""
	)
{
		DO_INIT_API;
		if(isLinuxTarget)
		{
			// Time comes from KUSER_SHARED_DATA (with a dbgeng session-time fallback); a Linux
			// process has neither, and ELF core dumps carry no capture timestamp at all.
			Out("!wtime is not supported in Linux\n");
			return;
		}
		if(pTarget->DumpTime() != S_OK)
		{
			Out("UNEXPECTED: Unable to display time\n");
		};

}

EXT_COMMAND(wmakesource,
	"It tries to reflect the current frame into source code. Use '!whelp wmakesource' for detailed help",
	 "{;e,o;;IPAddress,IP Address}"
	)
{
		DO_INIT_API;
		CLRDATA_ADDRESS ip = 0;
		if(HasUnnamedArg(0))
		{
			ip = GetUnnamedArgU64(0);
		}
		HRESULT hr;

		if(NULL != ip)
		{
			hr = pTarget->MakeSourceFromIp(ip);
		} else
		{
			hr = pTarget->MakeSource();
		}

		if(hr != S_OK)
		{
			Out("Error generating source and symbols\n");
		};

}

EXT_COMMAND(wconcurrentdict,
	"Dump ConcurrentDictionary. Use '!whelp wconcurrentdict' for detailed help",
	"{;e,r;;Address,ConcurrentDictionary Address}")
{
	INIT_API();

	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	ObjDetail obj_detail(addr);

	if (!obj_detail.IsValid() ||
		!obj_detail.classObj.Implement(L"System.Collections.Concurrent.ConcurrentDictionary*"))
	{
		Out("Object at %p is invalid or not of type [System.Collections.Concurrent.ConcurrentDictionary]\n", addr);
		return;
	}

	std::vector<std::string> fields;
	const std::string field("m_tables");
	fields.push_back(field);
	varMap fields_map;
	DumpFields(addr, fields, 0, &fields_map);

	CLRDATA_ADDRESS tablesAddr = fields_map[field].Value.ptr;
	if (tablesAddr == NULL)
	{
		Out("Empty ConcurrentDictionary\n");
		return;
	}

	fields.clear();
	fields_map.clear();
	fields.push_back("m_buckets");
	fields.push_back("m_countPerLock");
	DumpFields(tablesAddr, fields, 0, &fields_map);

	CLRDATA_ADDRESS countPerLockAddr = fields_map["m_countPerLock"].Value.ptr;
	ObjDetail countPerLocks(countPerLockAddr);
	if (!countPerLocks.IsValid())
	{
		Out("ConcurrentDictionary consistency compromised\n");
		return;
	}

	int count = 0;
	for (int i = 0; i < countPerLocks.NumComponents(); ++i)
	{
		ExtRemoteData rm(countPerLocks.DataPtr() + i * countPerLocks.InnerComponentSize(), sizeof(int));
		int current = rm.GetLong();
		count += current;
	}

	Out("Items   : %i\n", count);

	CLRDATA_ADDRESS bucketsAddr = fields_map["m_buckets"].Value.ptr;

	if (bucketsAddr == NULL)
	{
		Out("Empty ConcurrentDictionary\n");
		return;
	}

	ObjDetail buckets(bucketsAddr);
	if (!buckets.IsValid())
	{
		Out("ConcurrentDictionary consistency compromised\n");
		return;
	}

	auto mt = buckets.MethodTable();

	std::vector<CLRDATA_ADDRESS> addresses;
	SpecialCases::EnumArray(bucketsAddr, buckets.MethodTable(), nullptr, &addresses);

	for (const auto& address : addresses)
	{
		if (address == 0)
			continue;

		fields.clear();
		fields_map.clear();
		fields.push_back("m_key");
		fields.push_back("m_value");

		DumpFields(address, fields, mt);
	}
}
