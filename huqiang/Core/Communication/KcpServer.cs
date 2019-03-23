using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace huqiang
{
    public class KcpServer:KcpListener
    {
        public static int SingleCount = 2048;
        public static KcpServer Instance;
        Queue<SocData> queue;
        bool running;
        bool auto;
        Int16 id;
        Thread server;
        Thread[] threads;
        KcpLink[] links;
        int maxLink;
        int tCount;
        public Int32 allLink;
        public KcpServer(  int port=0, int remote=0, int threadCount = 8):base(port,remote)
        {
            Instance = this;
            tCount = threadCount;
            allLink = threadCount * SingleCount;
            links = new KcpLink[threadCount*SingleCount];
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(Run);
                threads[i].Start(i);
            }
        }
        public void Send(byte[] dat, IPEndPoint ip)
        {
            soc.Send(dat, dat.Length, ip);
        }
        void SendAll(byte[][] dat)
        {
            lock (links)
            {
                for (int i = 0; i < maxLink; i++)
                {
                    var link = links[i];
                    if (link != null)
                        for (int j = 0; j < dat.Length; j++)
                            soc.Send(dat[j], dat[j].Length, link.endpPoint);
                }
            }
        }
        void SendAll(byte[] dat)
        {
            lock (links)
            {
                for (int i = 0; i < maxLink; i++)
                {
                    var link = links[i];
                    if (link != null)
                        soc.Send(dat, dat.Length, link.endpPoint);
                }
            }
        }
        void Run(object index)
        {
            int os = (int)index;
            while (true)
            {
                var now = DateTime.Now;
                int a = now.Millisecond;
                int s = os;
                for (int i = 0; i < SingleCount; i++)
                {
                    var c = links[s];
                    if (c != null)
                    {
                        c.Recive(now.Ticks);
                    }
                    s += tCount;
                }
                int t = DateTime.Now.Millisecond;
                t -= a;
                if (t < 0)
                    t += 1000;
                t = 10 - t;
                if (t < 0)
                    t = 10;
                else if (t > 10)
                    t = 10;
                Thread.Sleep(t);
            }
        }
        public void Close()
        {
            soc.Close();
            running = false;
        }
        
        //设置用户的udp对象用于发送消息
        public KcpLink CreateNewLink(IPEndPoint ep)
        {
            var ip = ep.Address.GetAddressBytes();
            int id = 0;
            unsafe
            {
                fixed (byte* bp = &ip[0])
                    id = *(Int32*)bp;
            }
            int min = maxLink;
            for (int i = maxLink; i>=0; i--)
            {
                var lin = links[i];
                if (lin != null)
                {
                    if (id == lin.ip)
                    {
                        if (ep.Port == lin.port)
                        {
                            links[i].time = DateTime.Now.Ticks;
                            return links[i];
                        }
                    }
                }
                else min = i;
           
            }
            KcpLink link = new KcpLink(this);
            link.ip = id;
            link.port = ep.Port;
            link.endpPoint = ep;
            link.envelope = new KcpEnvelope();
            link.time = DateTime.Now.Ticks;
            links[min]=link;
            return link;
        }
        public override void Dispatch(byte[] dat, IPEndPoint endPoint)
        {
            var link = CreateNewLink(endPoint);
            lock (link.metaData)
                link.metaData.Enqueue(dat);
        }
    }
}
