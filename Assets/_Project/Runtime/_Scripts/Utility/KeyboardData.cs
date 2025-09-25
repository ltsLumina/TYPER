using System.Collections.Generic;
using UnityEngine;

public struct KeyboardData
{
	public struct Layouts
	{
		public struct QWERTY
		{
			public static List<KeyCode> Alphabetic { get; } = new ()
			{ KeyCode.Q,
			  KeyCode.W,
			  KeyCode.E,
			  KeyCode.R,
			  KeyCode.T,
			  KeyCode.Y,
			  KeyCode.U,
			  KeyCode.I,
			  KeyCode.O,
			  KeyCode.P,
			  KeyCode.A,
			  KeyCode.S,
			  KeyCode.D,
			  KeyCode.F,
			  KeyCode.G,
			  KeyCode.H,
			  KeyCode.J,
			  KeyCode.K,
			  KeyCode.L,
			  KeyCode.Z,
			  KeyCode.X,
			  KeyCode.C,
			  KeyCode.V,
			  KeyCode.B,
			  KeyCode.N,
			  KeyCode.M, };

			public static List<KeyCode> Numeric { get; } = new ()
			{ KeyCode.Alpha1,
			  KeyCode.Alpha2,
			  KeyCode.Alpha3,
			  KeyCode.Alpha4,
			  KeyCode.Alpha5,
			  KeyCode.Alpha6,
			  KeyCode.Alpha7,
			  KeyCode.Alpha8,
			  KeyCode.Alpha9,
			  KeyCode.Alpha0 };

			public static List<KeyCode> Alphanumeric { get; } = new ()
			{ KeyCode.Alpha1,
			  KeyCode.Alpha2,
			  KeyCode.Alpha3,
			  KeyCode.Alpha4,
			  KeyCode.Alpha5,
			  KeyCode.Alpha6,
			  KeyCode.Alpha7,
			  KeyCode.Alpha8,
			  KeyCode.Alpha9,
			  KeyCode.Alpha0,
			  KeyCode.Q,
			  KeyCode.W,
			  KeyCode.E,
			  KeyCode.R,
			  KeyCode.T,
			  KeyCode.Y,
			  KeyCode.U,
			  KeyCode.I,
			  KeyCode.O,
			  KeyCode.P,
			  KeyCode.A,
			  KeyCode.S,
			  KeyCode.D,
			  KeyCode.F,
			  KeyCode.G,
			  KeyCode.H,
			  KeyCode.J,
			  KeyCode.K,
			  KeyCode.L,
			  KeyCode.Z,
			  KeyCode.X,
			  KeyCode.C,
			  KeyCode.V,
			  KeyCode.B,
			  KeyCode.N,
			  KeyCode.M, };
		}
        
		public struct AZERTY
		{
			public static List<KeyCode> Alphabetic { get; } = new ()
			{ KeyCode.A,
			  KeyCode.Z,
			  KeyCode.E,
			  KeyCode.R,
			  KeyCode.T,
			  KeyCode.Y,
			  KeyCode.U,
			  KeyCode.I,
			  KeyCode.O,
			  KeyCode.P,
			  KeyCode.Q,
			  KeyCode.S,
			  KeyCode.D,
			  KeyCode.F,
			  KeyCode.G,
			  KeyCode.H,
			  KeyCode.J,
			  KeyCode.K,
			  KeyCode.L,
			  KeyCode.M,
			  KeyCode.W,
			  KeyCode.X,
			  KeyCode.C,
			  KeyCode.V,
			  KeyCode.B,
			  KeyCode.N, };
        
			public static List<KeyCode> Numeric { get; } = new ()
			{ KeyCode.Alpha1,
			  KeyCode.Alpha2,
			  KeyCode.Alpha3,
			  KeyCode.Alpha4,
			  KeyCode.Alpha5,
			  KeyCode.Alpha6,
			  KeyCode.Alpha7,
			  KeyCode.Alpha8,
			  KeyCode.Alpha9,
			  KeyCode.Alpha0 };
        
			public static List<KeyCode> Alphanumeric { get; } = new ()
			{ KeyCode.Alpha1,
			  KeyCode.Alpha2,
			  KeyCode.Alpha3,
			  KeyCode.Alpha4,
			  KeyCode.Alpha5,
			  KeyCode.Alpha6,
			  KeyCode.Alpha7,
			  KeyCode.Alpha8,
			  KeyCode.Alpha9,
			  KeyCode.Alpha0,
			  KeyCode.A,
			  KeyCode.Z,
			  KeyCode.E,
			  KeyCode.R,
			  KeyCode.T,
			  KeyCode.Y,
			  KeyCode.U,
			  KeyCode.I,
			  KeyCode.O,
			  KeyCode.P,
			  KeyCode.Q,
			  KeyCode.S,
			  KeyCode.D,
			  KeyCode.F,
			  KeyCode.G,
			  KeyCode.H,
			  KeyCode.J,
			  KeyCode.K,
			  KeyCode.L,
			  KeyCode.M,
			  KeyCode.W,
			  KeyCode.X,
			  KeyCode.C,
			  KeyCode.V,
			  KeyCode.B,
			  KeyCode.N, };
		}
        
		// TODO: Add DVORAK layout
	}
}
