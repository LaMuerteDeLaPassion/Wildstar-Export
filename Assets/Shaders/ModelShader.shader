Shader "Wildstar/ModelShader"{
    Properties{
        _Color0 ("Albedo (RGB)", 2D) = "bump" {}
        _Color1 ("Albedo (RGB)", 2D) = "bump" {}
        _Color2 ("Albedo (RGB)", 2D) = "bump" {}
        _Color3 ("Albedo (RGB)", 2D) = "bump" {}
        _Normal0 ("Albedo (RGB)", 2D) = "bump" {}
        _Normal1 ("Albedo (RGB)", 2D) = "bump" {}
        _Normal2 ("Albedo (RGB)", 2D) = "bump" {}
        _Normal3 ("Albedo (RGB)", 2D) = "bump" {}
        
        _cb1_0 ("cb1_0", Vector) = (0,0,0,1)
        _cb1_1 ("cb1_1", Vector) = (0,0,0,1)
        _cb1_2 ("cb1_2", Vector) = (0,0,0,1)
        _cb1_3 ("cb1_3", Vector) = (0,0,0,1)
        _cb1_4 ("cb1_4", Vector) = (0,0,0,1)
        _cb1_5 ("cb1_5", Vector) = (0,0,0,1)
        _cb1_6 ("cb1_6", Vector) = (0,0,0,1)
        _cb1_7 ("cb1_7", Vector) = (0,0,0,1)
        _cb1_8 ("cb1_8", Vector) = (0,0,0,1)
        _cb1_9 ("cb1_9", Vector) = (0,0,0,1)
        _cb1_10 ("cb1_10", Vector) = (0,0,0,1)
        _cb1_11 ("cb1_11", Vector) = (0,0,0,1)
        _cb1_12 ("cb1_12", Vector) = (0,0,0,1)
        _cb1_13 ("cb1_13", Vector) = (0,0,0,1)
        _cb1_14 ("cb1_14", Vector) = (0,0,0,1)
        _cb1_15 ("cb1_15", Vector) = (0,0,0,1)
        _cb1_16 ("cb1_16", Vector) = (0,0,0,1)
        _cb1_17 ("cb1_17", Vector) = (0,0,0,1)
        _cb1_18 ("cb1_18", Vector) = (0,0,0,1)
        _cb1_19 ("cb1_19", Vector) = (0,0,0,1)
        _cb1_20 ("cb1_20", Vector) = (0,0,0,1)
        _cb1_21 ("cb1_21", Vector) = (0,0,0,1)
        _cb1_22 ("cb1_22", Vector) = (0,0,0,1)
        _cb1_23 ("cb1_23", Vector) = (0,0,0,1)
        _cb1_24 ("cb1_24", Vector) = (0,0,0,1)
        _cb1_25 ("cb1_25", Vector) = (0,0,0,1)
        _cb1_28 ("cb1_28", Vector) = (0,0,0,1)
        
        _cb2_0 ("cb2_0", Vector) = (0,0,0,1)
        _cb2_1 ("cb2_1", Vector) = (0,0,0,1)
        _cb2_2 ("cb2_2", Vector) = (0,0,0,1)
        _cb2_3 ("cb2_3", Vector) = (0,0,0,1)
        _cb2_4 ("cb2_4", Vector) = (0,0,0,1)
        _cb2_5 ("cb2_5", Vector) = (0,0,0,1)
        _cb2_6 ("cb2_6", Vector) = (0,0,0,1)
        _cb2_7 ("cb2_7", Vector) = (0,0,0,1)
        _cb2_8 ("cb2_8", Vector) = (0,0,0,1)
        _cb2_9 ("cb2_9", Vector) = (0,0,0,1)
        _cb2_10 ("cb2_10", Vector) = (0,0,0,1)
    }
    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float4 color : COLOR;
                float4 normal : NORMAL;
                float4 tangent: TANGENT;
            };

            struct v2f{
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 blend : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            sampler2D _Color0;
            sampler2D _Color1;
            sampler2D _Color2;
            sampler2D _Color3;
            sampler2D _Normal0;
            sampler2D _Normal1;
            sampler2D _Normal2;
            sampler2D _Normal3;

            float4 _cb1_0;
            float4 _cb1_1;
            float4 _cb1_2;
            float4 _cb1_3;
            float4 _cb1_4;
            float4 _cb1_5;
            float4 _cb1_6;
            float4 _cb1_7;
            float4 _cb1_8;
            float4 _cb1_9;
            float4 _cb1_10;
            float4 _cb1_11;
            float4 _cb1_12;
            float4 _cb1_13;
            float4 _cb1_14;
            float4 _cb1_15;
            float4 _cb1_16;
            float4 _cb1_17;
            float4 _cb1_18;
            float4 _cb1_19;
            float4 _cb1_20;
            float4 _cb1_21;
            float4 _cb1_22;
            float4 _cb1_23;
            float4 _cb1_24;
            float4 _cb1_25;
            float4 _cb1_28;

            float4 _cb2_0;
            float4 _cb2_1;
            float4 _cb2_2;
            float4 _cb2_3;
            float4 _cb2_4;
            float4 _cb2_5;
            float4 _cb2_6;
            float4 _cb2_7;
            float4 _cb2_8;
            float4 _cb2_9;
            float4 _cb2_10;

            v2f vert (appdata v){
                v2f o;                
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = v.uv; //TRANSFORM_TEX(v.uv, _Color0);
                // o.uv1 = v.uv1;//TRANSFORM_TEX(v.uv1, _Color0);
                o.uv.xy = v.uv;
                o.uv.zw = v.uv1;
                o.uv1.xy = v.uv1;
                o.uv1.zw = v.uv;
                // o.uv.x = v.uv1.x;
                // o.uv.y = v.uv.y;
                // o.uv1.x = v.uv.x;
                // o.uv1.y = v.uv1.y;
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.color = v.color;
                o.blend.xy = v.uv2;
                o.blend.zw = v.uv3;
                o.normal.xyz = UnityObjectToWorldNormal(v.normal.xyz);//v.normal; //
                o.tangent.xyz = UnityObjectToWorldNormal(v.tangent.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
                float4 color0,color1,color2,color3,normal0,normal1,normal2,normal3;
                float4 colorData;
                float3 c0_A, c0_B, c1_A, c1_B, c2_A, c2_B, c3_A, c3_B;
                float3 normalData, alpha2, vertexNormal;
                float4 unk4_0;
                float tempf, al1, alpha;
                float4 v2 = i.color;
                float4 v3 = i.uv;
                float4 v4 = i.uv1;
                float4 v5 = i.blend;
                float4 v7 = i.normal;
                float4 v8 = i.tangent;
                // float4 cb1_0 =  float4(0, 0, 0, 1);     // which normal channel to use for blending (normal0)
                // float4 cb1_1 =  float4(0, 0, 1, 0);     // which normal channel to use for blending (normal1)
                // float4 cb1_2 =  float4(0, 0, 0, 1);     // which normal channel to use for blending (normal2)
                // float4 cb1_3 =  float4(0, 0, 0, 1);     // which normal channel to use for blending (normal3)
                // float4 cb1_4 =  float4(0, 0, 0, 1);     // which normal channel to use for transparency (normal0)
                // float4 cb1_5 =  float4(0, 0, 0, 1);     // which normal channel to use for transparency (normal1)
                // float4 cb1_6 =  float4(0, 0, 0, 1);     // which normal channel to use for transparency (normal2)
                // float4 cb1_7 =  float4(0, 0, 0, 1);     // which normal channel to use for transparency (normal3)
                // float4 cb1_8 =  float4(0, 0, 1, 0);      // somehow related to normal maps. is responsible to choose one of the channels (normal0)
                // float4 cb1_9 =  float4(1, 0, 0, 0);      // somehow related to normal maps. is responsible to choose one of the channels (normal1)
                // float4 cb1_10 = float4(0, 0, 1, 0);      // somehow related to normal maps. is responsible to choose one of the channels (normal2)
                // float4 cb1_11 = float4(0, 0, 0, 1);      // somehow related to normal maps. is responsible to choose one of the channels (normal3)
                // float4 cb1_12 = 0;          // subtract normal 0 layers to color 0
                // float4 cb1_13 = 0;
                // float4 cb1_14 = 0;
                // float4 cb1_15 = 0;
                // float4 cb1_16 = 0;
                // float4 cb1_17 = 0;
                // float4 cb1_18 = 0;
                // float4 cb1_19 = 0;
                // float4 cb1_20 = 1;      // which color channels are being used (color0)
                // float4 cb1_21 = 1;      // which color channels are being used (color1)
                // float4 cb1_22 = 1;      // which color channels are being used (color2)
                // float4 cb1_23 = 1;      // which color channels are being used (color3)
                // float4 cb1_24 = 1;      // blending weight per texture (x, y, z, w)
                // float4 cb1_25 = float4(0.001, 0.001, 0.001, 0.001); // prevents blending from being 0
                // float4 cb1_28 = float4(0, 0, 0, 1); // somehow related to normal mapping
                // float4 cb2_0 =  float4(0, 0, 0, 0); 
                // float4 cb2_1 =  float4(0, 0, 0, 0); // transparency treshold
                // float4 cb2_3 =  float4(0, 0, 0, 1);         //????
                // float4 cb2_4 =  0;
                // float4 cb2_5 =  0;
                // float4 cb2_6 =  float4(1.13725, 1.21569, 1.13725, 1.00);         // ????
                // float4 cb2_7 =  float4(1.13725, 1.21569, 1.13725, 1.00);         // ????
                // float4 cb2_9 =  0;
                // float4 cb2_10 = float4(0.00001, 0, 0, 0);

                bool IS_TRANSPARENT = _cb2_1.y > 0;
                bool USE_TEXTURE_BLENDING = (dot(_cb1_24, float4(1,1,1,1)) > 0) || _cb1_21.x > 0;
                float4 vertexBlend = 0;
                float4 blendComponent = 0;
                if (!USE_TEXTURE_BLENDING){
                    vertexBlend = v5;
                }
                float4 alphaComponent = 0;
                float4 normalComponent = 0;
                float4 specularComponent = 0;
                float4 transparencyComponent = 0;
                float4 uv1, uv2;
                // return v4;
                // if regular uvs
                uv1 = v3;
                uv2 = v4;
                // else
                // normal0.xyzw = t4.Sample(s4_s, v3.xy).zxyw;
                // normal0.z = 1;
                // tempf = dot(cb1[0].yzw, normal0.xyz).x * cb1[26].x + cb1[27].x;
                // uv1.xy = v10.xy * tempf + v3.xy;
                // endif
                if (_cb1_20.x > 0){
                    color0 = tex2D(_Color0, uv1.xy).xyzw;
                    if (dot(_cb1_8, float3(1,1,1)) > 0){          // sketchy condition....
                        normal0 = tex2D(_Normal0, uv1.xy).yzxw;
                        normal0.w = 1;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.x = _cb1_24.x * dot(_cb1_0.yzw, normal0.yzw);
                        }
                        normal0.x = color0.w;
                        tempf = dot(_cb1_12.xyzw, normal0.xyzw);
                        c0_A = -color0.xyz * tempf + color0.xyz;
                        c0_B = _cb1_20.xyz * color0.xyz * tempf;
                        alphaComponent.x = dot(_cb1_16.xyzw, normal0.xyzw);
                        normalComponent.x = dot(_cb1_8.xyzw, normal0.xyzw);
                        if(IS_TRANSPARENT){
                            transparencyComponent.x = dot(_cb1_4.xyzw, normal0.xyzw);//dot(float4(1,0,0,0), normal0.xyzw);//
                        }
                    }else{
                        c0_A = -color0.xyz * _cb1_12.www + color0.xyz;
                        c0_B = _cb1_20.xyz * _cb1_12.www * color0.xyz;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.x = _cb1_24.x * _cb1_0.w;
                        }
                        alphaComponent.x = _cb1_16.w;
                        normalComponent.x = _cb1_8.w;
                        // IF o2 FILETR:
                        //     specularComponent.x = _cb1_4.w;
                    }
                }else{
                    c0_A = 0;
                    c0_B = 0;
                }
                if (_cb1_21.x > 0){
                    color1 = tex2D(_Color1, uv1.zw);
                    if (dot(_cb1_9, float3(1,1,1)) > 0){          // sketchy condition....
                        normal1 = tex2D(_Normal1, uv1.zw).yzxw;
                        normal1.w = 1;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.y = _cb1_24.y * dot(_cb1_1.yzw, normal1.yzw);
                        }
                        normal1.x = color1.w;
                        tempf = dot(_cb1_13.xyzw, normal1.xyzw);
                        c1_A = -color1.xyz * tempf + color1.xyz;
                        c1_B = _cb1_21.xyz * color1.xyz * tempf;
                        alphaComponent.y = dot(_cb1_17.xyzw, normal1.xyzw);
                        normalComponent.y = dot(_cb1_9.xyzw, normal1.xyzw);
                    }else{
                        c1_A = -color1.xyz * _cb1_13.www + color1.xyz;
                        c1_B = _cb1_21.xyz * _cb1_13.www * color1.xyz;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.y = _cb1_24.y * _cb1_1.w;
                        }
                        alphaComponent.y = _cb1_17.w;
                        normalComponent.y = _cb1_9.w;
                        // IF o2 FILETR:
                        //     specularComponent.x = _cb1_5.w;
                    }
                }else{
                    c1_A = 0;
                    c1_B = 0;
                }
                if (_cb1_22.x > 0){
                    color2 = tex2D(_Color2, uv2.xy);
                    if (dot(_cb1_10, float3(1,1,1)) > 0){          // sketchy condition....
                        normal2 = tex2D(_Normal2, uv2.xy);
                        normal2.w = 1;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.z = _cb1_24.z * dot(_cb1_2.yzw, normal2.yzw);
                        }
                        normal2.x = color2.w;
                        tempf = dot(_cb1_14.xyzw, normal2.xyzw);
                        c2_A = -color2.xyz * tempf + color2.xyz;
                        c2_B = _cb1_22.xyz * color2.xyz * tempf;
                        alphaComponent.z = dot(_cb1_18.xyzw, normal2.xyzw);
                        normalComponent.z = dot(_cb1_10.xyzw, normal2.xyzw);
                    }else{
                        c2_A = -color2.xyz * _cb1_14.www + color2.xyz;
                        c2_B = _cb1_22.xyz * _cb1_14.www * color2.xyz;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.z = _cb1_24.z * _cb1_2.w;
                        }
                        alphaComponent.z = _cb1_18.w;
                        normalComponent.z = _cb1_10.w;
                        // IF o2 FILETR
                        //     specularComponent.z = _cb1_6.w;
                    }
                }else{
                    c2_A = 0;
                    c2_B = 0;
                }
                if (_cb1_23.x > 0){
                    color3 = tex2D(_Color3, uv2.zw);
                    if (dot(_cb1_11, float3(1,1,1)) > 0){          // sketchy condition....
                        normal3 = tex2D(_Normal3, uv2.zw);
                        normal3.w = 1;
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.w = _cb1_24.w * dot(_cb1_3.yzw, normal3.yzw);
                        }
                        normal3.x = color3.w;
                        tempf = dot(_cb1_15.xyzw, normal3.xyzw);
                        c3_A = -color3.xyz * tempf + color3.xyz;
                        c3_B = _cb1_23.xyz * color3.xyz * tempf;
                        alphaComponent.w = dot(_cb1_19.xyzw, normal3.xyzw);
                        normalComponent.w = dot(_cb1_11.xyzw, normal3.xyzw);
                    }else{
                        c3_A = -color3.xyz * _cb1_15.www + color3.xyz;
                        c3_B = (_cb1_23.xyz * _cb1_15.www * color3.xyz);
                        if(USE_TEXTURE_BLENDING){
                            blendComponent.w = _cb1_24.w * _cb1_3.w;
                        }
                        alphaComponent.w = _cb1_19.w;
                        normalComponent.w = _cb1_11.w;
                        // IF o2 FILETR
                        //     specularComponent.w = _cb1_7.w;
                    }
                }else{
                    c3_A = 0;
                    c3_B = 0;
                }
                if(IS_TRANSPARENT){
                    // float transparency = cmp(transparencyComponent.x * v2.w + -0 < 0);//cmp(transparencyComponent.x * v2.w + -_cb2_1.y < 0)
                    // if ((transparencyComponent.x * v2.w - _cb2_1.y < 0) != 0) discard;
                }

                // BLEND
                if(USE_TEXTURE_BLENDING){
                    vertexBlend.xyzw = v5.xyzw * (_cb1_25.xyzw + blendComponent.xyzw);
                    vertexBlend.xyzw = vertexBlend * vertexBlend / dot(vertexBlend, vertexBlend);
                    // vertexBlend.xyzw = vertexBlend.zyxw;       // dont think this is part of the original code... why is it needed here?!?!?!?
                }
                // IF o2 FILTER UNKNOWN FOR NOW SOME KIND OF SPECULAR MANIPULATION ?!?!
                //     o2w = dot(vertexBlend, specularComponent);
                //     o2.w = saturate(v6.x * max(cb2[1].x, v2.w * o2w));
                //     if (cmp(o2w * v2.w + -cb2[1].y < 0) != 0) discard;
                // ELSE
                //     o2.w = 1;

                // // PREPARE OUTPUT 1 (no idea what this does...)
                // IF BLEND
                //     o1w = dot(vertexBlend, normalComponent);
                    al1 = dot(vertexBlend, alphaComponent);
                // ELSE
                //     o1w = normalComponent.x ????
                //     al1 = alphaComponent.x ?????
                // o1.xy = cb1_28.xy * float2(0.00392156886,0.00392156886) + float2(0.00196078443,0.00196078443);
                alpha = al1;
                // o1.z = alpha;
                // o1.w = saturate(cb2[1].w * o1w);

                // BLEND NORMALS
                if(USE_TEXTURE_BLENDING){
                    c0_B = vertexBlend.x * c0_B;
                    c1_B = vertexBlend.y * c1_B;
                    c2_B = vertexBlend.z * c2_B;
                    c3_B = vertexBlend.w * c3_B;
                    normalData.xyz = c0_B + c1_B + c2_B + c3_B;
                }else{
                    normalData = c0_B;// ??????
                }
                alpha2 = alpha * (_cb2_7.xyz + -_cb2_6.xyz) + _cb2_6.xyz;
                normalData.xyz = normalData.xyz * alpha2.xyz + _cb2_3.xyz; 

                // no idea why they do this..
                unk4_0 = alpha * (_cb2_5.xyzw + -_cb2_4.xyzw) + _cb2_4.xyzw;
                normalData.xyz = unk4_0.w * (unk4_0.xyz * dot(normalData.xyz, float3(0.212500006,0.715399981,0.0720999986)) + -normalData.xyz) + normalData.xyz;
                // some kind of normal manipulation
                vertexNormal.xyz = v7.xyz * rsqrt(dot(v7.xyz, v7.xyz));   // normalize v7 (vertex normals) (pythagoras theorem to get the magnitude of the normal, then multiply by normal to get magnitude and orientation.)
                tempf = _cb2_9.w * (1 + -exp2(_cb2_10.x * log2(saturate(vertexNormal.z)))); // some kind of shinyness shading...
                normalData = tempf * (_cb2_9.xyz - normalData.xyz) + normalData.xyz;
                // BLEND COLOR DATA
                if(USE_TEXTURE_BLENDING){
                    c0_A = vertexBlend.x * c0_A;
                    c1_A = vertexBlend.y * c1_A;
                    c2_A = vertexBlend.z * c2_A;
                    c3_A = vertexBlend.w * c3_A;
                    colorData.xyz = c0_A + c1_A + c2_A + c3_A;
                }else{
                    colorData.xyz = c0_A;
                }
                colorData.xyz = v2.xyz * colorData * alpha2.xyz;
                colorData.xyz = tempf * -colorData.xyz + colorData.xyz + normalData; // why do they add normal data to color data??
                // COLOR OUTPUT
                colorData.xyz = colorData.xyz;
                float o01 = dot(normalData, float3(0.212500006,0.715399981,0.0720999986));
                float o02 = dot(colorData.xyz, float3(0.212500006,0.715399981,0.0720999986));
                colorData.w =  o01/ (0.00100000005 + o02); // alpha channel

                // // OTHER OUTPUTS
                // unk10.xy = vertexNormal.xy / dot(abs(vertexNormal.xyz), float3(1,1,1));
                // unk11 = cmp(unk10.xy >= float2(0,0)) ? float2(1,1) : float2(-1,-1);
                // o2.xy = (cmp(vertexNormal.z < 0) ? (-unk11 * abs(unk10.yx) + unk11) : unk10.xy) * float2(0.5,0.5) + float2(0.5,0.5);
                // o2.z = v6.y;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, colorData);
                return colorData;
            }
            ENDCG
        }
    }
}
