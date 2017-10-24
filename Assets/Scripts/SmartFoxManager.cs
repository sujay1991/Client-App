using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Logging;
using Sfs2X.Util;
using System;
using UnityEngine;

public class SmartFoxManager : MonoBehaviour
{
    public bool debugMode = false;

    private string defaultHost = "127.0.0.1";
    private int defaultTcpPort = 9933;
    private int defaultWsPort = 8080;

    private int httpPort = 8080;                // HTTP port (for BlueBox connection)
    private int httpsPort = 8443;				// HTTPS port (for protocol encryption initialization in non-websocket connections)

    private SmartFox sfs;
    private SFSRoom _room;

    void Start()
    {

        if (sfs == null || !sfs.IsConnected)
        {
            Debug.Log("Client Connecting...");
            sfs = new SmartFox();
            sfs.ThreadSafeMode = true;

            sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            sfs.AddEventListener(SFSEvent.CRYPTO_INIT, OnCryptoInit);
            sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
            sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            sfs.AddEventListener(SFSEvent.PING_PONG, OnPingPong);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnJoinRoom);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnJoinRoomError);
            sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
            sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
            sfs.AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, OnRoomVariableUpdated);

            sfs.AddLogListener(LogLevel.DEBUG, OnDebugMessage);
            sfs.AddLogListener(LogLevel.INFO, OnInfoMessage);
            sfs.AddLogListener(LogLevel.WARN, OnWarnMessage);
            sfs.AddLogListener(LogLevel.ERROR, OnErrorMessage);

            // Set connection parameters
            ConfigData cfg = new ConfigData();
            cfg.Host = defaultHost;
            cfg.Port = Convert.ToInt32(defaultTcpPort);
            cfg.HttpPort = httpPort;
            cfg.HttpsPort = httpsPort;
            cfg.Zone = "BasicExamples";
            cfg.Debug = debugMode;

            // Connect to SFS2X
            sfs.Connect(cfg);
        }

    }

    void Update()
    {
        // As Unity is not thread safe, we process the queued up callbacks on every frame
        if (sfs != null)
            sfs.ProcessEvents();
    }

    private void reset()
    {
        // Remove SFS2X listeners
        sfs.RemoveAllEventListeners();

        sfs.RemoveLogListener(LogLevel.DEBUG, OnDebugMessage);
        sfs.RemoveLogListener(LogLevel.INFO, OnInfoMessage);
        sfs.RemoveLogListener(LogLevel.WARN, OnWarnMessage);
        sfs.RemoveLogListener(LogLevel.ERROR, OnErrorMessage);

        sfs = null;
    }

    private void login()
    {
        // Login as guest

        Debug.Log("Starting to login as a student");

        sfs.Send(new Sfs2X.Requests.LoginRequest(""));
    }

    private void OnConnection(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            Debug.Log("Connection established successfully, SFS2X API version: " + sfs.Version + ", Connection mode is: " + sfs.ConnectionMode);
        }
        else
        {
            Debug.Log("Connection failed; is the server running at all?");

            // Remove SFS2X listeners and re-enable interface
            reset();
        }
    }

    private void OnConnectionLost(BaseEvent evt)
    {
        Debug.Log("Connection was lost; reason is: " + (string)evt.Params["reason"]);

        // Remove SFS2X listeners and re-enable interface
        reset();
    }

    private void OnCryptoInit(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            Debug.Log("Encryption initialized successfully");

            // Attempt login
            login();
        }
        else
        {
            Debug.Log("Encryption initialization failed: " + (string)evt.Params["errorMessage"]);
        }
    }

    private void OnLogin(BaseEvent evt)
    {
        User user = (User)evt.Params["user"];

        Debug.Log("Login successful");
        Debug.Log("Username is: " + user.Name);
        
        sfs.Send(new Sfs2X.Requests.JoinRoomRequest("The Lobby"));
    }

    private void OnLoginError(BaseEvent evt)
    {
        Debug.Log("Login failed: " + (string)evt.Params["errorMessage"]);
    }

    private void OnPingPong(BaseEvent evt)
    {
        Debug.Log("Measured lag is: " + (int)evt.Params["lagValue"] + "ms");
    }

    private void OnJoinRoom(BaseEvent evt)
    {
        var _room = (evt.Params["room"] as SFSRoom);
        Debug.Log("OnJoingRoom: [id = " + _room.Id + ", name = " + _room.Name + "]");

        /*var reqParams = new Sfs2X.Entities.Data.SFSObject();
        reqParams.PutInt("a", 25);
        reqParams.PutInt("b", 17);

        sfs.Send(new Sfs2X.Requests.ExtensionRequest("sum", reqParams, _room));*/
    }

    private void OnJoinRoomError(BaseEvent evt)
    {
        Debug.Log("OnJoinRoomFailed: " + (string)evt.Params["errorMessage"]);
    }

    private void OnExtensionResponse(BaseEvent evt)
    {
        var responseParams = (evt.Params["params"] as Sfs2X.Entities.Data.SFSObject);
        if (responseParams == null)
        {
            Debug.Log("error: cannot get params from the message");
            return;
        }
        int result = responseParams.GetInt("res");

        Debug.Log("sum = " + result);
    }

    private void OnPublicMessage(BaseEvent evt)
    {
        Debug.Log("OnPublicMessage: ");
    }

    private void OnRoomVariableUpdated(BaseEvent evt)
    {
        Debug.Log("OnRoomVariableUpdated: ");
    }

    public void OnDebugMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        ShowLogMessage("DEBUG", message);
    }

    public void OnInfoMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        ShowLogMessage("INFO", message);
    }

    public void OnWarnMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        ShowLogMessage("WARN", message);
    }

    public void OnErrorMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        ShowLogMessage("ERROR", message);
    }

    private void ShowLogMessage(string level, string message)
    {
        message = "[SFS > " + level + "] " + message;
        Debug.Log(message);
    }
}
