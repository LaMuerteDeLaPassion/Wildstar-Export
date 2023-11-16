using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Linq;


public class M3File{
    public Header header;
    public string name;
    
    public Header Load(string path){
        var name = path.Split('\\').Last().Split(".")[0];
        Debug.Log(name);
        ReadM3(path);
        this.header.name = name;
        return this.header;
    }
    
    public void ReadM3(string path){
        this.name = Path.GetFileNameWithoutExtension(path);
        byte[] inputData = DataManager.GetFileBytes(path);
        if (inputData == null)
            return;
        Debug.Log("TOTAL FILE SIZE: " + inputData.Length);
        using (MemoryStream ms = new MemoryStream(inputData)){
            using (BinaryReader br = new BinaryReader(ms)){
                ReadHeader(br);
            }
        }
    }
    public void ExportM3(string path, int[] submeshList){
        this.name = Path.GetFileNameWithoutExtension(path);
        byte[] inputData = DataManager.GetFileBytes(path);
        if (inputData == null)
            return;
        Debug.Log("TOTAL FILE SIZE: " + inputData.Length);
        using (MemoryStream ms = new MemoryStream(inputData)){
            using (BinaryReader br = new BinaryReader(ms)){
                ReadHeader(br);
            }
        }
        var location = Path.GetDirectoryName(path);
        var paths = location.Split("\\").ToList();
        paths.RemoveAt(0);
        location = string.Join("/", paths);
        this.SaveAsAsset(location, this.name, submeshList);
    }
    public void SaveAsAsset(string location, string filename, int[] submeshList){
        location = "Assets/Resources/" + location + "/" + filename;
        System.IO.Directory.CreateDirectory(location);

        var gameObject = new GameObject(filename);
        var anim = gameObject.AddComponent<Animation>();
        var rend = gameObject.AddComponent<SkinnedMeshRenderer>();
        Mesh mesh = new Mesh();
        mesh.subMeshCount = submeshList.Length;
        List<Vector3> new_vertices = new List<Vector3>();
        List<Vector3> new_normals = new List<Vector3>();
        List<Vector4> new_tangents = new List<Vector4>();
        List<Vector2> new_uv1 = new List<Vector2>();
        List<Vector2> new_uv2 = new List<Vector2>();
        List<Color> new_color = new List<Color>();
        List<Vector2> new_blendXY = new List<Vector2>();
        List<Vector2> new_blendZW = new List<Vector2>();
        List<Vector4> new_bone_weights = new List<Vector4>();
        List<Vector4> new_bone_index = new List<Vector4>();

        Submesh[] submeshes = new Submesh[submeshList.Length];
        List<int> indices2 = new List<int>();
        var sum_vertices = 0;
        var sum_indices = 0;
        for(var i=0;i<submeshList.Length;i++){
            var submesh_id = submeshList[i];
            for (var j=0; j<this.header.geometry.submesh[submesh_id].nrVertex; j++){
                var vertex_id = j+this.header.geometry.submesh[submesh_id].startVertex;
                new_vertices.Add(this.header.geometry.vertex_positions[vertex_id]);
                new_normals.Add(this.header.geometry.normals[vertex_id]);
                new_tangents.Add(this.header.geometry.tangents[vertex_id]);
                new_uv1.Add(this.header.geometry.uv1[vertex_id]);
                new_uv2.Add(this.header.geometry.uv2[vertex_id]);
                new_color.Add(this.header.geometry.vertexColor0[vertex_id]);
                new_blendXY.Add(this.header.geometry.vertexBlendXY[vertex_id]);
                new_blendZW.Add(this.header.geometry.vertexBlendZW[vertex_id]);
                new_bone_weights.Add(this.header.geometry.bone_weights[vertex_id]);
                new_bone_index.Add(this.header.geometry.bone_index[vertex_id]);
            }
            submeshes[i].startVertex = (uint)sum_vertices;
            submeshes[i].startIndex = (uint)sum_indices;
            sum_vertices += (int)this.header.geometry.submesh[submesh_id].nrVertex;
            sum_indices += (int)this.header.geometry.submesh[submesh_id].nrIndex;
            submeshes[i].nrVertex = (uint)this.header.geometry.submesh[submesh_id].nrVertex;
            submeshes[i].nrIndex = (uint)this.header.geometry.submesh[submesh_id].nrIndex;
            var start_idx = this.header.geometry.submesh[submesh_id].startIndex;
            for(var j=0;j<submeshes[i].nrIndex;j++){
                var xxx = this.header.geometry.indices[start_idx+j];
                indices2.Add(xxx);
            }
        }
        mesh.vertices = new_vertices.ToArray();
        mesh.normals = new_normals.ToArray();
        mesh.tangents = new_tangents.ToArray();
        mesh.uv = new_uv1.ToArray();
        mesh.uv2 = new_uv2.ToArray();
        mesh.colors = new_color.ToArray();
        mesh.uv3 = new_blendXY.ToArray();
        mesh.uv4 = new_blendZW.ToArray();
        for(var i=0;i<submeshes.Length;i++){
            var start_vert = submeshes[i].startVertex;
            var start_index = submeshes[i].startIndex;
            var nr_index = submeshes[i].nrIndex;
            var indices = new int[nr_index];
            for(var ind=0; ind<nr_index; ind++){
                indices[ind] = (int)start_vert + indices2[(int)start_index+ind];
            }
            mesh.SetTriangles(indices, i);
        }
        var materials = this.SaveRenderSubMeshMaterial(location, submeshList);
        rend.sharedMaterials = materials;
        mesh.RecalculateNormals();

        // BONE WEIGHTS
        BoneWeight[] weights = new BoneWeight[new_vertices.Count];
        if(this.header.bones.Length > 0){
            if(this.header.geometry.vertexSize == 20){
                for(var i=0; i<weights.Length; i++){
                    weights[i].boneIndex0 = 0;
                    weights[i].weight0 = 1;
                }
            }else{
                for(var i=0; i<weights.Length; i++){
                    weights[i].boneIndex0 = (int)new_bone_index[i].x;
                    weights[i].boneIndex1 = (int)new_bone_index[i].y;
                    weights[i].boneIndex2 = (int)new_bone_index[i].z;
                    weights[i].boneIndex3 = (int)new_bone_index[i].w;
                    weights[i].weight0 = new_bone_weights[i].x/255.0f;
                    weights[i].weight1 = new_bone_weights[i].y/255.0f;
                    weights[i].weight2 = new_bone_weights[i].z/255.0f;
                    weights[i].weight3 = new_bone_weights[i].w/255.0f;
                }
            }
            mesh.boneWeights = weights;
        }

        // SKELETON
        // Create Bone Transforms and Bind poses
        // One bone at the bottom and one at the top
        Transform[] bones = new Transform[this.header.bones.Length];
        Matrix4x4[] bindPoses = new Matrix4x4[this.header.bones.Length];
        string[] bonePath = new string[this.header.bones.Length];

        for(var i=0; i<this.header.bones.Length; i++){
            var a_bone = this.header.bones[i];
            bonePath[i] = "b_" + i.ToString();

            bones[i] = new GameObject("b_" + i.ToString()).transform;

            
            if(a_bone.parent_id>-1){
                bones[i].parent = bones[a_bone.parent_id].transform;
                bonePath[i] = bonePath[a_bone.parent_id] + "/" + bonePath[i];
            }else{
                bones[i].parent = gameObject.transform;
            }
            Matrix4x4 matrix = a_bone.TM.ConvertOld();
            bones[i].localScale = new Vector3(1,1,1);
            bones[i].rotation = matrix.ExtractRotation();
            bones[i].position = matrix.GetPosition();
            bindPoses[i] = a_bone.InverseTM;// * a_bone.rotationMatrix;
        }
        // K, I probably have butchered all the rules for doing matrix math, but it works.
        // Basically, multiply the bone to have mirror matrix then scale it back, but it is now in reverse orientation, pointing towards opposite direction.
        // To counter this, I rotate it 180 long z and y axis. For some reason, updating / refreshing scene was needed for this to work.
        // This yields the rotation I was looking for (so far it seems to work like intended):
        // def mirrorPose(bone):
        //     m = bone.matrix.Diagonal((-1,1,1,0))
        //     bone.matrix = m @ bone.matrix
        //     bone.scale = (1,1,1)
        //     bpy.context.view_layer.update()
        //     z = mathutils.Euler((0,0,math.radians(180))).to_matrix().to_4x4()
        //     bone.matrix = bone.matrix @ z
        //     bpy.context.view_layer.update()
        //     y = mathutils.Euler((0,math.radians(180),0)).to_matrix().to_4x4()
        //     bone.matrix = bone.matrix @ y


        // assign the bindPoses array to the bindposes array which is part of the mesh.
        mesh.bindposes = bindPoses;

        // Assign bones and bind poses
        rend.bones = bones;
        rend.rootBone = bones[0];
        rend.sharedMesh = mesh;

        
        // Create the clip with the curve
        AnimationClip clip = new AnimationClip();
        // Assign a simple waving animation to the bottom bone
        for(var bone_id=0; bone_id<this.header.bones.Length; bone_id++){
            var aa_bone = this.header.bones[bone_id];
            AnimationCurve curveSX = new AnimationCurve();
            AnimationCurve curveSY = new AnimationCurve();
            AnimationCurve curveSZ = new AnimationCurve();
            for(var j=0; j<aa_bone.timestamps1.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps1.keyFrames[j];
                curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
            }
            for(var j=0; j<aa_bone.timestamps3.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps3.keyFrames[j];
                curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
            }
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.x", curveSX);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.y", curveSY);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.z", curveSZ);

            AnimationCurve curveRX = new AnimationCurve();
            AnimationCurve curveRY = new AnimationCurve();
            AnimationCurve curveRZ = new AnimationCurve();
            AnimationCurve curveRW = new AnimationCurve();
            for(var j=0; j<aa_bone.timestamps5.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps5.keyFrames[j];
                curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
            }
            for(var j=0; j<aa_bone.timestamps6.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps6.keyFrames[j];
                curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
            }
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.x", curveRX);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.y", curveRY);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.z", curveRZ);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.w", curveRW);
            
            AnimationCurve curveTX = new AnimationCurve();
            AnimationCurve curveTY = new AnimationCurve();
            AnimationCurve curveTZ = new AnimationCurve();
            for(var j=0; j<aa_bone.timestamps7.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps7.keyFrames[j];
                curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
            }
            for(var j=0; j<aa_bone.timestamps8.keyFrames.Length; j++){
                var a_key = aa_bone.timestamps8.keyFrames[j];
                curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
            }
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.x", curveTX);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.y", curveTY);
            clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.z", curveTZ);
            
            clip.legacy = true;
            clip.wrapMode = WrapMode.Loop;
        }
        // Add and play the clip
        AssetDatabase.CreateAsset(clip, location + "/" + filename + ".anim");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var clip2 = Resources.Load("clip") as AnimationClip;
        anim.clip = clip;
        // anim.AddClip(clip, "test");
        // anim.Play("test");

        AssetDatabase.CreateAsset(mesh, location + "/" + filename + ".mesh");
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, location + "/" + filename + ".prefab", InteractionMode.AutomatedAction);
        anim = gameObject.GetComponent<Animation>();
        PrefabUtility.RecordPrefabInstancePropertyModifications(anim);
        AssetDatabase.SaveAssets();
        Debug.Log("DONE");
    }

    public void DrawSingleSubmesh(int submesh_id, string submesh_name){
        var gameObject = new GameObject(submesh_name);
        var anim = gameObject.AddComponent<Animation>();
        Mesh mesh = new Mesh();
        mesh.subMeshCount = this.header.geometry.submesh.Length;
        mesh.vertices = this.header.geometry.vertex_positions;
        mesh.normals = this.header.geometry.normals;
        mesh.tangents = this.header.geometry.tangents;
        mesh.uv = this.header.geometry.uv1;
        mesh.uv2 = this.header.geometry.uv2;
        mesh.colors = this.header.geometry.vertexColor0;
        mesh.uv3 = this.header.geometry.vertexBlendXY;
        mesh.uv4 = this.header.geometry.vertexBlendZW;
        Material[] materials = new Material[this.header.geometry.submesh.Length];
        for(var i=0;i<this.header.geometry.submesh.Length;i++){
            if(i != submesh_id){
                continue;
            }
            var start_vert = this.header.geometry.submesh[i].startVertex;
            var start_index = this.header.geometry.submesh[i].startIndex;
            var nr_index = this.header.geometry.submesh[i].nrIndex;
            var indices = new int[nr_index];
            for(var ind=0; ind<nr_index; ind++){
                indices[ind] = (int)start_vert + this.header.geometry.indices[start_index+ind];
            }
            mesh.SetTriangles(indices, i);

            this.RenderSubMeshMaterial(materials, i);
        }
        mesh.RecalculateNormals();

        if(this.header.nrModelAnimations > 0){
            // BONE WEIGHTS
            BoneWeight[] weights = new BoneWeight[this.header.geometry.vertex_positions.Length];
            if(this.header.bones.Length > 0){
                if(this.header.bones.Length == 1){
                    for(var i=0; i<weights.Length; i++){
                        weights[i].boneIndex0 = 0;//this.header.geometry.bone_index[i][0];
                        weights[i].weight0 = 1;//this.header.geometry.bone_weights[i][0]/255.0f;
                    }
                }else{
                    for(var i=0; i<weights.Length; i++){
                        weights[i].boneIndex0 = (int)this.header.geometry.bone_index[i].x;
                        weights[i].boneIndex1 = (int)this.header.geometry.bone_index[i].y;
                        weights[i].boneIndex2 = (int)this.header.geometry.bone_index[i].z;
                        weights[i].boneIndex3 = (int)this.header.geometry.bone_index[i].w;
                        weights[i].weight0 = this.header.geometry.bone_weights[i].x/255.0f;
                        weights[i].weight1 = this.header.geometry.bone_weights[i].y/255.0f;
                        weights[i].weight2 = this.header.geometry.bone_weights[i].z/255.0f;
                        weights[i].weight3 = this.header.geometry.bone_weights[i].w/255.0f;
                    }
                }
                mesh.boneWeights = weights;
            }


            // SKELETON
            // Create Bone Transforms and Bind poses
            // One bone at the bottom and one at the top
            Transform[] bones = new Transform[this.header.bones.Length];
            Matrix4x4[] bindPoses = new Matrix4x4[this.header.bones.Length];
            string[] bonePath = new string[this.header.bones.Length];

            for(var i=0; i<this.header.bones.Length; i++){
                var a_bone = this.header.bones[i];
                // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.parent = gameObject.transform;
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = a_bone.position;
                bonePath[i] = "b_" + i.ToString();

                bones[i] = new GameObject("b_" + i.ToString()).transform;

                
                if(a_bone.parent_id>-1){
                    bones[i].parent = bones[a_bone.parent_id].transform;
                    bonePath[i] = bonePath[a_bone.parent_id] + "/" + bonePath[i];
                    // bindPoses[i] = bones[i].parent.localToWorldMatrix*bindPoses[i];
                }else{
                    bones[i].parent = gameObject.transform;
                }
                Matrix4x4 matrix = a_bone.TM.ConvertOld();
                // Debug.Log("b_" + i.ToString() + ": " + matrix.ValidTRS());
                bones[i].localScale = new Vector3(1,1,1);
                bones[i].rotation = matrix.ExtractRotation();
                bones[i].position = matrix.GetPosition();
                
                // Set the position relative to the parent
                // bones[i].localRotation = Quaternion.identity;
                // bones[i].localPosition = a_bone.position;

                // bones[i].worldToLocalMatrix
                // gameObject.transform.localToWorldMatrix*
                // a_bone.TM*bones[i].localToWorldMatrix
                // matrix.inverse
                bindPoses[i] = a_bone.InverseTM;
                
                // Debug.Log(i + " " + matrix + " | " + bindPoses[i]);
                // bindPoses[i] = a_bone.TM;
            }
            // assign the bindPoses array to the bindposes array which is part of the mesh.
            mesh.bindposes = bindPoses;

            var rend = gameObject.AddComponent<SkinnedMeshRenderer>();
            rend.sharedMaterials = materials;
            // Assign bones and bind poses
            rend.bones = bones;
            rend.rootBone = bones[0];
            rend.sharedMesh = mesh;

            
            // Create the clip with the curve
            AnimationClip clip = new AnimationClip();
            // Assign a simple waving animation to the bottom bone
            for(var bone_id=0; bone_id<this.header.bones.Length; bone_id++){
            //     // var bone_id = 2;
                var aa_bone = this.header.bones[bone_id];
                AnimationCurve curveSX = new AnimationCurve();
                AnimationCurve curveSY = new AnimationCurve();
                AnimationCurve curveSZ = new AnimationCurve();
                for(var j=0; j<aa_bone.timestamps1.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps1.keyFrames[j];
                    curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                    curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                    curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
                }
                for(var j=0; j<aa_bone.timestamps3.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps3.keyFrames[j];
                    curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                    curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                    curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
                }
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.x", curveSX);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.y", curveSY);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.z", curveSZ);

                AnimationCurve curveRX = new AnimationCurve();
                AnimationCurve curveRY = new AnimationCurve();
                AnimationCurve curveRZ = new AnimationCurve();
                AnimationCurve curveRW = new AnimationCurve();
                // if(aa_bone.id == 2){
                //     aa_bone.timestamps5.keyFrames[0].Print();
                //     aa_bone.timestamps6.keyFrames[0].Print();
                //     aa_bone.timestamps5.keyFrames[0].Print2();
                //     aa_bone.timestamps6.keyFrames[0].Print2();
                    for(var j=0; j<aa_bone.timestamps5.keyFrames.Length; j++){
                        var a_key = aa_bone.timestamps5.keyFrames[j];
                        // var sum = a_key.funk1*a_key.funk1 + a_key.funk2*a_key.funk2 + a_key.funk3*a_key.funk3 + a_key.funk4*a_key.funk4;
                        // Debug.Log(sum);
                        // new Quaternion(a_key.funk1, a_key.funk2, a_key.funk3, a_key.funk4);
                        // if(bone_id == 43){
                        //     // var mt = Matrix4x4.identity;
                        //     // mt[0,0] = -1;
                        //     a_key.q.y = a_key.q.y*-1;
                        //     a_key.q.z = a_key.q.z*-1;
                        // }
                        curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                        curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                        curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                        curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
                    }
                    for(var j=0; j<aa_bone.timestamps6.keyFrames.Length; j++){
                        var a_key = aa_bone.timestamps6.keyFrames[j];
                        curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                        curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                        curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                        curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
                    }
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.x", curveRX);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.y", curveRY);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.z", curveRZ);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.w", curveRW);
                // }
                
                AnimationCurve curveTX = new AnimationCurve();
                AnimationCurve curveTY = new AnimationCurve();
                AnimationCurve curveTZ = new AnimationCurve();
                for(var j=0; j<aa_bone.timestamps7.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps7.keyFrames[j];
                    curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                    curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                    curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
                }
                for(var j=0; j<aa_bone.timestamps8.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps8.keyFrames[j];
                    curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                    curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                    curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
                }
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.x", curveTX);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.y", curveTY);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.z", curveTZ);
                
                clip.legacy = true;
                clip.wrapMode = WrapMode.Loop;
            }
            // Add and play the clip
            anim.AddClip(clip, "test");
            // anim.Play("test");
        }else{
            var rend = gameObject.AddComponent<MeshRenderer>();
            rend.sharedMaterials = materials;
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }

    }
    public void RenderM3(){
        var gameObject = new GameObject("TestObject");
        var anim = gameObject.AddComponent<Animation>();
        Mesh mesh = new Mesh();
        mesh.subMeshCount = this.header.geometry.submesh.Length;
        mesh.vertices = this.header.geometry.vertex_positions;
        mesh.normals = this.header.geometry.normals;
        mesh.tangents = this.header.geometry.tangents;
        mesh.uv = this.header.geometry.uv1;
        mesh.uv2 = this.header.geometry.uv2;
        mesh.colors = this.header.geometry.vertexColor0;
        mesh.uv3 = this.header.geometry.vertexBlendXY;
        mesh.uv4 = this.header.geometry.vertexBlendZW;
        Material[] materials = new Material[this.header.geometry.submesh.Length];
        for(var i=0;i<this.header.geometry.submesh.Length;i++){
            var start_vert = this.header.geometry.submesh[i].startVertex;
            var start_index = this.header.geometry.submesh[i].startIndex;
            var nr_index = this.header.geometry.submesh[i].nrIndex;
            var indices = new int[nr_index];
            for(var ind=0; ind<nr_index; ind++){
                indices[ind] = (int)start_vert + this.header.geometry.indices[start_index+ind];
            }
            mesh.SetTriangles(indices, i);

            this.RenderSubMeshMaterial(materials, i);
        }
        mesh.RecalculateNormals();

        if(this.header.nrModelAnimations > 0){
            // BONE WEIGHTS
            BoneWeight[] weights = new BoneWeight[this.header.geometry.vertex_positions.Length];
            if(this.header.bones.Length > 0){
                if(this.header.geometry.vertexSize == 20){
                    for(var i=0; i<weights.Length; i++){
                        weights[i].boneIndex0 = 0;
                        weights[i].weight0 = 1;
                    }
                }else{
                    for(var i=0; i<weights.Length; i++){
                        weights[i].boneIndex0 = (int)this.header.geometry.bone_index[i].x;
                        weights[i].boneIndex1 = (int)this.header.geometry.bone_index[i].y;
                        weights[i].boneIndex2 = (int)this.header.geometry.bone_index[i].z;
                        weights[i].boneIndex3 = (int)this.header.geometry.bone_index[i].w;
                        weights[i].weight0 = this.header.geometry.bone_weights[i].x/255.0f;
                        weights[i].weight1 = this.header.geometry.bone_weights[i].y/255.0f;
                        weights[i].weight2 = this.header.geometry.bone_weights[i].z/255.0f;
                        weights[i].weight3 = this.header.geometry.bone_weights[i].w/255.0f;
                    }
                }
                mesh.boneWeights = weights;
            }


            // SKELETON
            // Create Bone Transforms and Bind poses
            // One bone at the bottom and one at the top
            Transform[] bones = new Transform[this.header.bones.Length];
            Matrix4x4[] bindPoses = new Matrix4x4[this.header.bones.Length];
            string[] bonePath = new string[this.header.bones.Length];

            for(var i=0; i<this.header.bones.Length; i++){
                var a_bone = this.header.bones[i];
                // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.parent = gameObject.transform;
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = a_bone.position;
                bonePath[i] = "b_" + i.ToString();

                bones[i] = new GameObject("b_" + i.ToString()).transform;

                
                if(a_bone.parent_id>-1){
                    bones[i].parent = bones[a_bone.parent_id].transform;
                    bonePath[i] = bonePath[a_bone.parent_id] + "/" + bonePath[i];
                    // bindPoses[i] = bones[i].parent.localToWorldMatrix*bindPoses[i];
                }else{
                    bones[i].parent = gameObject.transform;
                }
                Matrix4x4 matrix = a_bone.TM.ConvertOld();
                // Debug.Log("b_" + i.ToString() + ": " + matrix.ValidTRS());
                bones[i].localScale = new Vector3(1,1,1);
                bones[i].rotation = matrix.ExtractRotation();
                bones[i].position = matrix.GetPosition();
                
                // Set the position relative to the parent
                // bones[i].localRotation = Quaternion.identity;
                // bones[i].localPosition = a_bone.position;

                // bones[i].worldToLocalMatrix
                // gameObject.transform.localToWorldMatrix*
                // a_bone.TM*bones[i].localToWorldMatrix
                // matrix.inverse
                bindPoses[i] = a_bone.InverseTM;
                
                // Debug.Log(i + " " + matrix + " | " + bindPoses[i]);
                // bindPoses[i] = a_bone.TM;
            }
            // assign the bindPoses array to the bindposes array which is part of the mesh.
            mesh.bindposes = bindPoses;

            var rend = gameObject.AddComponent<SkinnedMeshRenderer>();
            rend.sharedMaterials = materials;
            // Assign bones and bind poses
            rend.bones = bones;
            rend.rootBone = bones[0];
            rend.sharedMesh = mesh;

            
            // Create the clip with the curve
            AnimationClip clip = new AnimationClip();
            // Assign a simple waving animation to the bottom bone
            for(var bone_id=0; bone_id<this.header.bones.Length; bone_id++){
            //     // var bone_id = 2;
                var aa_bone = this.header.bones[bone_id];
                AnimationCurve curveSX = new AnimationCurve();
                AnimationCurve curveSY = new AnimationCurve();
                AnimationCurve curveSZ = new AnimationCurve();
                for(var j=0; j<aa_bone.timestamps1.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps1.keyFrames[j];
                    curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                    curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                    curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
                }
                for(var j=0; j<aa_bone.timestamps3.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps3.keyFrames[j];
                    curveSX.AddKey(new Keyframe(a_key.timeStamp, a_key.s.x, 0, 0));
                    curveSY.AddKey(new Keyframe(a_key.timeStamp, a_key.s.y, 0, 0));
                    curveSZ.AddKey(new Keyframe(a_key.timeStamp, a_key.s.z, 0, 0));
                }
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.x", curveSX);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.y", curveSY);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalScale.z", curveSZ);

                AnimationCurve curveRX = new AnimationCurve();
                AnimationCurve curveRY = new AnimationCurve();
                AnimationCurve curveRZ = new AnimationCurve();
                AnimationCurve curveRW = new AnimationCurve();
                // if(aa_bone.id == 2){
                //     aa_bone.timestamps5.keyFrames[0].Print();
                //     aa_bone.timestamps6.keyFrames[0].Print();
                //     aa_bone.timestamps5.keyFrames[0].Print2();
                //     aa_bone.timestamps6.keyFrames[0].Print2();
                    for(var j=0; j<aa_bone.timestamps5.keyFrames.Length; j++){
                        var a_key = aa_bone.timestamps5.keyFrames[j];
                        // var sum = a_key.funk1*a_key.funk1 + a_key.funk2*a_key.funk2 + a_key.funk3*a_key.funk3 + a_key.funk4*a_key.funk4;
                        // Debug.Log(sum);
                        // new Quaternion(a_key.funk1, a_key.funk2, a_key.funk3, a_key.funk4);
                        // if(bone_id == 43){
                        //     // var mt = Matrix4x4.identity;
                        //     // mt[0,0] = -1;
                        //     a_key.q.y = a_key.q.y*-1;
                        //     a_key.q.z = a_key.q.z*-1;
                        // }
                        curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                        curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                        curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                        curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
                    }
                    for(var j=0; j<aa_bone.timestamps6.keyFrames.Length; j++){
                        var a_key = aa_bone.timestamps6.keyFrames[j];
                        curveRX.AddKey(new Keyframe(a_key.timeStamp, a_key.q.x, 0, 0));
                        curveRY.AddKey(new Keyframe(a_key.timeStamp, a_key.q.y, 0, 0));
                        curveRZ.AddKey(new Keyframe(a_key.timeStamp, a_key.q.z, 0, 0));
                        curveRW.AddKey(new Keyframe(a_key.timeStamp, a_key.q.w, 0, 0));
                    }
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.x", curveRX);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.y", curveRY);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.z", curveRZ);
                    clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalRotation.w", curveRW);
                // }
                
                AnimationCurve curveTX = new AnimationCurve();
                AnimationCurve curveTY = new AnimationCurve();
                AnimationCurve curveTZ = new AnimationCurve();
                for(var j=0; j<aa_bone.timestamps7.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps7.keyFrames[j];
                    curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                    curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                    curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
                }
                for(var j=0; j<aa_bone.timestamps8.keyFrames.Length; j++){
                    var a_key = aa_bone.timestamps8.keyFrames[j];
                    curveTX.AddKey(new Keyframe(a_key.timeStamp, a_key.funk1, 0, 0));
                    curveTY.AddKey(new Keyframe(a_key.timeStamp, a_key.funk2, 0, 0));
                    curveTZ.AddKey(new Keyframe(a_key.timeStamp, a_key.funk3, 0, 0));
                }
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.x", curveTX);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.y", curveTY);
                clip.SetCurve(bonePath[bone_id], typeof(Transform), "m_LocalPosition.z", curveTZ);
                
                clip.legacy = true;
                clip.wrapMode = WrapMode.Loop;
            }
            // Add and play the clip
            anim.AddClip(clip, "test");
            // anim.Play("test");
        }else{
            var rend = gameObject.AddComponent<MeshRenderer>();
            rend.sharedMaterials = materials;
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        }
    }
    public Material[] SaveRenderSubMeshMaterial(string location, int[] submeshList){
        var paths = location.Split("/").ToList();
        paths.RemoveAt(0);
        paths.RemoveAt(0);
        var loadLocation = string.Join("/", paths);
        Material[] materials = new Material[submeshList.Length];
        for(var i=0;i<submeshList.Length;i++){
            var submesh_id = submeshList[i];
            var a_mat = header.materials[this.header.geometry.submesh[submesh_id].material_id];
            materials[i] = new Material(Shader.Find("Wildstar/ModelShader"));
            var texture = new Texture2D(1, 1);
            Vector4 cb1_0, cb1_4, cb1_8,  cb1_12, cb1_16, cb1_20;
            Vector4 cb1_1, cb1_5, cb1_9,  cb1_13, cb1_17, cb1_21;
            Vector4 cb1_2, cb1_6, cb1_10, cb1_14, cb1_18, cb1_22;
            Vector4 cb1_3, cb1_7, cb1_11, cb1_15, cb1_19, cb1_23;
            Vector4 cb1_24, cb1_25, cb1_28;
            Vector4 cb2_0, cb2_1, cb2_2, cb2_3, cb2_4, cb2_5, cb2_6, cb2_7, cb2_8, cb2_9, cb2_10;
            cb2_0  = new Vector4(0,0,0,1);
            cb2_1  = new Vector4(1,0,0,1);
            cb2_2  = new Vector4(1,0,0,0);
            cb2_3  = new Vector4(0,0,0,1);
            cb2_4  = new Vector4(0,0,0,0);
            cb2_5  = new Vector4(0,0,0,0);
            cb2_6  = new Vector4(1.13725f, 1.21569f, 1.13725f, 1);
            cb2_7  = new Vector4(1.13725f, 1.21569f, 1.13725f, 1);
            cb2_8  = new Vector4(0,0,0,0);
            cb2_9  = new Vector4(0,0,0,0);
            cb2_10  = new Vector4(0.00001f,0,0,0);
            if(a_mat.unk20 > 0){
                cb2_1  = new Vector4(1,0.5f,0,1);
            }


            cb1_0  = new Vector4(0,0,0,0);
            cb1_1  = new Vector4(0,0,0,0);
            cb1_2  = new Vector4(0,0,0,0);
            cb1_3  = new Vector4(0,0,0,0);
            cb1_8  = new Vector4(0,0,0,0);
            cb1_9  = new Vector4(0,0,0,0);
            cb1_10 = new Vector4(0,0,0,0);
            cb1_11 = new Vector4(0,0,0,0);
            cb1_24 = new Vector4(0,0,0,0);
            cb1_25 = new Vector4(0,0,0,0);
            cb1_28 = new Vector4(a_mat.cb1_28_x,a_mat.cb1_28_y,0,1);

            cb1_4  = new Vector4(0,0,0,1);  // alpha layer in normal map
            cb1_12 = new Vector4(0,0,0,0);
            cb1_16 = new Vector4(0,0,0,0);
            cb1_20 = new Vector4(0,0,0,0);
            if(a_mat.materialDescriptions.Length >= 1){
                cb1_20 = new Vector4(1,1,1,1);
                cb1_24.x = 1;
                cb1_25.x = 0.001f;
                if(a_mat.materialDescriptions[0].textureSelectorA == -1){
                    texture = Texture2D.whiteTexture;
                }else{
                    texture = this._getTexture(a_mat.materialDescriptions[0].textureColor, true);
                    materials[i].SetTexture("_Color0", texture);
                }
                if(a_mat.materialDescriptions[0].textureSelectorB == -1){
                    cb1_0 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                    cb1_8 = new Vector4(0,0,0,1);
                }else{
                    if(a_mat.materialDescriptions[0].unk4 == 5){
                        cb1_0 = new Vector4(0,0,0,1);
                        cb1_8 = new Vector4(0,0,1,0);
                    }else if(a_mat.materialDescriptions[0].unk4 == 1){
                        cb1_0 = new Vector4(0,0,1,0);
                        cb1_8 = new Vector4(1,0,0,0);
                    }
                    texture = this._getTexture(a_mat.materialDescriptions[0].textureNormal, true);
                    materials[i].SetTexture("_Normal0", texture);
                }
                if(a_mat.materialDescriptions[0].unk2 == 1){
                    cb1_4  = new Vector4(1,0,0,0);
                }
            }
            cb1_5  = new Vector4(0,0,0,1);  // alpha layer in normal map
            cb1_13 = new Vector4(0,0,0,0);
            cb1_17 = new Vector4(0,0,0,0);
            cb1_21 = new Vector4(0,0,0,0);
            if(a_mat.materialDescriptions.Length >= 2){
                cb1_21 = new Vector4(1,1,1,1);
                cb1_24.y = 1;
                cb1_25.y = 0.001f;
                if(a_mat.materialDescriptions[1].textureSelectorA == -1){
                    texture = Texture2D.whiteTexture;
                }else{
                    texture = this._getTexture(a_mat.materialDescriptions[1].textureColor, true);
                    materials[i].SetTexture("_Color1", texture);
                }
                if(a_mat.materialDescriptions[1].textureSelectorB == -1){
                    cb1_1 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                    cb1_9 = new Vector4(0,0,0,1);
                }else{
                    if(a_mat.materialDescriptions[1].unk4 == 5){
                        cb1_1 = new Vector4(0,0,0,1);
                        cb1_9 = new Vector4(0,0,1,0);
                    }else if(a_mat.materialDescriptions[1].unk4 == 1){
                        cb1_1 = new Vector4(0,0,1,0);
                        cb1_9 = new Vector4(1,0,0,0);
                    }
                    texture = this._getTexture(a_mat.materialDescriptions[1].textureNormal, true);
                    materials[i].SetTexture("_Normal1", texture);
                }
                if(a_mat.materialDescriptions[1].unk2 == 1){
                    cb1_5  = new Vector4(1,0,0,0);
                }
            }
            cb1_6  = new Vector4(0,0,0,1);  // alpha layer in normal map
            cb1_14 = new Vector4(0,0,0,0);
            cb1_18 = new Vector4(0,0,0,0);
            cb1_22 = new Vector4(0,0,0,0);
            if(a_mat.materialDescriptions.Length >= 3){
                cb1_22 = new Vector4(1,1,1,1);
                cb1_24.z = 1;
                cb1_25.z = 0.001f;
                if(a_mat.materialDescriptions[2].textureSelectorA == -1){
                    texture = Texture2D.whiteTexture;
                }else{
                    texture = this._getTexture(a_mat.materialDescriptions[2].textureColor, true);
                    materials[i].SetTexture("_Color2", texture);
                }
                if(a_mat.materialDescriptions[2].textureSelectorB == -1){
                    cb1_2 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                    cb1_10 = new Vector4(0,0,0,1);
                }else{
                    if(a_mat.materialDescriptions[2].unk4 == 5){
                        cb1_2 = new Vector4(0,0,0,1);
                        cb1_10 = new Vector4(0,0,1,0);
                    }else if(a_mat.materialDescriptions[2].unk4 == 1){
                        cb1_2 = new Vector4(0,0,1,0);
                        cb1_10 = new Vector4(1,0,0,0);
                    }
                    texture = this._getTexture(a_mat.materialDescriptions[2].textureNormal, true);
                    materials[i].SetTexture("_Normal2", texture);
                }
                if(a_mat.materialDescriptions[2].unk2 == 1){
                    cb1_6 = new Vector4(1,0,0,0);
                }
            }
            cb1_7  = new Vector4(0,0,0,1);  // alpha layer in normal map
            cb1_15 = new Vector4(0,0,0,0);
            cb1_19 = new Vector4(0,0,0,0);
            cb1_23 = new Vector4(0,0,0,0);
            if(a_mat.materialDescriptions.Length >= 4){
                cb1_23 = new Vector4(1,1,1,1);
                cb1_24.w = 1;
                cb1_25.w = 0.001f;
                if(a_mat.materialDescriptions[3].textureSelectorA == -1){
                    texture = Texture2D.whiteTexture;
                }else{
                    texture = this._getTexture(a_mat.materialDescriptions[3].textureColor, true);
                    materials[i].SetTexture("_Color3", texture);
                }
                if(a_mat.materialDescriptions[3].textureSelectorB == -1){
                    cb1_3 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                    cb1_11 = new Vector4(0,0,0,1);
                }else{
                    if(a_mat.materialDescriptions[3].unk4 == 5){
                        cb1_3 = new Vector4(0,0,0,1);
                        cb1_11 = new Vector4(0,0,1,0);
                    }else if(a_mat.materialDescriptions[3].unk4 == 1){
                        cb1_3 = new Vector4(0,0,1,0);
                        cb1_11 = new Vector4(1,0,0,0);
                    }
                    texture = this._getTexture(a_mat.materialDescriptions[3].textureNormal, true);
                    materials[i].SetTexture("_Normal3", texture);
                }
                if(a_mat.materialDescriptions[3].unk2 == 1){
                    cb1_7 = new Vector4(1,0,0,0);
                }
            }
            materials[i].renderQueue = 2450;
            // Debug.Log("cb1_0:  " + cb1_0);
            // Debug.Log("cb1_1:  " + cb1_1);
            // Debug.Log("cb1_2:  " + cb1_2);
            // Debug.Log("cb1_3:  " + cb1_3);
            // Debug.Log("cb1_4:  " + cb1_4);
            // Debug.Log("cb1_5:  " + cb1_5);
            // Debug.Log("cb1_6:  " + cb1_6);
            // Debug.Log("cb1_7:  " + cb1_7);
            // Debug.Log("cb1_8:  " + cb1_8);
            // Debug.Log("cb1_9:  " + cb1_9);
            // Debug.Log("cb1_10: " + cb1_10);
            // Debug.Log("cb1_11: " + cb1_11);
            // Debug.Log("cb1_12: " + cb1_12);
            // Debug.Log("cb1_13: " + cb1_13);
            // Debug.Log("cb1_14: " + cb1_14);
            // Debug.Log("cb1_15: " + cb1_15);
            // Debug.Log("cb1_16: " + cb1_16);
            // Debug.Log("cb1_17: " + cb1_17);
            // Debug.Log("cb1_18: " + cb1_18);
            // Debug.Log("cb1_19: " + cb1_19);
            // Debug.Log("cb1_20: " + cb1_20);
            // Debug.Log("cb1_21: " + cb1_21);
            // Debug.Log("cb1_22: " + cb1_22);
            // Debug.Log("cb1_23: " + cb1_23);
            // Debug.Log("cb1_24: " + cb1_24);
            // Debug.Log("cb1_25: " + cb1_25);
            // Debug.Log("cb1_25: " + cb1_28);

            materials[i].SetVector("_cb1_0", cb1_0);
            materials[i].SetVector("_cb1_1", cb1_1);
            materials[i].SetVector("_cb1_2", cb1_2);
            materials[i].SetVector("_cb1_3", cb1_3);
            materials[i].SetVector("_cb1_4", cb1_4);
            materials[i].SetVector("_cb1_5", cb1_5);
            materials[i].SetVector("_cb1_6", cb1_6);
            materials[i].SetVector("_cb1_7", cb1_7);
            materials[i].SetVector("_cb1_8", cb1_8);
            materials[i].SetVector("_cb1_9", cb1_9);
            materials[i].SetVector("_cb1_10", cb1_10);
            materials[i].SetVector("_cb1_11", cb1_11);
            materials[i].SetVector("_cb1_12", cb1_12);
            materials[i].SetVector("_cb1_13", cb1_13);
            materials[i].SetVector("_cb1_14", cb1_14);
            materials[i].SetVector("_cb1_15", cb1_15);
            materials[i].SetVector("_cb1_16", cb1_16);
            materials[i].SetVector("_cb1_17", cb1_17);
            materials[i].SetVector("_cb1_18", cb1_18);
            materials[i].SetVector("_cb1_19", cb1_19);
            materials[i].SetVector("_cb1_20", cb1_20);
            materials[i].SetVector("_cb1_21", cb1_21);
            materials[i].SetVector("_cb1_22", cb1_22);
            materials[i].SetVector("_cb1_23", cb1_23);
            materials[i].SetVector("_cb1_24", cb1_24);
            materials[i].SetVector("_cb1_25", cb1_25);
            materials[i].SetVector("_cb1_28", cb1_28);
            
            materials[i].SetVector("_cb2_0", cb2_0);
            materials[i].SetVector("_cb2_1", cb2_1);
            materials[i].SetVector("_cb2_2", cb2_2);
            materials[i].SetVector("_cb2_3", cb2_3);
            materials[i].SetVector("_cb2_4", cb2_4);
            materials[i].SetVector("_cb2_5", cb2_5);
            materials[i].SetVector("_cb2_6", cb2_6);
            materials[i].SetVector("_cb2_7", cb2_7);
            materials[i].SetVector("_cb2_8", cb2_8);
            materials[i].SetVector("_cb2_9", cb2_9);
            materials[i].SetVector("_cb2_10", cb2_10);
            AssetDatabase.CreateAsset(materials[i], location + "/material_" + submesh_id.ToString() + ".mat");

        }
        return materials;
    }
    private Texture2D _getTexture(string readLocation, bool save){
        var fileName = Path.GetFileNameWithoutExtension(readLocation);
        var saveLocation = Path.GetDirectoryName(readLocation);
        System.IO.Directory.CreateDirectory("Assets/Resources/" + saveLocation);
        if (!save){
            return new Tex().DecodeTexture(@"AIDX\" + readLocation);
        }
        // Debug.Log(readLocation);
        var bytes = new Tex().DecodeTexture(@"AIDX\" + readLocation).EncodeToPNG();
        File.WriteAllBytes("Assets/Resources/" + saveLocation + "/" + fileName + ".png", bytes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return Resources.Load(saveLocation + "/" + fileName) as Texture2D;
    }
    public void RenderSubMeshMaterial(Material[] materials, int submesh_id){
        var a_mat = header.materials[this.header.geometry.submesh[submesh_id].material_id];
        materials[submesh_id] = new Material(Shader.Find("Wildstar/ModelShader"));
        var texture = new Texture2D(1, 1);
        Vector4 cb1_0, cb1_4, cb1_8,  cb1_12, cb1_16, cb1_20;
        Vector4 cb1_1, cb1_5, cb1_9,  cb1_13, cb1_17, cb1_21;
        Vector4 cb1_2, cb1_6, cb1_10, cb1_14, cb1_18, cb1_22;
        Vector4 cb1_3, cb1_7, cb1_11, cb1_15, cb1_19, cb1_23;
        Vector4 cb1_24, cb1_25, cb1_28;
        Vector4 cb2_0, cb2_1, cb2_2, cb2_3, cb2_4, cb2_5, cb2_6, cb2_7, cb2_8, cb2_9, cb2_10;
        cb2_0  = new Vector4(0,0,0,1);
        cb2_1  = new Vector4(1,0,0,1);
        cb2_2  = new Vector4(1,0,0,0);
        cb2_3  = new Vector4(0,0,0,1);
        cb2_4  = new Vector4(0,0,0,0);
        cb2_5  = new Vector4(0,0,0,0);
        cb2_6  = new Vector4(1.13725f, 1.21569f, 1.13725f, 1);
        cb2_7  = new Vector4(1.13725f, 1.21569f, 1.13725f, 1);
        cb2_8  = new Vector4(0,0,0,0);
        cb2_9  = new Vector4(0,0,0,0);
        cb2_10  = new Vector4(0.00001f,0,0,0);
        if(a_mat.unk20 > 0){
            cb2_1  = new Vector4(1,0.5f,0,1);
        }

        cb1_0  = new Vector4(0,0,0,0);
        cb1_1  = new Vector4(0,0,0,0);
        cb1_2  = new Vector4(0,0,0,0);
        cb1_3  = new Vector4(0,0,0,0);
        cb1_8  = new Vector4(0,0,0,0);
        cb1_9  = new Vector4(0,0,0,0);
        cb1_10 = new Vector4(0,0,0,0);
        cb1_11 = new Vector4(0,0,0,0);
        cb1_24 = new Vector4(0,0,0,0);
        cb1_25 = new Vector4(0,0,0,0);

        cb1_4  = new Vector4(0,0,0,1);  // alpha layer in normal map
        cb1_12 = new Vector4(0,0,0,0);
        cb1_16 = new Vector4(0,0,0,0);
        cb1_20 = new Vector4(0,0,0,0);
        if(a_mat.materialDescriptions.Length >= 1){
            cb1_20 = new Vector4(1,1,1,1);
            cb1_24.x = 1;
            cb1_25.x = 0.001f;
            if(a_mat.materialDescriptions[0].textureSelectorA == -1){
                texture = Texture2D.whiteTexture;
            }else{
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[0].textureColor);
                materials[submesh_id].SetTexture("_Color0", texture);
            }
            if(a_mat.materialDescriptions[0].textureSelectorB == -1){
                cb1_0 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                cb1_8 = new Vector4(0,0,0,1);
            }else{
                if(a_mat.materialDescriptions[0].unk4 == 5){
                    cb1_0 = new Vector4(0,0,0,1);
                    cb1_8 = new Vector4(0,0,1,0);
                }else if(a_mat.materialDescriptions[0].unk4 == 1){
                    cb1_0 = new Vector4(0,0,1,0);
                    cb1_8 = new Vector4(1,0,0,0);
                }
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[0].textureNormal);
                materials[submesh_id].SetTexture("_Normal0", texture);
            }
            if(a_mat.materialDescriptions[0].unk2 == 1){
                cb1_4  = new Vector4(1,0,0,0);
            }
        }
        cb1_5  = new Vector4(0,0,0,1);  // alpha layer in normal map
        cb1_13 = new Vector4(0,0,0,0);
        cb1_17 = new Vector4(0,0,0,0);
        cb1_21 = new Vector4(0,0,0,0);
        if(a_mat.materialDescriptions.Length >= 2){
            cb1_21 = new Vector4(1,1,1,1);
            cb1_24.y = 1;
            cb1_25.y = 0.001f;
            if(a_mat.materialDescriptions[1].textureSelectorA == -1){
                texture = Texture2D.whiteTexture;
            }else{
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[1].textureColor);
                materials[submesh_id].SetTexture("_Color1", texture);
            }
            if(a_mat.materialDescriptions[1].textureSelectorB == -1){
                cb1_1 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                cb1_9 = new Vector4(0,0,0,1);
            }else{
                if(a_mat.materialDescriptions[1].unk4 == 5){
                    cb1_1 = new Vector4(0,0,0,1);
                    cb1_9 = new Vector4(0,0,1,0);
                }else if(a_mat.materialDescriptions[1].unk4 == 1){
                    cb1_1 = new Vector4(0,0,1,0);
                    cb1_9 = new Vector4(1,0,0,0);
                }
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[1].textureNormal);
                materials[submesh_id].SetTexture("_Normal1", texture);
            }
            if(a_mat.materialDescriptions[1].unk2 == 1){
                cb1_5 = new Vector4(1,0,0,0);
            }
        }
        cb1_6  = new Vector4(0,0,0,1);  // alpha layer in normal map
        cb1_14 = new Vector4(0,0,0,0);
        cb1_18 = new Vector4(0,0,0,0);
        cb1_22 = new Vector4(0,0,0,0);
        if(a_mat.materialDescriptions.Length >= 3){
            cb1_22 = new Vector4(1,1,1,1);
            cb1_24.z = 1;
            cb1_25.z = 0.001f;
            if(a_mat.materialDescriptions[2].textureSelectorA == -1){
                texture = Texture2D.whiteTexture;
            }else{
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[2].textureColor);
                materials[submesh_id].SetTexture("_Color2", texture);
            }
            if(a_mat.materialDescriptions[2].textureSelectorB == -1){
                cb1_2 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                cb1_10 = new Vector4(0,0,0,1);
            }else{
                if(a_mat.materialDescriptions[2].unk4 == 5){
                    cb1_2 = new Vector4(0,0,0,1);
                    cb1_10 = new Vector4(0,0,1,0);
                }else if(a_mat.materialDescriptions[2].unk4 == 1){
                    cb1_2 = new Vector4(0,0,1,0);
                    cb1_10 = new Vector4(1,0,0,0);
                }
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[2].textureNormal);
                materials[submesh_id].SetTexture("_Normal2", texture);
            }
            if(a_mat.materialDescriptions[2].unk2 == 1){
                cb1_6 = new Vector4(1,0,0,0);
            }
        }
        cb1_7  = new Vector4(0,0,0,1);  // alpha layer in normal map
        cb1_15 = new Vector4(0,0,0,0);
        cb1_19 = new Vector4(0,0,0,0);
        cb1_23 = new Vector4(0,0,0,0);
        if(a_mat.materialDescriptions.Length >= 4){
            cb1_23 = new Vector4(1,1,1,1);
            cb1_24.w = 1;
            cb1_25.w = 0.001f;
            if(a_mat.materialDescriptions[3].textureSelectorA == -1){
                texture = Texture2D.whiteTexture;
            }else{
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[3].textureColor);
                materials[submesh_id].SetTexture("_Color3", texture);
            }
            if(a_mat.materialDescriptions[3].textureSelectorB == -1){
                cb1_3 = new Vector4(0,0,0,1);  // blend map to use from normal maps
                cb1_11 = new Vector4(0,0,0,1);
            }else{
                if(a_mat.materialDescriptions[3].unk4 == 5){
                    cb1_3 = new Vector4(0,0,0,1);
                    cb1_11 = new Vector4(0,0,1,0);
                }else if(a_mat.materialDescriptions[3].unk4 == 1){
                    cb1_3 = new Vector4(0,0,1,0);
                    cb1_11 = new Vector4(1,0,0,0);
                }
                texture = new Tex().DecodeTexture(@"AIDX\" + a_mat.materialDescriptions[3].textureNormal);
                materials[submesh_id].SetTexture("_Normal3", texture);
            }
            if(a_mat.materialDescriptions[3].unk2 == 1){
                cb1_7  = new Vector4(1,0,0,0);
            }
        }
        materials[submesh_id].renderQueue = 2450;
        // Debug.Log("cb1_0:  " + cb1_0);
        // Debug.Log("cb1_1:  " + cb1_1);
        // Debug.Log("cb1_2:  " + cb1_2);
        // Debug.Log("cb1_3:  " + cb1_3);
        // Debug.Log("cb1_4:  " + cb1_4);
        // Debug.Log("cb1_5:  " + cb1_5);
        // Debug.Log("cb1_6:  " + cb1_6);
        // Debug.Log("cb1_7:  " + cb1_7);
        // Debug.Log("cb1_8:  " + cb1_8);
        // Debug.Log("cb1_9:  " + cb1_9);
        // Debug.Log("cb1_10: " + cb1_10);
        // Debug.Log("cb1_11: " + cb1_11);
        // Debug.Log("cb1_12: " + cb1_12);
        // Debug.Log("cb1_13: " + cb1_13);
        // Debug.Log("cb1_14: " + cb1_14);
        // Debug.Log("cb1_15: " + cb1_15);
        // Debug.Log("cb1_16: " + cb1_16);
        // Debug.Log("cb1_17: " + cb1_17);
        // Debug.Log("cb1_18: " + cb1_18);
        // Debug.Log("cb1_19: " + cb1_19);
        // Debug.Log("cb1_20: " + cb1_20);
        // Debug.Log("cb1_21: " + cb1_21);
        // Debug.Log("cb1_22: " + cb1_22);
        // Debug.Log("cb1_23: " + cb1_23);
        // Debug.Log("cb1_24: " + cb1_24);
        // Debug.Log("cb1_25: " + cb1_25);

        materials[submesh_id].SetVector("_cb1_0", cb1_0);
        materials[submesh_id].SetVector("_cb1_1", cb1_1);
        materials[submesh_id].SetVector("_cb1_2", cb1_2);
        materials[submesh_id].SetVector("_cb1_3", cb1_3);
        materials[submesh_id].SetVector("_cb1_4", cb1_4);
        materials[submesh_id].SetVector("_cb1_5", cb1_5);
        materials[submesh_id].SetVector("_cb1_6", cb1_6);
        materials[submesh_id].SetVector("_cb1_7", cb1_7);
        materials[submesh_id].SetVector("_cb1_8", cb1_8);
        materials[submesh_id].SetVector("_cb1_9", cb1_9);
        materials[submesh_id].SetVector("_cb1_10", cb1_10);
        materials[submesh_id].SetVector("_cb1_11", cb1_11);
        materials[submesh_id].SetVector("_cb1_12", cb1_12);
        materials[submesh_id].SetVector("_cb1_13", cb1_13);
        materials[submesh_id].SetVector("_cb1_14", cb1_14);
        materials[submesh_id].SetVector("_cb1_15", cb1_15);
        materials[submesh_id].SetVector("_cb1_16", cb1_16);
        materials[submesh_id].SetVector("_cb1_17", cb1_17);
        materials[submesh_id].SetVector("_cb1_18", cb1_18);
        materials[submesh_id].SetVector("_cb1_19", cb1_19);
        materials[submesh_id].SetVector("_cb1_20", cb1_20);
        materials[submesh_id].SetVector("_cb1_21", cb1_21);
        materials[submesh_id].SetVector("_cb1_22", cb1_22);
        materials[submesh_id].SetVector("_cb1_23", cb1_23);
        materials[submesh_id].SetVector("_cb1_24", cb1_24);
        materials[submesh_id].SetVector("_cb1_25", cb1_25);
            
        materials[submesh_id].SetVector("_cb2_0", cb2_0);
        materials[submesh_id].SetVector("_cb2_1", cb2_1);
        materials[submesh_id].SetVector("_cb2_2", cb2_2);
        materials[submesh_id].SetVector("_cb2_3", cb2_3);
        materials[submesh_id].SetVector("_cb2_4", cb2_4);
        materials[submesh_id].SetVector("_cb2_5", cb2_5);
        materials[submesh_id].SetVector("_cb2_6", cb2_6);
        materials[submesh_id].SetVector("_cb2_7", cb2_7);
        materials[submesh_id].SetVector("_cb2_8", cb2_8);
        materials[submesh_id].SetVector("_cb2_9", cb2_9);
        materials[submesh_id].SetVector("_cb2_10", cb2_10);
    }
    
    private void ReadHeader(BinaryReader br){
        header = new Header();
        header.signature = new string(br.ReadChars(4));
        header.version = br.ReadUInt32();
        Debug.Log("VERSION: " + header.version);
        if (header.version == 100){
            header.headerSize = 1584;
        }
        header.nrUnk08 = br.ReadInt32();
        header.ofsUnk08 = br.ReadInt32();
        // Debug.Log(string.Format("UNK_08: {0} => {1}", header.nrUnk08, header.ofsUnk08));
        header.nrModelAnimations = br.ReadInt64();
        header.ofsModelAnimations = br.ReadInt64();
        header.nrAnimationRelated020 = br.ReadInt64();
        header.ofsAnimationRelated020A = br.ReadInt64();
        header.ofsAnimationRelated020B = br.ReadInt64();
        // Debug.Log(string.Format("ANIMATIONS: {0} => {1}", header.nrModelAnimations, header.ofsModelAnimations));
        // Debug.Log(string.Format("UNK_20: {0} => {1}, {2}", header.nrAnimationRelated020, header.ofsAnimationRelated020A, header.ofsAnimationRelated020B));
        br.ReadInt64(); //padding?
        // Debug.Log(string.Format("UNK_40: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_50: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_60: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_70: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        header.nrUnk020 = br.ReadInt64();
        header.ofsUnk020 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_80: {0} => {1}", header.nrUnk020, header.ofsUnk020));
        // Debug.Log(string.Format("UNK_90: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_A0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_B0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_C0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_D0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_E0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        header.nrUnk0F0 = br.ReadInt64();
        header.ofsUnk0F0 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_F0: {0} => {1}", header.nrUnk0F0, header.ofsUnk0F0));
        // Debug.Log(string.Format("UNK_100: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_110: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_120: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_130: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_140: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_150: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_160: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_170: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        br.BaseStream.Position = 0x180;
        header.nrBones = br.ReadInt64();
        header.ofsBones = br.ReadInt64();
        // Debug.Log(string.Format("BONES_180: {0} => {1}", header.nrBones, header.ofsBones));
        // Debug.Log(string.Format("UNK_190: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 2bytes. More like an array.
        // Debug.Log(string.Format("UNK_1A0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 2bytes. More like an array.
        br.BaseStream.Position = 0x1B0;
        header.nrBonesTable = br.ReadInt64();
        header.ofsBonesTable = br.ReadInt64();
        // Debug.Log(string.Format("BONETABLES_1B0: {0} => {1}", header.nrBonesTable, header.ofsBonesTable));
        br.BaseStream.Position = 0x1C0;
        header.nrTextures = br.ReadInt64();
        header.ofsTextures = br.ReadInt64();
        // Debug.Log(string.Format("TEXTURES_1C0: {0} => {1}", header.nrTextures, header.ofsTextures));
        header.nrUnk1D0 = br.ReadInt64();
        header.ofsUnk1D0 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_1D0: {0} => {1}", header.nrUnk1D0, header.ofsUnk1D0));    // struct size 2bytes. More like an array.
        // Debug.Log(string.Format("UNK_1E0: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size approx 896 bytes.
        br.BaseStream.Position = 0x1F0; //materials/shader???
        header.nrMaterials = br.ReadInt64();
        header.ofsMaterials = br.ReadInt64();
        // Debug.Log(string.Format("MATERIALS_1F0: {0} => {1}", header.nrMaterials, header.ofsMaterials));
        br.BaseStream.Position = 0x200;
        header.nrSubmeshGroupsTable = br.ReadInt64();
        header.ofsSubmeshGroupsTable = br.ReadInt64();
        // Debug.Log(string.Format("SUBMESHGROUP_200: {0} => {1}", header.nrSubmeshGroupsTable, header.ofsSubmeshGroupsTable));
        // Debug.Log(string.Format("UNK_210: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 2bytes. More like an array.
        // Debug.Log(string.Format("UNK_220: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_230: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_240: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        br.BaseStream.Position = 0x250;
        header.nrGeometry = br.ReadInt64();
        header.ofsGeometry = br.ReadInt64();
        // Debug.Log(string.Format("GEOMETRY_250: {0} => {1}", header.nrGeometry, header.ofsGeometry));
        header.nrUnk1 = br.ReadInt64();
        header.ofsUnk1 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_260: {0} => {1}", header.nrUnk1, header.ofsUnk1));     // struct size 4 bytes.
        // Debug.Log(string.Format("UNK_270: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 2bytes. More like an array.
        // Debug.Log(string.Format("UNK_280: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 8bytes. 
        // Debug.Log(string.Format("UNK_290: {0} => {1}, {2}", br.ReadInt64(), br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_2A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_2B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 80
        // Debug.Log(string.Format("UNK_2C8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 8
        // Debug.Log(string.Format("UNK_2D8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_2E8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        header.nrUnk2F8 = br.ReadInt64();
        header.ofsUnk2F8 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_2F8: {0} => {1}", header.nrUnk2F8, header.ofsUnk2F8));    // struct size 8688
        // Debug.Log(string.Format("UNK_308: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_318: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_328: {0} => {1}", br.ReadInt64(), br.ReadInt64()));    // struct size 88
        header.nrUnk338 = br.ReadInt64();
        header.ofsUnk338 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_338: {0} => {1}", header.nrUnk338, header.ofsUnk338));
        // Debug.Log(string.Format("UNK_348: {0} => {1}, {2}, {3}", br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_368: {0} => {1}, {2}, {3}", br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()));

        // Debug.Log(string.Format("UNK_388: {0} => {1}", br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_390: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3A0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3B0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3C0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3D0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3E0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_400: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_410: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_420: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_430: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_440: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_450: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_460: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_470: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_480: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        header.nrUnk490 = br.ReadInt64();
        header.ofsUnk490 = br.ReadInt64();
        // Debug.Log(string.Format("UNK_490: {0} => {1}", header.nrUnk490, header.ofsUnk490));
        // Debug.Log(string.Format("UNK_4A0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_4B0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_4C0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_4D0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_4E0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_4F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3A8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_3B8: {0} => {1}", br.ReadInt64(), br.ReadInt64()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // Debug.Log(string.Format("UNK_500: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
        // HEADER END => 0x630 (1584)

        header.modelAnimations = ModelAnimations.ReadAll(br, 1584 + header.ofsModelAnimations, (int)header.nrModelAnimations);
        header.animationRelated020 = AnimationRelated020.ReadAll(br, 1584 + header.ofsAnimationRelated020A, 1584 + header.ofsAnimationRelated020B, (int)header.nrModelAnimations);
        // header.unk080 = Unk080.ReadAll(br, 1584 + header.ofsUnk08, (int)header.nrUnk08);
        // header.unk08 = Unk08.ReadAll(br, 1584+header.ofsUnk08, (int)header.nrUnk08);
        // header.unk0F0 = Unk0F0.ReadAll(br, 1584+header.ofsUnk0F0, (int)header.nrUnk0F0);

        // BONE MAPPING
        if(header.nrBonesTable > 0){
            header.boneMapping = new int[header.nrBonesTable];
            var bone_table_start = 1584 + header.ofsBonesTable;
            br.BaseStream.Position = bone_table_start;
            for(var i=0; i<header.nrBonesTable; i++){
                header.boneMapping[i] = br.ReadInt16();
            }
        }
        // BONES & BONE ANIMATIONS
        header.bones = Bone.ReadAll(br, 1584 + header.ofsBones, (int)header.nrBones);
        // SUBMESH GROUP TABLE
        header.SubmeshGroupsTable = SubmeshGroupTable.ReadAll(br, 1584 + header.ofsSubmeshGroupsTable, (int)header.nrSubmeshGroupsTable);
        // TEXTURES
        header.textures = Texture.ReadAll(br, 1584 + header.ofsTextures, (int)header.nrTextures);
        // MATERIALS
        header.materials = M3Material.ReadAll(br, 1584 + header.ofsMaterials, (int)header.nrMaterials, header);
        // header.unk1D0 =  Unk1D0.ReadAll(br, header.ofsUnk1D0, (int)header.nrUnk1D0);
        // GEOMETRY
        header.geometry = ReadGeometry(br);

        // header.unk2F8 = Unk2F8.ReadAll(br, header.ofsUnk2F8, (int)header.nrUnk2F8); // contains some tracks...
        // header.unk338 = Unk338.ReadAll(br, header.ofsUnk338, (int)header.nrUnk338);
        // header.unk490 = Unk490.ReadAll(br, header.ofsUnk490, (int)header.nrUnk490);
    }
    private Geometry ReadGeometry(BinaryReader br){
        var geometry = new Geometry();
        var geometryOfs = 1584 + header.ofsGeometry;
        br.BaseStream.Position = geometryOfs;
        br.BaseStream.Position = geometryOfs + 0x18;
        geometry.nrVertices = br.ReadUInt32();
        geometry.vertexSize = br.ReadInt16();
        geometry.vertexFlags = br.ReadInt16();
        // Debug.Log(string.Format("GEOMETRY_Vertex_0x18 => {0}-{1}",geometry.nrVertices,geometry.vertexSize));
        geometry.vertexFieldTypes = new byte[11];
        for(var i=0;i<11;i++){
            geometry.vertexFieldTypes[i] = br.ReadByte();
        }
        
        // Debug.Log(string.Format("hasposition: {0} => {1}", geometry.vertexFlags & 0x0001, geometry.vertexFieldTypes[0]));
        // Debug.Log(string.Format("hasTangent: {0} => {1}", geometry.vertexFlags & 0x0002, geometry.vertexFieldTypes[1]));
        // Debug.Log(string.Format("hasNormal: {0} => {1}", geometry.vertexFlags & 0x0004, geometry.vertexFieldTypes[2]));
        // Debug.Log(string.Format("hasBiTangent: {0} => {1}", geometry.vertexFlags & 0x0008, geometry.vertexFieldTypes[3]));
        // Debug.Log(string.Format("hasBoneIndices: {0} => {1}", geometry.vertexFlags & 0x0010, geometry.vertexFieldTypes[4]));
        // Debug.Log(string.Format("hasBoneWeights: {0} => {1}", geometry.vertexFlags & 0x0020, geometry.vertexFieldTypes[5]));
        // Debug.Log(string.Format("hasVertexColor0: {0} => {1}", geometry.vertexFlags & 0x0040, geometry.vertexFieldTypes[6]));
        // Debug.Log(string.Format("hasVertexBlend: {0} => {1}", geometry.vertexFlags & 0x0080, geometry.vertexFieldTypes[7]));
        // Debug.Log(string.Format("hasUV0: {0} => {1}", geometry.vertexFlags & 0x0100, geometry.vertexFieldTypes[8]));
        // Debug.Log(string.Format("hasUV1: {0} => {1}", geometry.vertexFlags & 0x0200, geometry.vertexFieldTypes[9]));
        // Debug.Log(string.Format("hasUnknown: {0} => {1}", geometry.vertexFlags & 0x0400, geometry.vertexFieldTypes[10]));
        // * This flag indicates which fields are used. The position of the flag (starting at 0) is also the array-index for lookup in any vertex related array.
        // *
        // * <li>vertexBlockFlags & 0x0001 != 0 -> vertexBlockFieldType[0] is used (value 1 or 2)</li>
        // * <li>vertexBlockFlags & 0x0002 != 0 -> vertexBlockFieldType[1] is used (value 3)</li>
        // * <li>vertexBlockFlags & 0x0004 != 0 -> vertexBlockFieldType[2] is used (value 3)</li>
        // * <li>vertexBlockFlags & 0x0008 != 0 -> vertexBlockFieldType[3] is used (value 3)</li>
        // * <li>vertexBlockFlags & 0x0010 != 0 -> vertexBlockFieldType[4] is used (value 4), 4 small numbers, ascending, 0 seems to indicate 'not used'. Bone
        // * indices</li>
        // * <li>vertexBlockFlags & 0x0020 != 0 -> vertexBlockFieldType[5] is used (value 4), 4 bytes, sums always up to 255. Bone weights</li>
        // * <li>vertexBlockFlags & 0x0040 != 0 -> vertexBlockFieldType[6] is used (value 4)</li>
        // * <li>vertexBlockFlags & 0x0080 != 0 -> vertexBlockFieldType[7] is used (value 4), 4 bytes, at any time, only one byte is set to -1, every other byte to
        // * 0</li>
        // * <li>vertexBlockFlags & 0x0100 != 0 -> vertexBlockFieldType[8] is used (value 5), uv map 1</li>
        // * <li>vertexBlockFlags & 0x0200 != 0 -> vertexBlockFieldType[9] is used (value 5, uv map 2</li>
        // * <li>vertexBlockFlags & 0x0400 != 0 -> vertexBlockFieldType[10] is used (value 6)</li>
        br.BaseStream.Position = geometryOfs + 0x68;
        geometry.nrIndices = br.ReadUInt32();
        br.BaseStream.Position = geometryOfs + 0x78;
        geometry.ofsIndices = br.ReadUInt32();
        // Debug.Log(string.Format("GEOMETRY_INDICES_68 => {0}-{1}",geometry.nrIndices,geometry.ofsIndices));
        br.BaseStream.Position = geometryOfs + 0x80;
        geometry.nrSubmeshes = br.ReadUInt32();
        br.BaseStream.Position = geometryOfs + 0x88;
        geometry.ofsSubmeshes = br.ReadUInt32();
        // Debug.Log(string.Format("GEOMETRY_SUBMESH_80 => {0}-{1}",geometry.nrSubmeshes,geometry.ofsSubmeshes));
        br.BaseStream.Position = geometryOfs + 0x90;
        geometry.unk0 = br.ReadUInt32(); // equal to the number of vertices?
        br.BaseStream.Position = geometryOfs + 0x98;
        geometry.nrUnk1 = br.ReadUInt32();
        br.BaseStream.Position = geometryOfs + 0xA0;
        geometry.ofsUnk1 = br.ReadUInt32();
        // Debug.Log(string.Format("GEOMETRY_UNK0_98 => {0}-{1}-{2}",geometry.unk0,geometry.nrUnk1,geometry.ofsUnk1));
        br.BaseStream.Position = geometryOfs + 0xA8;
        geometry.nrUnk2 = br.ReadUInt32();
        br.BaseStream.Position = geometryOfs + 0xB0;
        geometry.ofsUnk2 = br.ReadUInt32();
        // Debug.Log(string.Format("GEOMETRY_UNK2_A8 => {0}-{1}",geometry.nrUnk2,geometry.ofsUnk2)); // size 2 bytes
        br.BaseStream.Position = geometryOfs + 0xB8;
        geometry.nrUnk3 = br.ReadUInt32();
        br.BaseStream.Position = geometryOfs + 0xC0;
        geometry.ofsUnk3 = br.ReadUInt32();
        // Debug.Log(string.Format("GEOMETRY_UNK3_B8 => {0}-{1}",geometry.nrUnk3,geometry.ofsUnk3));
        // Debug.Log(geometry.unk0);
        // var xx = geometryOfs + 208 + geometry.ofsUnk1;
        // br.BaseStream.Position = xx;
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(br.ReadUInt32());
        // Debug.Log(geometry.nrUnk1);
        // Debug.Log(geometry.ofsUnk1);
        // Debug.Log(geometry.nrUnk2);
        // Debug.Log(geometry.ofsUnk2);
        // Debug.Log(geometry.nrUnk3);
        // Debug.Log(geometry.ofsUnk3);
        // Debug.Log(geometry.nrVertices + " " + geometry.vertexSize + " " + geometry.nrIndices + " " + geometry.ofsIndices + " " + geometry.nrSubmeshes);
        Vertex[] vertices = new Vertex[geometry.nrVertices];
        geometry.vertex_positions = new Vector3[geometry.nrVertices];
        geometry.vertices = new Vertex[geometry.nrVertices];
        geometry.normals = new Vector3[geometry.nrVertices];
        geometry.tangents = new Vector4[geometry.nrVertices];
        geometry.bitangents = new Vector4[geometry.nrVertices];
        geometry.uv1 = new Vector2[geometry.nrVertices];
        geometry.uv2 = new Vector2[geometry.nrVertices];
        geometry.vertexColor0 = new Color[geometry.nrVertices];
        geometry.vertexBlendXY = new Vector2[geometry.nrVertices];
        geometry.vertexBlendZW = new Vector2[geometry.nrVertices];
        geometry.bone_index = new Vector4[geometry.nrVertices];
        geometry.bone_weights = new Vector4[geometry.nrVertices];

        // READ ALL VERTICES
        for(var i = 0; i<geometry.nrVertices; i++){
            var vertexOfs = geometryOfs + 208 + i*geometry.vertexSize;
            br.BaseStream.Position = vertexOfs;
            var fieldType_count = 0;
            if((geometry.vertexFlags & 0x0001) == 1){      // position in 3d space
                geometry.vertex_positions[i] = VertexReadV3(br, geometry.vertexFieldTypes[0]);
            }
            if((geometry.vertexFlags & 0x0002) == 2){      // tangents
                geometry.tangents[i] = VertexReadV3(br, geometry.vertexFieldTypes[1]);
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0004) == 4){      // normals
                geometry.normals[i] = VertexReadV3(br, geometry.vertexFieldTypes[2]);  
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0008) == 8){      // bitangents
                geometry.bitangents[i] = VertexReadV3(br, geometry.vertexFieldTypes[3]);  
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0010) == 16){
                geometry.bone_index[i] = VertexReadV4(br, geometry.vertexFieldTypes[4]);
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0020) == 32){
                geometry.bone_weights[i] = VertexReadV4(br, geometry.vertexFieldTypes[5]);
                fieldType_count++;
            }else{
                geometry.bone_weights[i] = new Vector4(255,0,0,0);
            }
            if((geometry.vertexFlags & 0x0040) == 64){
                geometry.vertexColor0[i] = (Color)VertexReadV4(br, geometry.vertexFieldTypes[6])/255.0f;
                fieldType_count++;
            }else{
                geometry.vertexColor0[i] = new Color(1,1,1,1);
            }
            if((geometry.vertexFlags & 0x0080) == 128){
                Vector4 blend_weights = VertexReadV4(br, geometry.vertexFieldTypes[7])/255.0f;
                geometry.vertexBlendXY[i] = new Vector2(blend_weights.z, blend_weights.y);
                geometry.vertexBlendZW[i] = new Vector2(blend_weights.x, blend_weights.w);
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0100) == 256){
                geometry.uv1[i] = VertexReadV2(br, geometry.vertexFieldTypes[8]);//new Vector2(u1, v1);
                fieldType_count++;
            }
            if((geometry.vertexFlags & 0x0200) == 512){
                geometry.uv2[i] = VertexReadV2(br, geometry.vertexFieldTypes[9]);//new Vector2(u, v);
            }
        }

        int[] indices = new int[geometry.nrIndices];
        var indexOfs = geometryOfs + 208 + geometry.ofsIndices;
        br.BaseStream.Position = indexOfs;
        for(var i=0;i<geometry.nrIndices; i++){
            indices[i] = (int)br.ReadInt16();
        }
        geometry.indices = indices;
        Submesh[] submeshes = new Submesh[geometry.nrSubmeshes];
        var submeshOfs = geometryOfs + 208 + geometry.ofsSubmeshes;
        // Debug.Log("Submeshes");
        for(var i=0;i<geometry.nrSubmeshes; i++){
            var indexOfs2 = submeshOfs + i*0x70;
            br.BaseStream.Position = indexOfs2;
            submeshes[i].startIndex = br.ReadUInt32();
            submeshes[i].startVertex = br.ReadUInt32();
            submeshes[i].nrIndex = br.ReadUInt32();
            submeshes[i].nrVertex = br.ReadUInt32();
            // 0x10
            submeshes[i].startBoneMapping = br.ReadUInt16(); 
            submeshes[i].nrBoneMapping = br.ReadUInt16();
            submeshes[i].unk1 = br.ReadUInt16();
            submeshes[i].material_id = br.ReadUInt16();
            submeshes[i].unk2 = br.ReadUInt16();
            submeshes[i].unk3 = br.ReadUInt16();
            submeshes[i].unk4 = br.ReadUInt16();
            submeshes[i].group_id = br.ReadByte();
            submeshes[i].unk_Group_related = br.ReadByte(); 
            // 0x20
            submeshes[i].unk5 = br.ReadUInt16();
            submeshes[i].unk6 = br.ReadUInt16();
            submeshes[i].unk7 = br.ReadUInt16();
            submeshes[i].unk8 = br.ReadUInt16();
            submeshes[i].unk9 = br.ReadUInt16();
            submeshes[i].unk10 = br.ReadUInt16();
            submeshes[i].unk11 = br.ReadUInt16();
            submeshes[i].unk12 = br.ReadUInt16(); 
            // 0x30
            submeshes[i].color0 = new Color(br.ReadByte(),br.ReadByte(),br.ReadByte(),br.ReadByte());
            submeshes[i].color1 = new Color(br.ReadByte(),br.ReadByte(),br.ReadByte(),br.ReadByte());
            submeshes[i].unk13 = br.ReadByte();      // 0, 1, 2, 3, 4, 8, 10, 11, 12 (if set to 10 the mesh won't render)
            submeshes[i].unk14 = br.ReadByte();      // 0, 1, 2, 3
            submeshes[i].unk15 = br.ReadByte();
            submeshes[i].unk16 = br.ReadByte();
            submeshes[i].unk17 = br.ReadByte();
            submeshes[i].unk18 = br.ReadByte();
            submeshes[i].unk19 = br.ReadByte();
            submeshes[i].unk20 = br.ReadByte();
            // br.ReadBytes(6);    //gap?
            // 0x40
            submeshes[i].boundMin = new Vector4(br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),br.ReadSingle());
            submeshes[i].boundMax = new Vector4(br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),br.ReadSingle());
            submeshes[i].unkVec4 = new Vector4(br.ReadSingle(),br.ReadSingle(),br.ReadSingle(),br.ReadSingle());

            
            var pr = string.Format("SM: {0}\t {1}\t {2}\t {3}\t {4}\t {5}\t {6}\t {7}\t {8}\t {9}\t {10}\t {11}\t {12}\t {13}\t {14}\t {15}\t {16}\t {17}\t {18}\t {19}\t {20}\t {21}\t {22}\t {23}\t {24}\t {25}\t {26}\t {27}\t {28}\t {29}\t {30}", 
            i, submeshes[i].unk1, submeshes[i].material_id, submeshes[i].unk2, submeshes[i].unk3, submeshes[i].unk4, submeshes[i].group_id, 
            submeshes[i].unk_Group_related, submeshes[i].unk5, submeshes[i].unk6, submeshes[i].unk7, submeshes[i].unk8, submeshes[i].unk9, 
            submeshes[i].unk10, submeshes[i].unk11, submeshes[i].unk12, submeshes[i].unk13, submeshes[i].unk14, submeshes[i].unk15, submeshes[i].unk16, 
            submeshes[i].unk17, submeshes[i].unk18, submeshes[i].unk19, submeshes[i].unk20, submeshes[i].color0, submeshes[i].color1, submeshes[i].unk13, submeshes[i].unk14,
            submeshes[i].boundMin, submeshes[i].boundMax, submeshes[i].unkVec4);
            // Debug.Log(pr);

            var boneSubmap = new int[submeshes[i].nrBoneMapping];
            var submapIndex=0;
            for(var j=submeshes[i].startBoneMapping; j<submeshes[i].startBoneMapping + submeshes[i].nrBoneMapping; j++){
                boneSubmap[submapIndex] = this.header.boneMapping[j];
                submapIndex++;
            }
            // HANDLE BONE IDS OF VERTICES
            for(var j=submeshes[i].startVertex; j<submeshes[i].startVertex+submeshes[i].nrVertex; j++){
                geometry.bone_index[j].x = boneSubmap[(int)geometry.bone_index[j].x];
                geometry.bone_index[j].y = boneSubmap[(int)geometry.bone_index[j].y];
                geometry.bone_index[j].z = boneSubmap[(int)geometry.bone_index[j].z];
                geometry.bone_index[j].w = boneSubmap[(int)geometry.bone_index[j].w];
            }
        }
        geometry.submesh = submeshes;
        // Debug.Log("EndSubmeshes");
        return geometry;
    }
    // * 0 => Null
    // * 1 => Vector3, 32Bit
    // * 2 => Vector3, 16Bit
    // * 3 => Vector3, 8Bit
    // * 4 => Vector4, 8Bit
    // * 5 => Vector2, 16Bit
    // * 6 => unk
    public Vector3 VertexReadV3(BinaryReader br, int type){
        Vector3 output = new Vector3(0,0,0);
        if(type == 1){
            var x = br.ReadSingle();    // position in 3d space
            var y = br.ReadSingle();
            var z = br.ReadSingle();
            output = new Vector3(x, y, z);
        }else if(type == 2){
            var x = (float)((float)br.ReadInt16()/1024.0f);
            var y = (float)((float)br.ReadInt16()/1024.0f);
            var z = (float)((float)br.ReadInt16()/1024.0f);
            output = new Vector3(x, y, z);
        }else if(type == 3){
            // 1 = x2 + y2 + z2
            // z = sqrt(1-x2-y2)
            var x = (float)((float)br.ReadByte() - 127.0) / 127.0f;
            var y = (float)((float)br.ReadByte() - 127.0) / 127.0f;
            var z = 1.0f - (float)Mathf.Sqrt(x*x + y*y);   // why, Carbine? why? is 1 extra byte too much?? is it cheaper to do sqrt?
            output = new Vector3(x, y, z);  
        }
        return output;
    }
    public Vector4 VertexReadV4(BinaryReader br, int type){
        Vector4 output = new Vector4(1,1,1,1);
        if(type == 4){
            output = new Vector4(br.ReadByte(),br.ReadByte(),br.ReadByte(),br.ReadByte());
        }
        return output;
    }
    public Vector2 VertexReadV2(BinaryReader br, int type){
        Vector2 output = new Vector2(0,0);
        if(type == 5){
            var u = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);    //uv1
            var v = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
            output = new Vector2(u, v);
        }
        return output;
    }

    [System.Serializable]
    public struct Header{
        public string name;
        public string signature;
        public UInt32 version;
        public Int64 nrModelAnimations;
        public Int64 ofsModelAnimations;
        public Int64 nrAnimationRelated020;
        public Int64 ofsAnimationRelated020A;
        public Int64 ofsAnimationRelated020B;
        public Int32 nrUnk08;
        public Int32 ofsUnk08;
        public Unk08[] unk08;
        public Int64 nrUnk020;
        public Int64 ofsUnk020;
        public Int64 nrUnk1D0;
        public Int64 ofsUnk1D0;
        public Int64 nrUnk0F0;
        public Int64 ofsUnk0F0;
        public Int64 nrUnk2F8;
        public Int64 ofsUnk2F8;
        public Int64 nrUnk338;
        public Int64 ofsUnk338;
        public Int64 nrBones;
        public Int64 ofsBones;
        public Int64 nrBonesTable;
        public Int64 ofsBonesTable;
        public Int64 nrMaterials;
        public Int64 ofsMaterials;
        public Int64 nrTextures;
        public Int64 ofsTextures;
        public Int64 nrGeometry;
        public Int64 ofsGeometry;
        public Geometry geometry;
        public Texture[] textures;
        public Int64 nrSubmeshGroupsTable;
        public Int64 ofsSubmeshGroupsTable;
        public Int64 nrUnk490;
        public Int64 ofsUnk490;
        public SubmeshGroupTable[] SubmeshGroupsTable;
        public M3Material[] materials;
        public Bone[] bones;
        public ModelAnimations[] modelAnimations;
        public AnimationRelated020 animationRelated020;
        public Unk0F0[] unk0F0;
        public Unk1D0 unk1D0;
        public Unk2F8[] unk2F8;
        public Unk338[] unk338;
        public Unk490 unk490;
        public Int64 nrUnk1;
        public Int64 ofsUnk1;
        public int[] boneMapping;

        public int headerSize;
    }
    [System.Serializable]
    public struct SubmeshGroupTable {
        public UInt16 submesh_id;
        public UInt16 is_default;
        public static SubmeshGroupTable[] ReadAll(BinaryReader br, long startPos, int nr){
            var submeshGroupTableEntries = new SubmeshGroupTable[nr];
            for(var i = 0;i<nr;i++){
                br.BaseStream.Position = startPos + i*SubmeshGroupTable.GetSize();
                submeshGroupTableEntries[i].submesh_id = br.ReadUInt16();
                submeshGroupTableEntries[i].is_default = br.ReadUInt16();
                // submeshGroupTableEntries[i].Print();
            }
            return submeshGroupTableEntries;
        }
        public void Print(int i){
            Debug.Log(string.Format("{0}\t {1}\t {2}", i, this.submesh_id, this.is_default));
        }
        public static int GetSize(){
            return 4;
        }
    }
    [System.Serializable]
    public struct Geometry{
        public UInt32 nrVertices;
        public Int16 vertexSize;
        public Int16 vertexFlags;
        public byte[] vertexFieldTypes;
        public UInt32 nrIndices;
        public UInt32 ofsIndices;
        public UInt32 nrSubmeshes;
        public UInt32 ofsSubmeshes;
        public UInt32 unk0;
        public UInt32 nrUnk1;
        public UInt32 ofsUnk1;
        public UInt32 nrUnk2;
        public UInt32 ofsUnk2;
        public UInt32 nrUnk3;
        public UInt32 ofsUnk3;
        public Vertex[] vertices;
        public int[] indices;
        public Submesh[] submesh;
        public Vector3[] vertex_positions;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector4[] bitangents;
        public Vector2[] uv1;
        public Vector2[] uv2;
        public Color[] vertexColor0;
        public Vector2[] vertexBlendXY;
        public Vector2[] vertexBlendZW;
        public Vector4[] bone_index;
        public Vector4[] bone_weights;
    }
    [System.Serializable]
    public struct Vertex{
        public byte[] bi;
        public Vector4 bw;
        public float[] uv;
    }
    [System.Serializable]
    public struct Submesh{
        public UInt32 startIndex;
        public UInt32 startVertex;
        public UInt32 nrIndex;
        public UInt32 nrVertex;
        public UInt16 startBoneMapping;
        public UInt16 nrBoneMapping;
        public UInt16 unk1;
        public UInt16 material_id;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public byte group_id;
        public byte unk_Group_related;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public byte unk13;
        public byte unk14;
        public byte unk15;
        public byte unk16;
        public byte unk17;
        public byte unk18;
        public byte unk19;
        public byte unk20;
        public Color color0, color1;
        public Vector4 boundMin, boundMax, unkVec4;

        public static int GetSize(){
            return 112;
        }
    }
    [System.Serializable]
    public struct Texture{
        public Int16 unk0;
        public byte type;
        public byte unk1;
        public Int32 unk2;
        public float unk3;
        public byte unk4;
        public byte unk5;
        public byte unk6;
        public byte unk7;
        public UInt64 nrLetters;
        public UInt64 ofset;

        public string path;
        public static Texture[] ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = startPos;
            var textures = new Texture[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = startPos + i*Texture.GetSize();
                textures[i].unk0 = br.ReadInt16();
                textures[i].type = br.ReadByte();
                textures[i].unk1 = br.ReadByte();
                textures[i].unk2 = br.ReadInt32();
                textures[i].unk3 = br.ReadSingle();
                textures[i].unk4 = br.ReadByte();
                textures[i].unk5 = br.ReadByte();
                textures[i].unk6 = br.ReadByte();
                textures[i].unk7 = br.ReadByte();
                textures[i].nrLetters = br.ReadUInt64();
                textures[i].ofset = br.ReadUInt64();
                
                br.BaseStream.Position = startPos + nr*Texture.GetSize() + (int)textures[i].ofset;
                byte[] path = br.ReadBytes((int)textures[i].nrLetters*2);
                byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, path);
                textures[i].path = Encoding.Default.GetString(utf8Bytes);
                // textures[i].Print();
            }
            return textures;
        }
        public void Print(){
            var pr = string.Format("TEXTURE: {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",
            this.unk0, this.type, this.unk1, this.unk2, this.unk3, this.unk4, this.unk5, this.unk6, this.unk7, this.nrLetters, this.ofset, this.path);
            Debug.Log(pr);
        }
        public static int GetSize(){
            return 32;
        }
    }
    [System.Serializable]
    public struct M3Material{
        public byte unk0;
        public byte unk1;
        public byte unk2;
        public byte unk3;
        public byte unk4;
        public byte unk5;
        public byte unk6;
        public byte unk7;
        public byte unk8;
        public byte unk9;
        public byte unk10;
        public byte unk11;
        public UInt16 unk12; // flags: 0x40 - emission?
        public UInt16 unk14;    // padding?
        public UInt32 unk16;  // seems to be related to emission
        public UInt32 unk20;   // some kind of shader type/mode
        public int cb1_28_x; //specular multiplier x
        public int cb1_28_y; //specular multiplier y
        public UInt64 nrMaterialDescriptions;
        public UInt64 ofsMaterialDescriptions;
        public MaterialDescription[] materialDescriptions;
        public static M3Material[] ReadAll(BinaryReader br, long startPos, int nr, Header header){
            br.BaseStream.Position = startPos;
            var materials = new M3Material[nr];

            for(var i = 0;i<nr;i++){
                br.BaseStream.Position = startPos + i*M3Material.GetSize();
                materials[i].unk0 = br.ReadByte();
                materials[i].unk1 = br.ReadByte();
                materials[i].unk2 = br.ReadByte();
                materials[i].unk3 = br.ReadByte();
                materials[i].unk4 = br.ReadByte();
                materials[i].unk5 = br.ReadByte();
                materials[i].unk6 = br.ReadByte();
                materials[i].unk7 = br.ReadByte();
                materials[i].unk8 = br.ReadByte();
                materials[i].unk9 = br.ReadByte();
                materials[i].unk10 = br.ReadByte();
                materials[i].unk11 = br.ReadByte();
                materials[i].unk12 = br.ReadUInt16();
                materials[i].unk14 = br.ReadUInt16();
                materials[i].unk16 = br.ReadUInt32();    //0x10
                materials[i].unk20 = br.ReadUInt32();
                materials[i].cb1_28_x = br.ReadInt32();    //0x18
                materials[i].cb1_28_y = br.ReadInt32();
                materials[i].nrMaterialDescriptions = br.ReadUInt64();    //0x20
                materials[i].ofsMaterialDescriptions = br.ReadUInt64();
                // materials[i].Print();
                var mat_des_start_pos = startPos + nr*M3Material.GetSize() + (int)materials[i].ofsMaterialDescriptions;
                materials[i].materialDescriptions = MaterialDescription.ReadAll(br, mat_des_start_pos, (int)materials[i].nrMaterialDescriptions, header);
            }
            return materials;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log("MATERIAL: " + pr);
        }
        public static int GetSize(){
            return 0x30;
        }
    }
    
    [System.Serializable]
    public struct MaterialDescription{
        public short textureSelectorA;
        public short textureSelectorB;
        public UInt16 unk0;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public UInt16 unk16;
        public UInt16 unk17;
        public UInt16 unk18;
        public UInt16 unk19;
        public UInt16 unk20;
        public UInt16 unk21;
        public UInt16 unk22;
        public UInt16 unk23;
        public UInt16 unk24;
        public UInt16 unk25;
        public UInt16 unk26;
        public UInt16 unk27;
        public UInt16 unk28;
        public UInt16 unk29;
        public UInt16 unk30;
        public UInt16 unk31;
        public UInt16 unk32;
        public UInt16 unk33;
        public UInt16 unk34;
        public UInt16 unk35;
        public UInt16 unk36;
        public UInt16 unk37;
        public UInt16 unk38;
        public UInt16 unk39;
        public UInt16 unk40;
        public UInt16 unk41;
        public UInt16 unk42;
        public UInt16 unk43;
        public UInt16 unk44;
        public UInt16 unk45;
        public UInt16 unk46;
        public UInt16 unk47;
        public UInt16 unk48;
        public UInt16 unk49;
        public UInt16 unk50;
        public UInt16 unk51;
        public UInt16 unk52;
        public UInt16 unk53;
        public UInt16 unk54;
        public UInt16 unk55;
        public UInt16 unk56;
        public UInt16 unk57;
        public UInt16 unk58;
        public UInt16 unk59;
        public UInt16 unk60;
        public UInt16 unk61;
        public UInt16 unk62;
        public UInt16 unk63;
        public UInt16 unk64;
        public UInt16 unk65;
        public UInt16 unk66;
        public UInt16 unk67;
        public UInt16 unk68;
        public UInt16 unk69;
        public UInt16 unk70;
        public UInt16 unk71;
        public UInt16 unk72;
        public UInt16 unk73;
        public UInt16 unk74;
        public UInt16 unk75;
        public UInt16 unk76;
        public UInt16 unk77;
        public UInt16 unk78;
        public UInt16 unk79;
        public UInt16 unk80;
        public UInt16 unk81;
        public UInt16 unk82;
        public UInt16 unk83;
        public UInt16 unk84;
        public UInt16 unk85;
        public UInt16 unk86;
        public UInt16 unk87;
        public UInt16 unk88;
        public UInt16 unk89;
        public UInt16 unk90;
        public UInt16 unk91;
        public UInt16 unk92;
        public UInt16 unk93;
        public UInt16 unk94;
        public UInt16 unk95;
        public UInt16 unk96;
        public UInt16 unk97;
        public UInt16 unk98;
        public UInt16 unk99;
        public UInt16 unk100;
        public UInt16 unk101;
        public UInt16 unk102;
        public UInt16 unk103;
        public UInt16 unk104;
        public UInt16 unk105;
        public UInt16 unk106;
        public UInt16 unk107;
        public UInt16 unk108;
        public UInt16 unk109;
        public UInt16 unk110;
        public UInt16 unk111;
        public UInt16 unk112;
        public UInt16 unk113;
        public UInt16 unk114;
        public UInt16 unk115;
        public UInt16 unk116;
        public UInt16 unk117;
        public UInt16 unk118;
        public UInt16 unk119;
        public UInt16 unk120;
        public UInt16 unk121;
        public UInt16 unk122;
        public UInt16 unk123;
        public UInt16 unk124;
        public UInt16 unk125;
        public UInt16 unk126;
        public UInt16 unk127;
        public UInt16 unk128;
        public UInt16 unk129;
        public UInt16 unk130;
        public UInt16 unk131;
        public UInt16 unk132;
        public UInt16 unk133;
        public UInt16 unk134;
        public UInt16 unk135;
        public UInt16 unk136;
        public UInt16 unk137;
        public UInt16 unk138;
        public UInt16 unk139;
        public UInt16 unk140;
        public UInt16 unk141;
        public UInt16 unk142;
        public UInt16 unk143;
        public UInt16 unk144;
        public UInt16 unk145;
        public string textureColor, textureNormal;
        public static MaterialDescription[] ReadAll(BinaryReader br, long startPos, int nr, Header header){
            var materialDescriptions = new MaterialDescription[nr];
            for(var i=0; i<nr; i++){
                br.BaseStream.Position = startPos + i*MaterialDescription.GetSize();
                materialDescriptions[i].textureSelectorA = br.ReadInt16();
                materialDescriptions[i].textureSelectorB = br.ReadInt16();
                // materialDescriptions[i].unk0 = br.ReadInt32();
                // materialDescriptions[i].unk1 = br.ReadInt32();
                // materialDescriptions[i].unk2 = br.ReadInt32();
                // materialDescriptions[i].unk3 = br.ReadInt32();
                // materialDescriptions[i].unk4 = br.ReadInt32();
                // materialDescriptions[i].unk5 = br.ReadInt32();
                // materialDescriptions[i].unk6 = br.ReadInt32();
                materialDescriptions[i].unk0 = br.ReadUInt16();
                materialDescriptions[i].unk1 = br.ReadUInt16();
                materialDescriptions[i].unk2 = br.ReadUInt16();
                materialDescriptions[i].unk3 = br.ReadUInt16();
                materialDescriptions[i].unk4 = br.ReadUInt16();
                materialDescriptions[i].unk5 = br.ReadUInt16();
                materialDescriptions[i].unk6 = br.ReadUInt16();
                materialDescriptions[i].unk7 = br.ReadUInt16();
                materialDescriptions[i].unk8 = br.ReadUInt16();
                materialDescriptions[i].unk9 = br.ReadUInt16();
                materialDescriptions[i].unk10 = br.ReadUInt16();
                materialDescriptions[i].unk11 = br.ReadUInt16();
                materialDescriptions[i].unk12 = br.ReadUInt16();
                materialDescriptions[i].unk13 = br.ReadUInt16();
                materialDescriptions[i].unk14 = br.ReadUInt16();
                materialDescriptions[i].unk15 = br.ReadUInt16();
                materialDescriptions[i].unk16 = br.ReadUInt16();
                materialDescriptions[i].unk17 = br.ReadUInt16();
                materialDescriptions[i].unk18 = br.ReadUInt16();
                materialDescriptions[i].unk19 = br.ReadUInt16();
                materialDescriptions[i].unk20 = br.ReadUInt16();
                materialDescriptions[i].unk21 = br.ReadUInt16();
                materialDescriptions[i].unk22 = br.ReadUInt16();
                materialDescriptions[i].unk23 = br.ReadUInt16();
                materialDescriptions[i].unk24 = br.ReadUInt16();
                materialDescriptions[i].unk25 = br.ReadUInt16();
                materialDescriptions[i].unk26 = br.ReadUInt16();
                materialDescriptions[i].unk27 = br.ReadUInt16();
                materialDescriptions[i].unk28 = br.ReadUInt16();
                materialDescriptions[i].unk29 = br.ReadUInt16();
                materialDescriptions[i].unk30 = br.ReadUInt16();
                materialDescriptions[i].unk31 = br.ReadUInt16();
                materialDescriptions[i].unk32 = br.ReadUInt16();
                materialDescriptions[i].unk33 = br.ReadUInt16();
                materialDescriptions[i].unk34 = br.ReadUInt16();
                materialDescriptions[i].unk35 = br.ReadUInt16();
                materialDescriptions[i].unk36 = br.ReadUInt16();
                materialDescriptions[i].unk37 = br.ReadUInt16();
                materialDescriptions[i].unk38 = br.ReadUInt16();
                materialDescriptions[i].unk39 = br.ReadUInt16();
                materialDescriptions[i].unk40 = br.ReadUInt16();
                materialDescriptions[i].unk41 = br.ReadUInt16();
                materialDescriptions[i].unk42 = br.ReadUInt16();
                materialDescriptions[i].unk43 = br.ReadUInt16();
                materialDescriptions[i].unk44 = br.ReadUInt16();
                materialDescriptions[i].unk45 = br.ReadUInt16();
                materialDescriptions[i].unk46 = br.ReadUInt16();
                materialDescriptions[i].unk47 = br.ReadUInt16();
                materialDescriptions[i].unk48 = br.ReadUInt16();
                materialDescriptions[i].unk49 = br.ReadUInt16();
                materialDescriptions[i].unk50 = br.ReadUInt16();
                materialDescriptions[i].unk51 = br.ReadUInt16();
                materialDescriptions[i].unk52 = br.ReadUInt16();
                materialDescriptions[i].unk53 = br.ReadUInt16();
                materialDescriptions[i].unk54 = br.ReadUInt16();
                materialDescriptions[i].unk55 = br.ReadUInt16();
                materialDescriptions[i].unk56 = br.ReadUInt16();
                materialDescriptions[i].unk57 = br.ReadUInt16();
                materialDescriptions[i].unk58 = br.ReadUInt16();
                materialDescriptions[i].unk59 = br.ReadUInt16();
                materialDescriptions[i].unk60 = br.ReadUInt16();
                materialDescriptions[i].unk61 = br.ReadUInt16();
                materialDescriptions[i].unk62 = br.ReadUInt16();
                materialDescriptions[i].unk63 = br.ReadUInt16();
                materialDescriptions[i].unk64 = br.ReadUInt16();
                materialDescriptions[i].unk65 = br.ReadUInt16();
                materialDescriptions[i].unk66 = br.ReadUInt16();
                materialDescriptions[i].unk67 = br.ReadUInt16();
                materialDescriptions[i].unk68 = br.ReadUInt16();
                materialDescriptions[i].unk69 = br.ReadUInt16();
                materialDescriptions[i].unk70 = br.ReadUInt16();
                materialDescriptions[i].unk71 = br.ReadUInt16();
                materialDescriptions[i].unk72 = br.ReadUInt16();
                materialDescriptions[i].unk73 = br.ReadUInt16();
                materialDescriptions[i].unk74 = br.ReadUInt16();
                materialDescriptions[i].unk75 = br.ReadUInt16();
                materialDescriptions[i].unk76 = br.ReadUInt16();
                materialDescriptions[i].unk77 = br.ReadUInt16();
                materialDescriptions[i].unk78 = br.ReadUInt16();
                materialDescriptions[i].unk79 = br.ReadUInt16();
                materialDescriptions[i].unk80 = br.ReadUInt16();
                materialDescriptions[i].unk81 = br.ReadUInt16();
                materialDescriptions[i].unk82 = br.ReadUInt16();
                materialDescriptions[i].unk83 = br.ReadUInt16();
                materialDescriptions[i].unk84 = br.ReadUInt16();
                materialDescriptions[i].unk85 = br.ReadUInt16();
                materialDescriptions[i].unk86 = br.ReadUInt16();
                materialDescriptions[i].unk87 = br.ReadUInt16();
                materialDescriptions[i].unk88 = br.ReadUInt16();
                materialDescriptions[i].unk89 = br.ReadUInt16();
                materialDescriptions[i].unk90 = br.ReadUInt16();
                materialDescriptions[i].unk91 = br.ReadUInt16();
                materialDescriptions[i].unk92 = br.ReadUInt16();
                materialDescriptions[i].unk93 = br.ReadUInt16();
                materialDescriptions[i].unk94 = br.ReadUInt16();
                materialDescriptions[i].unk95 = br.ReadUInt16();
                materialDescriptions[i].unk96 = br.ReadUInt16();
                materialDescriptions[i].unk97 = br.ReadUInt16();
                materialDescriptions[i].unk98 = br.ReadUInt16();
                materialDescriptions[i].unk99 = br.ReadUInt16();
                materialDescriptions[i].unk100 = br.ReadUInt16();
                materialDescriptions[i].unk101 = br.ReadUInt16();
                materialDescriptions[i].unk102 = br.ReadUInt16();
                materialDescriptions[i].unk103 = br.ReadUInt16();
                materialDescriptions[i].unk104 = br.ReadUInt16();
                materialDescriptions[i].unk105 = br.ReadUInt16();
                materialDescriptions[i].unk106 = br.ReadUInt16();
                materialDescriptions[i].unk107 = br.ReadUInt16();
                materialDescriptions[i].unk108 = br.ReadUInt16();
                materialDescriptions[i].unk109 = br.ReadUInt16();
                materialDescriptions[i].unk110 = br.ReadUInt16();
                materialDescriptions[i].unk111 = br.ReadUInt16();
                materialDescriptions[i].unk112 = br.ReadUInt16();
                materialDescriptions[i].unk113 = br.ReadUInt16();
                materialDescriptions[i].unk114 = br.ReadUInt16();
                materialDescriptions[i].unk115 = br.ReadUInt16();
                materialDescriptions[i].unk116 = br.ReadUInt16();
                materialDescriptions[i].unk117 = br.ReadUInt16();
                materialDescriptions[i].unk118 = br.ReadUInt16();
                materialDescriptions[i].unk119 = br.ReadUInt16();
                materialDescriptions[i].unk120 = br.ReadUInt16();
                materialDescriptions[i].unk121 = br.ReadUInt16();
                materialDescriptions[i].unk122 = br.ReadUInt16();
                materialDescriptions[i].unk123 = br.ReadUInt16();
                materialDescriptions[i].unk124 = br.ReadUInt16();
                materialDescriptions[i].unk125 = br.ReadUInt16();
                materialDescriptions[i].unk126 = br.ReadUInt16();
                materialDescriptions[i].unk127 = br.ReadUInt16();
                materialDescriptions[i].unk128 = br.ReadUInt16();
                materialDescriptions[i].unk129 = br.ReadUInt16();
                materialDescriptions[i].unk130 = br.ReadUInt16();
                materialDescriptions[i].unk131 = br.ReadUInt16();
                materialDescriptions[i].unk132 = br.ReadUInt16();
                materialDescriptions[i].unk133 = br.ReadUInt16();
                materialDescriptions[i].unk134 = br.ReadUInt16();
                materialDescriptions[i].unk135 = br.ReadUInt16();
                materialDescriptions[i].unk136 = br.ReadUInt16();
                materialDescriptions[i].unk137 = br.ReadUInt16();
                materialDescriptions[i].unk138 = br.ReadUInt16();
                materialDescriptions[i].unk139 = br.ReadUInt16();
                materialDescriptions[i].unk140 = br.ReadUInt16();
                materialDescriptions[i].unk141 = br.ReadUInt16();
                materialDescriptions[i].unk142 = br.ReadUInt16();
                materialDescriptions[i].unk143 = br.ReadUInt16();
                materialDescriptions[i].unk144 = br.ReadUInt16();
                materialDescriptions[i].unk145 = br.ReadUInt16();
                if(materialDescriptions[i].textureSelectorA > -1){
                    materialDescriptions[i].textureColor = header.textures[materialDescriptions[i].textureSelectorA].path.Split(".")[0] + ".tex";
                }
                if(materialDescriptions[i].textureSelectorB > -1){
                    materialDescriptions[i].textureNormal = header.textures[materialDescriptions[i].textureSelectorB].path.Split(".")[0] + ".tex";
                }
                // materialDescriptions[i].Print(i);
            }
            return materialDescriptions;
        }
        public void Print(int id){
            // var pr = string.Format("MATERIAL DESCRIPTION: {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
            // id, this.textureSelectorA, this.textureSelectorB, this.unk0, this.unk1, this.unk2, this.unk3, this.unk4, this.unk5, this.unk6, this.unk6, this.unk6, this.unk6);
            // Debug.Log(pr);
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log("MATERIAL DESCRIPTION: " + pr);
        }
        public static int GetSize(){
            return 296;
        }
    }
    
    [System.Serializable]
    public struct Bone{
        public Int32 unk00, unk06;
        public Int16 unk01;
        public byte unk02, unk03, unk04, unk05;
        public int id;
        public Int32 parent_id;
        public Matrix4x4 TM, InverseTM, rotationMatrix;
        public Vector3 position;

        public AnimationTrack timestamps1, timestamps2, timestamps3, timestamps4, timestamps5, timestamps6, timestamps7, timestamps8;
        
        public static Bone[] ReadAll(BinaryReader br, long startPos, int nr){
            var bones = new Bone[nr];
            for(var i = 0;i<nr;i++){
                br.BaseStream.Position = startPos + i*Bone.GetSize();

                bones[i].id = i;
                bones[i].unk00 = br.ReadInt32();    //flags
                bones[i].parent_id = br.ReadInt16();
                bones[i].unk01 = br.ReadInt16(); // submesh id?
                bones[i].unk02 = br.ReadByte(); // unk
                bones[i].unk03 = br.ReadByte(); // unk
                bones[i].unk04 = br.ReadByte(); // unk
                bones[i].unk05 = br.ReadByte(); // unk
                bones[i].unk06 = br.ReadInt32(); //padding?
                var vv1 = new Vector4(bones[i].unk02, 0, bones[i].unk03, 0);
                var vv2 = new Vector4(0, 1, 0, 0);
                var vv3 = new Vector4(bones[i].unk04, 0, bones[i].unk05, 0);
                var vv4 = new Vector4(0, 0, 0, 1);
                bones[i].rotationMatrix = new Matrix4x4(vv1, vv2, vv3, vv4);
                
                // KEY FRAMES LOCATIONS
                bones[i].timestamps1 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 1);
                bones[i].timestamps2 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 2);
                bones[i].timestamps3 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 3);
                bones[i].timestamps4 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 4);
                bones[i].timestamps5 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 5);
                bones[i].timestamps6 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 6);
                bones[i].timestamps7 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 7);
                bones[i].timestamps8 = new Bone.AnimationTrack(br.ReadUInt64(), br.ReadInt64(), br.ReadInt64(), 8);
                // TRANSLATION MATRICES
                br.BaseStream.Position = startPos + i*Bone.GetSize() + 0xD0;   // can be removed?
                var v1 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var v2 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var v3 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var v4 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                bones[i].TM = new Matrix4x4(v1, v2, v3, v4);
                br.BaseStream.Position = startPos + i*Bone.GetSize() + 0x110;   // can be removed?
                v1 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                v2 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                v3 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                v4 = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                bones[i].InverseTM = new Matrix4x4(v1, v2, v3, v4);
                br.BaseStream.Position = startPos + i*Bone.GetSize() + 0x150;   // can be removed?
                bones[i].position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                var bone = bones[i];
                float unk02 = ((float)bone.unk02 - 127.0f)/127.0f;
                float unk03 = ((float)bone.unk03 - 127.0f)/127.0f;
                float unk04 = ((float)bone.unk04 - 127.0f)/127.0f;
                float unk05 = ((float)bone.unk05 - 127.0f)/127.0f;
                if(unk02 + unk03 + unk04 + unk05 != -4){
                    // var q = new Quaternion(unk02, unk03, unk04, unk05);
                    // bones[i].InverseTM = bones[i].InverseTM * Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
                }
                // bones[i].rotationMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
                // Debug.Log(string.Format("BONE_{0}:\t {1}\t {2}\t {3}\t {4}\t {5}\t {6}\t {7}\t {8}\t {9} {10}", bone.id, bone.parent_id, bone.unk00, bone.unk01, unk02, unk03, unk04, unk05, bone.unk06, bone.TM.ValidTRS(), bone.TM.ValidTRS()));
                // Debug.Log(string.Format("BONE_{0}:\t {1}\t {2}\t {3}\t {4}\t {5}\t {6}\t {7}\t {8}\t {9} {10}", bone.id, bone.parent_id, bone.unk00, bone.unk01, bone.unk02, bone.unk03, bone.unk04, bone.unk05, bone.unk06, bone.TM.ValidTRS(), bone.TM.ValidTRS()));
                // bone.timestamps1.PrintEstimates();
                // bone.timestamps2.PrintEstimates();
                // bone.timestamps3.PrintEstimates();
                // bone.timestamps4.PrintEstimates();
                // bone.timestamps5.PrintEstimates();
                // bone.timestamps6.PrintEstimates();
                // bone.timestamps7.PrintEstimates();
            }
            var animation_start = startPos+nr*Bone.GetSize();
            for(var i = 0;i<nr;i++){
                bones[i].timestamps1.ReadKeyFrames(br, animation_start);
                bones[i].timestamps2.ReadKeyFrames(br, animation_start);
                bones[i].timestamps3.ReadKeyFrames(br, animation_start);
                bones[i].timestamps4.ReadKeyFrames(br, animation_start);
                bones[i].timestamps5.ReadKeyFrames(br, animation_start);
                bones[i].timestamps6.ReadKeyFrames(br, animation_start);
                bones[i].timestamps7.ReadKeyFrames(br, animation_start);
                bones[i].timestamps8.ReadKeyFrames(br, animation_start);
            }
            return bones;
        }

        public struct AnimationTrack{
            public int format;
            // 1 -> seems to store 6 bytes per keyframe split into 3 halfs
            // 2 -> unknown, Rowsdower does not have data here.
            // 3 -> 2 singles?
            // 4 -> unknown, Rowsdower does not have data here.
            // 5 -> either 2 singles or 4 halfs. maybe rotations?? (some values appear to be NaN....)
            // 6 -> 4 halfs. some values are large, others are NaN
            // 7 -> 3 singles.
            // 8 -> 4 halfs. some values are large, others are NaN
            public UInt64 nr;
            public Int64 timestampOfset;
            public Int64 valueOfset;

            public KeyFrame[] keyFrames;
            public AnimationTrack(UInt64 nr, Int64 timestampOfset, Int64 valueOfset, int format){
                this.format = format;
                this.nr = nr;
                this.timestampOfset = timestampOfset;
                this.valueOfset = valueOfset;
                this.keyFrames = new KeyFrame[nr];
            }
            public void ReadKeyFrames(BinaryReader br, long startIndex){
                if(this.nr == 0){
                    return;
                }
                br.BaseStream.Position = startIndex + this.timestampOfset;
                for(var i=0; i < (int)this.nr; i++){
                    this.keyFrames[i] = new KeyFrame((float)(br.ReadUInt32()/1000.0f));
                }
                br.BaseStream.Position = startIndex + this.valueOfset;
                if(this.format == 1){
                    for(var i=0; i < (int)this.nr; i++){
                        var v1 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v2 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v3 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        this.keyFrames[i].SetScale(v1, v2, v3);
                    }
                }else if(this.format == 3){
                    for(var i=0; i < (int)this.nr; i++){
                        var v1 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v2 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v3 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        this.keyFrames[i].SetScale(v1, v2, v3);
                    }
                }else if(this.format == 5 || this.format == 6){
                    for(var i=0; i < (int)this.nr; i++){
                        var v1 = br.ReadInt16();
                        var v2 = br.ReadInt16();
                        var v3 = br.ReadInt16();
                        var v4 = br.ReadInt16();
                        this.keyFrames[i].SetQuaternion(v1, v2, v3, v4);
                    }
                }else if(this.format == 7){
                    for(var i=0; i < (int)this.nr; i++){
                        this.keyFrames[i].SetValues(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), 0);
                    }
                }else if(this.format == 8){
                    for(var i=0; i < (int)this.nr; i++){
                        var v1 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v2 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v3 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v4 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        this.keyFrames[i].SetValues(v1, v2, v3, v4);
                    }
                }else{
                    for(var i=0; i < (int)this.nr; i++){
                        var v1 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v2 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v3 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        var v4 = SystemHalf.Half.ToHalf(br.ReadBytes(2),0);
                        this.keyFrames[i].SetValues(v1, v2, v3, v4);
                    }
                }
                // Debug.Log(string.Format("KEYFRAME_00: {0}, {1}", br.ReadInt32(), br.ReadInt32()));
                // Debug.Log(string.Format("KEYFRAME_00: {0}, {1}, {2}, {3}", br.ReadInt16(), br.ReadInt16(), br.ReadInt16(), br.ReadInt16()));
                // Debug.Log(string.Format("KEYFRAME_00: {0}, {1}", br.ReadInt32(), br.ReadInt32()));

            }
            public void EstimateSize(){
                Debug.Log(string.Format("NR: {0},        {1}", this.nr, (float)(this.valueOfset-this.timestampOfset)/(float)this.nr));
            }
            public void Print(){
                Debug.Log(string.Format("Timestamps: {0}, {1}, {2}", this.nr, this.timestampOfset, this.valueOfset));
            }
            public void PrintEstimates(){
                var est1 = this.timestampOfset+(int)this.nr*4;
                var est2 = this.valueOfset+(int)this.nr*8;
                if(this.format == 1){
                    est2 = this.valueOfset+(int)this.nr*6;
                }else if(this.format == 7){
                    est2 = this.valueOfset+(int)this.nr*12;
                }
                Debug.Log(string.Format("Timestamps{0}: {1}, {2}, {3}, {4}, {5}", this.format, this.nr, this.timestampOfset, est1, this.valueOfset, est2));
            }
            public void PrintKeyFrames(){
                for(var i=0; i < (int)this.nr; i++){
                    this.keyFrames[i].Print();
                }
            }
        }
        public struct KeyFrame{
            public float timeStamp;
            public float funk1, funk2, funk3, funk4;
            public Quaternion q;
            public Vector3 s;
            public KeyFrame(float timestampValue){
                this.timeStamp = timestampValue;
                this.q = new Quaternion();
                this.s = new Vector3();
                this.funk1 = 0; 
                this.funk2 = 0;
                this.funk3 = 0;
                this.funk4 = 0;
            }
            private float Int16ToFloat(Int16 value){
                return (float) (value/16383.5f);
            }
            public void SetScale(float x, float y, float z){
                this.s = new Vector3(x,y,z);
            }
            public void SetValues(float unk1, float unk2, float unk3, float unk4){
                this.funk1 = unk1; 
                this.funk2 = unk2;
                this.funk3 = unk3;
                this.funk4 = unk4;
            }
            public void SetQuaternion(Int16 x, Int16 y, Int16 z, Int16 w){
                this.q = new Quaternion(this.Int16ToFloat(x),this.Int16ToFloat(y),this.Int16ToFloat(z),this.Int16ToFloat(w));
            }
            public void Print(){
                Debug.Log(string.Format("KEYFRAME: Timestamp:{0}, Values: {1}, {2}, {3}, {4}", this.timeStamp, this.funk1, this.funk2, this.funk3, this.funk4));
            }
            public void Print2(){
                Debug.Log(string.Format("KEYFRAME: Timestamp:{0}, Quaternion: {1}", this.timeStamp, this.q));
            }
        }

        public static int GetSize(){
            return 176*2;
        }
    }
    
    [System.Serializable]
    public struct ModelAnimations{
        public UInt16 modelSequenceDBid;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 fallbackSequence;
        public UInt32 timestampStart;
        public UInt32 timestampEnd;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public Vector3 unk16;
        public UInt32 unk19;    //padding?
        public Vector3 unk20;
        public UInt32 unk23;    //padding?
        public Vector3 unk24;
        public UInt32 unk25;    //padding?
        public Vector3 unk26;
        public UInt32 unk27;    //padding?
        public UInt64 unk28;    // only appears when there are 2 variants of the same animation. probably refers to a specific group or variant of the object. So animation 150-0 is playd with every variant, and 150-6 is played only with variant 6.
        public UInt64 unk29;
        public static ModelAnimations[] ReadAll(BinaryReader br, long startPos, int nr){
            var modelAnimations = new ModelAnimations[nr];
            for(var i=0; i<nr;i++){
                br.BaseStream.Position = startPos + i*ModelAnimations.GetSize();
                modelAnimations[i].modelSequenceDBid = br.ReadUInt16();
                modelAnimations[i].unk1 = br.ReadUInt16();
                modelAnimations[i].unk2 = br.ReadUInt16();
                modelAnimations[i].unk3 = br.ReadUInt16();
                modelAnimations[i].unk4 = br.ReadUInt16();
                modelAnimations[i].fallbackSequence = br.ReadUInt16();
                modelAnimations[i].timestampStart = br.ReadUInt32();      // 0x010
                modelAnimations[i].timestampEnd = br.ReadUInt32();
                modelAnimations[i].unk10 = br.ReadUInt16();
                modelAnimations[i].unk11 = br.ReadUInt16();
                modelAnimations[i].unk12 = br.ReadUInt16();
                modelAnimations[i].unk13 = br.ReadUInt16();
                modelAnimations[i].unk14 = br.ReadUInt16();
                modelAnimations[i].unk15 = br.ReadUInt16();      // 0x020
                modelAnimations[i].unk16 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                modelAnimations[i].unk19 = br.ReadUInt32();      // 0x030
                modelAnimations[i].unk20 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                modelAnimations[i].unk23 = br.ReadUInt32();      // 0x040
                modelAnimations[i].unk24 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                modelAnimations[i].unk25 = br.ReadUInt32();      // 0x050
                modelAnimations[i].unk26 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                modelAnimations[i].unk27 = br.ReadUInt32();      // 0x060
                modelAnimations[i].unk28 = br.ReadUInt64();
                modelAnimations[i].unk29 = br.ReadUInt64();
                // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = modelAnimations[i].unk16;
                // sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = modelAnimations[i].unk20;
                // sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = modelAnimations[i].unk24;
                // sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // sphere.transform.position = modelAnimations[i].unk26;
                // modelAnimations[i].Print();
            }
            return modelAnimations;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log(pr);
        }
        public static int GetSize(){
            return 112;
        }
    }
    
    [System.Serializable]
    public struct AnimationRelated020{
        public UInt32[] timestamps;
        public UInt16[] unk0;
        public static AnimationRelated020 ReadAll(BinaryReader br, long startPosA, long startPosB, int nr){
            var animationRelated = new AnimationRelated020();
            animationRelated.timestamps = new UInt32[nr];
            animationRelated.unk0 = new UInt16[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPosA;
                animationRelated.timestamps[i] = br.ReadUInt32();
            }
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPosB;
                animationRelated.timestamps[i] = br.ReadUInt16();
            }
            // animationRelated.Print();
            return animationRelated;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log(pr);
        }
    }
    
    [System.Serializable]
    public struct Unk1D0{
        public UInt16[] unk0;
        public static Unk1D0 ReadAll(BinaryReader br, long startPos, int nr){
            var unk1D0 = new Unk1D0();
            unk1D0.unk0 = new UInt16[nr];
            br.BaseStream.Position = 1584 + startPos;
            for(var i=0;i<nr;i++){
                unk1D0.unk0[i] = br.ReadUInt16();
            }
            unk1D0.Print();
            return unk1D0;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.unk0.Length; i++){
                pr += string.Format("{0}\t", this.unk0[i]);
            }
            Debug.Log(pr);
        }
    }
    
    [System.Serializable]
    public struct Unk2F8{
        public UInt64 unk0;
        public UInt64 unk4;
        public UInt32 unk16;
        public UInt32 unk17;
        public UInt32 unk18;
        public UInt32 unk19;
        public UInt64 unk20;
        public UInt32 unk21;
        public UInt32 unk22;
        public UInt64 nrUnk0;
        public UInt64 ofsUnk0;
        public UInt64 ofsUnk1;
        public UInt64 ofsUnk2;
        public UInt64 nrUnk1;
        public UInt64 ofsUnk3;
        public UInt64 ofsUnk4;
        public Vector3 unkVec0;
        public Vector3 unkVec1;
        public UInt64 ofsUnk5;
        public static Unk2F8[] ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos;
            var unk2F8s = new Unk2F8[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPos + i*Unk2F8.GetSize();
                unk2F8s[i].unk0 = br.ReadUInt64();
                unk2F8s[i].unk4 = br.ReadUInt64();
                unk2F8s[i].unk16 = br.ReadUInt32();
                unk2F8s[i].unk17 = br.ReadUInt32();
                unk2F8s[i].unk18 = br.ReadUInt32();
                unk2F8s[i].unk19 = br.ReadUInt32();
                unk2F8s[i].unk20 = br.ReadUInt64();
                unk2F8s[i].unk21 = br.ReadUInt32();
                unk2F8s[i].unk22 = br.ReadUInt32();
                unk2F8s[i].nrUnk0 = br.ReadUInt64();
                unk2F8s[i].ofsUnk0 = br.ReadUInt64();
                unk2F8s[i].ofsUnk1 = br.ReadUInt64();
                unk2F8s[i].ofsUnk2 = br.ReadUInt64();
                unk2F8s[i].nrUnk1 = br.ReadUInt64();
                unk2F8s[i].ofsUnk3 = br.ReadUInt64();
                unk2F8s[i].ofsUnk4 = br.ReadUInt64();
                br.ReadUInt64(); // padding?
                unk2F8s[i].unkVec0 = new Vector3(br.ReadSingle(),br.ReadSingle(),br.ReadSingle());
                br.ReadInt32(); // padding?
                unk2F8s[i].unkVec1 = new Vector3(br.ReadSingle(),br.ReadSingle(),br.ReadSingle());
                br.ReadInt32(); // padding?
                unk2F8s[i].ofsUnk5 = br.ReadUInt64();
                br.ReadUInt64(); // padding
                // unk2F8s[i].Print();
            }
            return unk2F8s;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log(pr);
        }
        public static int GetSize(){
            return 160; //0xA0
        }
    }
    
    [System.Serializable]
    public struct Unk080{
        public static Unk080 ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos; //2704
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            Debug.Log(string.Format("Unk080: {0}\t{1}\t{2}\t{3}", br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()));
            return new Unk080();
        }
    }
    
    [System.Serializable]
    public struct Unk0F0{
        public UInt16 unk0;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public UInt16 unk16;
        public UInt16 unk17;
        public UInt16 unk18;
        public UInt16 unk19;
        public UInt16 unk20;
        public UInt16 unk21;
        public UInt16 unk22;
        public UInt16 unk23;
        public UInt16 unk24;
        public UInt16 unk25;
        public UInt16 unk26;
        public UInt16 unk27;
        public UInt16 unk28;
        public UInt16 unk29;
        public UInt16 unk30;
        public UInt16 unk31;
        public UInt16 unk32;
        public UInt16 unk33;
        public UInt16 unk34;
        public UInt16 unk35;
        public UInt16 unk36;
        public UInt16 unk37;
        public UInt16 unk38;
        public UInt16 unk39;
        public UInt16 unk40;
        public UInt16 unk41;
        public UInt16 unk42;
        public UInt16 unk43;
        public UInt16 unk44;
        public UInt16 unk45;
        public UInt16 unk46;
        public UInt16 unk47;
        public UInt16 unk48;
        public UInt16 unk49;
        public UInt16 unk50;
        public UInt16 unk51;
        public UInt16 unk52;
        public UInt16 unk53;
        public UInt16 unk54;
        public UInt16 unk55;
        public UInt16 unk56;
        public UInt16 unk57;
        public UInt16 unk58;
        public UInt16 unk59;
        public UInt16 unk60;
        public UInt16 unk61;
        public UInt16 unk62;
        public UInt16 unk63;
        public UInt16 unk64;
        public UInt16 unk65;
        public UInt16 unk66;
        public UInt16 unk67;
        public UInt16 unk68;
        public UInt16 unk69;
        public UInt16 unk70;
        public UInt16 unk71;
        public UInt16 unk72;
        public UInt16 unk73;
        public UInt16 unk74;
        public UInt16 unk75;
        public UInt16 unk76;
        public UInt16 unk77;
        public UInt16 unk78;
        public UInt16 unk79;
        public UInt16 unk80;
        public UInt16 unk81;
        public UInt16 unk82;
        public UInt16 unk83;
        public UInt16 unk84;
        public UInt16 unk85;
        public UInt16 unk86;
        public UInt16 unk87;
        public UInt16 unk88;
        public UInt16 unk89;
        public UInt16 unk90;
        public UInt16 unk91;
        public static Unk0F0[] ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos;
            var unk0F0s = new Unk0F0[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPos + i*Unk0F0.GetSize();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk0 = br.ReadUInt16();
                // unk0F0s[i].unk1 = br.ReadUInt16();
                // unk0F0s[i].unk2 = br.ReadUInt16();
                // unk0F0s[i].unk3 = br.ReadUInt16();
                // unk0F0s[i].unk4 = br.ReadUInt16();
                // unk0F0s[i].unk5 = br.ReadUInt16();
                // unk0F0s[i].unk6 = br.ReadUInt16();
                // unk0F0s[i].unk7 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk8 = br.ReadUInt16();
                // unk0F0s[i].unk9 = br.ReadUInt16();
                // unk0F0s[i].unk10 = br.ReadUInt16();
                // unk0F0s[i].unk11 = br.ReadUInt16();
                // unk0F0s[i].unk12 = br.ReadUInt16();
                // unk0F0s[i].unk13 = br.ReadUInt16();
                // unk0F0s[i].unk14 = br.ReadUInt16();
                // unk0F0s[i].unk15 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk16 = br.ReadUInt16();
                // unk0F0s[i].unk17 = br.ReadUInt16();
                // unk0F0s[i].unk18 = br.ReadUInt16();
                // unk0F0s[i].unk19 = br.ReadUInt16();
                // unk0F0s[i].unk20 = br.ReadUInt16();
                // unk0F0s[i].unk21 = br.ReadUInt16();
                // unk0F0s[i].unk22 = br.ReadUInt16();
                // unk0F0s[i].unk23 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk24 = br.ReadUInt16();
                // unk0F0s[i].unk25 = br.ReadUInt16();
                // unk0F0s[i].unk26 = br.ReadUInt16();
                // unk0F0s[i].unk27 = br.ReadUInt16();
                // unk0F0s[i].unk28 = br.ReadUInt16();
                // unk0F0s[i].unk29 = br.ReadUInt16();
                // unk0F0s[i].unk30 = br.ReadUInt16();
                // unk0F0s[i].unk31 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk32 = br.ReadUInt16();
                // unk0F0s[i].unk33 = br.ReadUInt16();
                // unk0F0s[i].unk34 = br.ReadUInt16();
                // unk0F0s[i].unk35 = br.ReadUInt16();
                // unk0F0s[i].unk36 = br.ReadUInt16();
                // unk0F0s[i].unk37 = br.ReadUInt16();
                // unk0F0s[i].unk38 = br.ReadUInt16();
                // unk0F0s[i].unk39 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk40 = br.ReadUInt16();
                // unk0F0s[i].unk41 = br.ReadUInt16();
                // unk0F0s[i].unk42 = br.ReadUInt16();
                // unk0F0s[i].unk43 = br.ReadUInt16();
                // unk0F0s[i].unk44 = br.ReadUInt16();
                // unk0F0s[i].unk45 = br.ReadUInt16();
                // unk0F0s[i].unk46 = br.ReadUInt16();
                // unk0F0s[i].unk47 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk48 = br.ReadUInt16();
                // unk0F0s[i].unk49 = br.ReadUInt16();
                // unk0F0s[i].unk50 = br.ReadUInt16();
                // unk0F0s[i].unk51 = br.ReadUInt16();
                // unk0F0s[i].unk52 = br.ReadUInt16();
                // unk0F0s[i].unk53 = br.ReadUInt16();
                // unk0F0s[i].unk54 = br.ReadUInt16();
                // unk0F0s[i].unk55 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk56 = br.ReadUInt16();
                // unk0F0s[i].unk57 = br.ReadUInt16();
                // unk0F0s[i].unk58 = br.ReadUInt16();
                // unk0F0s[i].unk59 = br.ReadUInt16();
                // unk0F0s[i].unk60 = br.ReadUInt16();
                // unk0F0s[i].unk61 = br.ReadUInt16();
                // unk0F0s[i].unk62 = br.ReadUInt16();
                // unk0F0s[i].unk63 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk64 = br.ReadUInt16();
                // unk0F0s[i].unk65 = br.ReadUInt16();
                // unk0F0s[i].unk66 = br.ReadUInt16();
                // unk0F0s[i].unk67 = br.ReadUInt16();
                // unk0F0s[i].unk68 = br.ReadUInt16();
                // unk0F0s[i].unk69 = br.ReadUInt16();
                // unk0F0s[i].unk70 = br.ReadUInt16();
                // unk0F0s[i].unk71 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk72 = br.ReadUInt16();
                // unk0F0s[i].unk73 = br.ReadUInt16();
                // unk0F0s[i].unk74 = br.ReadUInt16();
                // unk0F0s[i].unk75 = br.ReadUInt16();
                // unk0F0s[i].unk76 = br.ReadUInt16();
                // unk0F0s[i].unk77 = br.ReadUInt16();
                // unk0F0s[i].unk78 = br.ReadUInt16();
                // unk0F0s[i].unk79 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk80 = br.ReadUInt16();
                // unk0F0s[i].unk81 = br.ReadUInt16();
                // unk0F0s[i].unk82 = br.ReadUInt16();
                // unk0F0s[i].unk83 = br.ReadUInt16();
                // unk0F0s[i].unk84 = br.ReadUInt16();
                // unk0F0s[i].unk85 = br.ReadUInt16();
                // unk0F0s[i].unk86 = br.ReadUInt16();
                // unk0F0s[i].unk87 = br.ReadUInt16();
                unk0F0s[i].unk88 = br.ReadUInt16();
                unk0F0s[i].unk89 = br.ReadUInt16();
                unk0F0s[i].unk90 = br.ReadUInt16();
                unk0F0s[i].unk91 = br.ReadUInt16();
                unk0F0s[i].Print();
                br.BaseStream.Position = 1584 + startPos + 16;
            }
            return unk0F0s;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log("UNK0F0: " + pr);
        }
        public static int GetSize(){
            // 184
            return 184;
        }
    }
    
    [System.Serializable]
    public struct Unk08{
        public UInt16 unk0;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public UInt16 unk16;
        public UInt16 unk17;
        public UInt16 unk18;
        public UInt16 unk19;
        public UInt16 unk20;
        public UInt16 unk21;
        public UInt16 unk22;
        public UInt16 unk23;
        public UInt16 unk24;
        public UInt16 unk25;
        public UInt16 unk26;
        public UInt16 unk27;
        public UInt16 unk28;
        public UInt16 unk29;
        public UInt16 unk30;
        public UInt16 unk31;
        public UInt16 unk32;
        public UInt16 unk33;
        public UInt16 unk34;
        public UInt16 unk35;
        public UInt16 unk36;
        public UInt16 unk37;
        public UInt16 unk38;
        public UInt16 unk39;
        public UInt16 unk40;
        public UInt16 unk41;
        public UInt16 unk42;
        public UInt16 unk43;
        public UInt16 unk44;
        public UInt16 unk45;
        public UInt16 unk46;
        public UInt16 unk47;
        public UInt16 unk48;
        public UInt16 unk49;
        public UInt16 unk50;
        public UInt16 unk51;
        public UInt16 unk52;
        public UInt16 unk53;
        public UInt16 unk54;
        public UInt16 unk55;
        public static Unk08[] ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos;
            var unk0F0s = new Unk08[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPos + i*Unk08.GetSize();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk0 = br.ReadUInt16();
                // unk0F0s[i].unk1 = br.ReadUInt16();
                // unk0F0s[i].unk2 = br.ReadUInt16();
                // unk0F0s[i].unk3 = br.ReadUInt16();
                // unk0F0s[i].unk4 = br.ReadUInt16();
                // unk0F0s[i].unk5 = br.ReadUInt16();
                // unk0F0s[i].unk6 = br.ReadUInt16();
                // unk0F0s[i].unk7 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk8 = br.ReadUInt16();
                // unk0F0s[i].unk9 = br.ReadUInt16();
                // unk0F0s[i].unk10 = br.ReadUInt16();
                // unk0F0s[i].unk11 = br.ReadUInt16();
                // unk0F0s[i].unk12 = br.ReadUInt16();
                // unk0F0s[i].unk13 = br.ReadUInt16();
                // unk0F0s[i].unk14 = br.ReadUInt16();
                // unk0F0s[i].unk15 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk16 = br.ReadUInt16();
                // unk0F0s[i].unk17 = br.ReadUInt16();
                // unk0F0s[i].unk18 = br.ReadUInt16();
                // unk0F0s[i].unk19 = br.ReadUInt16();
                // unk0F0s[i].unk20 = br.ReadUInt16();
                // unk0F0s[i].unk21 = br.ReadUInt16();
                // unk0F0s[i].unk22 = br.ReadUInt16();
                // unk0F0s[i].unk23 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk24 = br.ReadUInt16();
                // unk0F0s[i].unk25 = br.ReadUInt16();
                // unk0F0s[i].unk26 = br.ReadUInt16();
                // unk0F0s[i].unk27 = br.ReadUInt16();
                // unk0F0s[i].unk28 = br.ReadUInt16();
                // unk0F0s[i].unk29 = br.ReadUInt16();
                // unk0F0s[i].unk30 = br.ReadUInt16();
                // unk0F0s[i].unk31 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk32 = br.ReadUInt16();
                // unk0F0s[i].unk33 = br.ReadUInt16();
                // unk0F0s[i].unk34 = br.ReadUInt16();
                // unk0F0s[i].unk35 = br.ReadUInt16();
                // unk0F0s[i].unk36 = br.ReadUInt16();
                // unk0F0s[i].unk37 = br.ReadUInt16();
                // unk0F0s[i].unk38 = br.ReadUInt16();
                // unk0F0s[i].unk39 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk40 = br.ReadUInt16();
                // unk0F0s[i].unk41 = br.ReadUInt16();
                // unk0F0s[i].unk42 = br.ReadUInt16();
                // unk0F0s[i].unk43 = br.ReadUInt16();
                // unk0F0s[i].unk44 = br.ReadUInt16();
                // unk0F0s[i].unk45 = br.ReadUInt16();
                // unk0F0s[i].unk46 = br.ReadUInt16();
                // unk0F0s[i].unk47 = br.ReadUInt16();
                Debug.Log(string.Format("0F0: {0}, {1}, {2}, {3}", br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
                // unk0F0s[i].unk48 = br.ReadUInt16();
                // unk0F0s[i].unk49 = br.ReadUInt16();
                // unk0F0s[i].unk50 = br.ReadUInt16();
                // unk0F0s[i].unk51 = br.ReadUInt16();
                // unk0F0s[i].unk52 = br.ReadUInt16();
                // unk0F0s[i].unk53 = br.ReadUInt16();
                // unk0F0s[i].unk54 = br.ReadUInt16();
                // unk0F0s[i].unk55 = br.ReadUInt16();
                unk0F0s[i].Print();
            }
            return unk0F0s;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log("UNKO8: " + pr);
        }
        public static int GetSize(){
            return 112;
        }
    }
    
    [System.Serializable]
    public struct Unk338{
        public UInt16 unk0;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public UInt16 unk16;
        public UInt16 unk17;
        public UInt16 unk18;
        public UInt16 unk19;
        public UInt16 unk20;
        public UInt16 unk21;
        public UInt16 unk22;
        public UInt16 unk23;
        public UInt16 unk24;
        public UInt16 unk25;
        public static Unk338[] ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos;
            var unk338s = new Unk338[nr];
            for(var i=0;i<nr;i++){
                br.BaseStream.Position = 1584 + startPos + i*Unk338.GetSize();
                unk338s[i].unk0 = br.ReadUInt16();
                unk338s[i].unk1 = br.ReadUInt16();
                unk338s[i].unk3 = br.ReadUInt16();
                unk338s[i].unk4 = br.ReadUInt16();
                unk338s[i].unk5 = br.ReadUInt16();
                unk338s[i].unk6 = br.ReadUInt16();
                unk338s[i].unk7 = br.ReadUInt16();
                unk338s[i].unk8 = br.ReadUInt16();
                unk338s[i].unk9 = br.ReadUInt16();
                unk338s[i].unk10 = br.ReadUInt16();
                unk338s[i].unk11 = br.ReadUInt16();
                unk338s[i].unk13 = br.ReadUInt16();
                unk338s[i].unk14 = br.ReadUInt16();
                unk338s[i].unk15 = br.ReadUInt16();
                unk338s[i].unk16 = br.ReadUInt16();
                unk338s[i].unk17 = br.ReadUInt16();
                unk338s[i].unk18 = br.ReadUInt16();
                unk338s[i].unk19 = br.ReadUInt16();
                unk338s[i].unk20 = br.ReadUInt16();
                unk338s[i].unk21 = br.ReadUInt16();
                unk338s[i].unk23 = br.ReadUInt16();
                unk338s[i].unk24 = br.ReadUInt16();
                unk338s[i].unk25 = br.ReadUInt16();
                unk338s[i].Print();
            }
            return unk338s;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log(pr);
        }
        public static int GetSize(){
            return 50;
        }

    }
    
    [System.Serializable]
    public struct Unk490{
        public UInt16 unk0;
        public UInt16 unk1;
        public UInt16 unk2;
        public UInt16 unk3;
        public UInt16 unk4;
        public UInt16 unk5;
        public UInt16 unk6;
        public UInt16 unk7;
        public UInt16 unk8;
        public UInt16 unk9;
        public UInt16 unk10;
        public UInt16 unk11;
        public UInt16 unk12;
        public UInt16 unk13;
        public UInt16 unk14;
        public UInt16 unk15;
        public UInt16 unk16;
        public UInt16 unk17;
        public UInt16 unk18;
        public UInt16 unk19;
        public UInt16 unk20;
        public UInt16 unk21;
        public UInt16 unk22;
        public UInt16 unk23;
        public UInt16 unk24;
        public UInt16 unk25;
        public UInt16 unk26;
        public UInt16 unk27;
        public UInt16 unk28;
        public UInt16 unk29;
        public UInt16 unk30;
        public UInt16 unk31;
        public UInt16 unk32;
        public UInt16 unk33;
        public UInt16 unk34;
        public UInt16 unk35;
        public UInt16 unk36;
        public UInt16 unk37;
        public UInt16 unk38;
        public UInt16 unk39;
        public UInt16 unk40;
        public UInt16 unk41;
        public UInt16 unk42;
        public UInt16 unk43;
        public UInt16 unk44;
        public UInt16 unk45;
        public UInt16 unk46;
        public UInt16 unk47;
        public UInt16 unk48;
        public UInt16 unk49;
        public static Unk490 ReadAll(BinaryReader br, long startPos, int nr){
            br.BaseStream.Position = 1584 + startPos;
            var unk490s = new Unk490();
            br.BaseStream.Position = 1584 + startPos;
            unk490s.unk0 = br.ReadUInt16();
            unk490s.unk1 = br.ReadUInt16();
            unk490s.unk3 = br.ReadUInt16();
            unk490s.unk4 = br.ReadUInt16();
            unk490s.unk5 = br.ReadUInt16();
            unk490s.unk6 = br.ReadUInt16();
            unk490s.unk7 = br.ReadUInt16();
            unk490s.unk8 = br.ReadUInt16();
            unk490s.unk9 = br.ReadUInt16();
            unk490s.unk10 = br.ReadUInt16();
            unk490s.unk11 = br.ReadUInt16();
            unk490s.unk13 = br.ReadUInt16();
            unk490s.unk14 = br.ReadUInt16();
            unk490s.unk15 = br.ReadUInt16();
            unk490s.unk16 = br.ReadUInt16();
            unk490s.unk17 = br.ReadUInt16();
            unk490s.unk18 = br.ReadUInt16();
            unk490s.unk19 = br.ReadUInt16();
            unk490s.unk20 = br.ReadUInt16();
            unk490s.unk21 = br.ReadUInt16();
            unk490s.unk23 = br.ReadUInt16();
            unk490s.unk24 = br.ReadUInt16();
            unk490s.unk25 = br.ReadUInt16();
            unk490s.unk26 = br.ReadUInt16();
            unk490s.unk27 = br.ReadUInt16();
            unk490s.unk28 = br.ReadUInt16();
            unk490s.unk29 = br.ReadUInt16();
            unk490s.unk30 = br.ReadUInt16();
            unk490s.unk31 = br.ReadUInt16();
            unk490s.unk33 = br.ReadUInt16();
            unk490s.unk34 = br.ReadUInt16();
            // unk490s.unk35 = br.ReadUInt16();
            // unk490s.unk36 = br.ReadUInt16();
            // unk490s.unk37 = br.ReadUInt16();
            // unk490s.unk38 = br.ReadUInt16();
            // unk490s.unk39 = br.ReadUInt16();
            // unk490s.unk40 = br.ReadUInt16();
            // unk490s.unk41 = br.ReadUInt16();
            // unk490s.unk43 = br.ReadUInt16();
            // unk490s.unk44 = br.ReadUInt16();
            // unk490s.unk45 = br.ReadUInt16();
            // unk490s.unk46 = br.ReadUInt16();
            // unk490s.unk47 = br.ReadUInt16();
            // unk490s.unk48 = br.ReadUInt16();
            // unk490s.unk49 = br.ReadUInt16();
            unk490s.Print();
            return unk490s;
        }
        public void Print(){
            var pr = "";
            for(int i = 0; i < this.GetType().GetFields().Length; i++){
                var field = this.GetType().GetFields()[i];
                pr += string.Format("{0}\t", field.GetValue(this));
            }
            Debug.Log("490: " + pr);
        }
        public static int GetSize(){
            return 88;
        }
    }
}
// PRP_Tree_Deciduous_RootyMangrove_Violet_000
// 205696
// 08 => 1-0
// 80 => 3-224
// F0 => 2-368
// BONES 180 => 18-736
// UNK_190 => 129-12368
// UNK_1A0 =>  17 - 12640
// BONEMAP_1B0 => 15-12688
// TEXTURE_2C0 => 15-12720
// MATERIALS_1F0 => 2-14016
// GEOMETRY_250 => 1-15600
// GEOMETRY
// GEOMETRY_Vertex_18 => 3109-42-0
// GEOMETRY_INDICES_68 => 10506-130592
// GEOMETRY_SUBMESH_80 => 3-151616
// GEOMETRY_UNK0_98 => 3109-4-151952
// GEOMETRY_UNK2_A8 => 3503-151968
// GEOMETRY_UNK3_B8 => 4-158976

// 0 + 1586 + 15600 + 208 + 158976 => 176370


// btw the norm/tan/bitan look like this 
// typedef struct
// {
//     unsigned byte x8;
//     unsigned byte y8;
// }NORM8<read=ReadNorm8>;

// string ReadNorm8 (NORM8& n)
// {
//     float x = (n.x8 - 127) / 127.0;
//     float y = (n.y8 - 127) / 127.0;
//     float z = 1.0 - Sqrt(x*x + y*y);
//     string s;
//     SPrintf (s, "Vector3(%f, %f, %f)", x, y, z);
//     return s;
// }