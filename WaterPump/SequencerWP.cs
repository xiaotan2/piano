using System;
using System.Collections.Generic;
using System.ComponentModel;

class SequencerWP : IComponent
{
   private Sequence sequence = null;

   private List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>();

   private MessageDispatcher dispatcher = new MessageDispatcher();

   private MidiInternalClock clock = new MidiInternalClock();

   private ChannelChaser chaser = new ChannelChaser();

   private ChannelStopper stopper = new ChannelStopper();

   private readonly object lockObject = new object();

   private int tracksPlayingCount;

   private bool disposed = false;

   private bool playing = false;

   private ISite site = null;

   private int firstTick = -1;
   private int lastTick = -1;

   private bool loop = true;
   private int loopStart = -1;
   private int loopEnd = -1;

   public List<NotesList> notesList = new List<NotesList>();

   #region Events

   public event EventHandler PlayingCompleted;

   public event EventHandler<ChannelMessageEventArgs> ChannelMessagePlayed
   {
      add
      {
         dispatcher.ChannelMessageDispatched += value;
      }
      remove
      {
         dispatcher.ChannelMessageDispatched -= value;
      }
   }

   public event EventHandler<SysExMessageEventArgs> SysExMessagePlayed
   {
      add
      {
         dispatcher.SysExMessageDispatched += value;
      }
      remove
      {
         dispatcher.SysExMessageDispatched -= value;
      }
   }

   public event EventHandler<MetaMessageEventArgs> MetaMessagePlayed
   {
      add
      {
         dispatcher.MetaMessageDispatched += value;
      }
      remove
      {
         dispatcher.MetaMessageDispatched -= value;
      }
   }

   public event EventHandler<ChasedEventArgs> Chased
   {
      add
      {
         chaser.Chased += value;
      }
      remove
      {
         chaser.Chased -= value;
      }
   }

   public event EventHandler<StoppedEventArgs> Stopped
   {
      add
      {
         stopper.Stopped += value;
      }
      remove
      {
         stopper.Stopped -= value;
      }
   }

   #endregion

   public SequencerWP()
   {
      dispatcher.MetaMessageDispatched += delegate (object sender, MetaMessageEventArgs e)
      {
         if (e.Message.MetaType == MetaType.EndOfTrack)
         {
            tracksPlayingCount--;
            UnityEngine.Debug.LogFormat("end. t:{0}", tracksPlayingCount);
            if (tracksPlayingCount == 0)
            {
               Stop(true);
               // playing completed handles initialization if looping is enabled.
               OnPlayingCompleted(EventArgs.Empty);
            }
         }
         else
         {
            clock.Process(e.Message);
         }
      };

      dispatcher.ChannelMessageDispatched += delegate (object sender, ChannelMessageEventArgs e)
      {
         stopper.Process(e.Message);
      };

      clock.Tick += delegate (object sender, EventArgs e)
      {
         lock(lockObject)
         {
            if (!playing)
               return;

            if(Position == LoopEnd)
            {
               Stop(true);
               // playing completed handles initialization if looping is enabled.
               OnPlayingCompleted(EventArgs.Empty);
               return;
            }

            // only go through tick iterator if we are out of start offset
            if(Position >= LoopStart)
            {
               foreach (IEnumerator<int> enumerator in enumerators)
               {
                  enumerator.MoveNext();
               }
            }
         }
      };

      sequence = GlobalReference.sequence;

      if (Sequence == null)
      {
         throw new Exception("Sequence should not be null.");
      }
      // Process each track and produce the queue of notes to played. This queue is the player paced sheet music.
      Dictionary<int, NoteWithDuration> noteToTicksTable = new Dictionary<int, NoteWithDuration>();
      foreach (Track t in Sequence)
      {
         if(t.TrackId == -1)
         {
            throw new Exception("Track ID is 0. Track is not loaded correctly.");
         }

         // not part of user select track to play
         if (t.TrackId != GlobalReference.leftHandTrackIndex && t.TrackId != GlobalReference.rightHandTrackIndex)
            continue;

         noteToTicksTable.Clear();
         notesList.Add(new NotesList());
         int trackIndex = notesList.Count - 1;

         IEnumerator<MidiEvent> enumerator = t.Iterator().GetEnumerator();
         while(enumerator.MoveNext())
         {
            if (enumerator.Current.MidiMessage.MessageType != MidiMessageType.Channel)
               continue;

            ChannelMessage channelMsg = (ChannelMessage)enumerator.Current.MidiMessage;

            // Store absolute ticks of NoteOn message in hash table
            if (channelMsg.Command == ChannelCommand.NoteOn)
            {
               if (channelMsg.Data2 == 0) // Note on message with velocity of 0 is equivalent of Note Off Message
               {
                  NoteWithDuration noteOff;
                  if (noteToTicksTable.TryGetValue(channelMsg.Data1, out noteOff))
                  {
                     //UnityEngine.Debug.LogFormat("OFF, {0}", channelMsg.Data1);
                     noteOff.Duration = enumerator.Current.AbsoluteTicks - noteOff.AbsoluteTicks; // TODO: Test if this works
                     noteToTicksTable.Remove(channelMsg.Data1);
                  }
               }
               else
               {
                  //UnityEngine.Debug.LogFormat("ON, {0} Ticks:{1}", channelMsg.Data1, enumerator.Current.AbsoluteTicks);
                  if(!noteToTicksTable.ContainsKey(channelMsg.Data1))
                  {
                     NoteWithDuration note = new NoteWithDuration(enumerator.Current.AbsoluteTicks, channelMsg.Data1, -1, channelMsg.MidiTrack);
                     notesList[trackIndex].Add(ref note);
                     noteToTicksTable.Add(channelMsg.Data1, note);
                  }
                  else
                  {
                     UnityEngine.Debug.LogFormat("Multiple Note On Msgs. Ignored ID: {0}", channelMsg.Data1);
                  }
               }
            }
            // When encounter NoteOff message, Find corresponding NoteOn message in hash table
            // and calculate duration. Update the duration of that note.
            else if(channelMsg.Command == ChannelCommand.NoteOff)
            {
               NoteWithDuration noteOff;
               if (noteToTicksTable.TryGetValue(channelMsg.Data1, out noteOff))
               {
                  //UnityEngine.Debug.LogFormat("OFF, {0}", channelMsg.Data1);
                  noteOff.Duration = enumerator.Current.AbsoluteTicks - noteOff.AbsoluteTicks; // TODO: Test if this works
                  noteToTicksTable.Remove(channelMsg.Data1);
               }
            }
         }
      }

      // Find first tick of NoteOn message among the tracks
      for (int i = 0; i < notesList.Count; i++)
      {
         firstTick = (firstTick == -1 || notesList[i].FirstTick < firstTick) ? notesList[i].FirstTick : firstTick;
      }

      // Find last tick of NoteOn message among the tracks
      for (int i = 0; i < Sequence.Count; i++)
      {
         UnityEngine.Debug.LogFormat("Sequence track length: {0}", Sequence[i].Length);
         lastTick = (lastTick == -1 || Sequence[i].Length > lastTick) ? Sequence[i].Length : lastTick;
      }
   }

   ~SequencerWP()
   {
      UnityEngine.Debug.Log("Destruct is called.");
      Dispose(false);
   }

   public void Start(int offset = 0)
   {
      #region Require

      if (disposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      lock (lockObject)
      {
         Stop(true);

         // set the position to start offset
         SetPositionAtStartOffset(offset);

         Continue();
      }
   }

   public void Continue()
   {
      #region Require

      if (disposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      #region Guard

      if (Sequence == null)
      {
         return;
      }

      #endregion

      lock (lockObject)
      {
         Stop(false);

         enumerators.Clear();
         // ensure position is not affected by start offset
         int position = (Position >= 0) ? Position : 0;
         foreach (Track t in Sequence)
         {
            enumerators.Add(t.TickIterator(position, chaser, dispatcher).GetEnumerator());
         }

         tracksPlayingCount = Sequence.Count;
         playing = true;
         clock.Ppqn = sequence.Division;
         clock.Continue();
      }
   }

   public void Stop(bool allSoundOff)
   {
      #region Require

      if (disposed)
      {
         throw new ObjectDisposedException(this.GetType().Name);
      }

      #endregion

      lock (lockObject)
      {
         playing = false;
         clock.Stop();
         if(allSoundOff)
         {
            stopper.AllSoundOff();
         }
      }
   }

   public void SetPositionAtStartOffset(int offset)
   {
      // no tracks are assigned to be played by player
      if (GlobalReference.leftHandTrackIndex == -1 && GlobalReference.rightHandTrackIndex == -1)
      {
         Position = LoopStart;
         return;
      }
      if (LoopStart < 0 || offset < 0)
      {
         throw new Exception(string.Format("Failed to set position at start offset. first tick: {0}, speed: {1}", firstTick, offset));
      }
      Position = LoopStart - offset;
   }

   protected virtual void Dispose(bool disposing)
   {
      if (disposing)
      {
         UnityEngine.Debug.Log("Actual dispose is called.");
         lock (lockObject)
         {
            Stop(true);

            clock.Dispose();

            disposed = true;

            GC.SuppressFinalize(this);
         }

         for(int i = 0; i < notesList.Count; i++)
         {
            notesList[i].Clear();
         }
         notesList.Clear();
        
      }
   }

   protected virtual void OnPlayingCompleted(EventArgs e)
   {
      EventHandler handler = PlayingCompleted;

      if (handler != null)
      {
         handler(this, e);
      }
   }

   protected virtual void OnDisposed(EventArgs e)
   {
      EventHandler handler = Disposed;

      if (handler != null)
      {
         handler(this, e);
      }
   }

   public int Position
   {
      get
      {
         #region Require

         if (disposed)
         {
            throw new ObjectDisposedException(this.GetType().Name);
         }

         #endregion

         return clock.Ticks;
      }
      set
      {
         #region Require

         if (disposed)
         {
            throw new ObjectDisposedException(this.GetType().Name);
         }

         #endregion

         bool wasPlaying;

         lock (lockObject)
         {
            wasPlaying = playing;

            Stop(true);

            clock.SetTicks(value);
         }

         lock (lockObject)
         {
            if (wasPlaying)
            {
               Continue();
            }
         }
      }
   }

   public bool Playing
   {
      get
      {
         return playing;
      }
   }

   public Sequence Sequence
   {
      get
      {
         return sequence;
      }
   }

   public int FirstTick
   {
      get { return firstTick; }
   }
   public int LastTick
   {
      get { return lastTick; }
   }

   public int LoopStart
   {
      get { return loopStart; }
      set { loopStart = value; }
   }

   public int LoopEnd
   {
      get { return loopEnd; }
      set { loopEnd = value; }
   }

   #region IComponent Members

   public event EventHandler Disposed;

   public ISite Site
   {
      get
      {
         return site;
      }
      set
      {
         site = value;
      }
   }

   #endregion

   #region IDisposable Members

   public void Dispose()
   {
      UnityEngine.Debug.Log("Default dispose is called.");
      #region Guard

      if (disposed)
      {
         return;
      }

      #endregion

      Dispose(true);
   }

   #endregion
}

