﻿/*
 *  Log 过滤
 */

using System;
using System.Net;
using UnityEngine;
using DebugSocket;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

public static class StringExternsion
{
    public static byte[] Utf8Bytes(this string self)
    {
        return Encoding.UTF8.GetBytes(self);
    }
    public static byte[] DefaultBytes(this string self)
    {
        return Encoding.Default.GetBytes(self);
    }

    public static string RReplace(this string self, string pattern, string replacement)
    {
        return Regex.Replace(self, pattern, replacement);
    }
}

public static class AppLog
{
    public static string TAG = "[game]";

    static DebugSocket.Server mDebugServer = null;
    public static DebugSocket.Server DebugServer
    {
        get
        {
            if(mDebugServer == null)
            {
                // Create a listen server on localhost with port 80
                Server server = new Server(new IPEndPoint(IPAddress.Any, 7788));
                mDebugServer = server;
                server.OnClientConnected += (object sender, OnClientConnectedHandler e) =>
                {
                    //Console.WriteLine("Client with GUID: {0} Connected!", e.GetClient().GetGuid());
                    server.SendCacheMessage(e.GetClient());
                };

                server.OnClientDisconnected += (object sender, OnClientDisconnectedHandler e) =>
                {
                    Console.WriteLine("Client {0} Disconnected", e.GetClient().GetGuid());
                };

                server.OnMessageReceived += (object sender, OnMessageReceivedHandler e) =>
                {
                    //Console.WriteLine("Received Message: '{1}' from client: {0}", e.GetClient().GetGuid(), e.GetMessage());
                    server.BroadcastMessage(string.Format("{0}:\n\t{1}", e.GetClient().GetGuid(), e.GetMessage()).Utf8Bytes(), e.GetClient());
                };

                server.OnSendMessage += (object sender, OnSendMessageHandler e) =>
                {
                    //Console.WriteLine("Sent message: '{0}' to client {1}", e.GetMessage(), e.GetClient().GetGuid());
                };
            }
            return mDebugServer;
        }
    }

    //[Conditional("USE_LOG")]
    /// <summary>
    /// common log
    /// </summary>
    /// <param name="log"></param>
    public static void d(string log)
    {
        UnityEngine.Debug.Log("<color=yellow>" + TAG+ "</color> " + log);
        DebugServer.BroadcastMessage(log.ToString());
    }

    public static void d(string fmt, params object[] args)
    {
        d(string.Format(fmt, args));
    }
    public static void d(params object[] args)
    {
        var msg = "";
        foreach(var i in args)
        {
            msg += string.Format("{0};", i);
        }
        d(msg);
    }

    public static void w(string log)
    {
        UnityEngine.Debug.LogWarning(TAG + log);
        DebugServer.BroadcastMessage(log);
    }

    public static void w(string fmt, params object[] args)
    {
        w(string.Format(fmt, args));
    }

    private static void e(string log)
    {
        UnityEngine.Debug.LogErrorFormat("<color=red>{0}</color> {1}", TAG, log);
        DebugServer.BroadcastMessage("error: " + log);
    }

    public static void e(string fmt, params object[] args)
    {
        e(string.Format(fmt, args));
    }
    public static void e(params object[] args)
    {
        var msg = "";
        foreach(var i in args)
        {
            msg += string.Format("{0};", i);
        }
        e(msg);
    }

    public static void e(Exception ex)
    {
        e(ex.ToString());
    }

}
