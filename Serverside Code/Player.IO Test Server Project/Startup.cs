using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerGameCode.Helper;
using ServerGameCode.Services;

namespace DevelopmentTestServer {
	public static class Startup {
		[STAThread]
		static void Main() {
            // (Uncomment line to start server and make it simulate the user 'bob' connecting for 30 seconds)
            // (this is an easy way to debug serverside code)
            //
             //PlayerIO.DevelopmentServer.Server.StartWithDebugging("asynctbs-mgdqsxbhjemusoutd6elza", "public", new Dictionary<string, string>() { { "userId", "bob" } }, null, "Casual", null, null, 300000);


            // Start the server and wait for incomming connection
            PlayerIO.DevelopmentServer.Server.StartWithDebugging();
        }
	}
}