#region File Description
//-----------------------------------------------------------------------------
// IIdentity.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

namespace RobotGameData.GameInterface
{
	#region Interface

	/// <summary>
	/// his is an interface of inherited class which needs an identity number.
	/// </summary>
	public interface IIdentity
	{
		int Id { get; }
	}

	#endregion
}