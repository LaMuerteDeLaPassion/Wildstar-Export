using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public static class TexJPEG
{
    private static float[][] luminanceQuantTable, chrominanceQuantTable;
    private static Tex.Header header;
    // private  Huffmann huffmannTables;
    public static byte[] Decode(Tex.Header headerI, byte[] imageBytes){
        header = headerI;
        Initialize();
        BinaryReader2 reader = new BinaryReader2(new MemoryStream(imageBytes));
        var outImage = DecodeImage(reader);
        return outImage;
    }
    private static void Initialize(){
        luminanceQuantTable = new float[4][];
        chrominanceQuantTable = new float[4][];
        for (int i = 0; i < 4; i++) {
            if (header.layerInfos[i].quality > 100) {
                throw new Exception("Quality level must be <= 100");
            }
            luminanceQuantTable[i] = Huffmann.Instance.getQuaLu()[header.layerInfos[i].quality];
            chrominanceQuantTable[i] = Huffmann.Instance.getQuaCh()[header.layerInfos[i].quality];
        }
    }
    private static byte[] DecodeImage(BinaryReader2 br){
        int actualHeight = (int)((header.height + 7) & 0xFFFFFFF8);
        int actualWidth = (int)((header.width + 7) & 0xFFFFFFF8);
        byte[] outImage = null;
        if(header.compressionFormat==0){
            outImage = decodeImageType0(actualWidth, actualHeight, br);
        }else if(header.compressionFormat==1){
            outImage = decodeImageType1(actualWidth, actualHeight, br);
        }else if(header.compressionFormat==2){
            outImage = decodeImageType2(actualWidth, actualHeight, br);
        }else{
            Debug.Log("image format not supported");
        }
        return outImage;
    }
    private static byte[] decodeImageType0(int width, int height, BinaryReader2 br){
        int actualHeight = ((height + 15) / 16) * 16;
        int actualWidth = ((width + 15) / 16) * 16;
        int[][] lum0 = new int[4][];    //second is 64
        int[][] lum1 = new int[4][];    //second is 64
        int[] crom0 = new int[64];
        int[] crom1 = new int[64];
        int[] prevDc = new int[4];
        int[] colorBlock;
        byte[] imageData = new byte[actualWidth * actualHeight*4];
        for (var y = 0; y < (actualHeight / 16); ++y) {
            for (var x = 0; x < (actualWidth / 16); ++x) {
                for(var i=0;i<4;i++){
                    prevDc[0] = processBlock(prevDc[0], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[0], true, out lum0[i]);
                }
                prevDc[1] = processBlock(prevDc[1], br, Huffmann.Instance.getDcChr(), Huffmann.Instance.getAcChr(), chrominanceQuantTable[1], false, out crom0);
                prevDc[2] = processBlock(prevDc[2], br, Huffmann.Instance.getDcChr(), Huffmann.Instance.getAcChr(), chrominanceQuantTable[2], false, out crom1);
                for(var i=0;i<4;i++){
                    prevDc[3] = processBlock(prevDc[3], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[3], true, out lum1[i]);
                }

                colorBlock = decodeColorBlockType0(lum0, crom0, crom1, lum1);
                for (int row = 0; row < 16; row++) {
                    if (y * 16 + row >= height || x * 16 >= width) {
                        continue;
                    }

                    int numPixels = Math.Min(16, width - x * 16);
                    // Buffer.BlockCopy(colorBlock, row * 16*4, imageData, ((actualHeight-1-(y * 16 + row)) * width * 4  + x * 16 * 4), numPixels * 4);
                    Buffer.BlockCopy(colorBlock, row * 16*4, imageData, ((y * 16 + row) * width * 4  + x * 16 * 4), numPixels * 4);
                }
            }
        }
        return imageData;
    }

    private static byte[] decodeImageType1(int width, int height, BinaryReader2 br){
        int[][] lum = new int[4][];    //second is 64
        int actualWidth = ((width + 7) / 8) * 8;
        int actualHeight = ((height + 7) / 8) * 8;
        byte[] imageData = new byte[actualWidth * actualHeight*4];

        int[] prevDc = new int[4];
        for (var y = 0; y < (actualHeight / 8); ++y) {
            for (var x = 0; x < (actualWidth / 8); ++x) {
                for(var layer=0; layer<4; layer++){
                    if(header.layerInfos[layer].hasReplacement==0){
                        prevDc[layer] = processBlock(prevDc[layer], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[layer], true, out lum[layer]);
                    }else{
                        var temp = new int[64];
                        for(var i=0;i<64;i++){
                            temp[i] = (int)header.layerInfos[layer].replacement;
                        }
                        lum[layer] = temp;
                    }
                }
                // prevDc[1] = processBlock(prevDc[1], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[1], true, out lum[1]);
                // prevDc[2] = processBlock(prevDc[2], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[2], true, out lum[2]);
                // prevDc[3] = processBlock(prevDc[3], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[3], true, out lum[3]);

                for (var iy = 0; iy < 8; ++iy) {
                    for (var ix = 0; ix < 8; ++ix) {
                        if (y * 8 + iy >= height || x * 8 + ix >= width) {
                            continue;
                        }
                        // STRANGE BEHAVIOR: lum[1] seems to only show patterns in the second variant.
                        var r = (byte)(lum[0][iy * 8 + ix]);
                        var g = (byte)(lum[1][iy * 8 + ix]);
                        var b = (byte)(lum[2][iy * 8 + ix]);
                        var a = (byte)(lum[3][iy * 8 + ix]);
                        // var r = (byte)(lum[1][iy * 8 + ix]);
                        // var g = (byte)(lum[3][iy * 8 + ix]);
                        // var b = (byte)Clamp(255 - r - g, 0, 255);
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4] = r;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 1] = g;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 2] = b;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 3] = a;
                    }
                }
            }
        }
        return imageData;
    }
    private static byte[] decodeImageType2(int width, int height, BinaryReader2 br){
        int[][] lum = new int[4][];    //second is 64
        int actualWidth = ((width + 7) / 8) * 8;
        int actualHeight = ((height + 7) / 8) * 8;
        byte[] imageData = new byte[actualWidth * actualHeight*4];

        int[] prevDc = new int[4];
        for (var y = 0; y < (actualHeight / 8); ++y) {
            for (var x = 0; x < (actualWidth / 8); ++x) {
                if(header.layerInfos[0].hasReplacement==0){
                    prevDc[0] = processBlock(prevDc[0], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[0], true, out lum[0]);
                }else{
                    var temp = new int[64];
                    for(var i=0;i<64;i++){
                        temp[i] = (int)header.layerInfos[0].replacement;
                    }
                    lum[0] = temp;
                }
                if(header.layerInfos[1].hasReplacement==0){
                    prevDc[1] = processBlock(prevDc[1], br, Huffmann.Instance.getDcChr(), Huffmann.Instance.getAcChr(), luminanceQuantTable[1], true, out lum[1]);
                }else{
                    var temp = new int[64];
                    for(var i=0;i<64;i++){
                        temp[i] = (int)header.layerInfos[1].replacement;
                    }
                    lum[1] = temp;
                }
                if(header.layerInfos[2].hasReplacement==0){
                    prevDc[2] = processBlock(prevDc[2], br, Huffmann.Instance.getDcChr(), Huffmann.Instance.getAcChr(), luminanceQuantTable[2], true, out lum[2]);
                }else{
                    var temp = new int[64];
                    for(var i=0;i<64;i++){
                        temp[i] = (int)header.layerInfos[2].replacement;
                    }
                    lum[2] = temp;
                }
                if(header.layerInfos[3].hasReplacement==0){
                    prevDc[3] = processBlock(prevDc[3], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[3], true, out lum[3]);
                }else{
                    var temp = new int[64];
                    for(var i=0;i<64;i++){
                        temp[i] = (int)header.layerInfos[3].replacement;
                    }
                    lum[3] = temp;
                }
                // prevDc[1] = processBlock(prevDc[1], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[1], true, out lum[1]);
                // prevDc[2] = processBlock(prevDc[2], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[2], true, out lum[2]);
                // prevDc[3] = processBlock(prevDc[3], br, Huffmann.Instance.getDcLum(), Huffmann.Instance.getAcLum(), luminanceQuantTable[3], true, out lum[3]);

                for (var iy = 0; iy < 8; ++iy) {
                    for (var ix = 0; ix < 8; ++ix) {
                        if (y * 8 + iy >= height || x * 8 + ix >= width) {
                            continue;
                        }
                        // STRANGE BEHAVIOR: lum[1] seems to only show patterns in the second variant.
                        var r = (byte)(lum[0][iy * 8 + ix]);
                        var g = (byte)(lum[1][iy * 8 + ix]);
                        var b = (byte)(lum[2][iy * 8 + ix]);
                        var a = (byte)(lum[3][iy * 8 + ix]);
                        // var r = (byte)(lum[1][iy * 8 + ix]);
                        // var g = (byte)(lum[3][iy * 8 + ix]);
                        // var b = (byte)Clamp(255 - r - g, 0, 255);
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4] = r;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 1] = g;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 2] = b;
                        imageData[((y * 8 + iy) * width + (x * 8 + ix)) * 4 + 3] = a;
                    }
                }
            }
        }
        return imageData;
    }
    private static int[] decodeColorBlockType0(int[][] lum0, int[] crom0, int[] crom1, int[][] lum1) {
        var colors = new int[16*16];
        for (int y = 0; y < 16; ++y) {
            for (int x = 0; x < 16; ++x) {
                int cy = (y >= 8) ? 1 : 0;
                int cx = (x >= 8) ? 1 : 0;
                int by = y % 8;
                int bx = x % 8;

                int block = cy * 2 + cx;
                int lumIdx = by * 8 + bx;
                int crmIdx = (y / 2) * 8 + (x / 2);
                colors[y * 16 + x] = toColor(lum0[block][lumIdx], crom0[crmIdx], crom1[crmIdx], lum1[block][lumIdx]);
            }
        }
        return colors;
    }
    private static int toColor(int y, int cb, int cr, int yy) {
        int alpha = y - (cr >> 1);
        int beta = Clamp(alpha + cr, 0, 255);
        int gamma = Clamp(alpha - (cb >> 1), 0, 255);
        int delta = Clamp(gamma + cb, 0, 255);
        return (gamma & 0xFF) | ((beta & 0xFF) << 8) | ((delta & 0xFF) << 16) | ((yy & 0xFF) << 24);
    }
    private static int processBlock(int prevDc, BinaryReader2 br, Dictionary<string, int> dcTable, Dictionary<string, int> acTable, float[] quantTable, bool isLuminance, out int[] outBlock) {
        int[] workBlock = new int[64];
        outBlock = new int[64];
        int curDc = decodeBlock(br, out workBlock, dcTable, acTable, prevDc);
        workBlock = unzigzag(workBlock);
        workBlock = dequantize(workBlock, quantTable);
        workBlock = DCT.DCT.DoIdct(workBlock);

        for (var i = 0; i < 64; ++i) {
            int value = workBlock[i];
            if (isLuminance) {
                value = Clamp((int)(value + 128), 0, 255);
            } else {
                value = Clamp(value, -256, 255);
            }
            outBlock[i] = value;
        }
        return curDc;
    }
    
    private static int decodeBlock(BinaryReader2 br, out int[] block, Dictionary<string, int> dcTable, Dictionary<string, int> acTable, int prevDC = 0) {
        block = new int[64];
        int dcLen = decodeValue(br, dcTable);
        int epsilon = br.ReadBits((byte)dcLen);
        int deltaDC = extend(epsilon, dcLen);
        int curDC = deltaDC + prevDC;
        block[0] = curDC;

        for (int idx = 1; idx < 64;) {
            int acCodedValue = decodeValue(br, acTable);
            if (acCodedValue == 0) {
                break;
            }

            if (acCodedValue == 0xF0) {
                idx += 16;
                continue;
            }
            idx += (acCodedValue >> 4) & 0xF;
            var acLen = (byte)(acCodedValue & 0xF);
            epsilon = br.ReadBits(acLen);
            var acValue = extend(epsilon, acLen);
            block[idx] = acValue;
            idx++;
        }
        return curDC;
    }
    private static int decodeValue(BinaryReader2 br, Dictionary<string, int> table) {
        int word = 0;
        int wordLength = 0;
        do {
            word <<= 1;
            var x = br.ReadBit();
            word |= x;
            ++wordLength;
            var wk = wordLength.ToString() + "_" + word.ToString();
            if(table.ContainsKey(wk)){
                return table[wk];
            }
        } while (wordLength < 16);
        throw new Exception("Did not find value in huffman tree");
    }
    private static int extend(int value, int length) {
        if (value < (1 << (length - 1))) {
            return (value + (-1 << length) + 1);
        } else {
            return value;
        }
    }
    private static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>{
        if (val.CompareTo(min) < 0) return min;
        else if(val.CompareTo(max) > 0) return max;
        else return val;
    }    
    private static int[] unzigzag(int[] block) {
        int[] tmpBuffer = new int[64];
        for (int i = 0; i < 64; ++i) {
            tmpBuffer[i] = block[zigzag_mapping[i]];
        }

        return tmpBuffer;
    }
    private static int[] dequantize(int[] block, float[] quantizationTable) {
        for (var i = 0; i < 64; ++i) {
            block[i] = (int)Math.Round(block[i] * quantizationTable[i]+0.001);
        }
        return block;
    }
    public class BinaryReader2 : BinaryReader
    {
        private byte _current;
        private int _index = 8;
        public BinaryReader2(Stream baseStream) : base(baseStream){
        }
        public byte ReadBit() {
            if (_index >= 8) {
                _current = this.ReadByte();
                _index = 0;
            }
            var x = (_current >> 7-_index++);
            return (byte)(x & 0x1);
        }
        public UInt16 ReadBits(byte numBits) {
            if (numBits >= 16) {
                throw new Exception("Invalid bit count");
            }
            UInt16 ret = 0;
            for (byte i = 0; i < numBits; ++i) {
                ret <<= 1;
                ret |= this.ReadBit();
            }
            return ret;
        }
    }

    private static int[] zigzag_mapping = {0, 1, 5, 6, 14, 15, 27, 28, 2, 4, 7, 13, 16, 26, 29, 42, 3, 8, 12, 17, 25, 30, 41, 43,
                            9,
                            11, 18, 24, 31, 40, 44, 53, 10, 19, 23, 32, 39, 45, 52, 54, 20, 22, 33, 38, 46, 51, 55,
                            60,
                            21, 34, 37, 47, 50, 56, 59, 61, 35, 36, 48, 49, 57, 58, 62, 63};
}

