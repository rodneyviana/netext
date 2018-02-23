#include <map>
#include <string>
#include <vector>
#pragma once

enum ULSTraceLevel
{
	/// No trace entries will be written        
	None = 0,
	/// Unexpected codepath.  These should be monitored
	Unexpected = 10,
	/// Unusual codepath.  These should be monitored
	Monitorable = 15,
	/// High Importance
	High = 20,
	/// Medium Importance
	Medium = 50,
	/// Verbose Information
	Verbose = 100,
	/// Extra Versose Information
	VerboseEx = 200
};

class SPCategories
{
public:
	static std::map<int, std::string> catMap;
	SPCategories();
	~SPCategories();

	static void EnsureMap();
	
	static bool GetAreaName(int Category, std::string& Area, std::string& Product);

	static bool ContainsAreaName(int Category, std::string PartialName);

	static std::map<int,int> GetListAreaName(std::string PartialName);

	static std::string GetSevLevel(int SevId);
};

