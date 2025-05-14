using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class Encryption 
{
    private static readonly byte[] DataKey = { 0x3A, 0x7F, 0x1C, 0xD5 };
    public static void XOR(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= DataKey[i % DataKey.Length]; 
        }
    }
}
