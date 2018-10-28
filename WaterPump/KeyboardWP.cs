using System.Collections.Generic;
using UnityEngine;

public class KeyboardWP
{
   // Channel map to key which contains references to the transform objects
   public Dictionary<int, Transform> ChannelToKeyObjectTable;
   public System.Object[] lockObject;
   public const int numKeys = 128;

   public KeyboardWP()
   {
      ChannelToKeyObjectTable = new Dictionary<int, Transform>();
      lockObject = new System.Object[numKeys];
      for(int i = 0; i < numKeys; i++)
      {
         lockObject[i] = new System.Object();
      }
   }

   ~KeyboardWP()
   {
      ChannelToKeyObjectTable.Clear();
   }

   public void ChangeSprite(int note, Sprite sprite)
   {
      lock(lockObject[note])
      {
         Transform key;
         if (ChannelToKeyObjectTable.TryGetValue(note, out key))
         {
            var renderer = key.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
         }
      }
   }

}
