using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class NoteWithDuration
{
   private int note;
   private int duration;
   private int trackId;
   private int ticks;
   public NoteWithDuration next;

   public NoteWithDuration(int ticks, int note, int duration, int trackId)
   {
      this.ticks = ticks;
      this.note = note;
      this.duration = duration;
      this.trackId = trackId;
      next = null;
   }
   public int AbsoluteTicks
   {
      get { return ticks; }
   }
   public int Note
   {
      get { return note; }
   }
   public int Duration
   {
      get { return duration; }
      set { duration = value; }
   }
   public int TrackId
   {
      get { return trackId; }
   }
}

/// <summary>
/// A doublely linked list with each node being NoteWithDuration.
/// </summary>
public class NotesList
{
   private NoteWithDuration head;
   private NoteWithDuration tail;
   private int trackID;

   public NotesList()
   {
      head = null;
      tail = null;
      trackID = -1;
   }

   public void Add(int ticks, int note, int duration, int trackId)
   {
      if(trackID != -1 && trackId != trackID)
      {
         throw new Exception(string.Format("Adding a note with different track ID. Each NotesList can only support notes with same track ID. New note ID {0}, current ID {1}",
            trackId, trackID));
      }
      NoteWithDuration newNode = new NoteWithDuration(ticks, note, duration, trackId);
      Add(ref newNode);
   }

   public void Add(ref NoteWithDuration node)
   {
      if (trackID != -1 && node.TrackId != trackID)
      {
         throw new Exception(string.Format("Adding a note with different track ID. Each NotesList can only support notes with same track ID. New note ID {0}, current ID {1}", 
            node.TrackId, trackID));
      }
      if (head == null)
      {
         head = node;
         tail = head;
         trackID = node.TrackId;
         return;
      }
      // enforce ticks in ascending order
      if(tail.AbsoluteTicks > node.AbsoluteTicks)
      {
         throw new Exception("Adding a note with lower ticks than ticks of the note at the end of the list. Violates the tick order of the list.");
      }
      tail.next = node;
      tail = tail.next;
   }

   public void Clear()
   {
      head = null;
      tail = null;
      trackID = -1;
   }

   public IEnumerable<NoteWithDuration> NoteIterator(int position)
   {
      NoteWithDuration cursor = head;
      while(cursor != null && cursor.AbsoluteTicks < position)
      {
         cursor = cursor.next;
      }

      while(cursor != null)
      {
         yield return cursor;
         cursor = cursor.next;
      }
      yield return null;
   }

   public int FirstTick
   {
      get
      {
         if (head != null)
            return head.AbsoluteTicks;
         else
            return -1;
      }
   }
   public int LastTick
   {
      get {
         if (tail != null)
            return tail.AbsoluteTicks;
         else
            return -1;
      }
   }
   public NoteWithDuration Head
   {
      get { return head; }
   }
   public NoteWithDuration Tail
   {
      get { return tail; }
   }
   public int TrackID
   {
      get { return trackID; }
   }
}

