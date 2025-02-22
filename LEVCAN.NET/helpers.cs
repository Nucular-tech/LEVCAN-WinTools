﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LEVCAN
{
    public class Text8z : IDisposable
    {
        public IntPtr Pointer;
        public Encoding Codepage { get; private set; }

        public Text8z(string text)
        {
            Codepage = Encoding.GetEncoding(1250);
            Pointer = StringToPtr(text, Codepage);
        }

        public Text8z(string text, Encoding encoding)
        {
            Pointer = StringToPtr(text, encoding);
            Codepage = encoding;
        }

        public override string ToString()
        {
            return PtrToString(Pointer, Codepage);
        }

        private void ReleaseUnmanagedResources()
        {
            Marshal.FreeHGlobal(Pointer);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Text8z()
        {
            ReleaseUnmanagedResources();
        }

        public static IntPtr StringToPtr(string text, Encoding encoding)
        {
            if (text == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                byte[] bytes = encoding.GetBytes(text);
                var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
                Marshal.WriteByte(pointer, bytes.Length, 0);
                return pointer;
            }
        }

        public static string PtrToString(IntPtr pointer, Encoding encoding, int maxchar = int.MaxValue)
        {
            if (pointer == IntPtr.Zero) { return null; }
            int length;
            for (length = 0; Marshal.ReadByte(pointer, length) != 0 && length < maxchar; length++) ;
            byte[] bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            string str = encoding.GetString(bytes);
            return str;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            return ByteArrayToString(ba, ba.Length);
        }

        public static string ByteArrayToString(byte[] ba, int length, bool addspaces = false)
        {
            StringBuilder hex = new StringBuilder(length * (addspaces ? 3 : 2));
            for (int b = 0; b < length; b++)
            {
                if (addspaces)
                    hex.AppendFormat("{0:X2} ", ba[b]);
                else
                    hex.AppendFormat("{0:X2}", ba[b]);
            }
            return hex.ToString();
        }
    }

    //https://stackoverflow.com/questions/17339928/c-sharp-how-to-convert-object-to-intptr-and-back
    public static class ObjectHandleExtensions
    {
        public static IntPtr ToIntPtr(this object target)
        {
            return GCHandle.Alloc(target, GCHandleType.Pinned).AddrOfPinnedObject();
        }

        public static GCHandle ToGcHandle(this object target)
        {
            return GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        public static IntPtr ToIntPtr(this GCHandle target)
        {
            return target.AddrOfPinnedObject();
        }
    }

    public class GCHandleProvider : IDisposable
    {
        public GCHandleProvider(object target)
        {
            Handle = target.ToGcHandle();
        }

        public IntPtr Pointer => Handle.ToIntPtr();

        public GCHandle Handle { get; }

        private void ReleaseUnmanagedResources()
        {
            if (Handle.IsAllocated) Handle.Free();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~GCHandleProvider()
        {
            ReleaseUnmanagedResources();
        }
    }

    public static class CastingHelper
    {
        public static T CastToStruct<T>(this byte[] data) where T : struct
        {
            var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
            var result = (T)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(T));
            pData.Free();
            return result;
        }

        public static byte[] CastToArray<T>(this T data) where T : struct
        {
            var result = new byte[Marshal.SizeOf(typeof(T))];
            var pResult = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.StructureToPtr(data, pResult.AddrOfPinnedObject(), true);
            pResult.Free();
            return result;
        }
    }
}
