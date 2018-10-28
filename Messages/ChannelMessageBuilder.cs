using UnityEngine;
using System.Collections;

/// <summary>
/// Provides functionality for building ChannelMessages.
/// </summary>
public class ChannelMessageBuilder : IMessageBuilder
{
   #region ChannelMessageBuilder Members

   #region Class Fields

   // Stores the ChannelMessages.
   private static Hashtable messageCache = Hashtable.Synchronized(new Hashtable());

   #endregion

   #region Fields

   // The channel message as a packed integer.
   private int message = 0;

   // The track index
   private int track = 0;

   // The built ChannelMessage
   private ChannelMessage result = null;

   #endregion

   #region Construction

   /// <summary>
   /// Initializes a new instance of the ChannelMessageBuilder class.
   /// </summary>
   public ChannelMessageBuilder()
   {
      Command = ChannelCommand.Controller;
      MidiChannel = 0;
      Data1 = (int)ControllerType.AllSoundOff;
      Data2 = 0;
   }

   /// <summary>
   /// Initializes a new instance of the ChannelMessageBuilder class with
   /// the specified ChannelMessageEventArgs.
   /// </summary>
   /// <param name="message">
   /// The ChannelMessageEventArgs to use for initializing the ChannelMessageBuilder.
   /// </param>
   /// <remarks>
   /// The ChannelMessageBuilder uses the specified ChannelMessageEventArgs to 
   /// initialize its property values.
   /// </remarks>
   public ChannelMessageBuilder(ChannelMessage message)
   {
      Initialize(message);
   }

   #endregion

   #region Methods

   /// <summary>
   /// Initializes the ChannelMessageBuilder with the specified 
   /// ChannelMessageEventArgs.
   /// </summary>
   /// <param name="message">
   /// The ChannelMessageEventArgs to use for initializing the ChannelMessageBuilder.
   /// </param>
   public void Initialize(ChannelMessage message)
   {
      this.message = message.Message;
   }

   /// <summary>
   /// Clears the ChannelMessageEventArgs cache.
   /// </summary>
   public static void Clear()
   {
      messageCache.Clear();
   }

   #endregion

   #region Properties

   /// <summary>
   /// Gets the number of messages in the ChannelMessageEventArgs cache.
   /// </summary>
   public static int Count
   {
      get
      {
         return messageCache.Count;
      }
   }

   /// <summary>
   /// Gets the built ChannelMessageEventArgs.
   /// </summary>
   public ChannelMessage Result
   {
      get
      {
         return result;
      }
   }

   /// <summary>
   /// Gets or sets the ChannelMessageEventArgs as a packed integer. 
   /// </summary>
   internal int Message
   {
      get
      {
         return message;
      }
      set
      {
         message = value;
      }
   }

   /// <summary>
   /// Gets or sets the Command value to use for building the 
   /// ChannelMessageEventArgs.
   /// </summary>
   public ChannelCommand Command
   {
      get
      {
         return ChannelMessage.UnpackCommand(message);
      }
      set
      {
         message = ChannelMessage.PackCommand(message, value);
      }
   }

   /// <summary>
   /// Gets or sets the MIDI channel to use for building the 
   /// ChannelMessageEventArgs.
   /// </summary>
   /// <exception cref="ArgumentOutOfRangeException">
   /// MidiChannel is set to a value less than zero or greater than 15.
   /// </exception>
   public int MidiChannel
   {
      get
      {
         return ChannelMessage.UnpackMidiChannel(message);
      }
      set
      {
         message = ChannelMessage.PackMidiChannel(message, value);
      }
   }

   /// <summary>
   /// Gets or sets the MIDI track to use for building the 
   /// ChannelMessageEventArgs.
   /// </summary>
   public int MidiTrack
   {
      get
      {
         return track;
      }
      set
      {
         track = value;
      }
   }

   /// <summary>
   /// Gets or sets the first data value to use for building the 
   /// ChannelMessageEventArgs.
   /// </summary>
   /// <exception cref="ArgumentOutOfRangeException">
   /// Data1 is set to a value less than zero or greater than 127.
   /// </exception>
   public int Data1
   {
      get
      {
         return MIDIShortMessage.UnpackData1(message);
      }
      set
      {
         message = MIDIShortMessage.PackData1(message, value);
      }
   }

   /// <summary>
   /// Gets or sets the second data value to use for building the 
   /// ChannelMessageEventArgs.
   /// </summary>
   /// <exception cref="ArgumentOutOfRangeException">
   /// Data2 is set to a value less than zero or greater than 127.
   /// </exception>
   public int Data2
   {
      get
      {
         return MIDIShortMessage.UnpackData2(message);
      }
      set
      {
         message = MIDIShortMessage.PackData2(message, value);
      }
   }

   #endregion

   #endregion

   #region IMessageBuilder Members

   /// <summary>
   /// Builds a ChannelMessageEventArgs.
   /// </summary>
   public void Build()
   {
      result = (ChannelMessage)messageCache[message];

      // If the message does not exist.
      if (result == null)
      {
         result = new ChannelMessage(message, track);

         // Add message to cache.
         messageCache.Add(message, result);
      }
   }

   #endregion
}
