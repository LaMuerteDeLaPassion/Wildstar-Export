using UnityEngine;
 
public static class MatrixExtensions
{
    public static Quaternion ExtractRotation(this Matrix4x4 matrix){
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;
    
        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;
    
        return Quaternion.LookRotation(forward, upwards);
    }
    public static Quaternion ExtractRotation2(this Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w =Mathf.Sqrt(Mathf.Max(0,1+ m[0,0]+ m[1,1]+ m[2,2]))/2;
        q.x =Mathf.Sqrt(Mathf.Max(0,1+ m[0,0]- m[1,1]- m[2,2]))/2;
        q.y =Mathf.Sqrt(Mathf.Max(0,1- m[0,0]+ m[1,1]- m[2,2]))/2;
        q.z =Mathf.Sqrt(Mathf.Max(0,1- m[0,0]- m[1,1]+ m[2,2]))/2;
        q.x *=Mathf.Sign(q.x *(m[2,1]- m[1,2]));
        q.y *=Mathf.Sign(q.y *(m[0,2]- m[2,0]));
        q.z *=Mathf.Sign(q.z *(m[1,0]- m[0,1]));
        return q;
    }
    public static Quaternion ExtractRotation3(this Matrix4x4 m){
        Matrix4x4 mat = new Matrix4x4();
        mat[0,0] = m[0,0];
        mat[0,1] = m[0,1];
        mat[0,2] = m[0,2];
        mat[1,0] = m[1,0];
        mat[1,1] = m[1,1];
        mat[1,2] = m[1,2];
        mat[2,0] = m[2,0];
        mat[2,1] = m[2,1];
        mat[2,2] = m[2,2];
        mat[3,3] = 1;
        return mat.rotation;
    }
    public static Matrix4x4 ConvertOld(this Matrix4x4 m){
        var rot = m.ExtractRotation3();
        var scale = new Vector3(1,1,1);
        var pos = m.GetColumn(3);
        return Matrix4x4.TRS(pos, rot, scale);
    }
 
    public static Vector3 ExtractPosition(this Matrix4x4 matrix){
        return matrix.GetColumn(3);
    }
 
    public static Vector3 ExtractScale(this Matrix4x4 m)
    {
        var x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
        var y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
        var z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);

        return new Vector3(x, y, z);
    }
}
