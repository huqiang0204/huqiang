using UnityEngine;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace huqiang
{
    public enum EnvelopeType : byte
    {
        None = 0,
        AesData = 1,
        Picture = 2,
        Voice = 3,
    }
    public class Envelope
    {
        byte[] buffer;
        public struct EnvelopeItem
        {
            public EnvelopeHead head;
            public int part;
            public UInt32 rcvLen;
            public byte[] buff;
        }
        EnvelopeItem[] pool = new EnvelopeItem[128];
        int remain;
        public Envelope ()
        {
            buffer = new byte[262144];
        }
        public void Clear()
        {
            for (int i = 0; i < 128; i++)
            {
                pool[i].head.Tag = 0;
            }
            remain = 0;
        }
        public struct EnvelopeHead
        {
            public UInt32 Tag;
            public UInt32 Lenth;
            public UInt32 CurPart;
            public UInt32 AllPart;
            public UInt16 PartLen;
        }
        public static unsafe EnvelopeHead ReadHead(byte[] buff, int index)
        {
            fixed (byte* b = &buff[index])
            {
                return *(EnvelopeHead*)b;
            }
        }
        public void Dispatch(byte[] buff, int lenth, Action<byte[], UInt32> callback)
        {
            int s = remain;
            for (int i = 0; i <lenth;i++)
            {
                buffer[s] = buff[i];
                s++;
            }
            lenth += remain;
            int index = 0;
            for (int i = 0; i < 1024; i++)
            {
                EnvelopeHead head = ReadHead(buffer, index);
                if(head.PartLen>1442)
                {
                    remain = 0;
                    break;
                }
                if (index + 18 + head.PartLen > lenth)
                {
                    remain = lenth - index;
                    for (int j = 0; j < lenth; j++)
                    {
                        buffer[j] = buffer[index];
                        index++;
                    }
                    return;
                }
                index += 18;
                DispatchPart(buffer,ref head, index, callback);
                index += head.PartLen;
                if (index  >= lenth)
                {
                    remain = 0;
                    break;
                }
            }
        }
        void DispatchPart(byte[] buff, ref EnvelopeHead head, int dataStart, Action<byte[], UInt32> callback)
        {
            if (head.Tag == 0)
            {
                return;
            }
            if (head.AllPart > 1)
            {
                int s = -1;
                for (int i = 0; i < 128; i++)
                {
                    if (s < 0)
                    {
                        if (pool[i].head.Tag == 0)
                            s = i;
                    }
                    if (head.Tag == pool[i].head.Tag)
                    {
                            CopyToBuff(pool[i].buff, buff, dataStart,head);
                            pool[i].part++;
                            pool[i].rcvLen += head.PartLen;
                            if (pool[i].rcvLen >= head.Lenth)
                            {
                                if (callback != null)
                                    callback(pool[i].buff, head.Tag);
                                pool[i].head.Tag = 0;
                            }
                            return ;
                    }
                }
                pool[s].head = head;
                pool[s].part = 1;
                pool[s].rcvLen = head.PartLen;
                pool[s].buff = new byte[head.Lenth];
                CopyToBuff(pool[s].buff, buff, dataStart, head);
            }
            else
            {
                if (head.PartLen > 2)
                {
                    byte[] tmp = new byte[head.PartLen];
                    int s = dataStart;
                    for (int i = 0; i < head.PartLen; i++)
                    {
                        tmp[i] = buff[s];
                        s++;
                    }
                    if (callback != null)
                        callback(tmp, head.Tag);
                }
            }
        }
        public static void CopyToBuff(byte[] buff, byte[] src, int start, EnvelopeHead head)
        {
            UInt32 index = (head.CurPart - 1) * 1442;
            int len = head.PartLen;
            for (int i = 0; i < len; i++)
            {
                buff[index] = src[start];
                index++;
                start++;
            }
        }
        /// <summary>
        /// 标头4字节，总长度4字节, 当前分卷4字节，总分卷4字节，当前分卷长度2字节，总计18字节
        /// </summary>
        public static int Tag = 256;//
        //每个数据包大小1460字节
        public static void Send(Socket soc, byte[] buff, EnvelopeType type)
        {
            if (buff == null)
                return;
            if (buff.Length < 2)
                buff = new byte[2];
            Tag++;
            if (Tag >= 0xffffff)
                Tag = 256;
            int len = buff.Length;
            int part = len / 1442;
            int r = len % 1442;
            int allPart = part;
            if (r > 0)
                allPart++;
            for (int i = 0; i < part; i++)
                SendPart(soc, buff, i, 1442, type,len,allPart);
            if(r>0)
               SendPart(soc, buff, part, r, type,len,allPart);
        }
        static void SendPart(Socket soc, byte[] buff, int part, int partLen, EnvelopeType type,int len,int allPart)
        {
            byte[] tmp = new byte[partLen + 18];
            SetInt32(tmp, 0, Tag);
            tmp[3] = (byte)type;
            SetInt32(tmp, 4, len);
            SetInt32(tmp, 8, part + 1);
            SetInt32(tmp, 12, allPart);
            SetInt16(tmp, 16, (Int16)partLen);
            int index = part * 1442;
            int start = 18;
            for (int j = 0; j < partLen; j++)
            {
                tmp[start] = buff[index];
                start++;
                index++;
            }
            soc.Send(tmp);
        }
        public static void WriteHead(byte[] buf, int part, int partLen, EnvelopeType type, int len, int allPart)
        {
            SetInt32(buf, 0, Tag);
            buf[3] = (byte)type;
            SetInt32(buf, 4, len);
            SetInt32(buf, 8, part + 1);
            SetInt32(buf, 12, allPart);
            SetInt16(buf, 16, (Int16)partLen);
        }
        public static unsafe void SetInt16(byte[] buff, int index, Int16 value)
        {
            fixed (byte* b = &buff[index])
            {
                *(Int16*)b = value;
            }
        }
        public static unsafe void SetInt32(byte[] buff, int index, Int32 value)
        {
            fixed (byte* b = &buff[index])
            {
                *(Int32*)b = value;
            }
        }
        public static unsafe Int32 GetInt32(byte[] buff, int index)
        {
            fixed (byte* b = &buff[index])
            {
               return *(Int32*)b;
            }
        }
    }
    public class Socket2
    {
        const int bufferSize = 262144;
        Envelope envelope;
        Thread thread;
        private Socket client = null;
        public bool isConnection { get { if (client == null) return false; return client.Connected; } }
        public DataReaderManage drm;
        IPEndPoint iep;
        public Socket2()
        {
            buffer = new byte[bufferSize];
            envelope = new Envelope();
        }
        byte[] buffer;
        void Timer()
        {
            while (true)
            {
                if (close)
                {
                    if (client != null)
                    {
                        if (client.Connected)
                            client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    }
                    break;
                }
                if (client != null)
                {
                    if (client.Connected)
                    {
                        Receive();
                    }
                    else if (!close)
                    {
                        try
                        {
                            client.Close();
                            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            client.ReceiveTimeout = 5000;
                            client.Connect(iep);
                            if (client.Connected)
                            {
                                envelope.Clear();
                                if (Connected != null)
                                    Connected();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ConnectFaild != null)
                                ConnectFaild(ex.StackTrace);
                        }
                    }
                }
            }
            thread = null;
            client = null;
        }
        void Receive()
        {
            try
            {
                int len = client.Receive(buffer);
                if(Packaging)
                {
                    envelope.Dispatch(buffer, len, (o, e) => {
                        if (auto)
                        {
                            if (a_Dispatch != null)
                                a_Dispatch(o, e);
                        }
                        else drm.PushData(o, e);
                    });
                }
                else
                {
                    byte[] tmp = new byte[len];
                    for (int i = 0; i < len; i++)
                        tmp[i] = buffer[i];
                    if(auto)
                    {
                        if (a_Dispatch != null)
                            a_Dispatch(tmp,0);
                    }else
                    drm.PushData(tmp,0);
                }
            }
            catch (Exception ex)
            {
                if (Packaging)
                    envelope.Clear();
                if (ConnectFaild != null)
                    ConnectFaild(ex.StackTrace);
            }
        }

        unsafe int GetLenth(byte[] buff, int index)
        {
            fixed (byte* b = &buff[index])
            {
                return *(int*)b;
            }
        }
        bool auto = true;
        Action<byte[], UInt32> a_Dispatch;
        /// <summary>
        /// 设置消息派发函数
        /// </summary>
        /// <param name="DispatchMessage"></param>
        /// <param name="autodispatch">true由socket本身的线程进行派发，false为手动派发，请使用update函数</param>
        /// <param name="buff_size">手动派发时，缓存消息的队列大小,默认最小为32</param>
        public void SetDispatchMethod(Action<byte[], UInt32> DispatchMessage, bool autodispatch = true, int buff_size = 32)
        {
            a_Dispatch = DispatchMessage;
            auto = autodispatch;
            if (!auto)
            {
                if (buff_size < 16)
                    buff_size = 16;
                drm = new DataReaderManage(buff_size);
            }
        }
        public void ConnectServer(IPAddress ip, int _port)
        {
            if (thread != null)
            {
                return;
            }
            close = false;
            iep = new IPEndPoint(ip, _port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.ReceiveTimeout = 5000;
            if (thread == null)
            {
                thread = new Thread(Timer);
                thread.Start();
            }
        }

        /// <summary>
        /// 由其它线程进行消息派发，当异步线程终止时，开启线程
        /// </summary>
        public void Dispatch()
        {
            if(drm!=null)
            for (int i = 0; i < drm.max; i++)
            {
                var dat = drm.GetNextMetaData();
                if (dat.data == null)
                    break;
                if (a_Dispatch != null)
                    a_Dispatch(dat.data, dat.Tag);
            }
        }
        /// <summary>
        /// 向服务器发送消息
        /// </summary>
        /// <param name="data"></param>
        public bool SendMessage(byte[] data, EnvelopeType type)
        {
            if (client == null)
                return false;
            if (client.Connected)
            {
                try
                {
                    if (Packaging)
                        Envelope.Send(client, data, type);
                    else client.Send(data);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.StackTrace);
                    return false;
                }
                return true;
            }
            return false;
        }
        bool close;
        public void Close()
        {
            close = true;
            if (client != null)
                if (client.Connected)
                {
                    client.ReceiveTimeout = 10;
                    client.Shutdown(SocketShutdown.Both);
                }
        }
        public Action Connected;
        public Action<string> ConnectFaild;
        public bool Packaging;
    }
    public class DataReaderManage
    {
        public struct Data
        {
            public UInt32 Tag;
            public byte[] data;
        }
        Data[] ndr;
        public int max { get; private set; }
        int start = 0;
        int end = 0;
        public DataReaderManage(int count)
        {
            max = count;
            ndr = new Data[count];
        }
        public void PushData(byte[] data,UInt32 Tag)
        {
            ndr[start].Tag =Tag;
            ndr[start].data=data;
            start++;
            if (start >= max)
                start = 0;
        }
        public Data GetNextMetaData()
        {

            if (end != start)
            {
                var net = ndr[end];
                end++;
                if (end >= max)
                    end = 0;
                return net;
            }
            return new Data();
        }
    }
}