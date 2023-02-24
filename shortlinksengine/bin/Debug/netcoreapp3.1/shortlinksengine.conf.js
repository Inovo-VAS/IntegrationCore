<@  
	request.DBMS().RegisterDbConnection("shortlinkengine","SqlServer","Data Source=localhost;Initial Catalog=BCORING;User Id=SHORTLINK;Password=SHORTLINK;MultipleActiveResultSets=true;");
	//request.ENV().Root="D:/mystuff/bcoring/shortlink/";
	request.Listen("4000");
 @>