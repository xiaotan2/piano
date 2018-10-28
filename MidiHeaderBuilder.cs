using System.Collections;
using System.Runtime.InteropServices;
using System;

#region MIDI Capabilities Struct
/// <summary>
/// Represents MIDI input device capabilities.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MidiInCaps
{

   /// <summary>
   /// Manufacturer identifier of the device driver for the Midi output 
   /// device. 
   /// </summary>
   public short mid;

   /// <summary>
   /// Product identifier of the Midi output device. 
   /// </summary>
   public short pid;

   /// <summary>
   /// Version number of the device driver for the Midi output device. The 
   /// high-order byte is the major version number, and the low-order byte 
   /// is the minor version number. 
   /// </summary>
   public int driverVersion;

   /// <summary>
   /// Product name.
   /// </summary>
   [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
   public string name;

   /// <summary>
   /// Optional functionality supported by the device. 
   /// </summary>
   public int support;
}

/// <summary>
/// Represents MIDI output device capabilities.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MidiOutCaps
{
   #region MidiOutCaps Members

   /// <summary>
   /// Manufacturer identifier of the device driver for the Midi output 
   /// device. 
   /// </summary>
   public short mid;

   /// <summary>
   /// Product identifier of the Midi output device. 
   /// </summary>
   public short pid;

   /// <summary>
   /// Version number of the device driver for the Midi output device. The 
   /// high-order byte is the major version number, and the low-order byte 
   /// is the minor version number. 
   /// </summary>
   public int driverVersion;

   /// <summary>
   /// Product name.
   /// </summary>
   [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
   public string name;

   /// <summary>
   /// Flags describing the type of the Midi output device. 
   /// </summary>
   public short technology;

   /// <summary>
   /// Number of voices supported by an internal synthesizer device. If 
   /// the device is a port, this member is not meaningful and is set 
   /// to 0. 
   /// </summary>
   public short voices;

   /// <summary>
   /// Maximum number of simultaneous notes that can be played by an 
   /// internal synthesizer device. If the device is a port, this member 
   /// is not meaningful and is set to 0. 
   /// </summary>
   public short notes;

   /// <summary>
   /// Channels that an internal synthesizer device responds to, where the 
   /// least significant bit refers to channel 0 and the most significant 
   /// bit to channel 15. Port devices that transmit on all channels set 
   /// this member to 0xFFFF. 
   /// </summary>
   public short channelMask;

   /// <summary>
   /// Optional functionality supported by the device. 
   /// </summary>
   public int support;

   #endregion
}

#endregion

#region MIDI Header Struct
/// <summary>
/// Represents the Windows Multimedia MIDIHDR structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MidiHeader
{
   #region MidiHeader Members

   /// <summary>
   /// Pointer to MIDI data.
   /// </summary>
   public IntPtr data;

   /// <summary>
   /// Size of the buffer.
   /// </summary>
   public int bufferLength;

   /// <summary>
   /// Actual amount of data in the buffer. This value should be less than 
   /// or equal to the value given in the dwBufferLength member.
   /// </summary>
   public int bytesRecorded;

   /// <summary>
   /// Custom user data.
   /// </summary>
   public int user;

   /// <summary>
   /// Flags giving information about the buffer.
   /// </summary>
   public int flags;

   /// <summary>
   /// Reserved; do not use.
   /// </summary>
   public IntPtr next;

   /// <summary>
   /// Reserved; do not use.
   /// </summary>
   public int reserved;

   /// <summary>
   /// Offset into the buffer when a callback is performed. (This 
   /// callback is generated because the MEVT_F_CALLBACK flag is 
   /// set in the dwEvent member of the MidiEventArgs structure.) 
   /// This offset enables an application to determine which 
   /// event caused the callback. 
   /// </summary>
   public int offset;

   /// <summary>
   /// Reserved; do not use.
   /// </summary>
   [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
   public int[] reservedArray;

   #endregion
}
#endregion

#region MIDI Header Builder
/// <summary>
/// Builds a pointer to a MidiHeader structure.
/// </summary>
internal class MidiHeaderBuilder
{
   // The length of the system exclusive buffer.
   private int bufferLength;

   // The system exclusive data.
   private byte[] data;

   // Indicates whether the pointer to the MidiHeader has been built.
   private bool built = false;

   // The built pointer to the MidiHeader.
   private IntPtr result;

   /// <summary>
   /// Initializes a new instance of the MidiHeaderBuilder.
   /// </summary>
   public MidiHeaderBuilder()
   {
      BufferLength = 1;
   }

   #region Methods

   /// <summary>
   /// Builds the pointer to the MidiHeader structure.
   /// </summary>
   public void Build()
   {
      MidiHeader header = new MidiHeader();

      // Initialize the MidiHeader.
      header.bufferLength = BufferLength;
      header.bytesRecorded = 0;
      header.data = Marshal.AllocHGlobal(BufferLength);
      header.flags = 0;

      // Write data to the MidiHeader.
      for (int i = 0; i < BufferLength; i++)
      {
         Marshal.WriteByte(header.data, i, data[i]);
      }

      try
      {
         result = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHeader)));
      }
      catch (Exception)
      {
         Marshal.FreeHGlobal(header.data);

         throw;
      }

      try
      {
         Marshal.StructureToPtr(header, result, false);
      }
      catch (Exception)
      {
         Marshal.FreeHGlobal(header.data);
         Marshal.FreeHGlobal(result);

         throw;
      }

      built = true;
   }

   /// <summary>
   /// Initializes the MidiHeaderBuilder with the specified SysExMessage.
   /// </summary>
   /// <param name="message">
   /// The SysExMessage to use for initializing the MidiHeaderBuilder.
   /// </param>
   public void InitializeBuffer(SysExMessage message)
   {
      // If this is a start system exclusive message.
      if (message.SysExType == SysExType.Start)
      {
         BufferLength = message.Length;

         // Copy entire message.
         for (int i = 0; i < BufferLength; i++)
         {
            data[i] = message[i];
         }
      }
      // Else this is a continuation message.
      else
      {
         BufferLength = message.Length - 1;

         // Copy all but the first byte of message.
         for (int i = 0; i < BufferLength; i++)
         {
            data[i] = message[i + 1];
         }
      }
   }

   public void InitializeBuffer(ICollection events)
   {
      #region Require

      if (events == null)
      {
         throw new ArgumentNullException("events");
      }
      else if (events.Count % 4 != 0)
      {
         throw new ArgumentException("Stream events not word aligned.");
      }

      #endregion

      #region Guard

      if (events.Count == 0)
      {
         return;
      }

      #endregion

      BufferLength = events.Count;

      events.CopyTo(data, 0);
   }

   /// <summary>
   /// Releases the resources associated with the built MidiHeader pointer.
   /// </summary>
   public void Destroy()
   {
      #region Require

      if (!built)
      {
         throw new InvalidOperationException("Cannot destroy MidiHeader");
      }

      #endregion

      Destroy(result);
   }

   /// <summary>
   /// Releases the resources associated with the specified MidiHeader pointer.
   /// </summary>
   /// <param name="headerPtr">
   /// The MidiHeader pointer.
   /// </param>
   public void Destroy(IntPtr headerPtr)
   {
      MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

      Marshal.FreeHGlobal(header.data);
      Marshal.FreeHGlobal(headerPtr);
   }

   #endregion

   #region Properties

   /// <summary>
   /// The length of the system exclusive buffer.
   /// </summary>
   public int BufferLength
   {
      get
      {
         return bufferLength;
      }
      set
      {
         #region Require

         if (value <= 0)
         {
            throw new ArgumentOutOfRangeException("BufferLength", value,
                "MIDI header buffer length out of range.");
         }

         #endregion

         bufferLength = value;
         data = new byte[value];
      }
   }

   /// <summary>
   /// Gets the pointer to the MidiHeader.
   /// </summary>
   public IntPtr Result
   {
      get
      {
         return result;
      }
   }

   #endregion
}
#endregion