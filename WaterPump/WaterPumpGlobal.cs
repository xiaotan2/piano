using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class WaterPumpGlobal {

   // Channel map to string->ring->key combination which contains references to the transform objects
   public static KeyboardWP keyboard = new KeyboardWP();
   
   // A shared queue of integers that represents key's notes. It is for communication between piano control and sequencer
   public static Queue<int> NotesBeingPlayed = new Queue<int>();

   public static NoteBitMap notesPlayingBitmap = new NoteBitMap(GlobalReference.highestKeyInt - GlobalReference.lowestKeyInt);

   /// <summary>
   /// Initialize Channel To StringRingKeyComboObject table and Channel to Ball List Table by finding the corresponding transforms
   /// and store them for references.
   /// <param name="parent"> Parent transform object of keys, strings and rings transform objects. </param>
   /// </summary>
   public static void init(Transform parent)
   {
      // initialize global references. Add keys object references.
      for (int i = GlobalReference.lowestKeyInt; i <= GlobalReference.highestKeyInt; i++)
      {
         string keyName = "Set/" + PianoKeyToMessageDataTable.KeyIntToKeyNameTable[i];
         keyboard.ChannelToKeyObjectTable[i] = parent.Find(keyName);
         //var initPos = keyboard.ChannelToHighLightTable[i].transform.position;
         //keyboard.ChannelToHighLightTable[i].transform.position = new Vector3(initPos.x, initPos.y, initPos.z + 2f);
         //var initSca = keyboard.ChannelToHighLightTable[i].transform.localScale;
         //keyboard.ChannelToHighLightTable[i].transform.localScale = new Vector3(initSca.x, 1f, initSca.z);
      }
   }
   // TODO: Change name of Channel to Notes (avoid confusion).
   // TODO: Make this global class a singleton?
}
