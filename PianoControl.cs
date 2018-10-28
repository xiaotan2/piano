using UnityEngine;
using System;
using System.Collections.Generic;
using Sanford.Multimedia;

/// <summary>
/// The base class for all piano controllers.
/// </summary>
public abstract class PianoControl : MonoBehaviour {

   #region Private Members
   public static MIDIOutDevice outDevice;
   protected MIDIController inDevice;
   protected Queue<KeyValuePair<int, ChannelCommand>> keyEvents;
   #endregion

   protected void initPianoAndSoundDevices()
   {
      // initialize input and output device
      if (MIDIOutDevice.DeviceCount > 0)
      {
         outDevice = new MIDIOutDevice(0);
         Debug.Log("Output device setup complete.");
      }
      if (MIDIController.DeviceCount > 0)
      {
         inDevice = new MIDIController(0);
         inDevice.ChannelMessageReceived += HandleKeyPressed;
         inDevice.Error += new EventHandler<ErrorEventArgs>(inDevice_Error);
         Debug.Log("Input device setup complete.");
      }

      if (outDevice == null)
      {
         throw new Exception("No output device present. Please check if your sound system is working correctly.");
      }
      if (inDevice == null)
      {
         Debug.Log("Input Device or Output Device missing.");
         return;
      }

      // connect the input device to output device
      MIDIDevice.Connect(inDevice.Handle, outDevice.Handle);

      // initialize the queue: Key (Channel) Value (Channel Command)
      keyEvents = new Queue<KeyValuePair<int, ChannelCommand>>();

      try
      {
         inDevice.StartRecording();
      }
      catch (Exception e)
      {
         throw new Exception("Failed to start recording. " + e.Message);
      }
      Debug.Log("Successfully start!");
   }

   protected void handlePianoEvents()
   {
      while (inDevice != null && keyEvents.Count > 0)
      {
         KeyValuePair<int, ChannelCommand> pair = keyEvents.Dequeue();
         // piano key pressed
         if (pair.Value == ChannelCommand.NoteOn)
         {
            onKeyPressed(pair.Key);
         }
         // piano key lifted
         else if (pair.Value == ChannelCommand.NoteOff)
         {
            onKeyLifted(pair.Key);
         }
      }
   }

   protected abstract void onKeyPressed(int keyInt);

   protected abstract void onKeyLifted(int keyInt);

   protected void disposePianoAndSoundDevices()
   {
      if (outDevice != null && inDevice != null)
      {
         MIDIDevice.Disconnect(inDevice.Handle, outDevice.Handle);
      }
      if (outDevice != null)
      {
         Debug.Log("Dispose output device.");
         outDevice.Dispose();
         outDevice = null;
      }
      if (inDevice != null)
      {
         Debug.Log("Dispose input device.");
         try
         {
            inDevice.StopRecording();
         }
         catch (Exception e)
         {
            throw new Exception("Failed to stop recording. " + e.Message);
         }
         inDevice.Dispose();
         inDevice = null;
      }
   }

   private void inDevice_Error(object sender, ErrorEventArgs e)
   {
      throw new Exception(e.Error.Message);
   }

   private void HandleKeyPressed(object sender, ChannelMessageEventArgs e)
   {
      // audio plays the sound
      outDevice.Send(new ChannelMessage(e.Message.Command, e.Message.MidiChannel, e.Message.Data1, e.Message.Data2));

      // insert the key event queue
      if(e.Message.Data1 < GlobalReference.lowestKeyInt || e.Message.Data1 > GlobalReference.highestKeyInt)
      {
         Debug.LogFormat("Key Pressed out of range: {0}", e.Message.Data1);
      }
      if(e.Message.Command == ChannelCommand.NoteOn)
      {
         GlobalReference.playerHitKeysBitmap.Mask(e.Message.Data1 - GlobalReference.lowestKeyInt);
      }
      else if(e.Message.Command == ChannelCommand.NoteOff)
      {
         GlobalReference.playerHitKeysBitmap.Unmask(e.Message.Data1 - GlobalReference.lowestKeyInt);
      }
      keyEvents.Enqueue(new KeyValuePair<int, ChannelCommand>(e.Message.Data1, e.Message.Command));
   }

}
