using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GlobalReference {

   public static string chosenSongName = "";
   public static Sequence sequence;
   public static int speed = 1;
   public static int tempoFactor = 1;
   public static bool practiceMode = false;
   public static bool loopMode = false;

   public static int leftHandTrackIndex = -1; // TODO: Add -1 as not assigned enum
   public static int rightHandTrackIndex = -1;

   public static int lowestKeyInt = 24;
   public static int highestKeyInt = 107;

   // Members and Methods for conversion between Key Integer to X Position in game world.
   private const float startXPosition = -0.1f;
   private const float setDistance = 7.7f;
   private const int firstKeyData1 = 24;
   private const int numKeysPerSet = 12;

   public const float heightChange = 0.3f;
   public const float rotateDegree = 24.0f;

   public static NoteBitMap playerHitKeysBitmap = new NoteBitMap(highestKeyInt - lowestKeyInt);

   public static float convertKeyIntToXPosition(int key, out bool whiteBall)
   {
      // calculate the ball x location based on key
      float setNumber = (key - firstKeyData1) / numKeysPerSet;
      int keyLocation = (key - firstKeyData1) % numKeysPerSet;
      float xPosition = startXPosition + setNumber * setDistance + KeyToXOffsetTable[keyLocation];
      whiteBall = (keyLocation != 1 && keyLocation != 3 && keyLocation != 6 && keyLocation != 8 && keyLocation != 10);
      return xPosition;
   }

   public static int convertXPositionToKeyInt(float xPosition, out bool whiteBall)
   {
      xPosition -= startXPosition;
      int setNumber = (int)System.Math.Floor(xPosition / setDistance);
      float keyOffset = xPosition - (setNumber * setDistance);
      int keyLocation = (int)Mathf.Floor(keyOffset / 0.6f);
      int key = firstKeyData1 + setNumber * numKeysPerSet + keyLocation;
      whiteBall = (keyLocation != 1 && keyLocation != 3 && keyLocation != 6 && keyLocation != 8 && keyLocation != 10);
      return key;
   }

   private static Dictionary<int, float> KeyToXOffsetTable = new Dictionary<int, float>()
   {
      // Set 1
      {0      , 0.0f} ,
      {1      , 0.6f} ,
      {2      , 1.2f} ,
      {3      , 1.8f} ,
      {4      , 2.4f} ,
      {5      , 3.3f} ,
      {6      , 3.9f} ,
      {7      , 4.5f} ,
      {8      , 5.05f} ,
      {9      , 5.65f} ,
      {10     , 6.2f} ,
      {11     , 6.8f} ,
   };

}
