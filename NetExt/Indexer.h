/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "CLRHelper.h"
#include <map>
#include <string>
#include <vector>
#include <strstream>

using namespace std;

typedef std::vector<DWORD_PTR> AddressList;

struct typeIndex
{
	wstring typeName;
	MD_TypeData mtData;
	//DWORD_PTR EEClass;
	UINT64 size;
	AddressList Addresses;
	typeIndex():size(0){};
};

typedef map<DWORD_PTR, typeIndex> MTTable;
typedef multimap<wstring, DWORD_PTR> TypeTable;
typedef vector<AddressList*> MatchingAddresses;

struct AddressEnum
{
	MatchingAddresses* Addrs;
	MatchingAddresses::const_iterator it;
	AddressList::const_iterator ita;
	void Start(MatchingAddresses& AddressList)
	{
		Addrs=&AddressList;
		it=Addrs->begin();
		ita = (*it)->begin();
	}
	CLRDATA_ADDRESS GetNext()
	{
		if(it==Addrs->end()) return NULL;
		CLRDATA_ADDRESS temp = *ita;
		ita++;
		if(ita==(*it)->end())
		{
			it++;
			if(it!=Addrs->end())
				ita = (*it)->begin();
		}

		//sprintf_s(
		return temp;
	}
};

bool DisplayHeapEnum(MatchingAddresses& Addresses, bool Short=false);

#pragma once
class Indexer
{
private:
	MTTable mtT;
	TypeTable typeT;
	string signature;
	bool isValid;
	std::string treeFilename;
	KnownTypesMT commonMTs;
public:
	UINT64 count;
	UINT64 size;
	Indexer(void);
	~Indexer(void);
	static std::string CalculateSignature();
	bool DoIndex(bool ShowProgress=true);
	void GetByType(std::wstring PartialTypeNames, MatchingAddresses& Addresses);
	void GetByMethodName(std::string MethodNameStringList, MatchingAddresses& Addresses);
	void GetByFieldName(std::string FieldNameStringList, MatchingAddresses& Addresses);
	void GetByFieldType(std::string FieldTypeStringList, MatchingAddresses& Addresses);
	void GetByDerive(std::string DeriveStringList, MatchingAddresses& Addresses);
	void GetWithPointers(MatchingAddresses& Addresses);
	void AddAddress(DWORD_PTR Address, DWORD_PTR MethodTable, MD_TypeData* obj);
	bool IsUpToDate(void);
	void DumpTypes();
	void DumpTypesTree(EXT_CLASS *Ext);
	bool SaveIndex(std::string FileName);
	bool LoadIndex(std::string FileName, bool IgnoreSignature=false);
	bool WalkHeapAndCache(Indexer *idx, CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End, bool IsLarge, CLRDATA_ADDRESS Minimum, CLRDATA_ADDRESS Maximum, DumpHeapCache* Cache, bool quiet=false);
};

extern Indexer *indc;