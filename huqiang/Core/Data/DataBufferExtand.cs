using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace huqiang
{
    public class AttributeKey : Attribute
    {
        public int Index;
        public DataType type = DataType.Int;
    }
    public class DataBufferExtand
    {
        struct FieldDetial
        {
            public FieldInfo info;
            public AttributeKey attribute;
        }
        static Type dbType = typeof(AttributeKey);
        static Type fakeType = typeof(FakeStruct);
        static Type fakeArrayType = typeof(FakeStructArray);
        public static T ReadToClass<T>(DataBuffer buffer) where T : class, new()
        {
            var obj = new T();
            var typ = typeof(T);
            if (buffer != null)
                if (buffer.fakeStruct != null)
                    ReadToClass(typ, obj, buffer.fakeStruct);
            return obj;
        }
        public static T ReadToClass<T>(FakeStruct fake) where T : class, new()
        {
            var obj = new T();
            var typ = typeof(T);
            if (fake != null)
                ReadToClass(typ, obj, fake);
            return obj;
        }
        static void ReadToClass(Type typ, object obj, FakeStruct fake)
        {
            var fields = typ.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                var atts = f.GetCustomAttribute(dbType, false);
                if (atts != null)
                {
                    var dba = atts as AttributeKey;
                    var v = ReadValue(dba.Index, dba.type, fake);
                    if (dba.type == DataType.FakeStruct)
                    {
                        if (f.FieldType == fakeType)
                        {
                            f.SetValue(obj, v);
                        }
                        else
                        {
                            var fs = v as FakeStruct;
                            if (fs != null)
                            {
                                object instance = Activator.CreateInstance(f.FieldType);
                                ReadToClass(f.FieldType, instance, fs);
                                f.SetValue(obj, instance);
                            }
                        }
                    }
                    else if (dba.type == DataType.FakeStructArray)
                    {
                        if (f.FieldType == fakeArrayType)
                        {
                            f.SetValue(obj, v);
                        }
                        else
                        {
                            var fs = v as FakeStructArray;
                            if (fs != null)
                            {
                                Type eleType = f.FieldType.GetElementType();
                                f.SetValue(obj, ReadStructArray(eleType, fs));
                            }
                        }
                    }
                    else if (dba.type == DataType.FakeStringArray)
                    {
                        var fs = v as FakeStringArray;
                        if (fs != null)
                        {
                            f.SetValue(obj, fs.Data);
                        }
                    }
                    else f.SetValue(obj, v);
                }
            }
        }
        static object ReadValue(int index, DataType type, FakeStruct fake)
        {
            switch (type)
            {
                case DataType.Int:
                    return fake[index];
                case DataType.Float:
                    return fake.GetFloat(index);
                case DataType.Long:
                    return fake.GetInt64(index);
                case DataType.Double:
                    return fake.GetDouble(index);
                default:
                    return fake.GetData(index);
            }
        }
        static object ReadStructArray(Type eleType, FakeStructArray fake)
        {
            var fields = eleType.GetFields();
            Array array = Array.CreateInstance(eleType, fake.Length);
            for (int j = 0; j < fake.Length; j++)
            {
                var ins = Activator.CreateInstance(eleType);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    var atts = f.GetCustomAttribute(dbType, false);
                    if (atts != null)
                    {
                        var dba = atts as AttributeKey;
                        var v = ReadValue(j, dba.Index, dba.type, fake);
                        if (dba.type == DataType.FakeStruct)
                        {
                            var fs = v as FakeStruct;
                            if (fs != null)
                            {
                                object instance = Activator.CreateInstance(f.FieldType);
                                ReadToClass(f.FieldType, instance, fs);
                                f.SetValue(ins, instance);
                            }
                        }
                        else if (dba.type == DataType.FakeStructArray)
                        {
                            var fs = v as FakeStructArray;
                            if (fs != null)
                            {
                                Type slt = f.FieldType.GetElementType();
                                f.SetValue(ins, ReadStructArray(slt, fs));
                            }
                        }
                        else if (dba.type == DataType.FakeStringArray)
                        {
                            var fs = v as FakeStringArray;
                            if (fs != null)
                            {
                                f.SetValue(ins, fs.Data);
                            }
                        }
                        else f.SetValue(ins, v);
                    }
                }
                array.SetValue(ins, j);
            }
            return array;
        }
        static object ReadValue(int index, int offset, DataType type, FakeStructArray fake)
        {
            switch (type)
            {
                case DataType.Int:
                    return fake[index, offset];
                case DataType.Float:
                    return fake.GetFloat(index, offset);
                case DataType.Long:
                    return fake.GetInt64(index, offset);
                case DataType.Double:
                    return fake.GetDouble(index, offset);
                default:
                    return fake.GetData(index, offset);
            }
        }

        public static DataBuffer WriteToDataBuffer(object obj)
        {
            DataBuffer db = new DataBuffer();
            db.fakeStruct = WriteFakeStruct(obj, db);
            return db;
        }
        public static FakeStruct WriteFakeStruct(object obj, DataBuffer buffer)
        {
            if (obj == null)
                return null;
            var typ = obj.GetType();
            var feilds = typ.GetFields();
            if (feilds != null)
            {
                var len = feilds.Length;
                FieldDetial[] tmp = new FieldDetial[len];
                int c = 0;
                for (int i = 0; i < feilds.Length; i++)
                {
                    var f = feilds[i];
                    var atts = f.GetCustomAttribute(dbType, false);
                    if (atts != null)
                    {
                        tmp[c].info = f;
                        tmp[c].attribute = atts as AttributeKey;
                        c++;
                    }
                }
                if (c > 0)
                {
                    FakeStruct fs = new FakeStruct(buffer, c);
                    WriteFromCalss(obj, tmp, c, fs);
                    return fs;
                }
            }
            return null;
        }
        static void WriteFromCalss(object obj, FieldDetial[] tmp, int len, FakeStruct fake)
        {
            for (int i = 0; i < len; i++)
            {
                var att = tmp[i];
                switch (att.attribute.type)
                {
                    case DataType.Int:
                        fake[att.attribute.Index] = (Int32)att.info.GetValue(obj);
                        break;
                    case DataType.Float:
                        fake.SetFloat(att.attribute.Index, (float)att.info.GetValue(obj));
                        break;
                    case DataType.FakeStruct:
                        fake.SetData(att.attribute.Index, WriteFakeStruct(att.info.GetValue(obj), fake.buffer));
                        break;
                    case DataType.FakeStructArray:
                        fake.SetData(att.attribute.Index, WriteFakeStructArray(att.info.GetValue(obj), att.info.FieldType.GetElementType(), fake.buffer));
                        break;
                    case DataType.FakeStringArray:
                        string[] ss = att.info.GetValue(obj) as string[];
                        if (ss != null)
                            fake.SetData(att.attribute.Index, new FakeStringArray(ss));
                        break;
                    case DataType.Long:
                        fake.SetInt64(att.attribute.Index, (long)att.info.GetValue(obj));
                        break;
                    case DataType.Double:
                        fake.SetDouble(att.attribute.Index, (double)att.info.GetValue(obj));
                        break;
                    default:
                        fake.SetData(att.attribute.Index, att.info.GetValue(obj));
                        break;
                }
            }
        }
        static FakeStructArray WriteFakeStructArray(object obj, Type ele, DataBuffer buffer)
        {
            if (obj == null)
                return null;
            Array arry = obj as Array;
            if (arry == null)
                return null;
            var feilds = ele.GetFields();
            if (feilds != null)
            {
                var len = feilds.Length;
                FieldDetial[] tmp = new FieldDetial[len];
                int c = 0;
                for (int i = 0; i < feilds.Length; i++)
                {
                    var f = feilds[i];
                    var atts = f.GetCustomAttribute(dbType, false);
                    if (atts != null)
                    {
                        tmp[c].info = f;
                        tmp[c].attribute = atts as AttributeKey;
                        c++;
                    }
                }
                if (c > 0)
                {
                    FakeStructArray fs = new FakeStructArray(buffer, c, arry.Length);
                    WriteFakeStructArray(arry, ele, tmp, c, fs);
                    return fs;
                }
            }
            return null;
        }
        static void WriteFakeStructArray(Array array, Type ele, FieldDetial[] tmp, int len, FakeStructArray fake)
        {
            for (int j = 0; j < fake.Length; j++)
            {
                var obj = array.GetValue(j);
                if (obj != null)
                    for (int i = 0; i < len; i++)
                    {
                        var att = tmp[i];
                        switch (att.attribute.type)
                        {
                            case DataType.Int:
                                fake[j, att.attribute.Index] = (Int32)att.info.GetValue(obj);
                                break;
                            case DataType.Float:
                                fake.SetFloat(j, att.attribute.Index, (float)att.info.GetValue(obj));
                                break;
                            case DataType.FakeStruct:
                                fake.SetData(j, att.attribute.Index, WriteFakeStruct(att.info.GetValue(obj), fake.buffer));
                                break;
                            case DataType.FakeStructArray:
                                fake.SetData(j, att.attribute.Index, WriteFakeStructArray(att.info.GetValue(obj), att.info.FieldType.GetElementType(), fake.buffer));
                                break;
                            case DataType.FakeStringArray:
                                string[] ss = att.info.GetValue(obj) as string[];
                                if (ss != null)
                                    fake.SetData(j, att.attribute.Index, new FakeStringArray(ss));
                                break;
                            case DataType.Long:
                                fake.SetInt64(j, att.attribute.Index, (long)att.info.GetValue(obj));
                                break;
                            case DataType.Double:
                                fake.SetDouble(j, att.attribute.Index, (double)att.info.GetValue(obj));
                                break;
                            default:
                                fake.SetData(j, att.attribute.Index, att.info.GetValue(obj));
                                break;
                        }
                    }
            }
        }
    }
}