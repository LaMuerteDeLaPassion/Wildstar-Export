using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public  class Tex
{
    private uint headerSize = 112u;
    private Header header;
    private string filePath;

    public Texture2D DecodeTexture(string pathToFile){
        byte[] imgBytes = DataManager.GetFileBytes(pathToFile);
        filePath = pathToFile;
        BinaryReader reader = new BinaryReader(new MemoryStream(imgBytes));
        // BinaryReader reader = new BinaryReader(File.Open(pathToFile, FileMode.Open));
        readHeader(reader);
        var offset = headerSize;
        var byteSize = 0;
        if(header.imageSizesCount > 0){
            reader.BaseStream.Position = 56;
            for(var i=0;i<header.nrMipMaps-1;i++){
                offset += (uint)header.imageSizes[i];
            }
            byteSize = header.imageSizes[header.nrMipMaps-1];
        }else{
            byteSize = (int)(reader.BaseStream.Length) - (int)headerSize;

        }
        reader.BaseStream.Position = offset;

        var imageBytes = reader.ReadBytes((int)byteSize);
        
        byte[] rawImageData = null;
        Texture2D texture = null;
        if(header.format == 0){	// JPEG
            rawImageData = TexJPEG.Decode(header, imageBytes);
            texture = new Texture2D(header.width, header.height, TextureFormat.BGRA32, false);
            texture.LoadRawTextureData(rawImageData);
            texture.Apply();
        }else if(header.format == 1){	// argb
            Debug.Log("FORMAT FOUND: " + header.format + " File: " + pathToFile);
        }else if(header.format == 6){
            texture = new Texture2D(header.width, header.height, TextureFormat.R8, false);
            texture.LoadRawTextureData(imageBytes);
            texture.Apply();
        }else if(header.format == 13){
            Debug.Log("dxt1 FOUND: " + header.format + " File: " + pathToFile);
        }else if(header.format == 14){
            Debug.Log("dxt3 FOUND: " + header.format + " File: " + pathToFile);
        }else if(header.format == 15){
            reader.BaseStream.Position = offset;
            byte[] dxtBytes = this.GetDxtBytes(this.header.nrMipMaps, reader, 16);
            Texture2D upsideDown = new Texture2D(this.header.width, this.header.height, TextureFormat.DXT5, false);
            upsideDown.LoadRawTextureData(dxtBytes);
            upsideDown.Apply();
            upsideDown.Compress(false);
            texture = new Texture2D(this.header.width, this.header.height);
            for(int i=0;i<upsideDown.width;i++){   // flip up down
                for(int j=0;j<upsideDown.height;j++){
                    texture.SetPixel(i, upsideDown.height-j-1, upsideDown.GetPixel(i,j));
                }
            }
            texture.Apply();
            texture.Compress(false);
            texture = texture.DeCompress();
        }else{
            Debug.Log("NEW FORMAT FOUND: " + header.format + " File: " + pathToFile);
        }
        return texture;
    }

    
    private  void readHeader(BinaryReader br){
        var texHeader = new Header();
        texHeader.signature = new string(br.ReadChars(4));;
        texHeader.version = br.ReadInt32();
        texHeader.width = br.ReadInt32();
        texHeader.height = br.ReadInt32();
        texHeader.depth = br.ReadInt32();
        texHeader.sides = br.ReadInt32();
        texHeader.nrMipMaps = br.ReadInt32();
        texHeader.format = br.ReadInt32();
        texHeader.containsSizes = (int)br.ReadUInt32();
        texHeader.compressionFormat = br.ReadInt32();
        texHeader.layerInfos = new TexLayer[4];
        for(var i=0; i<4; i++){
            texHeader.layerInfos[i].quality = br.ReadByte();
            texHeader.layerInfos[i].hasReplacement = br.ReadByte();
            texHeader.layerInfos[i].replacement = br.ReadByte();
        }
        texHeader.imageSizesCount = br.ReadInt32();
        texHeader.imageSizes = new int[13];
        for(var i=0; i<13; i++){
            texHeader.imageSizes[i] = br.ReadInt32();
        }
        texHeader.unk_06C = br.ReadInt32();

        header = texHeader;
        // Debug.LogFormat("{0}, {1}, {2} --- {3}", texHeader.format, texHeader.compressionFormat, texHeader.width, filePath);
        // string str = "";
        // for(var iii = 0; iii<13; iii++){
        //     str += texHeader.imageSizes[iii].ToString() + ",";
        // }
        // Debug.Log(str);
        // Debug.LogFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}", texHeader.version, texHeader.width, texHeader.height, texHeader.depth, texHeader.sides, texHeader.nrMipMaps, texHeader.format, texHeader.containsSizes, texHeader.compressionFormat, texHeader.imageSizesCount, texHeader.unk_06C);
        // for(var i=0; i<4; i++){
        //     Debug.LogFormat("{0}, {1}, {2}", texHeader.layerInfos[i].quality, texHeader.layerInfos[i].hasReplacement, texHeader.layerInfos[i].replacement);
        // }
    }
    
    private byte[] GetDxtBytes(int miplevel, BinaryReader br, int blockSize){ // only for square images
        int w = this.header.width;
        int h = this.header.height;
        int x = (int)((w+3)/4) * (int)((h+3)/4) * blockSize;
        byte[] output = new byte[x];
        for (int m = this.header.nrMipMaps - 1; m >= 0; m--){
            int exp = (int)Mathf.Pow(2, m);
            int width = this.header.width / exp;
            int height = this.header.height / exp;
            var byteNr = (int)((width+3)/4) * (int)((height+3)/4) * blockSize;
            output = br.ReadBytes(byteNr);
        }
        return output;
    }
    public struct TexLayer{
        public byte quality;
        public byte hasReplacement;
        public byte replacement;
    }
    public struct Header
    {
        public string signature;
        public int version;
        public int width, height;
        public int depth;
        public int sides;
        public int nrMipMaps;
        public int format;
        public int containsSizes;
        public int compressionFormat;
        public TexLayer[] layerInfos;
        public int imageSizesCount;
        public int[] imageSizes;
        public int unk_06C;
    }
}
