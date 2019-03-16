using System;
using UnityEngine;
using UnityEngine.UI;

namespace huqiang
{
    public class ImageAnimat : AnimatInterface
    {
        public Image image { get; private set; }
        public ImageAnimat(Image img)
        {
            image = img;
            AnimationManage.Manage.AddAnimat(this);
        }
        Sprite[] sprites;
        public void Play(Sprite[] gif)
        {
            PlayTime = 0;
            if (gif != null)
            {
                sprites = gif;
                image.sprite = sprites[0];
                image.SetNativeSize();
                _playing = true;
            }
        }
        Sprite[][] spritesBuff;
        int curIndex=0;
        public void SetSprites(Sprite[][] sprites)
        {
            spritesBuff = sprites;
            curIndex = -1;
        }
        public void Play(int index,bool cover=true)
        {
            if (spritesBuff == null)
                return;
            if (index == curIndex)
                if (!cover)
                    return;
            if(index>-1&index<spritesBuff.Length)
            {
                curIndex = index;
                Play(spritesBuff[index]);
            }
        }
        public void Pause()
        {
            _playing = false;
        }
        public void Stop()
        {
            _playing = false;
            if (image != null)
            {
                if (sprites != null)
                {
                    image.sprite = sprites[0];
                    image.SetNativeSize();
                }
            }
        }
        public Action<ImageAnimat> PlayOver;
        public Action<ImageAnimat> Playing;
        public bool Loop;
        bool _playing;
        public bool IsPlaying { get { return _playing; } }
        public int PlayIndex { get { return curIndex; } }
        public float PlayTime = 0;
        public float Interval = 100;
        public bool autoHide;
        public void Update(float time)
        {
            if (_playing)
            {
                PlayTime += time;
                if (sprites != null)
                {
                    int c = (int)(PlayTime / Interval);
                    if (c >= sprites.Length)
                    {
                        if (Loop)
                        {
                            PlayTime = 0;
                            image.sprite = sprites[0];
                            image.SetNativeSize();
                        }
                        else
                        {
                            _playing = false;
                            if (PlayOver != null)
                                PlayOver(this);
                        }
                    }
                    else
                    {
                        image.sprite = sprites[c];
                        image.SetNativeSize();
                    }
                }
                if (Playing != null)
                    Playing(this);
            }
        }
        public void Dispose()
        {
            if (autoHide)
                image.gameObject.SetActive(false);
            AnimationManage.Manage.ReleaseAnimat(this);
        }
    }
}
