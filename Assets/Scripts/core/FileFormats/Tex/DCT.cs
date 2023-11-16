using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

namespace DCT{
    static class DCT{
		const int Side = 8;
		const int SideSquared = Side * Side;

		private static readonly double[] Dct = GenerateDct();
		private static readonly double[] DctT = Transpose(GenerateDct());

		private static double[] GenerateDct()
		{
			const int Size2 = Side * Side;

			double[] result = new double[Size2];
			for (int y = 0, o = 0; y < Side; y++)
				for (int x = 0; x < Side; x++)
					result[o++] =
						Math.Sqrt(y == 0 ? .125 : .250) *
						Math.Cos(((2 * x + 1) * y * Math.PI) * .0625);

			return result;
		}

		private static double[] Transpose(double[] m)
		{
			Debug.Assert(m != null && m.Length == SideSquared);

			for (int y = 0; y < Side; y++)
				for (int x = y + 1; x < Side; x++)
					m.Swap(y * Side + x, x * Side + y);

			return m;
		}

		private static double[] MatrixMultiply(double[] m1, double[] m2)
		{
			Debug.Assert(m1 != null && m1.Length == SideSquared);
			Debug.Assert(m2 != null && m1.Length == SideSquared);

			double[] result = new double[m1.Length];
			for (int y = 0; y < Side; y++)
				for (int x = 0; x < Side; x++)
				{
					double sum = 0;
					for (int k = 0; k < Side; k++)
						sum += m1[y * Side + k] * m2[k * Side + x];
					result[y * Side + x] = sum;
				}

			return result;
		}

		public static double[] ToDouble(int[] m)
		{
			double[] r = new double[m.Length];
			for (int i = 0; i < m.Length; i++)
				r[i] = (double)m[i];
			return r;
		}

		public static int[] ToInt(double[] m)
		{
			int[] r = new int[m.Length];
			for (int i = 0; i < m.Length; i++)
				r[i] = (int)Math.Round(m[i]);
			return r;
		}

		public static int[] DoDct(int[] m)
		{
			double[] source = ToDouble(m);
			source = MatrixMultiply(Dct, source);
			source = MatrixMultiply(source, DctT);
			return ToInt(source);
		}

		public static int[] DoIdct(int[] m)
		{
			double[] source = ToDouble(m);
			source = MatrixMultiply(DctT, source);
			source = MatrixMultiply(source, Dct);
			return ToInt(source);
		}

	}
}
public static class Extensions{
    public static void Swap<T>(this IList<T> arr, int i1, int i2)
    {
        Debug.Assert(i1 > 0 && i1 < arr.Count);
        Debug.Assert(i2 > 0 && i2 < arr.Count);

        T tempT = arr[i1];
        arr[i1] = arr[i2];
        arr[i2] = tempT;
    }

    public static Int16 ReadInt16BE(this BinaryReader reader)
    {
        byte[] temp = reader.ReadBytes(2);
        return (short)(
            temp[0] << 8 |
            temp[1]
        );
    }
}