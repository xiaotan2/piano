using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Threading;
using Sanford.Threading;
using Sanford.Multimedia;

public class MIDIOutDevice : MIDIDevice
{
   #region DLL Import
   [DllImport("winmm.dll")]
   private static extern int midiOutGetNumDevs();

   [DllImport("winmm.dll")]
   private static extern int midiOutGetDevCaps(int deviceID, ref MidiOutCaps caps, int sizeOfMidiInCaps);

   [DllImport("winmm.dll")]
   protected static extern int midiOutReset(int handle);

   [DllImport("winmm.dll")]
   protected static extern int midiOutShortMsg(int handle, int message);

   [DllImport("winmm.dll")]
   protected static extern int midiOutPrepareHeader(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   protected static extern int midiOutUnprepareHeader(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   protected static extern int midiOutLongMsg(int handle,
       IntPtr headerPtr, int sizeOfMidiHeader);

   [DllImport("winmm.dll")]
   private static extern int midiOutOpen(ref int handle, int deviceID,
            MidiOutProc proc, int instance, int flags);

   [DllImport("winmm.dll")]
   private static extern int midiOutClose(int handle);

   [DllImport("winmm.dll")]
   private static extern int midiOutStart(int handle);
   #endregion

   protected const int MOM_OPEN = 0x3C7;
   protected const int MOM_CLOSE = 0x3C8;
   protected const int MOM_DONE = 0x3C9;

   protected const int CALLBACK_FUNCTION = 196608;

   // Represents the method that handles messages from Windows.
   private delegate void MidiOutProc(int handle, int msg, int instance, int param1, int param2);

   protected static readonly int SizeOfMidiOutCaps = Marshal.SizeOf(typeof(MidiInCaps));

   private MidiOutProc midiOutProc;

   // The device handle.
   protected int hndle = 0;

   // For releasing buffers.
   protected DelegateQueue delegateQueue = new DelegateQueue();

   protected readonly object lockObject = new object();

   // The number of buffers still in the queue.
   protected int bufferCount = 0;

   // Builds MidiHeader structures for sending system exclusive messages.
   private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

   public override int Handle
   {
      get
      {
         return hndle;
      }
   }

   public static int DeviceCount
   {
      get
      {
         return midiOutGetNumDevs();
      }
   }

   public MIDIOutDevice(int deviceID) : base(deviceID)
   {
      
      // query number of input devices
      int numOutputDevices = midiOutGetNumDevs();
      if (numOutputDevices <= 0)
         return;

      // look for the input device that's MIDI controller
      for (int i = 0; i < numOutputDevices; i++)
      {
         MidiOutCaps caps = GetDeviceCapabilities(i);
      }

      // open the output device
      midiOutProc = HandleMessage;
      int result = midiOutOpen(ref hndle, deviceID, midiOutProc, 0, CALLBACK_FUNCTION);

      if (result != MIDIExceptions.MMSYSERR_NOERROR)
      {
         throw new OutputDeviceException(result);
      }
   }

   ~MIDIOutDevice()
   {
      Dispose(false);
   }

   /// <summary>
   /// Closes the OutputDevice.
   /// </summary>
   /// <exception cref="OutputDeviceException">
   /// If an error occurred while closing the OutputDevice.
   /// </exception>
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

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         delegateQueue.Dispose();

         lock (lockObject)
         {
            Reset();

            // Close the OutputDevice.
            int result = midiOutClose(Handle);

            if (result != MidiDeviceException.MMSYSERR_NOERROR)
            {
               // Throw an exception.
               throw new OutputDeviceException(result);
            }
         }
      }
      else
      {
         midiOutReset(Handle);
         midiOutClose(Handle);
      }

      base.Dispose(disposing);
   }

   public override void Dispose()
   {
      #region Guard

      if (IsDisposed)
      {
         return;
      }

      #endregion

      lock (lockObject)
      {
         Close();
      }
   }

   public override void Reset()
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      lock (lockObject)
      {
         // Reset the OutputDevice.
         int result = midiOutReset(Handle);

         if (result == MidiDeviceException.MMSYSERR_NOERROR)
         {
            while (bufferCount > 0)
            {
               Monitor.Wait(lockObject);
            }
         }
         else
         {
            // Throw an exception.
            throw new OutputDeviceException(result);
         }
      }
   }

   #region Send
   public virtual void Send(ChannelMessage message)
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      Send(message.Message);
   }

   public virtual void Send(SysExMessage message)
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      lock (lockObject)
      {
         headerBuilder.InitializeBuffer(message);
         headerBuilder.Build();

         // Prepare system exclusive buffer.
         int result = midiOutPrepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

         // If the system exclusive buffer was prepared successfully.
         if (result == MidiDeviceException.MMSYSERR_NOERROR)
         {
            bufferCount++;

            // Send system exclusive message.
            result = midiOutLongMsg(Handle, headerBuilder.Result, SizeOfMidiHeader);

            // If the system exclusive message could not be sent.
            if (result != MidiDeviceException.MMSYSERR_NOERROR)
            {
               midiOutUnprepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);
               bufferCount--;
               headerBuilder.Destroy();

               // Throw an exception.
               throw new OutputDeviceException(result);
            }
         }
         // Else the system exclusive buffer could not be prepared.
         else
         {
            // Destroy system exclusive buffer.
            headerBuilder.Destroy();

            // Throw an exception.
            throw new OutputDeviceException(result);
         }
      }
   }

   public virtual void Send(SysCommonMessage message)
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      Send(message.Message);
   }

   public virtual void Send(SysRealtimeMessage message)
   {
      #region Require

      if (IsDisposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      Send(message.Message);
   }

   protected void Send(int message)
   {
      lock (lockObject)
      {
         int result = midiOutShortMsg(Handle, message);

         if (result != MidiDeviceException.MMSYSERR_NOERROR)
         {
            throw new OutputDeviceException(result);
         }
      }
   }
   #endregion

   #region Helper Method
   // Get device capabilities.
   public static MidiOutCaps GetDeviceCapabilities(int deviceID)
   {
      int result;
      MidiOutCaps caps = new MidiOutCaps();

      result = midiOutGetDevCaps(deviceID, ref caps, Marshal.SizeOf(caps));

      if (result != MIDIExceptions.MMSYSERR_NOERROR)
      {
         throw new OutputDeviceException(result);
      }

      return caps;
   }

   // Handles Windows messages.
   protected virtual void HandleMessage(int handle, int msg, int instance, int param1, int param2)
   {
      if (msg == MOM_OPEN)
      {
      }
      else if (msg == MOM_CLOSE)
      {
      }
      else if (msg == MOM_DONE)
      {
         delegateQueue.Post(ReleaseBuffer, new IntPtr(param1));
      }
   }

   // Releases buffers.
   private void ReleaseBuffer(object state)
   {
      lock (lockObject)
      {
         IntPtr headerPtr = (IntPtr)state;

         // Unprepare the buffer.
         int result = midiOutUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

         if (result != MIDIExceptions.MMSYSERR_NOERROR)
         {
            Exception ex = new OutputDeviceException(result);

            OnError(new ErrorEventArgs(ex));
         }

         // Release the buffer resources.
         headerBuilder.Destroy(headerPtr);

         bufferCount--;

         Monitor.Pulse(lockObject);

         Debug.Assert(bufferCount >= 0);
      }
   }
   #endregion

   /// <summary>
   /// The exception that is thrown when a error occurs with the OutputDevice
   /// class.
   /// </summary>
   public class OutputDeviceException : MidiDeviceException
   {
      #region OutputDeviceException Members

      #region Win32 Midi Output Error Function

      [DllImport("winmm.dll")]
      private static extern int midiOutGetErrorText(int errCode,
          StringBuilder message, int sizeOfMessage);

      #endregion

      #region Fields

      // The error message.
      private StringBuilder message = new StringBuilder(128);

      #endregion

      #region Construction

      /// <summary>
      /// Initializes a new instance of the OutputDeviceException class with
      /// the specified error code.
      /// </summary>
      /// <param name="errCode">
      /// The error code.
      /// </param>
      public OutputDeviceException(int errCode) : base(errCode)
      {
         // Get error message.
         midiOutGetErrorText(errCode, message, message.Capacity);
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
            return message.ToString();
         }
      }

      #endregion

      #endregion
   }
}
