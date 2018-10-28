using System;
using System.Collections.Generic;
using System.Text;

public class InvalidShortMessageEventArgs : EventArgs
{
	private int message;

	public InvalidShortMessageEventArgs(int message)
	{
		this.message = message;
	}

	public int Message
	{
		get
		{
			return message;
		}
	}
}
