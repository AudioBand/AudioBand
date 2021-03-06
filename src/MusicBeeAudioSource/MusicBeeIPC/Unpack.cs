// <auto-generated/>
//--------------------------------------------------------//
// MusicBeeIPCSDK C# v2.0.0                               //
// Copyright © Kerli Low 2014                             //
// This file is licensed under the                        //
// BSD 2-Clause License                                   //
// See LICENSE_MusicBeeIPCSDK for more information.       //
//--------------------------------------------------------//

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

[System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
public partial class MusicBeeIPC
{
    // --------------------------------------------------------------------------------
    // All strings are encoded in UTF-16 little endian
    // --------------------------------------------------------------------------------

    // --------------------------------------------------------------------------------
    // -Int32:  Byte count of the string
    // -byte[]: String data
    // Free lr after use
    // --------------------------------------------------------------------------------
    private bool Unpack(IntPtr lr, out string string_1)
    {
        string_1 = "";

        BinaryReader reader = GetMmfReader(lr);
        if (reader == null)
            return false;

        try
        {
            int byteCount = reader.ReadInt32();

            if (byteCount > 0)
            {
                byte[] bytes = reader.ReadBytes(byteCount);

                string_1 = System.Text.Encoding.Unicode.GetString(bytes);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // --------------------------------------------------------------------------------
    // -Int32:  Number of strings
    // -Int32:  Byte count of 1st string
    // -byte[]: 1st string data
    // -Int32:  Byte count of 2nd string
    // -byte[]: 2nd string data
    // -...
    // Free lr after use
    // --------------------------------------------------------------------------------
    private bool Unpack(IntPtr lr, out string[] strings)
    {
        strings = new string[0];

        BinaryReader reader = GetMmfReader(lr);
        if (reader == null)
            return false;

        try
        {
            int strCount = reader.ReadInt32();

            int byteCount = 0;

            strings = new string[strCount];

            for (int i = 0; i < strCount; i++)
            {
                byteCount = reader.ReadInt32();

                if (byteCount > 0)
                {
                    byte[] bytes = reader.ReadBytes(byteCount);

                    strings[i] = System.Text.Encoding.Unicode.GetString(bytes);
                }
                else
                    strings[i] = "";
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // --------------------------------------------------------------------------------
    // -Int32: 32 bit integer
    // -Int32: 32 bit integer
    // Free lr after use
    // --------------------------------------------------------------------------------
    private bool Unpack(IntPtr lr, out int int32_1, out int int32_2)
    {
        int32_1 = int32_2 = 0;

        BinaryReader reader = GetMmfReader(lr);
        if (reader == null)
            return false;

        try
        {
            int32_1 = reader.ReadInt32();
            int32_2 = reader.ReadInt32();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // --------------------------------------------------------------------------------
    // -Int32: Number of integers
    // -Int32: 1st 32 bit integer
    // -Int32: 2nd 32 bit integer
    // -...
    // Free lr after use
    // --------------------------------------------------------------------------------
    private bool Unpack(IntPtr lr, out int[] int32s)
    {
        int32s = new int[0];

        BinaryReader reader = GetMmfReader(lr);
        if (reader == null)
            return false;

        try
        {
            int int32Count = reader.ReadInt32();

            int32s = new int[int32Count];

            for (int i = 0; i < int32Count; i++)
                int32s[i] = reader.ReadInt32();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private BinaryReader GetMmfReader(IntPtr lr)
    {
        if (lr == IntPtr.Zero)
            return null;

        try
        {
            LRUShort ls = new LRUShort(lr);

            MemoryMappedFile mmf =
                MemoryMappedFile.OpenExisting("mbipc_mmf_" + ls.low.ToString(), MemoryMappedFileRights.Read);

            long size = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read).ReadInt64(ls.high);

            MemoryMappedViewStream stream =
                mmf.CreateViewStream(ls.high + Marshal.SizeOf(typeof(long)), size, MemoryMappedFileAccess.Read);

            return new BinaryReader(stream);
        }
        catch
        {
            return null;
        }
    }
}
