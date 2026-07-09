/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "netext.h"

using namespace std;


ostringstream EXT_CLASS::regexsearch(const string& Target, const string& Pattern, bool Not = false, bool CaseSensitive = false, const string& Flavor = "ecmascript")
{
	istringstream stream(Target);
	ostringstream results(ios_base::in | ios::out);
	regex_constants::syntax_option_type intFlags = GetFlavor(Flavor);
	if(!CaseSensitive)
		intFlags |= regex_constants::icase;

	regex reg(Pattern.c_str(), intFlags);
	


	string line;
	long lines = 0;
	while(getline(stream, line))
	{
		bool found = regex_search(line, reg);
		if((found && !Not) || (!found && Not))
		{
			lines++;
			results << line << "\n";
		}
	}
	results << "\n\n\t(" << lines << ") line(s) found using flavor '" << Flavor << "' Flags: " << intFlags << " and case sensivity equals to '" << CaseSensitive << "'\n";
	return results;
}



//----------------------------------------------------------------------------
//
// mt extension command.
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

const char *regsearchhelp =	"Find and print lines matching the pattern\n\n"
	"Usage: !regsearch [-case] [-flavor (basic | extended | ecmascript | awk | grep | egrep)] [-not] <pattern> <command-list>\n"
							"Where:\n"
							"\t-case is the switch for case sensitiviness. If present search is case sensitive (default is case insensitive)\n"
							"\t-flavor is the flavor of the T1 Regex. See http://msdn.microsoft.com/en-us/library/bb982727.aspx\n"
							"\t<pattern> is the regex pattern. See examples here: http://msdn.microsoft.com/en-us/library/ms972966.aspx\n"
							"\tExamples:\n"
							"\nLists only stack objects containing httpcontext\n"
							"\t!regseach  httpcontext !dso"
							;

EXT_COMMAND(regsearch,
            regsearchhelp,
			"{case;b,o;case;;if present search is case sensitive}{flavor;s;o;d=ECMAScript;flavor,Defines the regex flavor.\nValues are basic, extended, ecmascript, awk, grep, egrep}"
			"{not;b,o;not;;list lines not containing the pattern}{;s,r;case;pattern;Regex pattern}{;x,r;commands;Commands to run separated by ';'}")
{
	
	int lowcase = HasArg("case");
	int notF = HasArg("not");
	string flavor("ecmascript");
	if(HasArg("flavor"))
		flavor.assign(GetArgStr("flavor", false));
		
	string pattern(GetUnnamedArgStr(0));
	string command(GetUnnamedArgStr(1));


	Out("Parameters - case: %i, flavor: %s, not: %i, pattern: %s, command: %s", lowcase, flavor.c_str(), notF, pattern.c_str(), command.c_str()  );

	

	string cmd(Execute(command));
	Out(regexsearch(cmd, pattern, notF != 0, lowcase != 0, flavor).str().c_str());

}



