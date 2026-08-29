using System;
using System.Net.Sockets;
using System.Text;

// FIXME: хочется плакать, надеюсь я когда-нибудь переделаю сетевую игру
public static class Networking 
{
    static public void SendCode(byte mes, NetworkStream stream)
    {
        byte[] send_bytes = new byte[1] { mes };
        stream.Write(send_bytes, 0, 1);
    }
    static public byte RecvCode(NetworkStream stream)
    {
        byte[] recv_bytes = new byte[1];
        stream.Read(recv_bytes, 0, 1);
        return recv_bytes[0];
    }


    static public void SendString(string str, NetworkStream stream)
    {
        byte[] str_bytes = Encoding.Default.GetBytes(str);
        SendInt(str_bytes.Length, stream);
        stream.Write(str_bytes, 0, str_bytes.Length);
    }
    static public string RecvString(NetworkStream stream)
    {
        int str_len = RecvInt(stream); 
        byte[] str_bytes = new byte[str_len];
        stream.Read(str_bytes, 0, str_len);
        return Encoding.Default.GetString(str_bytes);
    }


    static public void SendGameInfo(GameInfo game, NetworkStream stream)
    {
        SendString(game.Name, stream);
        SendInt(game.TimeContol.MaxMinutes, stream);
        SendInt(game.TimeContol.AddedSeconds, stream);
        SendInt((int)game.Color, stream);
    }
    static public GameInfo RecvGameInfo(NetworkStream stream)
    {
        uint id = RecvUInt(stream);
        string name = RecvString(stream);
        int max_time = RecvInt(stream);
        int add_time = RecvInt(stream);
        ColorChoice color = (ColorChoice)RecvInt(stream);
        return new GameInfo {ID = id, Name = name, TimeContol = new(max_time, add_time), Color = color};
    }


    static public void SendMove(byte[] move_bytes, NetworkStream stream)
    {
        const byte move_code = 10;
        SendCode(move_code, stream);
        stream.Write(move_bytes, 0, 5);
    }
    static public byte[] RecvMove(NetworkStream stream)
    {
        byte[] move_bytes = new byte[5];
        stream.Read(move_bytes, 0, 5);
        return move_bytes;
    }


    static public void SendInt(int mes, NetworkStream stream)
    {
        stream.Write(BitConverter.GetBytes(mes), 0, 4);
    }
    static public int RecvInt(NetworkStream stream)
    {
        byte[] recv_bytes = new byte[4];
        stream.Read(recv_bytes, 0, 4);
        return BitConverter.ToInt32(recv_bytes);
    }
    static public uint RecvUInt(NetworkStream stream)
    {
        byte[] recv_bytes = new byte[4];
        stream.Read(recv_bytes, 0, 4);
        return BitConverter.ToUInt32(recv_bytes);
    }
}
