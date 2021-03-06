﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace huqiang.UIModel
{
    public class ScrollY : ScrollContent
    {
        static void CenterScroll(ScrollY scroll)
        {
            var eve = scroll.eventCall;
            var tar = scroll.eventCall.ScrollDistanceY;
            float v = scroll.Point + tar;
            float sy = scroll.ItemSize.y;
            float oy = v % sy;
            tar -= oy;
            if (oy > sy * 0.5f)
                tar += sy;
            scroll.eventCall.ScrollDistanceY = tar;
            v = scroll.Point + tar + scroll.ScrollView.sizeDelta.y * 0.5f;
            int i = (int)(v / sy);
            int c = scroll.DataLength;
            i %= c;
            if (i < 0)
                i += c - 1;
            scroll.PreDockindex = i;
        }
        public EventCallBack eventCall;//scrollY自己的按钮
        protected float height;
        int Column = 1;
        float m_point;
        /// <summary>
        /// 滚动的当前位置，从0开始
        /// </summary>
        public float Point { get { return m_point; } set { Refresh(0,value - m_point); } }
        float m_pos = 0;
        /// <summary>
        /// 0-1之间
        /// </summary>
        public float Pos
        {
            get {
                float a = m_point / (ActualSize.y - Size.y);
                if (a < 0)
                    a = 0;
                else if (a > 1)
                    a = 1;
                return a;
            }
            set
            {
                if (value < 0 | value > 1)
                    return;
                m_point = value * (ActualSize.y - Size.y);
                Order();
            }
        }
        //public float OffsetStart;
        //public float OffsetEnd;
        public bool ItemDockCenter;
        public int PreDockindex { get; private set; }
        public Vector2 ContentSize { get; private set; }
        public ScrollY()
        {
        }
        public ScrollY(RectTransform rect)
        {
            Initial(rect, null);
        }
        public override void Initial(RectTransform rect, ModelElement model)
        {
            ScrollView = rect;
            eventCall = EventCallBack.RegEventCallBack<EventCallBack>(rect);
            eventCall.Drag = Draging;
            eventCall.DragEnd = (o, e, s) => {
                Scrolling(o, s);
                if (ItemDockCenter)
                    CenterScroll(this);
                if (ScrollStart != null)
                    ScrollStart(this);
                if (eventCall.VelocityY== 0)
                    OnScrollEnd(o);
            };
            eventCall.Scrolling = Scrolling;
            eventCall.PointerUp = (o, e) => { };
            eventCall.ScrollEndY = OnScrollEnd;
            eventCall.ForceEvent = true;
            eventCall.AutoColor = false;
            Size = ScrollView.sizeDelta;
            ScrollView.anchorMin = ScrollView.anchorMax = ScrollView.pivot = Center;
            eventCall.CutRect = true;
            if (model != null)
            {
                ItemMod = model.FindChild("Item");
                if (ItemMod != null)
                    ItemSize = ItemMod.transAttribute.sizeDelta;
            }
        }
        public Action<ScrollY, Vector2> Scroll;
        public Action<ScrollY> ScrollStart;
        public Action<ScrollY> ScrollEnd;
        public Action<ScrollY> ScrollToTop;
        public Action<ScrollY> ScrollToDown;
        void Draging(EventCallBack back, UserAction action, Vector2 v)
        {
            back.DecayRateY = 0.998f;
            Scrolling(back, v);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="back"></param>
        /// <param name="v">移动的实际像素位移</param>
        void Scrolling(EventCallBack back, Vector2 v)
        {
            if (ScrollView == null)
                return;
            v.y /= eventCall.Target.localScale.y;
            float y = Limit(back, v.y);
            Order();
            if (y != 0)
            {
                if (Scroll != null)
                    Scroll(this, v);
            }
            else
            {
                if (ScrollEnd != null)
                    ScrollEnd(this);
            }
        }
        void OnScrollEnd(EventCallBack back)
        {
            if (scrollType == ScrollType.BounceBack)
            {
                if (m_point < 0)
                {
                    back.DecayRateY = 0.988f;
                    float d = 0.25f - m_point;
                    back.ScrollDistanceY = d * eventCall.Target.localScale.y;
                }
                else if (m_point + Size.y > ActualSize.y)
                {
                    back.DecayRateY = 0.988f;
                    float d = ActualSize.y - m_point - Size.y - 0.25f;
                    back.ScrollDistanceY = d * eventCall.Target.localScale.y;
                }
                else
                {
                    if (ScrollEnd != null)
                        ScrollEnd(this);
                }
            }
            else if (ScrollEnd != null)
                ScrollEnd(this);
        }
        public void Calcul()
        {
            float w = Size.x - ItemOffset.x;
            w /= ItemSize.x;
            Column = (int)w;
            if (Column < 1)
                Column = 1;
            int c = DataLength;
            int a = c % Column;
            c /= Column;
            if (a > 0)
                c++;
            height = c * ItemSize.y;
            //height += OffsetStart + OffsetEnd;
            if (height < Size.y)
                height = Size.y;
            ActualSize = new Vector2(Size.x, height);
        }
        public override void Refresh(float x = 0, float y = 0)
        {
            Size = ScrollView.sizeDelta;
            ActualSize = Vector2.zero;
            if (DataLength== 0)
            {
                for (int i = 0; i < Items.Count; i++)
                    Items[i].target.SetActive(false);
                return;
            }
            if (ItemMod == null)
            {
                return;
            }
            if (ItemSize.y == 0)
            {
                return;
            }
            Calcul();
            Order(true);
        }
        /// <summary>
        /// 指定下标处的位置重排
        /// </summary>
        /// <param name="_index"></param>
        public void ShowByIndex(int _index)
        {
            Size = ScrollView.sizeDelta;
            ActualSize = Vector2.zero;
            if (DataLength==0)
            {
                for (int i = 0; i < Items.Count; i++)
                    Items[i].target.SetActive(false);
                return;
            }
            if (ItemMod == null)
            {
                return;
            }
            if (ItemSize.y == 0)
            {
                return;
            }
            float y = _index * ItemSize.y;
            m_point = y;
            Calcul();
            Order(true);
        }
        void Order(bool force=false)
        {
            int len = DataLength;
            float ly = ItemSize.y;
            int sr = (int)(m_point /ly);//起始索引
            int er = (int)((m_point + Size.y) / ly)+1;
            sr *= Column;
            er *= Column;//结束索引
            int e = er - sr;//总计显示数据
            if (e > len)
                e = len;
            if(scrollType==ScrollType.Loop)
            {
                if (er >= len)
                {
                    er -= len;
                    RecycleInside(er, sr);
                }
                else
                {
                    RecycleOutside(sr, er);
                }
            }
            else
            {
                if (sr < 0)
                    sr = 0;
                if (er >= len)
                    er = len;
                e = er - sr;
                RecycleOutside(sr, er);
            }
       
            PushItems();//将未被回收的数据压入缓冲区
            int index = sr;
            float oy = 0;
            for (int i=0;i<e;i++)
            {
                UpdateItem(index,oy,force);
                index++;
                if (index >= len)
                {
                    index = 0;
                    oy = ActualSize.y;
                }
            }
        }
        void UpdateItem(int index,float oy,bool force)
        {
            float ly = ItemSize.y;
            int row = index / Column;
            float dy = ly * row + oy;
            dy -= m_point;
            float ss = 0.5f * Size.y - 0.5f * ly;
            dy = ss - dy;
            float ox = (index%Column) * ItemSize.x + ItemSize.x * 0.5f + ItemOffset.x - Size.x * 0.5f;
            var a = PopItem(index);
            a.target.transform.localPosition = new Vector3(ox, dy, 0);
            if(a.index<0 | force)
            {
                var dat = GetData(index);
                a.datacontext = dat;
                a.index = index;
                if (ItemUpdate != null)
                {
                    if (a.obj == null)
                        ItemUpdate(a.target, dat, index);
                    else ItemUpdate(a.obj, dat, index);
                }
            }
        }
        public void SetSize(Vector2 size)
        {
            Size = size;
            ScrollView.sizeDelta = size;
            Refresh();
        }
        protected float Limit(EventCallBack callBack, float y)
        {
            var size = Size;
            switch (scrollType)
            {
                case ScrollType.None:
                    if (y == 0)
                        return 0;
                    float vy = m_point + y;
                    if (vy < 0)
                    {
                        m_point = 0;
                        eventCall.VelocityY = 0;
                        if (ScrollToTop != null)
                            ScrollToTop(this);
                        return 0;
                    }
                    else if (vy + size.y > ActualSize.y)
                    {
                        m_point = ActualSize.y - size.y;
                        eventCall.VelocityY = 0;
                        if (ScrollToDown != null)
                            ScrollToDown(this);
                        return 0;
                    }
                    m_point += y;
                    break;
                case ScrollType.Loop:
                    if (y == 0)
                        return 0;
                    m_point += y;
                    float ay = ActualSize.y;
                    if (m_point < 0)
                        m_point += ay;
                    else if (m_point > ay)
                        m_point %= ay;
                    break;
                case ScrollType.BounceBack:
                    m_point += y;
                    if (!callBack.Pressed)
                    {
                        if (m_point < 0)
                        {
                            if (y < 0)
                            {
                                if (eventCall.DecayRateY >= 0.99f)
                                {
                                    eventCall.DecayRateY = 0.9f;
                                    eventCall.VelocityY = eventCall.VelocityY;
                                }
                            }
                        }
                        else if (m_point + size.y > ActualSize.y)
                        {
                            if (y > 0)
                            {
                                if (eventCall.DecayRateY >= 0.99f)
                                {
                                    eventCall.DecayRateY = 0.9f;
                                    eventCall.VelocityY = eventCall.VelocityY;
                                }
                            }
                        }
                    }
                    break;
            }
            return y;
        }
    }
}
