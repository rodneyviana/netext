/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "selectparser.h"


namespace sqlparser
{
#if DEBUG

	std::string showvec(const std::vector<std::string>& vec)
	{

		std::string temp("");
		for(int i=0; i < vec.size(); i++)
		{
			if(temp.length() > 0) temp +=", ";
			temp += "[" + vec[i] + "]";
		}
		return temp;
	}
#endif


	std::string ParseSql(const std::string& SelectStr, sqlparser::selectobj *sql)
	{
		using boost::spirit::ascii::space;
		typedef std::string::const_iterator iterator_type;
		typedef sqlparser::select_parser<iterator_type> sql_parser_t;

		sql_parser_t g; // Our grammar

		
		std::string::const_iterator iter = SelectStr.begin();
		std::string::const_iterator end = SelectStr.end();
		
		bool r = phrase_parse(iter, end, g, space, *sql);
		

		if(r && iter == end)
		{
			return "";
		} else
		{
			std::string errString(iter,end);
			return errString;
		}
	
	}

	void Split(const std::string& Str, std::vector<std::string>& Splitted)
	{
		bool p=boost::spirit::qi::parse(Str.begin(), Str.end(),
			+(boost::spirit::qi::alnum | '_' | ':' | '$' | '[' | ']' | '*' | '?') % '.' ,
			Splitted);
	}
}