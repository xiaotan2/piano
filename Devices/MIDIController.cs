using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Sanford.Threading;
using Sanford.Multimedia;
using System.Collections.Generic;
using System.Threading;
using System.Text;

public class MIDIController : MIDIDevice {

   #region DLL Import
   [DllImport("winmm.dll")]
   private static extern int midiInOpen(ref int handle, int deviceID,
            MidiInProc proc, int instance, int flags);

   [DllImport("winmm.dll")]
   private static extern int midiInClose(int handle);

   [DllImport("winmm.dll")]
   private static extern int midiInStart(int handle);

   [DllImport("winmm.dll")]
   private static extern int midiInStop(int handle);

   [DllImport("winmm.dll")]
   private static extern int midiInReset(int handle);

   [DllImport("winmm.dll")]
   private static extern int midiInPrepareHeader(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   private static extern int midiInUnprepareHeader(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   private static extern int midiInAddBuffer(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   private static extern int midiInGetDevCaps(int deviceID,
       ref MidiInCaps caps, int sizeOfMidiInCaps);

   [DllImport("winmm.dll")]
   private static extern int midiInGetNumDevs();
   #endregion

   private const int MIDI_IO_STATUS = 0x00000020;

   private const int MIM_OPEN = 0x3C1;
   private const int MIM_CLOSE = 0x3C2;
   private const int MIM_DATA = 0x3C3;
   private const int MIM_LONGDATA = 0x3C4;
   private const int MIM_ERROR = 0x3C5;
   private const int MIM_LONGERROR = 0x3C6;
   private const int MIM_MOREDATA = 0x3CC;
   private const int MHDR_DONE = 0x00000001;

   // Represents the method that handles messages from Windows.
   private delegate void MidiInProc(int handle, int msg, int instance, int param1, int param2);

   #region Fields
   private delegate void GenericDelegate<T>(T args);

   private DelegateQueue delegateQueue = null;

   private volatile int bufferCount = 0;

   private readonly object lockObject = new object();

   private MidiInProc midiInProc;

   private bool recording = false;

   private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

   private ChannelMessageBuilder cmBuilder = new ChannelMessageBuilder();

   private SysCommonMessageBuilder scBuilder = new SysCommonMessageBuilder();

   private int handle = 0;

   private volatile bool resetting = false;

   private int sysExBufferSize = 4096;

   private List<byte> sysExData = new List<byte>();

   public override int Handle
   {
      get
      {
         return handle;
      }
   }

   public int SysExBufferSize
   {
      get
      {
         return sysExBufferSize;
      }
      set
      {
         #region Require

         if (value < 1)
         {
            throw new ArgumentOutOfRangeException();
         }

         #endregion

         sysExBufferSize = value;
      }
   }

   public static int DeviceCount
   {
      get
      {
         return midiInGetNumDevs();
      }
   }
   #endregion

   #region Construction
   /// <summary>
   /// Initializes a new instance of the InputDevice class with the 
   /// specified device ID.
   /// </summary>
   public MIDIController(int deviceID) : base(deviceID)
   {
      midiInProc = HandleMessage;

      int result = midiInOpen(ref handle, deviceID, midiInProc, 0, CALLBACK_FUNCTION);

      if (result == MidiDeviceException.MMSYSERR_NOERROR)
      {
         delegateQueue = new DelegateQueue();
      }
      else
      {
         throw new InputDeviceException(result);
      }
   }

   ~MIDIController()
   {
      if (!IsDisposed)
      {
         midiInReset(Handle);
         midiInClose(Handle);
      }
   }
   #endregion

   #region Event Handling
   public event EventHandler<ChannelMessageEventArgs> ChannelMessageReceived;

   public event EventHandler<SysExMessageEventArgs> SysExMessageReceived;

   public event EventHandler<SysCommonMessageEventArgs> SysCommonMessageReceived;

   public event EventHandler<SysRealtimeMessageEventArgs> SysRealtimeMessageReceived;

   public event EventHandler<InvalidShortMessageEventArgs> InvalidShortMessageReceived;

   public event EventHandler<InvalidSysExMessageEventArgs> InvalidSysExMessageReceived;

   protected virtual void OnChannelMessageReceived(ChannelMessageEventArgs e)
   {
      EventHandler<ChannelMessageEventArgs> handler = ChannelMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }

   protected virtual void OnSysExMessageReceived(SysExMessageEventArgs e)
   {
      EventHandler<SysExMessageEventArgs> handler = SysExMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }

   protected virtual void OnSysCommonMessageReceived(SysCommonMessageEventArgs e)
   {
      EventHandler<SysCommonMessageEventArgs> handler = SysCommonMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }

   protected virtual void OnSysRealtimeMessageReceived(SysRealtimeMessageEventArgs e)
   {
      EventHandler<SysRealtimeMessageEventArgs> handler = SysRealtimeMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }

   protected virtual void OnInvalidShortMessageReceived(InvalidShortMessageEventArgs e)
   {
      EventHandler<InvalidShortMessageEventArgs> handler = InvalidShortMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }

   protected virtual void OnInvalidSysExMessageReceived(InvalidSysExMessageEventArgs e)
   {
      EventHandler<InvalidSysExMessageEventArgs> handler = InvalidSysExMessageReceived;

      if (handler != null)
      {
         context.Post(delegate (object dummy)
         {
            handler(this, e);
         }, null);
      }
   }
   #endregion

   public override void Close()
   {
      #region Guard

      if (IsDisposed)
      {
         return;
      }

      #endregion

      Dispose(true);
   }

   public void StartRecording()
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException("InputDevice");
      }

      #endregion

      #region Guard

      if (recording)
      {
         return;
      }

      #endregion

      lock (lockObject)
      {
         int result = AddSysExBuffer();

         if (result == MIDIExceptions.MMSYSERR_NOERROR)
         {
            result = AddSysExBuffer();
         }

         if (result == MIDIExceptions.MMSYSERR_NOERROR)
         {
            result = AddSysExBuffer();
         }

         if (result == MIDIExceptions.MMSYSERR_NOERROR)
         {
            result = AddSysExBuffer();
         }

         if (result == MIDIExceptions.MMSYSERR_NOERROR)
         {
            result = midiInStart(Handle);
         }

         if (result == MidiDeviceException.MMSYSERR_NOERROR)
         {
            recording = true;
         }
         else
         {
            throw new InputDeviceException(result);
         }
      }
   }

   public void StopRecording()
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException("InputDevice");
      }

      #endregion

      #region Guard

      if (!recording)
      {
         return;
      }

      #endregion

      lock (lockObject)
      {
         int result = midiInStop(Handle);

         if (result == MidiDeviceException.MMSYSERR_NOERROR)
         {
            recording = false;
         }
         else
         {
            throw new InputDeviceException(result);
         }
      }
   }

   public override void Reset()
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException("InputDevice");
      }

      #endregion

      lock (lockObject)
      {
         resetting = true;

         int result = midiInReset(Handle);

         if (result == MidiDeviceException.MMSYSERR_NOERROR)
         {
            recording = false;

            while (bufferCount > 0)
            {
               Monitor.Wait(lockObject);
            }

            resetting = false;
         }
         else
         {
            resetting = false;

            throw new InputDeviceException(result);
         }
      }
   }

   public static MidiInCaps GetDeviceCapabilities(int deviceID)
   {
      int result;
      MidiInCaps caps = new MidiInCaps();

      result = midiInGetDevCaps(deviceID, ref caps, SizeOfMidiHeader);

      if (result != MidiDeviceException.MMSYSERR_NOERROR)
      {
         throw new InputDeviceException(result);
      }

      return caps;
   }

   public override void Dispose()
   {
      #region Guard

      if (IsDisposed)
      {
         return;
      }

      #endregion

      Dispose(true);
   }

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         lock (lockObject)
         {
            Reset();

            int result = midiInClose(handle);

            if (result == MidiDeviceException.MMSYSERR_NOERROR)
            {
               delegateQueue.Dispose();
            }
            else
            {
               throw new InputDeviceException(result);
            }
         }
      }
      else
      {
         midiInReset(Handle);
         midiInClose(Handle);
      }

      base.Dispose(disposing);
   }

   #region Handle Messaging
   private void HandleMessage(int handle, int msg, int instance, int param1, int param2)
   {
      if (msg == MIM_OPEN)
      {
      }
      else if (msg == MIM_CLOSE)
      {
      }
      else if (msg == MIM_DATA)
      {
         delegateQueue.Post(HandleShortMessage, param1);
      }
      else if (msg == MIM_MOREDATA)
      {
         delegateQueue.Post(HandleShortMessage, param1);
      }
      else if (msg == MIM_LONGDATA)
      {
         delegateQueue.Post(HandleSysExMessage, new IntPtr(param1));
      }
      else if (msg == MIM_ERROR)
      {
         delegateQueue.Post(HandleInvalidShortMessage, param1);
      }
      else if (msg == MIM_LONGERROR)
      {
         delegateQueue.Post(HandleInvalidSysExMessage, new IntPtr(param1));
      }
   }

   private void HandleShortMessage(object state)
   {
      int message = (int)state;
      int status = MIDIShortMessage.UnpackStatus(message);

      if (status >= (int)ChannelCommand.NoteOff &&
          status <= (int)ChannelCommand.PitchWheel +
          ChannelMessage.MidiChannelMaxValue)
      {
         cmBuilder.Message = message;
         cmBuilder.Build();

         OnChannelMessageReceived(new ChannelMessageEventArgs(cmBuilder.Result));
      }
      else if (status == (int)SysCommonType.MidiTimeCode ||
          status == (int)SysCommonType.SongPositionPointer ||
          status == (int)SysCommonType.SongSelect ||
          status == (int)SysCommonType.TuneRequest)
      {
         scBuilder.Message = message;
         scBuilder.Build();

         OnSysCommonMessageReceived(new SysCommonMessageEventArgs(scBuilder.Result));
      }
      else
      {
         SysRealtimeMessageEventArgs e = null;

         switch ((SysRealtimeType)status)
         {
            case SysRealtimeType.ActiveSense:
               e = SysRealtimeMessageEventArgs.ActiveSense;
               break;

            case SysRealtimeType.Clock:
               e = SysRealtimeMessageEventArgs.Clock;
               break;

            case SysRealtimeType.Continue:
               e = SysRealtimeMessageEventArgs.Continue;
               break;

            case SysRealtimeType.Reset:
               e = SysRealtimeMessageEventArgs.Reset;
               break;

            case SysRealtimeType.Start:
               e = SysRealtimeMessageEventArgs.Start;
               break;

            case SysRealtimeType.Stop:
               e = SysRealtimeMessageEventArgs.Stop;
               break;

            case SysRealtimeType.Tick:
               e = SysRealtimeMessageEventArgs.Tick;
               break;
         }

         OnSysRealtimeMessageReceived(e);
      }
   }

   private void HandleSysExMessage(object state)
   {
      lock (lockObject)
      {
         IntPtr headerPtr = (IntPtr)state;

         MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

         if (!resetting)
         {
            for (int i = 0; i < header.bytesRecorded; i++)
            {
               sysExData.Add(Marshal.ReadByte(header.data, i));
            }

            if (sysExData[0] == 0xF0 && sysExData[sysExData.Count - 1] == 0xF7)
            {
               SysExMessage message = new SysExMessage(sysExData.ToArray());

               sysExData.Clear();

               OnSysExMessageReceived(new SysExMessageEventArgs(message));
            }

            int result = AddSysExBuffer();

            if (result != MIDIExceptions.MMSYSERR_NOERROR)
            {
               Exception ex = new InputDeviceException(result);

               OnError(new ErrorEventArgs(ex));
            }
         }

         ReleaseBuffer(headerPtr);
      }
   }

   private void HandleInvalidShortMessage(object state)
   {
      OnInvalidShortMessageReceived(new InvalidShortMessageEventArgs((int)state));
   }

   private void HandleInvalidSysExMessage(object state)
   {
      lock (lockObject)
      {
         IntPtr headerPtr = (IntPtr)state;

         MidiHeader header = (MidiHeader)Marshal.PtrToStructure(headerPtr, typeof(MidiHeader));

         if (!resetting)
         {
            byte[] data = new byte[header.bytesRecorded];

            Marshal.Copy(header.data, data, 0, data.Length);

            OnInvalidSysExMessageReceived(new InvalidSysExMessageEventArgs(data));

            int result = AddSysExBuffer();

            if (result != MIDIExceptions.MMSYSERR_NOERROR)
            {
               Exception ex = new InputDeviceException(result);

               OnError(new ErrorEventArgs(ex));
            }
         }

         ReleaseBuffer(headerPtr);
      }
   }

   private void ReleaseBuffer(IntPtr headerPtr)
   {
      int result = midiInUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

      if (result != MIDIExceptions.MMSYSERR_NOERROR)
      {
         Exception ex = new InputDeviceException(result);

         OnError(new ErrorEventArgs(ex));
      }

      headerBuilder.Destroy(headerPtr);

      bufferCount--;

      Debug.Assert(bufferCount >= 0);

      Monitor.Pulse(lockObject);
   }

   public int AddSysExBuffer()
   {
      int result;

      // Initialize the MidiHeader builder.
      headerBuilder.BufferLength = sysExBufferSize;
      headerBuilder.Build();

      // Get the pointer to the built MidiHeader.
      IntPtr headerPtr = headerBuilder.Result;

      // Prepare the header to be used.
      result = midiInPrepareHeader(Handle, headerPtr, SizeOfMidiHeader);

      // If the header was perpared successfully.
      if (result == MIDIExceptions.MMSYSERR_NOERROR)
      {
         bufferCount++;

         // Add the buffer to the InputDevice.
         result = midiInAddBuffer(Handle, headerPtr, SizeOfMidiHeader);

         // If the buffer could not be added.
         if (result != MidiDeviceException.MMSYSERR_NOERROR)
         {
            // Unprepare header - there's a chance that this will fail 
            // for whatever reason, but there's not a lot that can be
            // done at this point.
            midiInUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

            bufferCount--;

            // Destroy header.
            headerBuilder.Destroy();
         }
      }
      // Else the header could not be prepared.
      else
      {
         // Destroy header.
         headerBuilder.Destroy();
      }

      return result;
   }
   #endregion
}

/// <summary>
/// The exception that is thrown when a error occurs with the InputDevice
/// class.
/// </summary>
public class InputDeviceException : MidiDeviceException
{
   #region InputDeviceException Members

   #region Win32 Midi Input Error Function

   [DllImport("winmm.dll")]
   private static extern int midiInGetErrorText(int errCode,
       StringBuilder errMsg, int sizeOfErrMsg);

   #endregion

   #region Fields

   // Error message.
   private StringBuilder errMsg = new StringBuilder(128);

   #endregion

   #region Construction

   /// <summary>
   /// Initializes a new instance of the InputDeviceException class with
   /// the specified error code.
   /// </summary>
   /// <param name="errCode">
   /// The error code.
   /// </param>
   public InputDeviceException(int errCode) : base(errCode)
   {
      // Get error message.
      midiInGetErrorText(errCode, errMsg, errMsg.Capacity);
   }

   #endregion

   #region Properties

   /// <summary>
   /// Gets a message that describes the current exception.
   /// </summary>
   public override string Message
   {
      get
      {
         return errMsg.ToString();
      }
   }

   #endregion

   #endregion
}
