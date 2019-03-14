using huqiang.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace huqiang
{
    public class AttributeKey : Attribute
    {
        public int Index;
        public short type = DataType.Int;
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
                var field = fields[i];
                var atts = field.GetCustomAttributes(dbType, false);
                if (atts != null)
                {
                    var att = atts[0] as AttributeKey;
                    int index = att.Index;
                    switch (att.type)
                    {
                        case DataType.Int:
                            field.SetValue(obj, fake[index]);
                            break;
                        case DataType.Float:
                            field.SetValue(obj, fake.GetFloat(index));
                            break;
                        case DataType.Long:
                            field.SetValue(obj, fake.GetInt64(index));
                            break;
                        case DataType.Double:
                            field.SetValue(obj, fake.GetDouble(index));
                            break;
                        case DataType.FakeStruct:
                            if (field.FieldType == fakeType)
                                field.SetValue(obj, fake.GetData(index));
                            else
                            {
                                var fs = fake.GetData(index) as FakeStruct;
                                if (fs != null)
                                {
                                    object instance = Activator.CreateInstance(field.FieldType);
                                    ReadToClass(field.FieldType, instance, fs);
                                    field.SetValue(obj, instance);
                                }
                            }
                            break;
                        case DataType.FakeStructArray:
                            if (field.FieldType == fakeType)
                                field.SetValue(obj, fake.GetData(index));
                            else
                            {
                                var fs = fake.GetData(index) as FakeStructArray;
                                if (fs != null)
                                {
                                    Type ele = field.FieldType.GetElementType();
                                    field.SetValue(obj, ReadStructArray(ele, fs));
                                }
                            }
                            break;
                        default:
                            field.SetValue(obj, fake.GetData(index));
                            break;
                    }
                }
            }
        }
        static object ReadStructArray(Type eleType, FakeStructArray fake)
        {
            var fields = eleType.GetFields();
            Array array = Array.CreateInstance(eleType, fake.Length);
            for (int index = 0; index < fake.Length; index++)
            {
                var obj = Activator.CreateInstance(eleType);
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var atts = field.GetCustomAttributes(dbType, false);
                    if (atts != null)
                    {
                        var att = atts[0] as AttributeKey;
                        int offset = att.Index;
                        switch (att.type)
                        {
                            case DataType.Int:
                                field.SetValue(obj, fake[index, offset]);
                                break;
                            case DataType.Float:
                                field.SetValue(obj, fake.GetFloat(index, offset));
                                break;
                            case DataType.Long:
                                field.SetValue(obj, fake.GetInt64(index, offset));
                                break;
                            case DataType.Double:
                                field.SetValue(obj, fake.GetDouble(index, offset));
                                break;
                            case DataType.FakeStruct:
                                if (field.FieldType == fakeType)
                                    field.SetValue(obj, fake.GetData(index, offset));
                                else
                                {
                                    var fs = fake.GetData(index, offset) as FakeStruct;
                                    if (fs != null)
                                    {
                                        object instance = Activator.CreateInstance(field.FieldType);
                                        ReadToClass(field.FieldType, instance, fs);
                                        field.SetValue(obj, instance);
                                    }
                                }
                                break;
                            case DataType.FakeStructArray:
                                if (field.FieldType == fakeType)
                                    field.SetValue(obj, fake.GetData(index, offset));
                                else
                                {
                                    var fs = fake.GetData(index, offset) as FakeStructArray;
                                    if (fs != null)
                                    {
                                        Type ele = field.FieldType.GetElementType();
                                        field.SetValue(obj, ReadStructArray(ele, fs));
                                    }
                                }
                                break;
                            default:
                                field.SetValue(obj, fake.GetData(index, offset));
                                break;
                        }
                    }
                }
                array.SetValue(obj, index);
            }
            return array;
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
                    var atts = f.GetCustomAttributes(dbType, false);
                    if (atts != null)
                    {
                        tmp[c].info = f;
                        tmp[c].attribute = atts[0] as AttributeKey;
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
                var att = tmp[i].attribute;
                int index = att.Index;
                var field = tmp[i].info;
                switch (att.type)
                {
                    case DataType.Int:
                        fake[index] = (Int32)field.GetValue(obj);
                        break;
                    case DataType.Float:
                        fake.SetFloat(index, (float)field.GetValue(obj));
                        break;
                    case DataType.Long:
                        fake.SetInt64(index, (Int64)field.GetValue(obj));
                        break;
                    case DataType.Double:
                        fake.SetDouble(index, (Double)field.GetValue(obj));
                        break;
                    case DataType.FakeStruct:
                        fake.SetData(index, WriteFakeStruct(field.GetValue(obj), fake.buffer));
                        break;
                    case DataType.FakeStructArray:
                        fake.SetData(index, WriteFakeStructArray(field.GetValue(obj), field.FieldType.GetElementType(), fake.buffer));
                        break;
                    default:
                        fake.SetData(index, field.GetValue(obj));
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
                    var atts = f.GetCustomAttributes(dbType, false);
                    if (atts != null)
                    {
                        tmp[c].info = f;
                        tmp[c].attribute = atts[0] as AttributeKey;
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
            for (int index = 0; index < fake.Length; index++)
            {
                var obj = array.GetValue(index);
                if (obj != null)
                    for (int i = 0; i < len; i++)
                    {
                        var att = tmp[i].attribute;
                        int offset = att.Index;
                        var field = tmp[i].info;
                        switch (att.type)
                        {
                            case DataType.Int:
                                fake[index, offset] = (Int32)field.GetValue(obj);
                                break;
                            case DataType.Float:
                                fake.SetFloat(index, offset, (float)field.GetValue(obj));
                                break;
                            case DataType.Long:
                                fake.SetInt64(index, offset, (Int64)field.GetValue(obj));
                                break;
                            case DataType.Double:
                                fake.SetDouble(index, offset, (Double)field.GetValue(obj));
                                break;
                            case DataType.FakeStruct:
                                fake.SetData(index, offset, WriteFakeStruct(field.GetValue(obj), fake.buffer));
                                break;
                            case DataType.FakeStructArray:
                                fake.SetData(index, offset, WriteFakeStructArray(field.GetValue(obj), field.FieldType.GetElementType(), fake.buffer));
                                break;
                            default:
                                fake.SetData(index, offset, field.GetValue(obj));
                                break;
                        }
                    }
            }
        }
    }
}