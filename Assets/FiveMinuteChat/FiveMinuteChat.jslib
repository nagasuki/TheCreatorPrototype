mergeInto(LibraryManager.library, {
    ConnectExt: async function ( endpoint, port, listenMethod ) {
        window.signalRConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${UTF8ToString(endpoint)}:${port}/signalr`)
          .configureLogging(signalR.LogLevel.Information)
          .build();
      
        async function start() {
            try {
                await window.signalRConnection.start();
                console.log("FiveMinuteChat: SignalR connected successfully");
                myGameInstance.SendMessage("WebGLCallbackListener","On","Connected-true");
            } catch (err) {
                console.log(err);
                setTimeout(start, 5000);
            }
        };
        
        window.signalRConnection.onclose(async () => {
            await start();
        });
                
        console.log(`FiveMinuteChat: Listening to ${UTF8ToString(listenMethod)}`);
        window.signalRConnection.on(UTF8ToString(listenMethod), (payload) => {
            myGameInstance.SendMessage("WebGLCallbackListener","On", `GenericEncodedBinary-${payload}`);
        });
        
        console.log(`FiveMinuteChat: SignalR connecting to ${UTF8ToString(endpoint)}:${port}/signalr`);
        start();
    },
  
    SendExt: async function ( message ) {
        if(!window.signalRConnection){
            console.log("SignalR not connected.");
            return;
        }
        // console.log(`FiveMinuteChat: SignalR sending ${UTF8ToString(message)}`);
        window.signalRConnection.invoke("GenericEncodedBinary", UTF8ToString(message));
    },
       
     StopExt: async function ( message ) {
         if(!window.signalRConnection){
             console.log("SignalR not connected.");
             return;
         }
         window.signalRConnection.stop();
     },
});
