using System;
using System.Net.Sockets;
using System.Text;

// FIXME: хочется плакать, надеюсь я когда-нибудь переделаю сетевую игру
public static class Networking
{
    public static void SendCode(byte mes, NetworkStream stream)
    {
        byte[] sendBytes = new byte[1] { mes };
        stream.Write(sendBytes, 0, 1);
    }
    public static byte RecvCode(NetworkStream stream)
    {
        byte[] recvBytes = new byte[1];
        stream.Read(recvBytes, 0, 1);
        return recvBytes[0];
    }

    public static void SendString(string str, NetworkStream stream)
    {
        byte[] strBytes = Encoding.Default.GetBytes(str);
        SendInt(strBytes.Length, stream);
        stream.Write(strBytes, 0, strBytes.Length);
    }
    public static string RecvString(NetworkStream stream)
    {
        int strLen = RecvInt(stream);
        byte[] strBytes = new byte[strLen];
        stream.Read(strBytes, 0, strLen);
        return Encoding.Default.GetString(strBytes);
    }

    public static void SendGameInfo(GameInfo game, NetworkStream stream)
    {
        SendString(game.Name, stream);
        SendInt(game.TimeControl.MaxMinutes, stream);
        SendInt(game.TimeControl.AddedSeconds, stream);
        SendInt((int)game.Color, stream);
    }
    public static GameInfo RecvGameInfo(NetworkStream stream)
    {
        uint id = RecvUInt(stream);
        string name = RecvString(stream);
        int maxTime = RecvInt(stream);
        int addTime = RecvInt(stream);
        ColorChoice color = (ColorChoice)RecvInt(stream);
        return new GameInfo {ID = id, Name = name, TimeControl = new(maxTime, addTime), Color = color};
    }

    public static void SendMove(byte[] moveBytes, NetworkStream stream)
    {
        const byte MoveCode = 10;
        SendCode(MoveCode, stream);
        stream.Write(moveBytes, 0, 5);
    }
    public static byte[] RecvMove(NetworkStream stream)
    {
        byte[] moveBytes = new byte[5];
        stream.Read(moveBytes, 0, 5);
        return moveBytes;
    }

    public static void SendInt(int mes, NetworkStream stream)
    {
        stream.Write(BitConverter.GetBytes(mes), 0, 4);
    }
    public static int RecvInt(NetworkStream stream)
    {
        byte[] recvBytes = new byte[4];
        stream.Read(recvBytes, 0, 4);
        return BitConverter.ToInt32(recvBytes);
    }
    public static uint RecvUInt(NetworkStream stream)
    {
        byte[] recvBytes = new byte[4];
        stream.Read(recvBytes, 0, 4);
        return BitConverter.ToUInt32(recvBytes);
    }
}
