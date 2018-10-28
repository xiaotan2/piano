using UnityEngine;
using System;
using System.Collections.Generic;
using Sanford.Multimedia;
using System.Threading;

public class PianoControlWP : PianoControl {

   # region Public Member
   public Sprite whiteKeyHitGreat;
   public Sprite whiteKeyHit;
   public Sprite whiteKey;
   public Sprite blackKeyHitGreat;
   public Sprite blackKeyHit;
   public Sprite blackKey;
   #endregion

   #region Unity Functions
   /// <summary>
   /// Initialize Input Device (MIDI Controller) and Output Device (Stereo). Connect Input
   /// and Output Device if they are all present. Try to start recording of Input Device.
   /// </summary>
   void Start()
   {
      initPianoAndSoundDevices();
   }

   /// <summary>
	/// Is called every frame.
   /// Get events from the KeyEvents queue. Process the event:
   /// Make animation of key and change its color.
	/// </summary>
   void Update()
   {
      handlePianoEvents();
   }

   void OnDestroy()
   {
      disposePianoAndSoundDevices();
   }

   #endregion

   #region Key Event Handler
   protected override void onKeyPressed(int keyInt)
   {
      bool white;
      GlobalReference.convertKeyIntToXPosition(keyInt, out white);
      Sprite sprite;
      if (white)
      {
         sprite = (WaterPumpGlobal.notesPlayingBitmap.GetMaskedIndexes().Contains(keyInt - GlobalReference.lowestKeyInt)) ? whiteKeyHitGreat : whiteKeyHit;
      }
      else
      {
         sprite = (WaterPumpGlobal.notesPlayingBitmap.GetMaskedIndexes().Contains(keyInt - GlobalReference.lowestKeyInt)) ? blackKeyHitGreat : blackKeyHit;
      }
      WaterPumpGlobal.keyboard.ChangeSprite(keyInt, sprite);
   }

   protected override void onKeyLifted(int keyInt)
   {
      bool white;
      GlobalReference.convertKeyIntToXPosition(keyInt, out white);
      Sprite sprite = (white) ? whiteKey : blackKey;
      WaterPumpGlobal.keyboard.ChangeSprite(keyInt, sprite);
   }
   #endregion
}
