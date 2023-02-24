<@  
	request.DBMS().RegisterDbConnection("mailchatexporter","SqlServer","Data Source=localhost;Initial Catalog=BCORING;User Id=bcoring;Password=bc@r1ng;MultipleActiveResultSets=true;");
	request.Listen("3000");
 @>