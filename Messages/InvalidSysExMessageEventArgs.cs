using System;
using System.Collections;
using System.Text;

public class InvalidSysExMessageEventArgs : EventArgs
{
	private byte[] messageData;

	public InvalidSysExMessageEventArgs(byte[] messageData)
	{
		this.messageData = messageData;
	}

	public ICollection MessageData
	{
		get
		{
			return messageData;
		}
	}
}
