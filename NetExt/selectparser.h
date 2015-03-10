/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include <boost/none.hpp>
#include <boost/config/warning_disable.hpp>
#include <boost/spirit/include/qi.hpp>
#include <boost/spirit/include/phoenix_core.hpp>
#include <boost/spirit/include/phoenix_operator.hpp>
#include <boost/spirit/include/phoenix_object.hpp>
#include <boost/fusion/include/adapt_struct.hpp>
#include <boost/fusion/include/io.hpp>


#include <iostream>
#include <fstream>
#include <string>
#include <vector>


//   #define BOOST_SPIRIT_DEBUG  ///$$$ DEFINE THIS WHEN DEBUGGING $$$///

namespace sqlparser
{
	/*
    namespace fusion = boost::fusion;
    namespace phoenix = boost::phoenix;
    namespace qi = boost::spirit::qi;
    namespace ascii = boost::spirit::ascii;
	*/
	namespace qi = boost::spirit::qi;
    namespace ascii = boost::spirit::ascii;



	struct selectobj
	{
		std::string _mt;
		std::vector<std::string> _fieldlist;
		std::string _obj;
		//std::vector<std::string> _wherelist;

		selectobj()
		{
			_mt="";
		}
	};

};

	BOOST_FUSION_ADAPT_STRUCT(
		sqlparser::selectobj,
		(std::string, _mt)
		(std::vector<std::string>, _fieldlist)
		(std::string, _obj)
		);

namespace sqlparser
{
    template <typename Iterator>
    struct select_parser : qi::grammar<Iterator, selectobj(), ascii::space_type>
    {
        select_parser() : select_parser::base_type(start)
        {
            using qi::int_;
			using qi::ulong_long;
            using qi::lit;
            using qi::double_;
            using qi::lexeme;
            using ascii::char_;
			using ascii::digit;
			using ascii::alnum;

//            quoted_string %= '"' >> +(char_ - '"') >> '"';
			quoted_string = '"' >> *(escapes | qi::alnum | "\\x" >> qi::hex) >> '"';

			escapes.add("\\a", '\a')("\\b", '\b')("\\f", '\f')("\\n", '\n')
              ("\\r", '\r')("\\t", '\t')("\\v", '\v')("\\\\", '\\')
              ("\\\'", '\'')("\\\"", '\"');

			sources.add("gac", "gac")("stack", "stack");


			//top = lexeme[lit("top") >> double_];
			field_name =  lexeme[!digit >> *(alnum | '_' | '.' | ':' | '$' | '[' | ']' | '*' | '?')] ;
			field_items =  field_name % ',';
			//decimals = lexeme[-'0' >> char_("nN") >> +char_("0123456789")];
			//hexav = +char_("0123456789abcdefABCDEF");
				//-('0' >> char_("xX")) >> +char_("0123456789abcdefABCDEF");
			obj = +char_;
            start =
                +lit("wselect")
				>> -(lit("mt") >> +(alnum | '\'' | '$' | '(' | ')'))
                >> field_items 
                >> lit("from") 
                >>  (obj | sources) 
                ;
        };

        //qi::rule<Iterator, double, ascii::space_type> top;
		//qi::rule<Iterator, std::string(), ascii:space_type> decimals;
		//qi::rule<Iterator, std::string(), ascii:space_type> hexav;

        qi::rule<Iterator, std::string(), ascii::space_type> obj;
        qi::rule<Iterator, std::vector<std::string>(), ascii::space_type> field_items;

		qi::rule<Iterator, std::string(), ascii::space_type> field_name;
		qi::rule<Iterator, std::string()> quoted_string;
		qi::rule<Iterator, selectobj(), ascii::space_type> start;

		qi::symbols<char const, char const> escapes;
		qi::symbols<char const, char const*> sources;

		//		qi::rule<Iterator, std::string(), ascii::space_type> quoted_string;
		
    };



	std::string ParseSql(const std::string& SelectStr, sqlparser::selectobj *sql);
	void Split(const std::string& Str, std::vector<std::string>& Splitted);

}
