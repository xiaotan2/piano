using System;


public class MinNoteHeap
{
   private int capacity;
   private int size;
   private NoteWithDuration[] noteHeap;

   public MinNoteHeap (int capacity)
   {
      noteHeap = new NoteWithDuration[capacity];
      this.capacity = capacity;
      size = 0;
   }

   public void Clear()
   {
      size = 0;
   }

   public void Insert(NoteWithDuration note)
   {
      if (note == null)
         return;
      if (size >= capacity)
      {
         throw new Exception("Can't insert. The size of the heap will exceed capacity if insert is done.");
      }
      noteHeap[size++] = note;
      BubbleUp(size - 1);
   }

   public NoteWithDuration DeleteMin()
   {
      if (size == 0)
         return null;

      NoteWithDuration ret = noteHeap[0];
      noteHeap[0] = noteHeap[size - 1];
      size--;
      BubbleDown(0);
      return ret;
   }

   public int PeekMinTicks()
   {
      if (Size == 0)
         throw new Exception("Can't peek at empty heap.");

      return noteHeap[0].AbsoluteTicks;
   }

   private void BubbleUp(int index)
   {
      while(index != 0)
      {
         int parent = (index % 2 == 0) ? (index - 2) / 2 : (index - 1) / 2;

         if (noteHeap[parent].AbsoluteTicks > noteHeap[index].AbsoluteTicks)
         {
            SwitchNote(parent, index);
            index = parent;
         }
         else
         {
            break;
         }
      }
   }

   private void BubbleDown(int index)
   {
      while(index * 2 + 1 < size) // left child empty
      {
         int left = index * 2 + 1;
         int right = index * 2 + 2;
         int minIndex = index;
         int minTick = noteHeap[index].AbsoluteTicks;

         if (right < size)
         {
            // compare with left
            minIndex = (noteHeap[minIndex].AbsoluteTicks < noteHeap[left].AbsoluteTicks) ? minIndex : left;
            minTick = (noteHeap[minIndex].AbsoluteTicks < noteHeap[left].AbsoluteTicks) ? noteHeap[minIndex].AbsoluteTicks : noteHeap[left].AbsoluteTicks;
            // compare with right
            minIndex = (noteHeap[minIndex].AbsoluteTicks < noteHeap[right].AbsoluteTicks) ? minIndex : right;
            minTick = (noteHeap[minIndex].AbsoluteTicks < noteHeap[right].AbsoluteTicks) ? noteHeap[minIndex].AbsoluteTicks : noteHeap[right].AbsoluteTicks;
         }
         else // right child is empty, only compare index and left
         {
            // compare with left
            minIndex = (noteHeap[minIndex].AbsoluteTicks < noteHeap[left].AbsoluteTicks) ? minIndex : left;
            minTick = (noteHeap[minIndex].AbsoluteTicks < noteHeap[left].AbsoluteTicks) ? noteHeap[minIndex].AbsoluteTicks : noteHeap[left].AbsoluteTicks;
         }

         if (minIndex == index)
            break;

         SwitchNote(minIndex, index);
         index = minIndex;
      }
   }

   private void SwitchNote(int note1, int note2)
   {
      NoteWithDuration temp = noteHeap[note1];
      noteHeap[note1] = noteHeap[note2];
      noteHeap[note2] = temp;
   }

   public int Size
   {
      get { return size; }
   }

   public int Capacity
   {
      get { return capacity; }
   }
}

