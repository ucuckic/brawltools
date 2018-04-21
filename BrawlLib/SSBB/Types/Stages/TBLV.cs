﻿using System;
using System.Runtime.InteropServices;

namespace BrawlLib.SSBBTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TBLV // Table (Lava)
    {
        public const uint Tag = 0x564C4254;

        public uint _tag;
        public bint _unk0;
        public bint _unk1;
        public bint _unk2;
        //public bint _entryOffset;

        public VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
        public bfloat* Entries { get { return (bfloat*)(Address + 0x10); } }
    }
}
