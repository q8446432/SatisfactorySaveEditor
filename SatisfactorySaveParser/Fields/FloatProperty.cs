﻿using System;
using System.Diagnostics;
using System.IO;

namespace SatisfactorySaveParser.Fields
{
    public class FloatProperty : ISerializedField
    {
        public float Value { get; set; }

        public override string ToString()
        {
            return $"float: {Value}";
        }

        public static FloatProperty Parse(string fieldName, BinaryReader reader)
        {
            var result = new FloatProperty();

            var unk3 = reader.ReadByte();
            Trace.Assert(unk3 == 0);

            result.Value = reader.ReadSingle();

            return result;
        }
    }
}
