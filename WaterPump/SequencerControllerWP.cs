using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public enum HighlightState
{
   Expand,
   Move,
   Shrink
}

public class highlight
{
   private NoteWithDuration note;
   private HighlightState state;
   private int ticks;
   private float zHigh;
   private const float yHigh = 1f;
   private const float yLow = 0f;
   public bool done = false;
   public Transform trans;
   public highlight(NoteWithDuration Note, int Ticks, Transform Trans, HighlightState State, float ZHigh)
   {
      note = Note;
      ticks = Ticks;
      trans = Trans;
      state = State;
      zHigh = ZHigh;
   }

   public void update(int tickDiff, float posSpeed, float scaleSpeed, int waterSpeed)
   {
      var initPos = trans.position;
      var initSca = trans.localScale;
      float newZPos = 0f;
      float newYSca = 0f;
      float newZHigh = zHigh;
      switch (state)
      {
         case HighlightState.Expand:
            newZPos = initPos.z + (posSpeed * (float)tickDiff);
            newZPos = (newZPos > zHigh) ? zHigh : newZPos;
            newYSca = initSca.y + (scaleSpeed * (float)tickDiff);
            newYSca = (newYSca > yHigh) ? yHigh : newYSca;
            trans.position = new Vector3(initPos.x, initPos.y, newZPos);
            trans.localScale = new Vector3(initSca.x, newYSca, initSca.z);
            if (initPos.z == zHigh)
            {
               zHigh = zHigh + 2f;
               state = HighlightState.Shrink;
               WaterPumpGlobal.notesPlayingBitmap.Mask(note.Note - GlobalReference.lowestKeyInt);
            }
            else if (ticks <= 0)
            {
               zHigh = initPos.z + (zHigh - initPos.z) * 2f;
               state = HighlightState.Move;
            }
            ticks = ticks - tickDiff;
            break;
         case HighlightState.Move:
            newZPos = initPos.z + (posSpeed * (float)tickDiff * 2f);
            newZPos = (newZPos > zHigh) ? zHigh : newZPos;
            trans.position = new Vector3(initPos.x, initPos.y, newZPos);
            state = (initPos.z == zHigh) ? HighlightState.Shrink : HighlightState.Move;
            if (initPos.z == zHigh)
            {
               WaterPumpGlobal.notesPlayingBitmap.Mask(note.Note - GlobalReference.lowestKeyInt);
            }
            zHigh = (initPos.z == zHigh) ? zHigh + 2f : zHigh;
            break;
         case HighlightState.Shrink:
            // in case note duration is very large that highlight should not shrink, don't update transform or consider remove node
            if (ticks < waterSpeed)
            {
               newZPos = initPos.z + (posSpeed * (float)tickDiff);
               newZPos = (newZPos > zHigh) ? zHigh : newZPos;
               newYSca = initSca.y - (scaleSpeed * (float)tickDiff);
               newYSca = (newYSca < yLow) ? yLow : newYSca;
               trans.position = new Vector3(initPos.x, initPos.y, newZPos);
               trans.localScale = new Vector3(initSca.x, newYSca, initSca.z);

               // remove node
               if (newYSca == yLow)
               {
                  WaterPumpGlobal.notesPlayingBitmap.Unmask(note.Note - GlobalReference.lowestKeyInt);
                  done = true;
                  break;
               }
            }
            ticks = ticks - tickDiff;
            break;
         default:
            throw new Exception("Highlight state does not exist.");
      }
   }
}

public class SequencerControllerWP : MonoBehaviour {

   # region Public Member
   //public Transform keyHitAnimation;
   public Transform highlightGreenPrefab;
   public Transform highlightRedPrefab;
   //public int waterSpeed = 500; // unit of ticks
   public Slider slider;
   public Slider loopStartSlider;
   public Slider loopEndSlider;
   public Toggle practiceToggle;
   public Toggle loopToggle;
   public Button ContinueButton;
   #endregion

   #region Private Member

   private SequencerWP sequencer = null;
   private int currTick;

   private LinkedList<highlight>[] highlights;
   private HashSet<int> notesHighlighted; // notes that have highlights

   private Dictionary<int, IEnumerator<NoteWithDuration>> TrackIdToEnumerator;

   private MinNoteHeap noteListHeap;

   private int waterSpeed;
   private int ticksPerBar;
   private int ticksRemainInLastBar;

   private float posSpeed = 0f;
   private float scaleSpeed = 0f;

   private const int keyLow = 24;
   private const float zLow = -4f;
   private const float zSize = 2f;
   private const float yLow = 0f;
   private const float yHigh = 1f;
   #endregion

   #region Unity Functions

   // Use this for initialization
   void Start () {

      Debug.Log("Sequencer Controller Start.");
      // initialize global references. Add keys object references.
      WaterPumpGlobal.init(transform);

      if (GlobalReference.sequence == null)
      {
         throw new UnityException("Sequence is empty. It is not being properly loaded before start playing.");
      }

      waterSpeed = GlobalReference.speed * 100;
      Debug.LogFormat("Speed: {0}", waterSpeed);

      // initialize sequencer. The constructor will initialize the notesList in sequencer.
      sequencer = new SequencerWP();
      sequencer.LoopStart = 0;
      sequencer.LoopEnd = sequencer.LastTick;
      sequencer.SetPositionAtStartOffset(waterSpeed);
      currTick = sequencer.Position;
      sequencer.ChannelMessagePlayed += new EventHandler<ChannelMessageEventArgs>(HandleChannelMessagePlayed);
      sequencer.SysExMessagePlayed += new EventHandler<SysExMessageEventArgs>(HandleSysExMessagePlayed);
      sequencer.PlayingCompleted += new EventHandler(HandlePlayingCompleted);
      sequencer.Stopped += new EventHandler<StoppedEventArgs>(HandleStopped);
      sequencer.Chased += new EventHandler<ChasedEventArgs>(HandleChased);

      highlights = new LinkedList<highlight>[84];
      notesHighlighted = new HashSet<int>();
      TrackIdToEnumerator = new Dictionary<int, IEnumerator<NoteWithDuration>>();
      noteListHeap = new MinNoteHeap(sequencer.notesList.Count);

      for(int i = 0; i < highlights.Length; i++)
      {
         highlights[i] = new LinkedList<highlight>();
      }

      if(sequencer.FirstTick != -1 && sequencer.LastTick != -1)
      {
         slider.minValue = 0;
         slider.maxValue = sequencer.LastTick;
         slider.interactable = false;

         ticksPerBar = (int)(((float)4 / sequencer.Sequence.TimeSignatureDenominator) * sequencer.Sequence.TimeSignatureNumerator) * sequencer.Sequence.Division;
         ticksRemainInLastBar = sequencer.LastTick % ticksPerBar;

         loopStartSlider.minValue = 0;
         loopStartSlider.maxValue = sequencer.LastTick / ticksPerBar;
         loopStartSlider.interactable = true;
         loopStartSlider.onValueChanged.AddListener(delegate { LoopStartValueChangeCheck(); });

         loopEndSlider.minValue = 0;
         loopEndSlider.maxValue = sequencer.LastTick / ticksPerBar;
         loopEndSlider.interactable = true;
         loopEndSlider.value = loopEndSlider.maxValue;
         loopEndSlider.onValueChanged.AddListener(delegate { LoopEndValueChangeCheck(); });

         if (ticksRemainInLastBar != 0)
         {
            Debug.LogFormat("Last tick {0} and ticks/Bar {1} does not divide in whole number.", sequencer.LastTick, ticksPerBar);
            loopStartSlider.maxValue += 1;
            loopEndSlider.maxValue += 1;
            loopEndSlider.value = loopEndSlider.maxValue;
         }
      }
      else
      {
         Debug.Log("The first tick and/or last tick of the sequencer is -1.");
      }

      practiceToggle.isOn = false;
      if (GlobalReference.leftHandTrackIndex == -1 && GlobalReference.rightHandTrackIndex == -1)
         practiceToggle.enabled = false;
      loopToggle.isOn = false;
      GlobalReference.practiceMode = practiceToggle.isOn;
      GlobalReference.loopMode = loopToggle.isOn;
      ContinueButton.enabled = false;

      posSpeed = 2f / waterSpeed;
      scaleSpeed = 1f / waterSpeed;
   }
	
	// Update is called once per frame
	void Update ()
   {

      if (GlobalReference.practiceMode && sequencer.Position >= sequencer.LoopStart && sequencer.Position < sequencer.LoopEnd)
      {
         // Player played keys on keyboard does not match notes
         if (!WaterPumpGlobal.notesPlayingBitmap.Match(GlobalReference.playerHitKeysBitmap) && // match when all keys being played match exactly with the keys
                !GlobalReference.playerHitKeysBitmap.Fill(WaterPumpGlobal.notesPlayingBitmap) && // fill when all keys needs to be played are being played, and player could be playing more keys
                !WaterPumpGlobal.notesPlayingBitmap.IsClear()) // keys are not empty
         {
            if(sequencer.Playing)
            {
               sequencer.Stop(false);
            }
            return;
         }
         // Player played keys on keyboard match notes
         else
         {
            // Cancel notes here (player has already played the notes, these notes will disappear from now)
            WaterPumpGlobal.notesPlayingBitmap.UnmaskAll();
            if (!sequencer.Playing)
            {
               sequencer.Continue();
            }
         }
      }

      if (!sequencer.Playing)
         return;

      // Ensure tick diff is positive. Sequencer's position can be loop back to a small number
      int tickDiff = (currTick <= sequencer.Position) ? (sequencer.Position - currTick) : 0;
      currTick = sequencer.Position;
      slider.value = sequencer.Position;

      // process each highlighted note
      List<int> notesToRemove = new List<int>();
      foreach(int note in notesHighlighted)
      {
         // process each highlight linked list node
         LinkedListNode<highlight> head = highlights[note - keyLow].First;
         if(head == null)
         {
            notesToRemove.Add(note);
         }
         while(head != null)
         {
            //Transform trans = head.Value.trans;
            //var initPos = trans.position;
            //var initSca = trans.localScale;
            //float newZPos = 0f;
            //float newYSca = 0f;
            //HighlightState newState = HighlightState.Expand;
            //int newTicks = 0;
            //float newZHigh = head.Value.zHigh;
            LinkedListNode<highlight> next = head.Next;
            //switch (head.Value.state)
            //{
            //   case HighlightState.Expand:
            //      newZPos = initPos.z + (posSpeed * (float)tickDiff);
            //      newZPos = (newZPos > head.Value.zHigh) ? head.Value.zHigh : newZPos;
            //      newYSca = initSca.y + (scaleSpeed * (float)tickDiff);
            //      newYSca = (newYSca > yHigh) ? yHigh : newYSca;
            //      trans.position = new Vector3(initPos.x, initPos.y, newZPos);
            //      trans.localScale = new Vector3(initSca.x, newYSca, initSca.z);
            //      if (initPos.z == head.Value.zHigh)
            //      {
            //         newZHigh = head.Value.zHigh + 2f;
            //         newState = HighlightState.Shrink;
            //         WaterPumpGlobal.notesPlayingBitmap.Mask(head.Value.note.Note - GlobalReference.lowestKeyInt);
            //      }
            //      else if (head.Value.ticks <= 0)
            //      {
            //         newZHigh = initPos.z + (head.Value.zHigh - initPos.z) * 2f;
            //         newState = HighlightState.Move;
            //      }
            //      newTicks = head.Value.ticks - tickDiff;
            //      head.Value = new highlight(head.Value.note, newTicks, trans, newState, newZHigh);
            //      break;
            //   case HighlightState.Move:
            //      newZPos = initPos.z + (posSpeed * (float)tickDiff * 2f);
            //      newZPos = (newZPos > head.Value.zHigh) ? head.Value.zHigh : newZPos;
            //      trans.position = new Vector3(initPos.x, initPos.y, newZPos);
            //      newState = (initPos.z == head.Value.zHigh) ? HighlightState.Shrink : HighlightState.Move;
            //      newZHigh = (initPos.z == head.Value.zHigh) ? head.Value.zHigh + 2f : head.Value.zHigh;
            //      if(initPos.z == head.Value.zHigh)
            //      {
            //         WaterPumpGlobal.notesPlayingBitmap.Mask(head.Value.note.Note - GlobalReference.lowestKeyInt);
            //      }
            //      head.Value = new highlight(head.Value.note, head.Value.ticks, trans, newState, newZHigh);
            //      break;
            //   case HighlightState.Shrink:
            //      // in case note duration is very large that highlight should not shrink, don't update transform or consider remove node
            //      if (head.Value.ticks < waterSpeed)
            //      {
            //         newZPos = initPos.z + (posSpeed * (float)tickDiff);
            //         newZPos = (newZPos > head.Value.zHigh) ? head.Value.zHigh : newZPos;
            //         newYSca = initSca.y - (scaleSpeed * (float)tickDiff);
            //         newYSca = (newYSca < yLow) ? yLow : newYSca;
            //         trans.position = new Vector3(initPos.x, initPos.y, newZPos);
            //         trans.localScale = new Vector3(initSca.x, newYSca, initSca.z);

            //         // remove node
            //         if (newYSca == yLow)
            //         {
            //            Destroy(head.Value.trans.gameObject);
            //            highlights[note - keyLow].Remove(head);
            //            WaterPumpGlobal.notesPlayingBitmap.Unmask(head.Value.note.Note - GlobalReference.lowestKeyInt);
            //            break;
            //         }
            //      }
            //      newState = HighlightState.Shrink;
            //      newTicks = head.Value.ticks - tickDiff;
            //      head.Value = new highlight(head.Value.note, newTicks, trans, newState, newZHigh);
            //      break;
            //   default:
            //      throw new Exception("Highlight state does not exist.");
            //}
            head.Value.update(tickDiff, posSpeed, scaleSpeed, waterSpeed);
            if(head.Value.done)
            {
               Destroy(head.Value.trans.gameObject);
               highlights[note - keyLow].Remove(head);
            }
            head = next;
         }
      }

      foreach(int note in notesToRemove)
      {
         notesHighlighted.Remove(note);
      }

      // find notes to be played in the near future
      if(noteListHeap.Size == 0)
         return;

      // look ahead and start rendering future nots within the window
      int windowEnd = Math.Min(sequencer.Position + waterSpeed, sequencer.LoopEnd);
      if (windowEnd >= noteListHeap.PeekMinTicks())
      {
         int minTick = noteListHeap.PeekMinTicks();
         while (noteListHeap.Size > 0 && noteListHeap.PeekMinTicks() == minTick)
         {
            NoteWithDuration sameNote = noteListHeap.DeleteMin();
            if (sameNote == null)
            {
               Debug.Log("Delete min note is null.");
               return;
            }

            TrackIdToEnumerator[sameNote.TrackId].MoveNext();
            if (TrackIdToEnumerator[sameNote.TrackId].Current != null)
               noteListHeap.Insert(TrackIdToEnumerator[sameNote.TrackId].Current);

            // generate highlight and put it into the linkedlist corresponds to its note
            bool whiteKey;
            float xPosition = GlobalReference.convertKeyIntToXPosition(sameNote.Note, out whiteKey);
            float yPosition = (whiteKey) ? 0.1f : -0.2f;
            float zPosition = (whiteKey) ? zLow : zLow + 4f;
            Transform highlightPrefab = (sameNote.TrackId == GlobalReference.leftHandTrackIndex) ? highlightRedPrefab : highlightGreenPrefab;
            Transform h = Instantiate(highlightPrefab, new Vector3(xPosition, 0.15f, zPosition), transform.rotation) as Transform;
            h.Rotate(Vector3.right, 90);
            h.transform.localScale = new Vector3(1f, 0f, 1f);
            highlights[sameNote.Note - keyLow].AddLast(new highlight(sameNote, sameNote.Duration, h, HighlightState.Expand, zPosition + zSize));

            // add it to notes highlighted
            if(!notesHighlighted.Contains(sameNote.Note))
            {
               notesHighlighted.Add(sameNote.Note);
            }
         }
      }
   }

   // OnApplicationQuit is called when user exits the application
   void OnApplicationQuit()
   {
      Debug.Log("Sequencer Controller Application Quit.");
      GlobalReference.sequence.Dispose();
   }

   // OnDestroy is called when the scene is destroyed
   void OnDestroy()
   {
      Debug.Log("Sequencer Controller Destroy.");
      sequencer.Dispose();
      WaterPumpGlobal.notesPlayingBitmap.UnmaskAll();
   }

   #endregion

   #region Button Click Functions

   void ContinueSequence()
   {
      loopStartSlider.interactable = false;
      loopEndSlider.interactable = false;

      if (TrackIdToEnumerator.Count != 0)
         return;

      foreach (NotesList list in sequencer.notesList)
      {
         TrackIdToEnumerator.Add(list.TrackID, list.NoteIterator(sequencer.Position).GetEnumerator());
         TrackIdToEnumerator[list.TrackID].MoveNext();
         if (TrackIdToEnumerator[list.TrackID].Current != null)
            noteListHeap.Insert(TrackIdToEnumerator[list.TrackID].Current);
      }

      sequencer.Continue();
   }

   void StopSequence()
   {
      sequencer.Stop(true);
      TrackIdToEnumerator.Clear();
      noteListHeap.Clear();

      loopStartSlider.interactable = true;
      loopEndSlider.interactable = true;
   }

   void RestartSequence()
   {
      StopSequence();

      loopStartSlider.interactable = false;
      loopEndSlider.interactable = false;
      ContinueButton.enabled = true;

      if (TrackIdToEnumerator.Count != 0)
         return;

      // Setting correct track enumerator using loop start
      foreach (NotesList list in sequencer.notesList)
      {
         TrackIdToEnumerator.Add(list.TrackID, list.NoteIterator(sequencer.LoopStart).GetEnumerator());
         TrackIdToEnumerator[list.TrackID].MoveNext();
         if (TrackIdToEnumerator[list.TrackID].Current != null)
            noteListHeap.Insert(TrackIdToEnumerator[list.TrackID].Current);
      }

      // remove all highlights and clear notesHighlighted
      foreach (int note in notesHighlighted)
      {
         LinkedListNode<highlight> head = highlights[note - keyLow].First;
         while(head != null)
         {
            LinkedListNode<highlight> next = head.Next;
            Destroy(head.Value.trans.gameObject);
            highlights[note - keyLow].Remove(head);
            head = next;
         }
      }
      notesHighlighted.Clear();

      WaterPumpGlobal.notesPlayingBitmap.UnmaskAll();

      Debug.Log("Start is called");
      sequencer.Start(waterSpeed);
   }

   #endregion

   #region Event Handler

   private void HandleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
   {
      //Debug.Log(e.Message.ToString());
      if(e.Message.MidiTrack != GlobalReference.leftHandTrackIndex && e.Message.MidiTrack != GlobalReference.rightHandTrackIndex)
      {
         PianoControl.outDevice.Send(e.Message);
      }
   }

   private void HandleSysExMessagePlayed(object sender, SysExMessageEventArgs e)
   {
      // Not doing anything right now
      // PianoControl.outDevice.Send(e.Message); Sometimes causes an exception to be thrown because the output device is overloaded.
   }

   private void HandleStopped(object sender, StoppedEventArgs e)
   {
      foreach (ChannelMessage message in e.Messages)
      {
         PianoControl.outDevice.Send(message);
         // TODO: Lift all piano keys currently being pressed
      }
   }

   private void HandleChased(object sender, ChasedEventArgs e)
   {
      foreach (ChannelMessage message in e.Messages)
      {
         PianoControl.outDevice.Send(message);
      }
   }

   private void HandlePlayingCompleted(object sender, EventArgs e)
   {
      // preprocess data structures for the loop
      if (GlobalReference.loopMode)
      {
         sequencer.Stop(true);

         TrackIdToEnumerator.Clear();
         noteListHeap.Clear();

         // Setting correct track enumerator using loop start
         sequencer.Position = sequencer.LoopStart;
         foreach (NotesList list in sequencer.notesList)
         {
            TrackIdToEnumerator.Add(list.TrackID, list.NoteIterator(sequencer.Position).GetEnumerator());
            TrackIdToEnumerator[list.TrackID].MoveNext();
            if (TrackIdToEnumerator[list.TrackID].Current != null)
               noteListHeap.Insert(TrackIdToEnumerator[list.TrackID].Current);
         }

         sequencer.Start(waterSpeed);
      }
   }

   public void LoopStartValueChangeCheck()
   {
      if(loopStartSlider.value >= loopEndSlider.value)
      {
         loopStartSlider.value = loopEndSlider.value - 1;
      }
      sequencer.LoopStart = (int)loopStartSlider.value * ticksPerBar;

      // when loopStart is over the position, practice mode and MIDI player won't work correctly when continue
      if (sequencer.LoopStart > sequencer.Position)
         ContinueButton.enabled = false;
   }

   public void LoopEndValueChangeCheck()
   {
      if (loopStartSlider.value >= loopEndSlider.value)
      {
         loopEndSlider.value = loopStartSlider.value + 1;
      }
      if(loopEndSlider.value == loopEndSlider.maxValue)
      {
         sequencer.LoopEnd = sequencer.LastTick;
      }
      else
      {
         sequencer.LoopEnd = (int)loopEndSlider.value * ticksPerBar;
      }
   }

   #endregion
}
