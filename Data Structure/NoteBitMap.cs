using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: make it thread safe
public class NoteBitMap
{
   private int[] bitMap;
   private int numInts;
   private const int BITS_PER_INT = 32;
   private bool clear;

   public NoteBitMap(int size)
   {
      numInts = (size % BITS_PER_INT == 0) ? size / BITS_PER_INT : size / BITS_PER_INT + 1;
      bitMap = new int[numInts];
      clear = true;
   }

   // index from 0 to size-1
   public void Mask(int index)
   {
      int intIndex = index / BITS_PER_INT;
      int offset = index % BITS_PER_INT;
      int mask = 1;
      for(int i = 0; i < offset; i++)
      {
         mask = mask << 1;
      }
      bitMap[intIndex] = bitMap[intIndex] | mask;
      clear = false;
   }

   // index from 0 to size-1
   public void Unmask(int index)
   {
      int intIndex = index / BITS_PER_INT;
      int offset = index % BITS_PER_INT;
      int mask = 1;
      for (int i = 0; i < offset; i++)
      {
         mask = mask << 1;
      }
      bitMap[intIndex] = bitMap[intIndex] & (mask ^ -1);
   }

   public void UnmaskAll()
   {
      for(int i = 0; i < numInts; i++)
      {
         bitMap[i] &= 0;
      }
      clear = true;
   }

   public bool Match(NoteBitMap compBitmap)
   {
      for(int i = 0; i < bitMap.Length; i++)
      {
         if((bitMap[i] ^ compBitmap.Bitmap[i]) != 0)
         {
            return false;
         }
      }
      return true;
   }

   // one map fills another map means all 1s in "mapToFill" are present in "filler", while "filler" can have more 1s present.
   public bool Fill(NoteBitMap mapToFill)
   {
      for(int i = 0; i < bitMap.Length; i++)
      {
         if((bitMap[i] & mapToFill.Bitmap[i]) != mapToFill.Bitmap[i])
         {
            return false;
         }
      }
      return true;
   }

   public HashSet<int> GetMaskedIndexes()
   {
      HashSet<int> res = new HashSet<int>();
      for(int i = 0; i < bitMap.Length; i++)
      {
         int mask = 1;
         for (int j = 0; j < BITS_PER_INT; j++)
         {
            if((bitMap[i] & mask) != 0)
            {
               res.Add(i * BITS_PER_INT + j);
            }
            mask = mask << 1;
         }
      }
      return res;
   }

   public int[] Bitmap
   {
      get
      {
         return bitMap;
      }
   }

   public bool IsClear()
   {
        if(!clear)
        {
            // check if every integer is 0 for clear
            for (int i = 0; i < numInts; i++)
            {
                if (bitMap[i] != 0)
                    return false;
            }
            clear = true;
        }
        return clear;
   }
}

