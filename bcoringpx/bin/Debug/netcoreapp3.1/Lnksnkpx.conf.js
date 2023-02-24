<@  
	request.DBMS().RegisterDbConnection("eptpui","SqlServer","Data Source=localhost;Initial Catalog=BCORING;User Id=EPTP;Password=EPTPEPTP;MultipleActiveResultSets=true;");
	request.DBMS().RegisterDbConnection("eptpuiremote","Remote","http://localhost:2000/dbms-eptpui/.json");
	request.DBMS().RegisterDbConnection("ptp","SqlServer","Data Source=localhost;Initial Catalog=BCORING;User Id=PTP;Password=PTPPTP;MultipleActiveResultSets=true;");
	//request.ENV().Root="D:/mystuff/bcoring/ptp/";
	request.ENV().Root="D:/mystuff/bcoring/modules/";
	/*request.SCHEDULES().RegisterSchedule("exports");
	request.SCHEDULES().Get("exports").AddActionRequest("request1","/test.js");
	request.SCHEDULES().Get("exports").AddActionRequest("request2","/test.js");
	request.SCHEDULES().Get("exports").AddActionRequest("request3","/test.js");
	request.SCHEDULES().Get("exports").Seconds=20;
	request.SCHEDULES().Get("exports").Start();
	
	request.SCHEDULES().RegisterSchedule("exports2");
	request.SCHEDULES().Get("exports2").AddActionRequest("request1","/test.js");
	request.SCHEDULES().Get("exports2").AddActionRequest("request2","/test.js");
	request.SCHEDULES().Get("exports2").AddActionRequest("request3","/test.js");
	request.SCHEDULES().Get("exports2").Seconds=20;
	request.SCHEDULES().Get("exports2").Start();*/
	//request.Listen("ssl:certfile:certificate.pfx:3000");
	request.Listen("2000");
 @>