/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "netext.h"


using namespace std;


regex_constants::syntax_option_type EXT_CLASS::GetFlavor(const string& flavor)
{
	const char* flavorStrings[] = { "basic", "extended", "ecmascript", "awk", "grep", "egrep" };
	const regex_constants::syntax_option_type flavors[] = { regex_constants::basic, regex_constants::extended, regex_constants::ECMAScript, regex_constants::awk,
							regex_constants::grep, regex_constants::egrep };

	string normalized(flavor);
	transform(normalized.begin(), normalized.end(), normalized.begin(),(int(*)(int))tolower);
	

	for(int i=0;i<6;i++)
	{
		{
		if(strcmp(normalized.c_str(), flavorStrings[i])==0)
			return flavors[i];
		}
	}
	throw ExtStatusException(E_FAIL,
                                 "ExtEffectiveProcessorTypeHolder::"
                                 "Refresh failed");




}

smatch GetMatches(const string &Target, const string& Pattern)
{
	regex reg(Pattern, regex_constants::icase);
	smatch matches;
	regex_search(Target, matches, reg);
	return matches;
}
std::ostringstream EXT_CLASS::regexmatch(const string& Target, const string& Pattern, bool CaseSensitive = false, const string& Flavor = "ecmascript", bool Run=false, const string& Format="")
{
	table mymatches;

	std::istringstream stream(Target);
	std::ostringstream results(ios_base::in | ios::out);
	regex_constants::syntax_option_type intFlags = GetFlavor(Flavor);
	if(!CaseSensitive)
		intFlags |= regex_constants::icase;

	regex reg(Pattern.c_str(), intFlags);
	smatch matches;	


	string line;
	long lines = 0;
	while(getline(stream, line))
	{
		if(regex_search(line, matches, reg))
		{
			string newFormat;

			columns col;
			if(Format == "")
				results << line << "\r\n";
			else
			{
				newFormat = regex_replace(line, reg, Format, regex_constants::format_no_copy);
				results << newFormat << "\r\n";
			}
			for(int i=1;i<matches.size();i++)
			{
				
				results << "\t[" << i << "] : " << matches[i].str() << "\r\n";
				col.push_back(matches[i].str());

			}
			mymatches.push_back(col);
			lines++;
			if(Run)
			{
				if(newFormat.length() > 0)
				{
					results << Execute(newFormat) << "\r\n";
				}
			}
		}
	}
	results << "\r\n\t(" << lines << ") line(s) found using flavor '" << Flavor << "' Flags: " << intFlags << " and case sensivity equals to '" << CaseSensitive << "'\r\n";
	return results;
}



//----------------------------------------------------------------------------
//
// regmatch extension command.
//
// This command displays the MethodTable of a .NET object
//
// The argument string means:
//
//   {;          - No name for the first argument.
//   e,          - The argument is an expression.
//   o,          - The argument is optional.
//   ;			 - There is no argument's default expression
//   tegexp;     - The argument's short description is "Object".
//   Object address - The argument's long description.
//   }           - No further arguments.
//
// This extension has a single, optional argument that
// is an expression for the PEB address.
//
//----------------------------------------------------------------------------

const char *regmatchhelp =	"Find and print lines matching the pattern\n\n"
	"Usage: !regmatch [-case] [-flavor (basic | extended | ecmascript | awk | grep | egrep)] [-not] '<pattern>' '<execute-pattern>' << <command-list>\n"
							"Where:\n"
							"\t-case is the switch for case sensitiviness. If present search is case sensitive (default is case insensitive)\n"
							"\t-flavor is the flavor of the T1 Regex. See http://msdn.microsoft.com/en-us/library/bb982727.aspx\n"
							"\t<pattern> is the regex pattern. See examples here: http://msdn.microsoft.com/en-us/library/ms972966.aspx\n"
							"\tExamples:\n"
							"\nDump all stack object\n"
							"\t!regmatch -run '(^[0-9a-fA-F]+)\\s+([0-9a-fA-F]+)\\s' \"!do $2\" << !dso"
							;
EXT_COMMAND(regmatch,
            regmatchhelp,
			"{case;b,o;;case;if present search is case sensitive}{flavor;s;o;d=ECMAScript;flavor,Defines the regex flavor.\nValues are basic, extended, ecmascript, awk, grep, egrep}"
			"{run;b,o;;run;if present search is case sensitive}{;x;r;;pattern format << commands;Commands to run separated by ';'}")
{
	
	int lowcase = HasArg("case");

	int run = HasArg("run");

	string flavor("ecmascript");
	if(HasArg("flavor"))
		flavor.assign(GetArgStr("flavor", false));
	string custom(GetUnnamedArgStr(0));
	smatch matches = GetMatches(custom, "('([^']+)'|\"([^\"]+)\")\\s+('([^']+)'|\"([^\"]+)\")\\s+<<\\s+(.+)");
	if(matches.size() != 8)
	{
		Out("Pars: %i, Invalid Syntax: %s\r\n", matches.size(), custom.c_str());
		Out(regmatchhelp);
	}
	string format(matches[5]);
	format.append(matches[6]);
	string pattern(matches[2]);
	pattern.append(matches[3]);
	string command(matches[7]);


	Out("Parameters - case: %i, flavor: %s, run: %i, pattern: %s, format: %s, command: %s", lowcase, flavor.c_str(), run, pattern.c_str(), format.c_str(), command.c_str()  );

	

	string cmd(Execute(command));
	Out(regexmatch(cmd, pattern, lowcase, flavor, run, format).str().c_str());

}
